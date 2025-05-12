using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DeckSetupUI : MonoBehaviour
{
    public CardCollection mazoBasico; // Asset del mazo básico (ScriptableObject)
    public Button botonAgregarAlMazo;
    public List<ScriptableCard> cartasDisponibles = new List<ScriptableCard>(); // Llena esta lista desde tus ScriptableCard

    private List<ScriptableCard> cartasSeleccionadas = new List<ScriptableCard>();

    void Start()
    {
        botonAgregarAlMazo.onClick.AddListener(AgregarAlMazoBasico);
    }

    // Este método deberías llamarlo desde tu UI para seleccionar cartas
    public void SeleccionarCarta(ScriptableCard carta)
    {
        if (!cartasSeleccionadas.Contains(carta))
            cartasSeleccionadas.Add(carta);
    }

    public void AgregarAlMazoBasico()
    {
        if (mazoBasico == null)
        {
            Debug.LogError("❌ El mazo básico no está asignado.");
            return;
        }
        if (cartasSeleccionadas.Count == 0)
        {
            Debug.LogError("❌ No hay cartas seleccionadas para agregar.");
            return;
        }

        mazoBasico.Cartas.Clear();
        mazoBasico.Cartas.AddRange(cartasSeleccionadas);
        Debug.Log("✅ Cartas agregadas al mazo básico.");
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(mazoBasico);
        UnityEditor.AssetDatabase.SaveAssets();
#endif
    }
}