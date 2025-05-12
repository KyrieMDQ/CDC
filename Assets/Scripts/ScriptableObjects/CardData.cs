using UnityEngine;

[CreateAssetMenu(fileName = "NuevaCarta", menuName = "Cartas/Nueva Carta")]
public class CardData : ScriptableObject
{
    public string nombre;
    public string descripcion;
    public int costo;
    public int influencia;
    public int resistencia;
    public Sprite imagen;
}
