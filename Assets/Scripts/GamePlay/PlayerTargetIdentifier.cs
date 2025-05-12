using UnityEngine;
using UnityEngine.EventSystems;

public class PlayerTargetIdentifier : MonoBehaviour, IPointerClickHandler
{
    public PlayerStats associatedPlayerStats; // Asignar el PlayerStats de este jugador (IA)

    void Start()
    {
        if (associatedPlayerStats == null) { associatedPlayerStats = GetComponentInParent<PlayerStats>(); }
        if (associatedPlayerStats == null) { Debug.LogError($"PlayerTargetIdentifier en '{gameObject.name}' no tiene associatedPlayerStats."); }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver) return; // Chequeo fin de juego
        if (eventData.button != PointerEventData.InputButton.Left) return;
        if (GameManager.Instance == null || GameManager.Instance.selectedCardAttacker == null) return; // No hay atacante

        // Asegurarse que el objetivo (este PlayerStats) es el oponente del jugador actual
        if (this.associatedPlayerStats == GameManager.Instance.GetOpponentPlayer())
        {
            BoardCardController atacante = GameManager.Instance.selectedCardAttacker;

            // --- VERIFICACIÓN DE FUEROS ---
            BoardPlayZone opponentBoard = GameManager.Instance.GetOpponentPlayerBoardZone(); // El tablero del jugador atacado
            if (opponentBoard != null && opponentBoard.HayCartasConFuerosActivas()) // Usamos el helper
            {
                // Si hay cartas con Fueros en el tablero del oponente, NO se puede atacar al jugador directamente
                Debug.LogWarning($"Ataque inválido al jugador: '{atacante.cardData?.NombreCarta}' no puede atacar a '{this.associatedPlayerStats.name}' porque hay Políticos con Fueros.");
                if (UIManager.Instance != null) UIManager.Instance.ShowStatusMessage("¡Debes atacar a un Político con Fueros primero!");
                // No deseleccionamos, permitimos elegir otro objetivo
                return; // Detener el ataque al jugador
            }
            // --- FIN VERIFICACIÓN DE FUEROS ---

            // Si pasa la verificación, procede a atacar al jugador
            // Debug.Log($"'{atacante.cardData.NombreCarta}' ATACA AL JUGADOR Oponente: {associatedPlayerStats.name}");
            atacante.AtacarJugador(this.associatedPlayerStats);
            GameManager.Instance.ClearAttackerSelection(); // Limpiar selección después del ataque
        }
        // else { // Se hizo click en el avatar del jugador actual o algo inesperado }
    }
}