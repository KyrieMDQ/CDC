using UnityEngine;
using UnityEngine.UI;
// Asegúrate de tener TextMeshPro si CardUI o ScriptableCard lo usan implícitamente.
// using TMPro;

public class CardZoomManager : MonoBehaviour
{
    [Header("Referencias")]
    public GameObject zoomPanel;       // El panel que contiene todo el zoom
    public Transform zoomContainer;    // El objeto padre donde se instancia la carta zoomeada
    public GameObject cardPrefab;      // El prefab de la carta que se muestra en grande

    [Header("UI")]
    [SerializeField] private Slider zoomSlider; // Referencia al Slider que arrastraremos

    private GameObject zoomedCard;     // La instancia de la carta que está actualmente en zoom
    private const float DEFAULT_ZOOM = 1.1f; // Valor por defecto si no hay slider

    private void Awake()
    {
        Debug.Log("CardZoomManager: Awake iniciado.");
        // Es crucial que zoomPanel esté asignado en el Inspector
        if (zoomPanel != null)
        {
            zoomPanel.SetActive(false); // Ocultar el panel al inicio
            Debug.Log("CardZoomManager: zoomPanel ocultado en Awake.");
        }
        else
        {
            Debug.LogError("¡ERROR CRÍTICO! La referencia 'zoomPanel' no está asignada en el Inspector de CardZoomManager.");
        }
        ConfigureSlider();
    }

    private void ConfigureSlider()
    {
        // Es crucial que zoomSlider esté asignado en el Inspector si quieres usarlo
        if (zoomSlider == null)
        {
            // Esto NO es un error si no quieres usar slider, pero sí si esperas que funcione.
            Debug.LogWarning("Advertencia: La referencia 'zoomSlider' no está asignada en el Inspector de CardZoomManager. El zoom por slider no funcionará.");
            return; // Salir de la configuración del slider si no está asignado
        }

        Debug.Log("CardZoomManager: Configurando Slider...");
        // Configurar valores del Slider
        zoomSlider.minValue = 0.5f;
        zoomSlider.maxValue = 2f;
        zoomSlider.value = DEFAULT_ZOOM;

        // Añadir listener - importante para que reaccione a cambios
        // Primero removemos por si acaso se añadió antes (ej. al reactivar el objeto)
        zoomSlider.onValueChanged.RemoveAllListeners();
        zoomSlider.onValueChanged.AddListener(OnZoomValueChanged);
        Debug.Log("Slider configurado exitosamente y listener añadido.");
    }

    // Esta función se llama CADA VEZ que el valor del Slider cambia
    private void OnZoomValueChanged(float value)
    {
        // Este log es útil para ver si el slider está respondiendo
        Debug.Log($"OnZoomValueChanged llamado con valor: {value}");
        if (zoomedCard != null)
        {
            // Cambia la escala de la carta instanciada
            zoomedCard.transform.localScale = Vector3.one * value;
            // Quita este log si genera demasiados mensajes una vez que funcione
            // Debug.Log($"Zoom actualizado a: {value}");
        }
         else
        {
             Debug.LogWarning("OnZoomValueChanged: Se intentó cambiar escala pero 'zoomedCard' es null.");
        }
    }

    // --- Función Principal para Mostrar la Carta en Zoom ---
    public void ShowCard(ScriptableCard card)
    {
        // ***** ESTE ES EL DEBUG.LOG QUE PEDISTE, PUESTO AL INICIO DE LA FUNCIÓN *****
        // Se ejecutará si ALGO llama a esta función ShowCard.
        // Usamos 'card' (el parámetro que recibe la función), no 'laCarta'.
        Debug.Log("-----> ShowCard EJECUTADO. Intentando mostrar carta: " + (card != null ? card.NombreCarta : "¡ERROR: CARTA ES NULL!"));

        // --- Verificaciones Críticas ---
        if (card == null)
        {
            Debug.LogError("ShowCard fue llamado con 'card' siendo NULL. No se puede continuar.");
            return; // Salir inmediatamente si no hay datos de carta
        }
        if (zoomPanel == null)
        {
             Debug.LogError("ShowCard no puede continuar porque 'zoomPanel' es NULL.");
             return;
        }
         if (zoomContainer == null)
        {
             Debug.LogError("ShowCard no puede continuar porque 'zoomContainer' es NULL.");
             return;
        }
          if (cardPrefab == null)
        {
             Debug.LogError("ShowCard no puede continuar porque 'cardPrefab' es NULL.");
             return;
        }
        // --- Fin Verificaciones ---


        // Activa el panel principal del zoom
        zoomPanel.SetActive(true);
        Debug.Log("ShowCard: zoomPanel activado.");

        // Antes de instanciar una nueva, destruye la anterior si existe
        if (zoomedCard != null)
        {
             Debug.LogWarning("ShowCard: Ya existía una 'zoomedCard', destruyendo la anterior.");
             Destroy(zoomedCard);
        }

        // Instancia el prefab de la carta DENTRO del contenedor
        zoomedCard = Instantiate(cardPrefab, zoomContainer);
        Debug.Log($"ShowCard: Prefab '{cardPrefab.name}' instanciado dentro de '{zoomContainer.name}'.");

        // Establecer escala inicial (basada en el slider si existe, o el default)
        float initialScale = (zoomSlider != null) ? zoomSlider.value : DEFAULT_ZOOM;
        zoomedCard.transform.localScale = Vector3.one * initialScale;
        Debug.Log($"ShowCard: Escala inicial establecida a {initialScale}");

        // Intentamos quitar componentes que no queremos en la versión zoom
        var hoverEffect = zoomedCard.GetComponent<CardHoverEffect>();
        if (hoverEffect != null)
        {
            Destroy(hoverEffect);
            Debug.Log("ShowCard: Componente CardHoverEffect destruido.");
        }

        // Asumo que tienes un script llamado 'Card' en tu prefab original
        var cardComponent = zoomedCard.GetComponent<Card>();
        if (cardComponent != null)
        {
            Destroy(cardComponent);
            Debug.Log("ShowCard: Componente 'Card' destruido.");
        }
        else {
            Debug.LogWarning("ShowCard: No se encontró el componente 'Card' para destruir en la carta instanciada.");
        }


        // Intentamos configurar la UI de la carta instanciada
        // Asumo que tienes un script llamado 'CardUI' en tu prefab
        var cardUI = zoomedCard.GetComponent<CardUI>();
        if (cardUI != null)
        {
            Debug.Log("ShowCard: Encontrado componente CardUI, llamando a Setup...");
            // Aquí es donde se deberían poner los datos (nombre, imagen, etc.)
            cardUI.Setup(card); // ¡ASEGÚRATE QUE CardUI.Setup HACE LO QUE DEBE!
            Debug.Log("ShowCard: CardUI.Setup llamado exitosamente.");
        }
        else
        {
            // Si no encuentra CardUI, no podrá mostrar la información
            Debug.LogError("¡ERROR CRÍTICO! No se encontró el componente 'CardUI' en el prefab instanciado ('zoomedCard'). No se pueden mostrar los datos de la carta.");
        }
         Debug.Log("<----- Fin de ShowCard para: " + card.NombreCarta);
    }
    // --- Fin de la Función ShowCard ---

    // Función para ocultar el panel de zoom
    public void HideCard()
    {
        Debug.Log("HideCard llamado.");
        if (zoomedCard != null)
        {
            Destroy(zoomedCard);
            zoomedCard = null; // Importante ponerlo a null después de destruir
            Debug.Log("HideCard: zoomedCard destruido.");
        }
        if (zoomPanel != null)
        {
            zoomPanel.SetActive(false);
            Debug.Log("HideCard: zoomPanel desactivado.");
        }
    }

    // Limpieza al destruir el objeto que tiene este script
    private void OnDestroy()
    {
        Debug.Log("CardZoomManager: OnDestroy llamado.");
        // Quitar el listener del slider para evitar errores si el slider sobrevive a este objeto
        if (zoomSlider != null)
        {
            zoomSlider.onValueChanged.RemoveListener(OnZoomValueChanged);
            Debug.Log("CardZoomManager: Listener del Slider removido en OnDestroy.");
        }
    }
}