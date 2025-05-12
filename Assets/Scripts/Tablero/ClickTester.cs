using UnityEngine;
using UnityEngine.UI; // Necesario para Button

public class ClickTester : MonoBehaviour
{
    void Start()
    {
        Button btn = GetComponent<Button>();
        if (btn != null)
        {
            // Añade una acción que se ejecutará cuando se haga clic
            btn.onClick.AddListener(TaskOnClick);
            Debug.Log($"Listener añadido al botón '{gameObject.name}'");
        } else {
            Debug.LogError($"ClickTester no encontró componente Button en {gameObject.name}");
        }
    }

    void TaskOnClick()
    {
        // Mensaje que SÍ O SÍ debe aparecer si el clic llega al botón
        Debug.LogError($"!!!!!!!!!!!!!! CLIC DETECTADO EN BOTÓN DE PRUEBA '{gameObject.name}' !!!!!!!!!!!!!!");
    }

     // Opcional: Añadir esto para ver si el EventSystem lo detecta al pasar por encima
     public void OnPointerEnter() // No necesita interfaz si el Button ya la maneja
     {
         Debug.Log($"<<<< POINTER ENTER Botón '{gameObject.name}' >>>>");
     }
}