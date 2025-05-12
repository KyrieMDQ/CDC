using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CartaDetalleUI : MonoBehaviour
{
    [Header("Referencias UI")]
    [SerializeField] private TextMeshProUGUI nombreCartaText;
    [SerializeField] private TextMeshProUGUI descripcionText;
    [SerializeField] private TextMeshProUGUI influenciaText;
    [SerializeField] private TextMeshProUGUI resistenciaText;
    [SerializeField] private Image ilustracionImage;

    private ScriptableCard cartaActual;

    public void MostrarDetalleCarta(ScriptableCard carta)
    {
        cartaActual = carta;

        nombreCartaText.text = carta.NombreCarta;
        descripcionText.text = carta.Descripcion;
        influenciaText.text = carta.Influencia.ToString();
        resistenciaText.text = carta.Resistencia.ToString();
        ilustracionImage.sprite = carta.Ilustracion;
    }
}