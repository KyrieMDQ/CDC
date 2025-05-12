// CardUI.cs
using UnityEngine;
using UnityEngine.UI; // Necesario para Image
using TMPro;          // Necesario para TextMeshProUGUI

public class CardUI : MonoBehaviour
{
    [Header("Referencias UI")]
    [SerializeField] private TextMeshProUGUI nombreCartaText;
    [SerializeField] private TextMeshProUGUI descripcionText;
    [SerializeField] private TextMeshProUGUI costoPoderPoliticoText; // Texto del costo ORIGINAL
    [SerializeField] private TextMeshProUGUI influenciaText;
    [SerializeField] private TextMeshProUGUI resistenciaText;
    [SerializeField] private TextMeshProUGUI faccionText;
    [SerializeField] private Image ilustracionImage;
    [SerializeField] private TextMeshProUGUI costoModificadoText; // Para tu costo alternativo

    private ScriptableCard cartaReferencia;

    // Definimos los colores que quieres usar
    private static readonly Color COLOR_COSTO_NORMAL = Color.red;     // ROJO como default para costo normal
    private static readonly Color COLOR_COSTO_REDUCIDO = Color.green;  // VERDE para costo reducido
    private static readonly Color COLOR_COSTO_AUMENTADO = new Color(1f, 0.5f, 0f); // Naranja para costo aumentado (ejemplo)


    void Awake()
    {
        // Asegurarse que el costo modificado esté oculto al inicio
        if (costoModificadoText != null)
        {
            costoModificadoText.gameObject.SetActive(false);
        }
        // El color del costo original se establecerá en Setup
    }

    public void Setup(ScriptableCard carta)
    {
        if (carta == null) {
            // Limpiar campos
            if (nombreCartaText != null) nombreCartaText.text = "";
            if (descripcionText != null) descripcionText.text = "";
            if (costoPoderPoliticoText != null) {
                 costoPoderPoliticoText.text = "";
                 costoPoderPoliticoText.gameObject.SetActive(true); // Mostrar por si estaba oculto
            }
            if (influenciaText != null) influenciaText.text = "";
            if (resistenciaText != null) resistenciaText.text = "";
            if (faccionText != null) faccionText.text = "";
            if (ilustracionImage != null) ilustracionImage.sprite = null;
            if (costoModificadoText != null) costoModificadoText.gameObject.SetActive(false); // Ocultar
            cartaReferencia = null;
            return;
        }

        cartaReferencia = carta;

        if (nombreCartaText != null) nombreCartaText.text = carta.NombreCarta;
        if (descripcionText != null) descripcionText.text = carta.Descripcion;
        
        if (costoPoderPoliticoText != null) {
            costoPoderPoliticoText.text = carta.CostoPoderPolitico.ToString();
            costoPoderPoliticoText.color = COLOR_COSTO_NORMAL; // Aplicar tu color ROJO por defecto
            costoPoderPoliticoText.gameObject.SetActive(true); 
        }

        // Asegurar que el costo modificado esté oculto al (re)configurar la carta
        if (costoModificadoText != null) {
            costoModificadoText.gameObject.SetActive(false);
        }

        if (influenciaText != null) influenciaText.text = carta.Influencia.ToString();
        if (resistenciaText != null) resistenciaText.text = carta.Resistencia.ToString();
        if (ilustracionImage != null) ilustracionImage.sprite = carta.Ilustracion;

        if (faccionText != null) {
            if (!string.IsNullOrEmpty(carta.Faccion)) {
                faccionText.text = carta.Faccion;
                faccionText.gameObject.SetActive(true);
            } else {
                faccionText.gameObject.SetActive(false);
            }
        }
        // Debug.Log($"CardUI: Setup para '{carta.NombreCarta}', costo puesto a color {COLOR_COSTO_NORMAL}");
    }

    // Método público para actualizar el costo visual (texto y color) usando tu idea de campos separados
    public void ActualizarCostoVisual(int costoRealAMostrar)
    {
        if (cartaReferencia == null || costoPoderPoliticoText == null || costoModificadoText == null) 
        {
            return;
        }

        if (costoRealAMostrar < cartaReferencia.CostoPoderPolitico) {
            // Costo REDUCIDO
            costoPoderPoliticoText.gameObject.SetActive(false); // Ocultar original
            costoModificadoText.text = costoRealAMostrar.ToString();
            costoModificadoText.color = COLOR_COSTO_REDUCIDO;    // VERDE
            costoModificadoText.gameObject.SetActive(true);      // Mostrar modificado
        } else if (costoRealAMostrar > cartaReferencia.CostoPoderPolitico) {
            // Costo AUMENTADO
            costoPoderPoliticoText.gameObject.SetActive(false); // Ocultar original
            costoModificadoText.text = costoRealAMostrar.ToString();
            costoModificadoText.color = COLOR_COSTO_AUMENTADO; // NARANJA/ROJO AUMENTADO
            costoModificadoText.gameObject.SetActive(true);      // Mostrar modificado
        } else { 
            // Costo NORMAL (igual al original de la carta)
            if (costoModificadoText != null) costoModificadoText.gameObject.SetActive(false); // Ocultar modificado
            
            if (costoPoderPoliticoText != null) {
                costoPoderPoliticoText.text = cartaReferencia.CostoPoderPolitico.ToString(); // Asegurar texto original
                costoPoderPoliticoText.color = COLOR_COSTO_NORMAL; // Usar el color ROJO original
                costoPoderPoliticoText.gameObject.SetActive(true);      // Mostrar original
            }
        }
    }
}