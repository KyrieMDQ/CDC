using UnityEngine;
using UnityEngine.UI; // Para Image y Sprite
using UnityEngine.EventSystems;

public class Card : MonoBehaviour, IPointerClickHandler // Asegúrate que implementa IPointerClickHandler
{
    public ScriptableCard cardData;
    private CardUI cardUI;
    private CardZoomManager zoomManager;

    [Header("Feedback Visual (Selección en Mano)")]
    public GameObject highlightBorderObject; // Borde verde

    [Header("Componentes Visuales del Frente")]
    public GameObject artObject;
    public GameObject descripcionObject;
    public GameObject nombreObject;
    public GameObject ataqueObject;
    public GameObject defensaObject;
    public GameObject costoObject;
    public Image marcoRarezaImage;      // El Image del marco
    public GameObject elementoObject;    // El GO que contiene la Faccion

    [Header("Recursos de Marcos por Rareza")]
    public Sprite marcoComunSprite;
    public Sprite marcoRaraSprite;
    public Sprite marcoEpicaSprite;
    public Sprite marcoLegendariaSprite;

    [Header("Feedback Visual - Descarte")]
    public GameObject discardBorderObject; // Borde negro

    [Header("Interacción")]
    [Tooltip("El GameObject invisible que detecta los clicks en toda la carta.")]
    public GameObject clickHitboxObject; // Arrastra aquí el hijo "ClickHitbox"
    public bool isInteractable = true; // Controla si la carta responde a clicks

    public ScriptableCard CardData => cardData;

    private void Awake()
    {
        cardUI = GetComponent<CardUI>();
        if (cardUI == null) cardUI = GetComponentInChildren<CardUI>();
        if (cardUI == null) Debug.LogError($"Error en '{gameObject.name}': No se encontró CardUI.");

        // Asignar/Buscar referencias (MEJOR ASIGNAR EN INSPECTOR)
        ConfigureHighlight(ref highlightBorderObject, "HighlightBorder");
        AssignChildObject(ref artObject, "Art");
        AssignChildObject(ref descripcionObject, "Descripcion");
        AssignChildObject(ref nombreObject, "Nombre");
        AssignChildObject(ref ataqueObject, "Ataque");
        AssignChildObject(ref defensaObject, "Defensa");
        AssignChildObject(ref costoObject, "Costo");
        AssignChildObject(ref elementoObject, "Elemento");
        AssignImageComponent(ref marcoRarezaImage, "MarcoRareza");
        ConfigureHighlight(ref discardBorderObject, "DiscardBorder");
        AssignChildObject(ref clickHitboxObject, "ClickHitbox");

        if (clickHitboxObject != null) {
             Image hitboxImage = clickHitboxObject.GetComponent<Image>();
             if (hitboxImage == null || !hitboxImage.raycastTarget) Debug.LogWarning($"'{name}': ClickHitbox necesita Image con Raycast Target activado.");
        } else { Debug.LogWarning($"'{name}': clickHitboxObject no asignado/encontrado."); }
    }

    private void ConfigureHighlight(ref GameObject highlightGO, string childName) { if (highlightGO == null) { Transform found = transform.Find(childName); if (found != null) highlightGO = found.gameObject; } if (highlightGO != null) { highlightGO.SetActive(false); } }
    private void AssignChildObject(ref GameObject field, string childName) { if (field == null) { Transform found = transform.Find(childName); if (found != null) field = found.gameObject; } }
    private void AssignImageComponent(ref Image imageComp, string childName) { if (imageComp == null) { Transform found = transform.Find(childName); if (found != null) imageComp = found.GetComponent<Image>(); if (imageComp == null && found != null) Debug.LogError($"El hijo '{childName}' no tiene Image."); } }

    private void Start()
    {
        zoomManager = FindFirstObjectByType<CardZoomManager>();
        if (zoomManager == null) Debug.LogError($"No se encontró CardZoomManager para '{gameObject.name}'.");
        UpdateCardUI();
    }

    public void SetCardData(ScriptableCard newCardData)
    {
        cardData = newCardData;
        UpdateCardUI();
    }

    private void UpdateCardUI()
    {
        if (cardData == null) return;
        if (cardUI != null && cardUI.enabled) { cardUI.Setup(cardData); }
        UpdateCardFrame();
    }

    private void UpdateCardFrame()
    {
         if (cardData == null || marcoRarezaImage == null) return;
         Sprite spriteToShow = marcoComunSprite;
         switch (cardData.Rareza?.ToLowerInvariant()) {
             case "rara": spriteToShow = marcoRaraSprite; break;
             case "epica": spriteToShow = marcoEpicaSprite; break;
             case "legendaria": spriteToShow = marcoLegendariaSprite; break;
             case "comun": default: spriteToShow = marcoComunSprite; break;
         }
         if (spriteToShow != null) { marcoRarezaImage.sprite = spriteToShow; }
         else { marcoRarezaImage.sprite = marcoComunSprite; }
    }

    // ---- MÉTODO CORREGIDO A PUBLIC ----
    public void OnPointerClick(PointerEventData eventData)
    {
        if (!isInteractable || (GameManager.Instance != null && GameManager.Instance.IsGameOver)) return;

        if (eventData.button == PointerEventData.InputButton.Left)
        {
            DeckManager currentDeckManager = GameManager.Instance?.GetCurrentPlayerDeckManager();
            // Solo procesar si está en la mano del jugador activo
            if (currentDeckManager != null && transform.parent == currentDeckManager.handContainer)
            {
                 if (GameManager.Instance.selectedCardObject == this.gameObject) { /* Deseleccionar */
                     GameManager.Instance.selectedCardObject = null; ToggleHighlight(false);
                } else { /* Seleccionar */
                    if (GameManager.Instance.selectedCardObject != null) { Card prev = GameManager.Instance.selectedCardObject.GetComponent<Card>(); if (prev != null) prev.ToggleHighlight(false); }
                    GameManager.Instance.selectedCardObject = this.gameObject; ToggleHighlight(true);
                }
            }
        }
        else if (eventData.button == PointerEventData.InputButton.Right) // ZOOM
        {
            if (zoomManager != null && cardData != null) { zoomManager.ShowCard(cardData); }
        }
    }
    // ---------------------------------

    public void ToggleHighlight(bool highlightOn)
    {
        if (highlightBorderObject != null) { highlightBorderObject.SetActive(highlightOn); }
    }

    // Muestra/Oculta los elementos VISUALES del frente/reverso
    public void ShowAsCardBack(bool showBack)
    {
        if (artObject != null) artObject.SetActive(!showBack);
        if (descripcionObject != null) descripcionObject.SetActive(!showBack);
        if (nombreObject != null) nombreObject.SetActive(!showBack);
        if (ataqueObject != null) ataqueObject.SetActive(!showBack);
        if (defensaObject != null) defensaObject.SetActive(!showBack);
        if (costoObject != null) costoObject.SetActive(!showBack);
        if (elementoObject != null) elementoObject.SetActive(!showBack);
        if (marcoRarezaImage != null) marcoRarezaImage.gameObject.SetActive(!showBack); // Ocultar marco si es reverso

        if (cardUI != null) { cardUI.enabled = !showBack; }
    }

    // --- MÉTODO AÑADIDO para look de descarte ---
    public void SetDiscardedLook(bool isDiscarded)
    {
        if (discardBorderObject != null)
        {
            discardBorderObject.SetActive(isDiscarded);
        }
    }

    // --- MÉTODO AÑADIDO para controlar interactividad y hitbox ---
    public void SetInteractable(bool canInteract)
    {
        this.isInteractable = canInteract;
        if (clickHitboxObject != null)
        {
            // Activar/Desactivar el hitbox es una forma de controlar la interactividad
            // si tu Card.OnPointerClick NO verifica isInteractable al inicio.
            // Pero como sí lo verifica, podrías dejar el hitbox siempre activo
            // o controlarlo aquí como doble seguridad.
            clickHitboxObject.SetActive(canInteract);
        }
        if (!canInteract)
        {
             ToggleHighlight(false); // Quitar highlight si se vuelve no interactiva
        }
    }
}