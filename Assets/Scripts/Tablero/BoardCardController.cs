using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class BoardCardController : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    private CardAnimator cardAnimator; 
    public ScriptableCard cardData;

    [Header("Game Stats")]
    public int influencia;
    public int resistencia;
    public bool yaAtaco;
    public bool enCooldown;

    [Header("UI Elements")]
    public TextMeshProUGUI influenciaText;
    public TextMeshProUGUI resistenciaText;

    [Header("Visual Feedback")]
    public GameObject attackerHighlightObject;
    public GameObject attackIndicator;

    [Header("Block Settings")]
    [SerializeField] private float bloqueoFlashSpeed = 1f;
    private Coroutine bloqueoRoutine;
    private bool _canAttack = true;

    public BoardPlayZone parentBoardZone { get; private set; }
    public bool isDying { get; private set; } = false;
    private CanvasGroup _canvasGroup;
    private CardZoomManager zoomManager;

    void Awake()
    {
        cardAnimator = GetComponent<CardAnimator>();
        _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null) _canvasGroup = gameObject.AddComponent<CanvasGroup>();
        zoomManager = FindFirstObjectByType<CardZoomManager>();
    }

    public void InitializeOnBoard(ScriptableCard newCardData, BoardPlayZone assignedZone)
    {
        this.cardData = newCardData;
        this.parentBoardZone = assignedZone;
        _canvasGroup.alpha = 1f;
        isDying = false;
        _canAttack = true;

        if (this.cardData != null) {
            this.name = this.cardData.NombreCarta + " (En Juego)";
            influencia = this.cardData.Influencia;
            resistencia = this.cardData.Resistencia;
            enCooldown = !this.cardData.AccionInmediata;
            yaAtaco = false;
            ActualizarStatsVisuales();
            
            Card cardComponentVisual = GetComponent<Card>();
            if (cardComponentVisual != null) {
                cardComponentVisual.SetCardData(this.cardData);
                cardComponentVisual.ShowAsCardBack(false);
                cardComponentVisual.isInteractable = true; 
            }
            
            CardHoverEffect handHover = GetComponent<CardHoverEffect>();
            if(handHover != null) {
                handHover.SetHoverActive(false);
            }

            if (attackerHighlightObject == null) {
                Transform foundHighlight = transform.Find("AttackerHighlightBorder");
                if (foundHighlight != null) attackerHighlightObject = foundHighlight.gameObject;
            }
            if (attackerHighlightObject != null) attackerHighlightObject.SetActive(false);
            
            BoxCollider2D boxCol = GetComponent<BoxCollider2D>();
            if (boxCol == null && GetComponent<Collider>() == null) {
                 boxCol = gameObject.AddComponent<BoxCollider2D>();
                 boxCol.isTrigger = true;
            } else if (boxCol != null) { boxCol.enabled = true; }
            if (boxCol != null) {
                 RectTransform rt = GetComponent<RectTransform>();
                 if (rt != null) boxCol.size = rt.sizeDelta;
            }
        } else { Debug.LogError($"InitializeOnBoard ({this.name}): newCardData es null."); }
    }

    // Métodos para resolver los errores
    public void ResetTurnStatus()
    {
        if (!isDying)
        {
            enCooldown = false;
            yaAtaco = false;
            ToggleAttackerHighlight(false);
            UpdateAttackVisual();
        }
    }

    public void AtacarJugador(PlayerStats jugadorOponente)
    {
        if (isDying || !PuedeAtacar() || cardData == null || jugadorOponente == null || !gameObject.activeInHierarchy) return;
        
        this.yaAtaco = true;
        ToggleAttackerHighlight(false);
        UpdateAttackVisual();

        if (cardAnimator != null)
        {
            cardAnimator.PlayAttackPlayerAnimation(jugadorOponente, transform.localPosition, transform.localScale, () =>
            {
                if (jugadorOponente != null) jugadorOponente.RecibirDaño(this.influencia);
            });
        }
        else
        {
            jugadorOponente.RecibirDaño(this.influencia);
        }
    }

    public void SetFinalDeployedScale(Vector3 finalScale)
    {
        transform.localScale = finalScale;
        if (cardAnimator != null) cardAnimator.SetBoardRestingScale(finalScale);
    }

    public void SetCanAttack(bool canAttack)
    {
        _canAttack = canAttack;
        
        if (bloqueoRoutine != null) 
        {
            StopCoroutine(bloqueoRoutine);
            if (attackerHighlightObject != null) 
                attackerHighlightObject.SetActive(false);
        }
        
        if (!canAttack && attackerHighlightObject != null)
        {
            bloqueoRoutine = StartCoroutine(ParpadearBordeBloqueo());
        }
        
        UpdateAttackVisual();
    }

    // Resto de los métodos existentes...
    private IEnumerator ParpadearBordeBloqueo()
    {
        while (true) 
        {
            if (attackerHighlightObject != null)
                attackerHighlightObject.SetActive(!attackerHighlightObject.activeSelf);
            yield return new WaitForSeconds(bloqueoFlashSpeed);
        }
    }

    public bool PuedeAtacar()
    {
        return _canAttack && !enCooldown && !yaAtaco && !isDying && influencia > 0;
    }

    private void UpdateAttackVisual()
    {
        if (attackIndicator != null)
        {
            attackIndicator.SetActive(PuedeAtacar());
        }
    }

    public void ToggleAttackerHighlight(bool isOn)
    {
        if (isDying && isOn) { 
            if (attackerHighlightObject != null) attackerHighlightObject.SetActive(false); 
            return; 
        }
        if (attackerHighlightObject != null) attackerHighlightObject.SetActive(isOn);
    }

    public void ActualizarStatsVisuales()
    {
        if (influenciaText != null) influenciaText.text = influencia.ToString();
        if (resistenciaText != null) {
            resistenciaText.text = resistencia.ToString();
            if (cardData != null) {
                if (resistencia < cardData.Resistencia && resistencia > 0) resistenciaText.color = Color.red;
                else if (resistencia > cardData.Resistencia) resistenciaText.color = Color.green;
                else if (resistencia <= 0) resistenciaText.color = Color.grey;
                else resistenciaText.color = Color.white;
            }
        }
    }

    public void Atacar(BoardCardController objetivo)
    {
        if (isDying || !PuedeAtacar() || objetivo == null || objetivo.isDying || !gameObject.activeInHierarchy) return;
        if (cardData == null) return; 
        
        this.yaAtaco = true; 
        ToggleAttackerHighlight(false);
        UpdateAttackVisual();

        if (cardAnimator != null) {
            cardAnimator.PlayAttackAnimation(objetivo, transform.localPosition, transform.localScale, () => {
                if (objetivo != null && objetivo.gameObject.activeInHierarchy && !objetivo.isDying) {
                    objetivo.RecibirDaño(this.influencia);
                    if (!this.isDying && this.gameObject.activeInHierarchy && !objetivo.isDying && 
                        objetivo.influencia > 0 && objetivo.PuedeAtacar()) {
                        this.RecibirDaño(objetivo.influencia);
                    }
                }
            });
        } else { 
            if (objetivo != null && objetivo.gameObject.activeInHierarchy && !objetivo.isDying) {
                objetivo.RecibirDaño(this.influencia);
                if (!this.isDying && this.gameObject.activeInHierarchy && !objetivo.isDying && 
                    objetivo.influencia > 0 && objetivo.PuedeAtacar()) {
                    this.RecibirDaño(objetivo.influencia);
                }
            }
        }
    }

    public void RecibirDaño(int cantidad)
    {
        if (isDying || cardData == null || cantidad <= 0) return;
        
        resistencia -= cantidad; 
        ActualizarStatsVisuales();
        
        if (cardAnimator != null) 
            cardAnimator.PlayDamageReceivedAnimation(this.resistenciaText);
            
        if (resistencia <= 0) { 
            if (!isDying) StartCoroutine(ManejarMuerte()); 
        }
    }

    private IEnumerator ManejarMuerte()
{
    if(isDying) yield break; 
    
    isDying = true; 
    
    // Detener animaciones antes de destruir
    if (cardAnimator != null)
    {
        cardAnimator.StopAllAnimations();
    }
    
    Collider2D col2D = GetComponent<Collider2D>(); 
    if (col2D != null) col2D.enabled = false;
    Collider col = GetComponent<Collider>(); 
    if (col != null) col.enabled = false;
    
    ToggleAttackerHighlight(false);
    if (attackIndicator != null) attackIndicator.SetActive(false);
    
    if (cardAnimator != null) 
    {
        yield return StartCoroutine(cardAnimator.PlayDeathAnimation());
    }
    else
    {
        yield return new WaitForSeconds(0.5f);
    }
        
    FinalizeDestructionLogic();
}
    
    private void FinalizeDestructionLogic()
    {
        if (cardData != null && GameManager.Instance != null && parentBoardZone != null) {
            DeckManager ownerDeckManager = null;
            if (parentBoardZone == GameManager.Instance.boardPlayZoneJugador1) 
                ownerDeckManager = GameManager.Instance.deckManagerJugador1;
            else if (parentBoardZone == GameManager.Instance.boardPlayZoneJugador2) 
                ownerDeckManager = GameManager.Instance.GetOpponentPlayerDeckManager();
            
            if (ownerDeckManager != null) 
                ownerDeckManager.AddToDiscard(this.cardData);
        }
        
        if (parentBoardZone != null) 
            parentBoardZone.NotifyCardRemovedFromBoard(this.gameObject);
            
        if(gameObject != null) 
            Destroy(gameObject);
    }

    // Implementación de interfaces
    public void OnPointerClick(PointerEventData eventData)
    {
        if (parentBoardZone == null || isDying || (GameManager.Instance != null && GameManager.Instance.IsGameOver)) return;
        
        if (eventData.button == PointerEventData.InputButton.Right) {
            if (zoomManager != null && cardData != null) zoomManager.ShowCard(cardData);
            return;
        }
        
        if (eventData.button == PointerEventData.InputButton.Left) {
            HandleLeftClick();
        }
    }

    private void HandleLeftClick()
    {
        if (GameManager.Instance == null || cardData == null) return;
        
        bool isCurrentPlayerCard = (parentBoardZone == GameManager.Instance.GetCurrentPlayerBoardZone());
        
        if (isCurrentPlayerCard) {
            HandleCurrentPlayerClick();
        } else {
            HandleOpponentCardClick();
        }
    }

    private void HandleCurrentPlayerClick()
    {
        if (GameManager.Instance.selectedCardAttacker == this) {
            GameManager.Instance.ClearAttackerSelection();
        }
        else if (PuedeAtacar()) {
            GameManager.Instance.ClearAttackerSelection(); 
            GameManager.Instance.selectedCardAttacker = this;
            ToggleAttackerHighlight(true);
            UIManager.Instance?.ShowStatusMessage($"'{cardData.NombreCarta}' listo. Selecciona objetivo.");
        } else {
            string razon = isDying ? "(Muriendo)" : 
                         enCooldown ? "(Cooldown)" : 
                         yaAtaco ? "(Ya Atacó)" : 
                         (influencia <= 0 ? "(Sin Influencia)" : "(No puede)");
            UIManager.Instance?.ShowStatusMessage($"'{cardData.NombreCarta}' no puede atacar {razon}");
            GameManager.Instance.ClearAttackerSelection();
        }
    }

    private void HandleOpponentCardClick()
    {
        if (GameManager.Instance.selectedCardAttacker != null) {
            BoardCardController atacante = GameManager.Instance.selectedCardAttacker;
            if (parentBoardZone == GameManager.Instance.GetOpponentPlayerBoardZone()) {
                bool hayFueros = parentBoardZone.HayCartasConFuerosActivas();
                if (hayFueros && (this.cardData == null || !this.cardData.TieneFueros)) {
                    UIManager.Instance?.ShowStatusMessage("¡Debes atacar a un Político con Fueros!"); 
                    return;
                }
                
                string atipo = atacante.cardData?.TipoCarta?.Trim().ToLowerInvariant()??"";
                string otipo = this.cardData?.TipoCarta?.Trim().ToLowerInvariant()??"";
                if ((atipo=="político"||atipo=="politico") && otipo=="apoyo") {
                    UIManager.Instance?.ShowStatusMessage("Políticos no atacan Apoyos."); 
                    return;
                }
                
                atacante.Atacar(this); 
                GameManager.Instance.ClearAttackerSelection();
            }
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (parentBoardZone == null || isDying || cardAnimator == null || 
            (GameManager.Instance != null && GameManager.Instance.IsGameOver)) return;
        if (eventData.pointerDrag != null && eventData.pointerDrag != this.gameObject) return; 
        
        cardAnimator.AnimateBoardCardHoverPulse(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (parentBoardZone == null || cardAnimator == null || isDying) return; 
        
        cardAnimator.AnimateBoardCardHoverPulse(false);
    }
}