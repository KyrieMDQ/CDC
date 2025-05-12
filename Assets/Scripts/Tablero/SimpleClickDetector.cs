using UnityEngine;
using UnityEngine.EventSystems;

public class SimpleClickDetector : MonoBehaviour, IPointerClickHandler
{
    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.LogError($"!!!!!!!!!!!!!! CLIC DETECTADO EN {gameObject.name} !!!!!!!!!!!!!!");
    }
     public void OnPointerEnter(PointerEventData eventData)
    {
         Debug.LogWarning($"<<<< POINTER ENTER SIMPLE -> {gameObject.name} >>>>");
    }
     public void OnPointerExit(PointerEventData eventData)
    {
        Debug.LogWarning($"<<<< POINTER EXIT SIMPLE -> {gameObject.name} >>>>");
    }
}