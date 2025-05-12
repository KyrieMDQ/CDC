// BoardPlayZone.cs
using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using DG.Tweening; // Solo si usas DOTween aquí para animaciones de la zona

public class BoardPlayZone : MonoBehaviour, IPointerClickHandler, IDropHandler
{
    [Header("Configuración Carta Jugada")]
    public Transform playedCardContainer; 
    [Range(0.1f, 2f)] public float scaleOnBoard = 0.4f; 
    public float rotationOnBoard = 0f;
    public float cardEntryAnimationDuration = 0.5f; 
    public float cardReorganizeAnimationDuration = 0.3f;

    [Header("Configuración de Slots en Tablero")]
    public int maxSlots = 8; 
    [Tooltip("Espacio entre centros de cartas en el tablero.")]
    public float cardSpacingOnBoard = 160f; 
    [Tooltip("Offset vertical para las cartas en el tablero.")]
    public float verticalOffsetOnBoard = 0f;  

    private List<GameObject> cardsOnBoard = new List<GameObject>();

    private void Awake()
    {
        UnityEngine.UI.Image img = GetComponent<UnityEngine.UI.Image>();
        if (img == null) { img = gameObject.AddComponent<UnityEngine.UI.Image>(); img.color = new Color(1,1,1,0); }
        if (!img.raycastTarget) img.raycastTarget = true;
        if (playedCardContainer == null) playedCardContainer = this.transform;
    }

    public void OnDrop(PointerEventData eventData)
    {
        GameObject droppedCardGO = eventData.pointerDrag;
        if (droppedCardGO == null) return;
        Card cardComponent = droppedCardGO.GetComponent<Card>();
        CardDragHandler dragHandler = droppedCardGO.GetComponent<CardDragHandler>(); 
        if (dragHandler == null) { Debug.LogError("[BPZ.OnDrop] No CardDragHandler: " + droppedCardGO.name); return; }
        if (cardComponent == null || cardComponent.cardData == null) { Debug.LogError("[BPZ.OnDrop] No Card/CardData: " + droppedCardGO.name); dragHandler.dropSuccessful = false; return; }
        dragHandler.dropSuccessful = false; 
        GameManager gm = GameManager.Instance;
        if (gm == null) { dragHandler.dropSuccessful = false; return; } // Asegurar que dragHandler sepa del fallo
        if (gm.GetCurrentPlayerBoardZone() != this) { UIManager.Instance?.ShowStatusMessage("Solo en tu zona!"); dragHandler.dropSuccessful = false; return; }
        ScriptableCard cardData = cardComponent.cardData;
        PlayerStats ownerStats = gm.GetCurrentPlayer();
        DeckManager deckManager = gm.GetCurrentPlayerDeckManager();
        if (ownerStats == null || deckManager == null) { dragHandler.dropSuccessful = false; return; } 
        string tipoCartaNormalizado = cardData.TipoCarta?.Trim().ToLowerInvariant() ?? "desconocido";
        bool playedSuccessfullyLogic = false;
        if (tipoCartaNormalizado == "accion" || tipoCartaNormalizado == "acción" || tipoCartaNormalizado == "evento") {
            if (ownerStats.PuedePagar(cardData)) {
                ownerStats.Pagar(cardData);
                ActionEffectManager.Instance?.ExecuteEffect(cardData, ownerStats, gm.GetOpponentPlayer(), this, gm.GetOpponentPlayerBoardZone());
                deckManager.AddToDiscard(cardData); deckManager.RemoveCardFromHand(cardData); 
                playedSuccessfullyLogic = true;
            } else { UIManager.Instance?.ShowStatusMessage($"Sin Poder para '{cardData.NombreCarta}'!"); }
        } else if (tipoCartaNormalizado == "político" || tipoCartaNormalizado == "politico" || tipoCartaNormalizado == "apoyo" || tipoCartaNormalizado == "personaje") {
            bool requiresLimitedSlot = (tipoCartaNormalizado == "político" || tipoCartaNormalizado == "politico" || tipoCartaNormalizado == "personaje");
            if (requiresLimitedSlot && cardsOnBoard.Count >= maxSlots) { UIManager.Instance?.ShowStatusMessage("No hay más espacio!"); } 
            else { if (deckManager.TryPlayCardFromHandToBoard(droppedCardGO, this, ownerStats)) playedSuccessfullyLogic = true; }
        } else { Debug.LogWarning($"[BPZ.OnDrop] Tipo carta no manejado: '{tipoCartaNormalizado}'"); }
        dragHandler.dropSuccessful = playedSuccessfullyLogic;
    }

    public void AddInstantiatedCardToBoardAndAnimate(GameObject newBoardCardGO, ScriptableCard cardDataForInit)
    {
        if (newBoardCardGO == null || playedCardContainer == null) { if(newBoardCardGO != null) Destroy(newBoardCardGO); return; }
        newBoardCardGO.transform.SetParent(playedCardContainer, false);
        BoardCardController bcc = newBoardCardGO.GetComponent<BoardCardController>();
        CardAnimator animator = newBoardCardGO.GetComponent<CardAnimator>();
        if (bcc == null) { Debug.LogError($"Instanced card {newBoardCardGO.name} missing BCC!"); Destroy(newBoardCardGO); return; }
        
        bcc.InitializeOnBoard(cardDataForInit, this); 

        CardDragHandler dragHandlerOnBoardCard = newBoardCardGO.GetComponent<CardDragHandler>();
        if (dragHandlerOnBoardCard != null) dragHandlerOnBoardCard.enabled = false;
        Card cardHandLogicComponent = newBoardCardGO.GetComponent<Card>();
        if (cardHandLogicComponent != null) cardHandLogicComponent.enabled = false;
        CardHoverEffect handHover = newBoardCardGO.GetComponent<CardHoverEffect>();
        if (handHover != null && handHover.enabled) handHover.DisableHandHoverEffect(); // Usa el método de tu hover

       if (!cardsOnBoard.Contains(newBoardCardGO))
    cardsOnBoard.Add(newBoardCardGO);
else
{
    ReorganizeBoardLayout(); // <-- Esto solo se llama si ya existe. ¡Mal!
    return;
}

        int cardIndex = cardsOnBoard.Count - 1;
        Vector3 finalPos = CalculateBoardSlotPosition(cardIndex, cardsOnBoard.Count);
        Vector3 finalScale = Vector3.one * scaleOnBoard; 
        Quaternion finalRot = Quaternion.Euler(0f, 0f, rotationOnBoard);

        if (animator != null && Application.isPlaying) {
            animator.AnimateEntryToBoard(finalPos, finalScale, finalRot, cardEntryAnimationDuration, () => {
                if (bcc != null) bcc.SetFinalDeployedScale(finalScale); 
                ReorganizeBoardLayout(); 
            });

            if (!cardsOnBoard.Contains(newBoardCardGO))
    cardsOnBoard.Add(newBoardCardGO);

ReorganizeBoardLayout(); // <-- ¡Siempre lo llamás, incluso si era nueva!

        } else {
            RectTransform rect = newBoardCardGO.GetComponent<RectTransform>();
            if (rect != null) { rect.localPosition = finalPos; rect.localScale = finalScale; rect.localRotation = finalRot; }
            if (bcc != null) bcc.SetFinalDeployedScale(finalScale);
            ReorganizeBoardLayout();
        }
    }
    
    public void OnPointerClick(PointerEventData eventData) {
        if (eventData.button != PointerEventData.InputButton.Left) return;
        if (GameManager.Instance == null || GameManager.Instance.IsGameOver) return;
        if (GameManager.Instance.selectedCardObject == null) {
            if (GameManager.Instance.selectedCardAttacker != null && this == GameManager.Instance.GetOpponentPlayerBoardZone()) {
                PlayerStats oponente = GameManager.Instance.GetOpponentPlayer();
                if (oponente != null) {
                    if (this.HayCartasConFuerosActivas()) { UIManager.Instance?.ShowStatusMessage("¡Ataca a Fueros primero!"); return; }
                    GameManager.Instance.selectedCardAttacker.AtacarJugador(oponente);
                    GameManager.Instance.ClearAttackerSelection();
                }
            } return; 
        }
        if (this != GameManager.Instance.GetCurrentPlayerBoardZone()) {
            UIManager.Instance?.ShowStatusMessage("Solo en tu zona.");
            Card prevSel = GameManager.Instance.selectedCardObject.GetComponent<Card>();
            if(prevSel != null) prevSel.ToggleHighlight(false); // Asume que Card.cs tiene ToggleHighlight
            GameManager.Instance.selectedCardObject = null; return;
        }
        GameObject cardObjInHand = GameManager.Instance.selectedCardObject;
        Card cardInHand = cardObjInHand.GetComponent<Card>();
        if (cardInHand == null || cardInHand.cardData == null) {
            if(cardInHand != null) cardInHand.ToggleHighlight(false); // Asume que Card.cs tiene ToggleHighlight
            GameManager.Instance.selectedCardObject = null; return;
        }
        ScriptableCard scToPlay = cardInHand.cardData;
        PlayerStats pStats = GameManager.Instance.GetCurrentPlayer();
        DeckManager dManager = GameManager.Instance.GetCurrentPlayerDeckManager();
        if (pStats == null || dManager == null) {
             if(cardInHand != null) cardInHand.ToggleHighlight(false); // Asume que Card.cs tiene ToggleHighlight
            GameManager.Instance.selectedCardObject = null; return;
        }
        string tipo = scToPlay.TipoCarta?.Trim().ToLowerInvariant() ?? "desconocido";
        bool played = false;
        if (tipo == "accion" || tipo == "acción" || tipo == "evento") {
            if (!pStats.PuedePagar(scToPlay)) { UIManager.Instance?.ShowStatusMessage("Sin Poder!"); return; }
            pStats.Pagar(scToPlay);
            ActionEffectManager.Instance?.ExecuteEffect(scToPlay, pStats, GameManager.Instance.GetOpponentPlayer(), this, GameManager.Instance.GetOpponentPlayerBoardZone());
            dManager.AddToDiscard(scToPlay); dManager.RemoveCardFromHand(scToPlay); played = true;
        } else if (tipo == "político" || tipo == "politico" || tipo == "apoyo" || tipo == "personaje") {
            if (!pStats.PuedePagar(scToPlay)) { UIManager.Instance?.ShowStatusMessage("Sin Poder!"); return; }
            bool reqSlot = (tipo == "político" || tipo == "politico" || tipo == "personaje");
            if (reqSlot && cardsOnBoard.Count >= maxSlots) { UIManager.Instance?.ShowStatusMessage("Sin espacio!"); return; }
            pStats.Pagar(scToPlay); dManager.RemoveCardFromHand(scToPlay); 
            if (dManager.cardPrefab != null) { 
                GameObject newGO = Instantiate(dManager.cardPrefab);
                AddInstantiatedCardToBoardAndAnimate(newGO, scToPlay); // Llama a tu método modificado
                played = true; 
            } 
            else { Debug.LogError("DeckManager sin cardPrefab."); }
        } else { UIManager.Instance?.ShowStatusMessage($"Tipo '{scToPlay.TipoCarta}' no manejado."); }
        if (played) GameManager.Instance.selectedCardObject = null; // El ToggleHighlight(false) lo debe manejar la lógica de selección en Card.cs
    }

    private Vector3 CalculateBoardSlotPosition(int cardIndex, int totalCards) {
        if (totalCards <= 0) return new Vector3(0, verticalOffsetOnBoard, 0);
        float spacing = Mathf.Max(10f, cardSpacingOnBoard);
        float totalWidth = (totalCards > 1) ? (totalCards - 1) * spacing : 0;
        float startX = -totalWidth / 2f;
        float cardX = startX + cardIndex * spacing;
        float cardY = verticalOffsetOnBoard;
        float cardZ = -cardIndex * 0.01f;
        return new Vector3(cardX, cardY, cardZ);
    }

    public void ReorganizeBoardLayout() {
        cardsOnBoard.RemoveAll(item => item == null);
        int cardCount = cardsOnBoard.Count;
        Vector3 targetScale = Vector3.one * scaleOnBoard;
        Quaternion targetRotation = Quaternion.Euler(0f, 0f, rotationOnBoard);
        for (int i = 0; i < cardCount; i++) {
            GameObject cartaGO = cardsOnBoard[i];
            if (cartaGO == null) continue;
            Vector3 targetLocalPosition = CalculateBoardSlotPosition(i, cardCount);
            CardAnimator animator = cartaGO.GetComponent<CardAnimator>();
            if (animator != null && Application.isPlaying) {
                animator.AnimateToSlot(targetLocalPosition, targetScale, targetRotation, cardReorganizeAnimationDuration);
            } else {
                RectTransform rt = cartaGO.GetComponent<RectTransform>();
                if (rt != null) { rt.localPosition = targetLocalPosition; rt.localScale = targetScale; rt.localRotation = targetRotation; }
            }
        }
    }
    public void NotifyCardRemovedFromBoard(GameObject cardObject) { if (cardsOnBoard.Remove(cardObject)) ReorganizeBoardLayout(); }
    public List<GameObject> CartasEnZona() { cardsOnBoard.RemoveAll(item => item == null); return new List<GameObject>(cardsOnBoard); }
    public bool HayCartasConFuerosActivas() { 
        foreach (GameObject cGO in CartasEnZona()) { 
            if (cGO != null) { BoardCardController b = cGO.GetComponent<BoardCardController>(); 
                if (b != null && b.cardData != null && b.cardData.TieneFueros && b.resistencia > 0 && !b.isDying) return true; } } return false; }
}