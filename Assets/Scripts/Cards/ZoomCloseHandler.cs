using UnityEngine;
using UnityEngine.EventSystems;

public class ZoomCloseHandler : MonoBehaviour, IPointerClickHandler
{
    private CardZoomManager zoomManager;

    private void Awake()
    {
        zoomManager = FindFirstObjectByType<CardZoomManager>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // Si el clic es en el fondo, cerramos
        if (eventData.pointerCurrentRaycast.gameObject == gameObject)
        {
            zoomManager.HideCard();
        }
    }
}