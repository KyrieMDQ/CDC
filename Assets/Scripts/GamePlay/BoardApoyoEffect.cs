// BoardApoyoEffect.cs
using UnityEngine;

// Este componente se añade a los GameObjects de cartas de Apoyo en el tablero
// para gestionar sus efectos mientras están en juego.
public class BoardApoyoEffect : MonoBehaviour
{
    private ScriptableCard cardData; // Referencia a los datos de la carta de Apoyo
    private PlayerStats ownerPlayer; // El jugador dueño de esta carta de Apoyo
    private BoardPlayZone parentBoardZone; // La zona del tablero donde está

    // Método llamado para inicializar el componente con los datos de la carta y el dueño
    public void Initialize(ScriptableCard card, PlayerStats owner, BoardPlayZone zone)
    {
        cardData = card;
        ownerPlayer = owner;
        parentBoardZone = zone;

        if (cardData == null || ownerPlayer == null || parentBoardZone == null)
        {
            Debug.LogError($"BoardApoyoEffect en '{gameObject.name}': Inicialización con referencias nulas.");
            enabled = false; // Desactivar si no se inicializa correctamente
            return;
        }

        Debug.Log($"BoardApoyoEffect: Inicializado para '{cardData.NombreCarta}' (Dueño: {ownerPlayer.name})");

        // TODO: Aquí puedes activar efectos continuos o suscribirte a eventos del juego
        // para efectos activados (ej. al inicio del turno, al jugar otra carta, etc.).

        // Ejemplo: Si un Apoyo da un buff continuo a todas las criaturas del dueño
        // Podrías tener un método en GameManager o PlayerStats que itere sobre las criaturas
        // y este componente llame a ese método periódicamente o cuando sea necesario.
        // O el efecto podría simplemente modificar las stats de las criaturas al entrar en juego.

        // Ejemplo de efecto simple "Al entrar en juego":
        // if (cardData.NombreCarta == "Apoyo que da +1/+1") {
        //     ApplyBuffToCreatures(ownerPlayer.myBoardZone.CartasEnZona(), 1, 1); // Implementar ApplyBuffToCreatures
        // }
    }

    // TODO: Implementar lógica para efectos continuos (en Update, FixedUpdate, o suscrito a eventos)
    // Ejemplo: Si el efecto ocurre cada turno al inicio del turno del dueño
    // void OnEnable() { GameManager.Instance.OnTurnStart += CheckEffectOnTurnStart; } // Asumiendo evento en GameManager
    // void OnDisable() { GameManager.Instance.OnTurnStart -= CheckEffectOnTurnStart; }
    // void CheckEffectOnTurnStart(PlayerStats player) {
    //     if (player == ownerPlayer) {
    //         Debug.Log($"BoardApoyoEffect: Efecto de '{cardData.NombreCarta}' activado al inicio del turno de {ownerPlayer.name}.");
    //         // Ejecutar lógica del efecto continuo/por turno
    //     }
    // }


    // TODO: Implementar lógica para efectos activados por eventos específicos (ej. al robar carta, al jugar carta, al atacar)
    // Esto requeriría suscribirse a eventos relevantes en GameManager, DeckManager, BoardCardController, etc.


    // Método llamado cuando la carta de Apoyo es removida del tablero (ej. destruida)
    void OnDestroy()
    {
        Debug.Log($"BoardApoyoEffect: Destruido para '{cardData?.NombreCarta ?? "Carta Nula"}'.");
        // TODO: Aquí debes remover cualquier efecto continuo que este Apoyo estuviera aplicando.
        // Ejemplo: Si daba un buff, remover ese buff de las criaturas afectadas.
    }

    // Método helper de ejemplo para aplicar un buff a una lista de GameObjects de cartas
    // (Necesitaría acceso a los BoardCardController de esos GOs)
    /*
    void ApplyBuffToCreatures(List<GameObject> creatureGOs, int influenceBuff, int resistanceBuff) {
        foreach(GameObject go in creatureGOs) {
            BoardCardController bcc = go.GetComponent<BoardCardController>();
            if (bcc != null) {
                bcc.influencia += influenceBuff;
                bcc.resistencia += resistanceBuff;
                // TODO: Actualizar UI de la carta en el tablero para mostrar los nuevos stats
            }
        }
    }
    */
}
