// CardHoverEffect.cs (TU VERSIÓN SIMPLE Y ORIGINAL)
using UnityEngine;
using UnityEngine.EventSystems;

public class CardHoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private Vector3 originalScale;
    public float scaleMultiplier = 1.15f; // Ajusta este valor en el Inspector si quieres

    // Guardar la escala actual como la original para este script.
    // DeckManager se encargará de llamar a SetActive(true/false) en este componente.
    // Y DeckManager.ArrangeHandVisuals establecerá la escala final de la carta en mano.
    // Es CRUCIAL que originalScale se actualice si la escala base de la mano cambia.
    void Awake()
    {
        // Esto tomará la escala del prefab. Si ArrangeHandVisuals la cambia, necesitamos un update.
        originalScale = transform.localScale; 
    }
public void SetHoverActive(bool isActive)
{
    this.enabled = isActive;
}
public void DisableHandHoverEffect()
{
    this.enabled = false;
}
    // Llamado por DeckManager para establecer la escala base correcta después de organizar la mano
    public void UpdateBaseScale(Vector3 newBaseScale)
    {
        originalScale = newBaseScale;
        // Si no está hovereada, asegurarse que tenga esta escala.
        // Esto es importante si la carta fue reorganizada mientras no estaba hovereada.
        if (transform.localScale != originalScale * scaleMultiplier) 
        {
            transform.localScale = originalScale;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // Solo si el script está habilitado (DeckManager lo habilita para la mano del jugador)
        if (!this.enabled) return;
        
        // Podrías añadir chequeo de _cardLogic.isInteractable aquí si tienes esa referencia
        // Card _cardLogic = GetComponent<Card>();
        // if (_cardLogic != null && !_cardLogic.isInteractable) return;

        // En caso de que UpdateBaseScale no se haya llamado recientemente, o la escala haya cambiado por otra razón:
        // originalScale = transform.localScale / (isCurrentlyHovered ? scaleMultiplier : 1f); // No es ideal
        // Mejor confiar en que UpdateBaseScale se llama cuando es necesario.

        transform.localScale = originalScale * scaleMultiplier;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!this.enabled) return;

        transform.localScale = originalScale;
    }
}