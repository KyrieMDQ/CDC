// DeckManager.cs
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq; // Para FirstOrDefault si lo usas
using DG.Tweening;

public class DeckManager : MonoBehaviour
{
    [Header("Mazo Asignado")]
    public CardCollection mazoAsignado;

    [Header("Prefab de Carta y Referencias Visuales")]
    public GameObject cardPrefab;
    public Transform handContainer;
    public int cartasEnManoInicial = 5;

    [Header("Configuración Visual de la Mano")]
    public float handCardScale = 1.0f; // Si tu hover simple toma la escala en Awake, esta debería ser 1 o la del prefab.
                                       // Si handCardScale es diferente de la escala original del prefab, 
                                       // originalScale en CardHoverEffect.Awake() será incorrecta.
                                       // Por eso añadimos UpdateBaseScale en CardHoverEffect.
    public float handCardSpacing = 180f; // Un poco más de espacio

    [Header("Configuración Visual de la Cripta (Discard Pile)")]
    public Transform discardPileVisualArea;
    public float discardPileCardScale = 0.4f;
    [Range(0f, 45f)] public float discardPileMaxRotation = 10f;

    [Header("Configuración del Jugador")]
    public bool esJugadorHumano = true;
    public int maxCartasEnMano = 7;

    [Header("Referencias Asociadas")]
    public PlayerStats associatedPlayerStats;

    [Header("Estado en Juego")]
    public List<ScriptableCard> mazo = new List<ScriptableCard>();
    public List<ScriptableCard> playerHand = new List<ScriptableCard>();
    public List<GameObject> handCardObjects = new List<GameObject>();
    public List<ScriptableCard> discardPile = new List<ScriptableCard>();
    private GameObject lastDiscardedCardVisual = null;

    [Header("Debug")]
    public bool DEBUG_ShowOpponentHandCosts = false;

    void Awake()
    {
        if (cardPrefab == null) Debug.LogError($"DeckManager ({gameObject.name}): Card Prefab no asignado!");
        if (handContainer == null) Debug.LogError($"DeckManager ({gameObject.name}): Hand Container no asignado!");
        if (associatedPlayerStats == null) Debug.LogError($"DeckManager ({gameObject.name}): Associated Player Stats no asignado!");
        CrearMazos();
    }

    public void CrearMazos()
    {
        mazo.Clear(); playerHand.Clear(); discardPile.Clear();
        foreach (GameObject go in handCardObjects) { if (go != null) Destroy(go); }
        handCardObjects.Clear();
        if (lastDiscardedCardVisual != null) { Destroy(lastDiscardedCardVisual); lastDiscardedCardVisual = null; }

        if (mazoAsignado != null && mazoAsignado.Cartas != null && mazoAsignado.Cartas.Count > 0) {
            mazo.AddRange(mazoAsignado.Cartas);
        } else { Debug.LogError($"DeckManager ({gameObject.name}): No hay mazoAsignado o está vacío."); }
        
        // Llama a UIManager si existe y tiene el método. Comenta si da error.
        // if (UIManager.Instance != null && UIManager.Instance.GetType().GetMethod("UpdateDiscardCountUI") != null) 
        //      UIManager.Instance.UpdateDiscardCountUI(this, 0);
    }

    public void BarajarMazo()
    {
        for (int i = 0; i < mazo.Count; i++) {
            ScriptableCard t = mazo[i]; int r = UnityEngine.Random.Range(i, mazo.Count); mazo[i] = mazo[r]; mazo[r] = t;
        }
    }

    public IEnumerator SetupPlayerDeckAndDealInitialHand()
    {
        if (mazo == null) { Debug.LogError($"[DeckManager] Mazo es null en Setup para {name}"); yield break; }
        if (mazo.Count == 0 && mazoAsignado != null && mazoAsignado.Cartas.Count > 0) CrearMazos();
        if (mazo.Count == 0) { Debug.LogWarning($"[DeckManager] Mazo vacío en Setup para {name}"); yield break; }
        BarajarMazo();
        yield return StartCoroutine(DealInitialHand(cartasEnManoInicial));
    }

    public IEnumerator DealInitialHand(int cantidad)
    {
        for (int i = 0; i < cantidad; i++) {
            if (playerHand.Count >= maxCartasEnMano && esJugadorHumano) break;
            if (mazo.Count == 0) break;
            yield return StartCoroutine(RobarCartaVisual());
        }
    }

    public void ArrangeHandVisuals(bool animate)
    {
        handCardObjects.RemoveAll(item => item == null);
        int cardCount = handCardObjects.Count;
        if (cardCount == 0 && handContainer != null && handContainer.childCount > 0) {
            handCardObjects.Clear();
            foreach (Transform child in handContainer) if (child.gameObject.GetComponent<Card>() != null) handCardObjects.Add(child.gameObject);
            cardCount = handCardObjects.Count;
        }
        if (cardCount == 0) return;

        float spacing = handCardSpacing;
        float totalWidth = (cardCount > 1) ? (cardCount - 1) * spacing : 0;
        Vector3 targetLocalScaleForHand = Vector3.one * handCardScale;

        for (int i = 0; i < cardCount; i++) {
            GameObject cGO = handCardObjects[i];
            if (cGO == null) continue;
            if (cGO.transform.parent != handContainer) cGO.transform.SetParent(handContainer, false);

            Vector3 tPos = new Vector3(-totalWidth / 2f + i * spacing, 0, -i * 0.02f);
            RectTransform rt = cGO.GetComponent<RectTransform>();
            CardHoverEffect hoverEffect = cGO.GetComponent<CardHoverEffect>(); // TU HOVER SIMPLE

            if (rt != null) {
                rt.DOKill();
                
                Action onCompleteOrDirectAction = () => {
                    if (hoverEffect != null) {
                        if (esJugadorHumano) {
                            hoverEffect.enabled = true;
                            hoverEffect.UpdateBaseScale(targetLocalScaleForHand); // ACTUALIZA LA ESCALA BASE DEL HOVER
                        } else {
                            hoverEffect.enabled = false;
                        }
                    }
                };

                if (animate && Application.isPlaying) {
                    Sequence s = DOTween.Sequence();
                    s.Append(rt.DOLocalMove(tPos, 0.3f).SetEase(Ease.OutQuad));
                    s.Join(rt.DOScale(targetLocalScaleForHand, 0.3f).SetEase(Ease.OutQuad));
                    s.Join(rt.DOLocalRotateQuaternion(Quaternion.identity, 0.3f).SetEase(Ease.OutQuad));
                    s.OnComplete(() => onCompleteOrDirectAction.Invoke());
                } else {
                    rt.localPosition = tPos; rt.localScale = targetLocalScaleForHand; rt.localRotation = Quaternion.identity;
                    onCompleteOrDirectAction.Invoke();
                }
            }
        }
    }

    public IEnumerator RobarCartaVisual()
    {
        if (playerHand.Count >= maxCartasEnMano && esJugadorHumano) { yield break; }
        if (mazo.Count == 0) { GameManager.Instance?.EndGameByDeckOut(associatedPlayerStats); yield break; }
        ScriptableCard cartaRobada = mazo[0]; mazo.RemoveAt(0); playerHand.Add(cartaRobada);
        if (cardPrefab == null || handContainer == null) { yield break; }
        GameObject nuevaCartaGO = Instantiate(cardPrefab, handContainer);
        nuevaCartaGO.name = cartaRobada.NombreCarta + (esJugadorHumano ? " (Mano Jugador)" : " (Mano IA)");
        Card cardComponent = nuevaCartaGO.GetComponent<Card>();
        if (cardComponent != null) {
            cardComponent.SetCardData(cartaRobada);
            cardComponent.ShowAsCardBack(!esJugadorHumano);
            cardComponent.SetInteractable(esJugadorHumano);
        }
        handCardObjects.Add(nuevaCartaGO);
        ArrangeHandVisuals(true);
        ActualizarCostosVisualesEnMano(); // Este método debe existir
        // if (UIManager.Instance != null) UIManager.Instance.UpdateDeckCountUI(this, mazo.Count);
        yield return null;
    }

    public void AddToDiscard(ScriptableCard cardData) {
        if (cardData == null) return;
        discardPile.Add(cardData);
        UpdateDiscardVisual(cardData);
        // if (UIManager.Instance != null) UIManager.Instance.UpdateDiscardCountUI(this, discardPile.Count);
    }

    private void UpdateDiscardVisual(ScriptableCard lastCard) {
        if (discardPileVisualArea == null || cardPrefab == null || lastCard == null) return;
        if (lastDiscardedCardVisual != null) Destroy(lastDiscardedCardVisual);
        lastDiscardedCardVisual = Instantiate(cardPrefab, discardPileVisualArea);
        lastDiscardedCardVisual.name = lastCard.NombreCarta + " (Cripta)";
        Card cardComp = lastDiscardedCardVisual.GetComponent<Card>();
        if (cardComp != null) {
            cardComp.SetCardData(lastCard); cardComp.ShowAsCardBack(false); cardComp.isInteractable = false;
            cardComp.SetDiscardedLook(true); 
            CardHoverEffect hov = lastDiscardedCardVisual.GetComponent<CardHoverEffect>(); if (hov != null) hov.enabled = false;
            BoardCardController bcc = lastDiscardedCardVisual.GetComponent<BoardCardController>(); if (bcc != null) Destroy(bcc);
            CardDragHandler cdh = lastDiscardedCardVisual.GetComponent<CardDragHandler>(); if (cdh != null) Destroy(cdh);
        }
        lastDiscardedCardVisual.transform.localScale = Vector3.one * discardPileCardScale;
        lastDiscardedCardVisual.transform.localRotation = Quaternion.Euler(0,0,UnityEngine.Random.Range(-discardPileMaxRotation, discardPileMaxRotation));
        lastDiscardedCardVisual.transform.localPosition = Vector3.zero;
    }

    public int GetDiscardPileCount() { return discardPile.Count; }

    public ScriptableCard EncontrarCartaJugable(int poderActualIgnorado, BoardPlayZone zonaTableroIA) {
        if (associatedPlayerStats == null) return null;
        List<ScriptableCard> manoJugable = new List<ScriptableCard>();
        foreach(var carta in playerHand) {
            if (carta == null) continue;
            if (associatedPlayerStats.PuedePagar(carta)) { // Asume que PlayerStats.PuedePagar existe
                 string tipoNorm = carta.TipoCarta?.Trim().ToLowerInvariant() ?? "";
                 if (tipoNorm == "político" || tipoNorm == "politico" || tipoNorm == "personaje") { 
                    if (zonaTableroIA != null && zonaTableroIA.CartasEnZona().Count < zonaTableroIA.maxSlots) {
                        manoJugable.Add(carta);
                    }
                 } else { manoJugable.Add(carta); }
            }
        }
        if (manoJugable.Count == 0) return null;
        // Lógica IA simple: Jugar la más cara
        manoJugable.Sort((a, b) => b.CostoPoderPolitico.CompareTo(a.CostoPoderPolitico)); // Asume CostoPoderPolitico en ScriptableCard
        return manoJugable.Count > 0 ? manoJugable[0] : null;
    }
    
    public GameObject GetHandCardObject(ScriptableCard cardData) {
        if (cardData == null) return null;
        foreach (GameObject cardGO in handCardObjects) {
            if (cardGO != null) { Card c = cardGO.GetComponent<Card>(); if (c != null && c.CardData == cardData) return cardGO; }
        }
        return null;
    }
    
    public void ActualizarCostosVisualesEnMano() {
        if (associatedPlayerStats == null) return;
        if (!esJugadorHumano && (GameManager.Instance == null || !GameManager.Instance.DEBUG_ShowOpponentHandCosts)) return; 
        foreach(GameObject cardGO in handCardObjects) {
            if (cardGO == null) continue;
            Card cardC = cardGO.GetComponent<Card>(); CardUI cardUI = cardGO.GetComponent<CardUI>();
            if (cardC != null && cardC.CardData != null && cardUI != null) {
                cardUI.ActualizarCostoVisual(associatedPlayerStats.GetCostoRealCarta(cardC.CardData)); // Asume GetCostoRealCarta en PlayerStats
            }
        }
    }

    public void ReturnCardToHand(GameObject cardObject) {
        if (cardObject == null || handContainer == null) return;
        Card cardComp = cardObject.GetComponent<Card>();
        if (cardComp != null && cardComp.CardData != null) {
            if (!playerHand.Contains(cardComp.CardData)) playerHand.Add(cardComp.CardData);
        }
        if (!handCardObjects.Contains(cardObject)) handCardObjects.Add(cardObject);
        // CardDragHandler se encarga de la posición/escala visual antes de llamar aquí
        ArrangeHandVisuals(true); 
        ActualizarCostosVisualesEnMano(); 
    }

    public bool TryPlayCardFromHandToBoard(GameObject cardHandGO, BoardPlayZone targetZone, PlayerStats ownerStats) {
        if (cardHandGO == null || targetZone == null || ownerStats == null) { Debug.LogError("[DM] Parámetros inválidos TryPlayCardFromHandToBoard"); return false; }
        Card cardComp = cardHandGO.GetComponent<Card>();
        if (cardComp == null || cardComp.CardData == null) { Debug.LogError("[DM] Carta sin Card o CardData"); return false; }
        ScriptableCard cardDataToPlay = cardComp.CardData;
        if (!ownerStats.PuedePagar(cardDataToPlay)) { UIManager.Instance?.ShowStatusMessage($"Sin Poder!"); return false; }
        ownerStats.Pagar(cardDataToPlay); 
        RemoveCardFromHand(cardDataToPlay); 
        if (this.cardPrefab != null) { 
            GameObject newBoardInstance = Instantiate(this.cardPrefab);
            targetZone.AddInstantiatedCardToBoardAndAnimate(newBoardInstance, cardDataToPlay);
            return true; 
        } else { Debug.LogError($"[DM] CardPrefab es null."); return false; }
    }

    public void RemoveCardFromHand(ScriptableCard carta) {
        if (carta == null) return;
        bool removedLogic = playerHand.Remove(carta); 
        if (removedLogic) {
            GameObject goToRemove = null;
            for (int i = handCardObjects.Count - 1; i >= 0; i--) {
                GameObject cGO = handCardObjects[i];
                if (cGO != null) { Card c = cGO.GetComponent<Card>();
                    if (c != null && c.CardData == carta) { goToRemove = cGO; handCardObjects.RemoveAt(i); break; }
                } else { handCardObjects.RemoveAt(i); }
            }
            if (goToRemove != null) Destroy(goToRemove); 
            ArrangeHandVisuals(true); 
            ActualizarCostosVisualesEnMano();
        }
    }
}