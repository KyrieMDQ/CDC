using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NuevoMazo", menuName = "Cartas/Card Collection", order = 1)]
public class CardCollection : ScriptableObject
{
    public List<ScriptableCard> Cartas;
}