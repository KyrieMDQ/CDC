using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CardZoomDisplay : MonoBehaviour
{
    [Header("Referencias UI")]
    [SerializeField] private TextMeshProUGUI nombreCartaText;
    [SerializeField] private TextMeshProUGUI descripcionText;
    [SerializeField] private Image ilustracionImage;

    private ScriptableCard cartaActual;

    public void MostrarCarta(ScriptableCard carta)
    {
        cartaActual = carta;
        
        nombreCartaText.text = carta.NombreCarta;
        descripcionText.text = carta.Descripcion;
        ilustracionImage.sprite = carta.Ilustracion;
    }
}