// CardDragHandler.cs
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class CardDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private Transform originalParent;
    private CanvasGroup canvasGroup;
    private Vector3 originalLocalPosition; 
    private Vector3 originalScale;
    private int originalSiblingIndex;

    public bool dropSuccessful = false; 

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();

        originalScale = transform.localScale;
        originalParent = transform.parent; 
        originalLocalPosition = transform.localPosition; 
        originalSiblingIndex = transform.GetSiblingIndex();
        dropSuccessful = false; 

        transform.SetParent(GetComponentInParent<Canvas>().transform, true); 
        transform.SetAsLastSibling(); 
        transform.localScale = originalScale * 1.1f; 
        
        canvasGroup.blocksRaycasts = false; 
        canvasGroup.alpha = 0.8f; 
    }

    public void OnDrag(PointerEventData eventData)
    {
        transform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!dropSuccessful)
        {
            ReturnToHandVisuals(); 
            
            if (originalParent != null) {
                DeckManager deckManager = originalParent.GetComponentInParent<DeckManager>();
                if (deckManager != null) {
                    deckManager.ReturnCardToHand(gameObject); 
                } else {
                     Debug.LogError($"[CardDragHandler] No se encontró DeckManager en {originalParent.name} para {gameObject.name}.");
                }
            } else {
                 Debug.LogError($"[CardDragHandler] originalParent es NULL para {gameObject.name} en OnEndDrag. Carta flotante no pudo ser devuelta lógicamente.");
                 Destroy(gameObject); // Destruir si no puede volver a ningún lado
            }
        }
        else 
        {
            // Si el drop fue exitoso, la carta de mano (este GO) se destruye por DeckManager.RemoveCardFromHand
            // o por la lógica de acción/evento. No necesitamos hacer nada aquí, y de hecho, este GO podría ya no existir.
        }

        // Siempre restaurar esto si el objeto aún existe (puede que no si dropSuccessful fue true)
        if (canvasGroup != null && gameObject != null && gameObject.activeInHierarchy) { // Check gameObject active
             canvasGroup.blocksRaycasts = true;
             canvasGroup.alpha = 1f;
        }
    }

    private void ReturnToHandVisuals()
    {
        if (originalParent == null) {
            Debug.LogError($"[CardDragHandler] {gameObject.name}: originalParent es NULL en ReturnToHandVisuals. Destruyendo.");
            if(gameObject != null) Destroy(gameObject); 
            return;
        }

        transform.SetParent(originalParent, false); 
        transform.SetSiblingIndex(originalSiblingIndex);
        transform.localScale = originalScale;
        transform.localPosition = originalLocalPosition; 
    }
}