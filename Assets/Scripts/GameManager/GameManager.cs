// GameManager.cs
using UnityEngine;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public bool DEBUG_ShowOpponentHandCosts = false;

    [Header("Estado del Juego")]
    public GameObject selectedCardObject = null; 
    public BoardCardController selectedCardAttacker = null; 
    private bool _isGameOver = false;
    public bool IsGameOver => _isGameOver;

    [Header("Referencias Jugador 1")]
    public PlayerStats jugador1;
    public BoardPlayZone boardPlayZoneJugador1;
    public DeckManager deckManagerJugador1;

    [Header("Referencias Jugador 2 (IA)")]
    public PlayerStats jugador2;
    public BoardPlayZone boardPlayZoneJugador2;
    public DeckManager deckManagerJugador2;

    private bool turnoJugador1 = true;

    private void Awake()
    {
        if (Instance == null) { Instance = this; }
        else { Destroy(gameObject); return; }
        _isGameOver = false;
        Time.timeScale = 1f; 

        if (!jugador1 || !jugador2 || !boardPlayZoneJugador1 || !boardPlayZoneJugador2 || !deckManagerJugador1 || !deckManagerJugador2) {
            Debug.LogError("¡FALTAN REFERENCIAS CRUCIALES EN GAMEMANAGER! Asigna jugadores, board zones y deck managers en el Inspector.");
            enabled = false; 
            return;
        }
    }

    void Start()
{
    if (!_isGameOver && enabled)
    {
        // ---- INICIO DE CÓDIGO AÑADIDO/MODIFICADO ----
        Debug.Log("[GameManager] Iniciando configuración de mazos y manos iniciales...");

        // Configurar y repartir mano inicial para Jugador 1
        if (deckManagerJugador1 != null)
        {
            Debug.Log("[GameManager] Mandando a DeckManager Jugador 1 a preparar mazo y mano inicial.");
            StartCoroutine(deckManagerJugador1.SetupPlayerDeckAndDealInitialHand());
        }
        else
        {
            Debug.LogError("¡GameManager no tiene asignado deckManagerJugador1!");
        }

        // Configurar y repartir mano inicial para Jugador 2 (IA)
        if (deckManagerJugador2 != null)
        {
            Debug.Log("[GameManager] Mandando a DeckManager Jugador 2 a preparar mazo y mano inicial.");
            StartCoroutine(deckManagerJugador2.SetupPlayerDeckAndDealInitialHand());
        }
        else
        {
            Debug.LogError("¡GameManager no tiene asignado deckManagerJugador2!");
        }
        // ---- FIN DE CÓDIGO AÑADIDO/MODIFICADO ----

        // Ahora, proceder a iniciar el primer turno.
        // Las corutinas de SetupPlayerDeckAndDealInitialHand comenzarán a ejecutarse.
        // IniciarPrimerTurno() luego robará la carta del *primer turno*.
        IniciarPrimerTurno();
    }
}
    
    void IniciarPrimerTurno() {
        turnoJugador1 = true; 
        Debug.Log("Iniciando primer turno para Player1.");
        IniciarTurno();
    }

    public PlayerStats GetCurrentPlayer() { return turnoJugador1 ? jugador1 : jugador2; }
    public PlayerStats GetOpponentPlayer() { return turnoJugador1 ? jugador2 : jugador1; }
    public BoardPlayZone GetCurrentPlayerBoardZone() { return turnoJugador1 ? boardPlayZoneJugador1 : boardPlayZoneJugador2; }
    public BoardPlayZone GetOpponentPlayerBoardZone() { return turnoJugador1 ? boardPlayZoneJugador2 : boardPlayZoneJugador1; }
    public DeckManager GetCurrentPlayerDeckManager() { return turnoJugador1 ? deckManagerJugador1 : deckManagerJugador2; }

    public void IniciarTurno()
    {
        if (_isGameOver) return;
        PlayerStats currentPlayer = GetCurrentPlayer();
        if (currentPlayer == null) { Debug.LogError("IniciarTurno: currentPlayer es null."); return; }
        
        Debug.Log($"==== INICIANDO TURNO DE: {currentPlayer.name} (Es turno de Player1: {turnoJugador1}) ====");

        currentPlayer.IniciarPreparativosDeTurno(); 

        DeckManager currentDeckManager = GetCurrentPlayerDeckManager();
        if (currentDeckManager != null) {
            if (currentDeckManager.mazo.Count == 0 && currentDeckManager.playerHand.Count == 0) { 
                EndGameByDeckOut(currentPlayer); 
                return;
            } else if (currentDeckManager.mazo.Count > 0) {
                 StartCoroutine(currentDeckManager.RobarCartaVisual());
            } else {
                 Debug.LogWarning($"[GameManager] {currentPlayer.name} no tiene cartas en el mazo para robar, pero podría tener cartas en mano.");
            }
        } else { Debug.LogError($"IniciarTurno: DeckManager null para {currentPlayer.name}."); }

        BoardPlayZone cpbz = GetCurrentPlayerBoardZone();
        if (cpbz != null) { 
            List<GameObject> cardsOnBoardCopy = new List<GameObject>(cpbz.CartasEnZona());
            foreach (GameObject cardGO in cardsOnBoardCopy) {
                if (cardGO != null) { 
                    var bcc = cardGO.GetComponent<BoardCardController>(); 
                    if (bcc) { 
                        bcc.ResetTurnStatus(); 
                    } 
                }
            }
        }

        if (!turnoJugador1) { 
            if (jugador2?.IAController != null) { 
                StartCoroutine(jugador2.IAController.TomarTurno()); 
            } else { 
                Debug.LogWarning("Turno de IA, pero jugador2 o su IAController no está asignado/configurado."); 
            }
        }
    }

    public void FinalizarTurno()
    {
        if (_isGameOver) return;

        PlayerStats jugadorQueTerminaTurno = GetCurrentPlayer();
        Debug.Log($"--- FINALIZANDO TURNO DE: {jugadorQueTerminaTurno.name} ---");
        ClearAttackerSelection(); 

        if (jugadorQueTerminaTurno != null)
        {
            // --- LLAMADA IMPORTANTE PARA PROCESAR MODIFICADORES ---
            jugadorQueTerminaTurno.ProcesarFinDeTurnoParaModificadores();
            // -------------------------------------------------------
        }

        turnoJugador1 = !turnoJugador1; 
        IniciarTurno(); 
    }

    public DeckManager GetOpponentPlayerDeckManager()
{
    return turnoJugador1 ? deckManagerJugador2 : deckManagerJugador1;
}

    public void ClearAttackerSelection() { 
        if (selectedCardAttacker != null) { 
            if (selectedCardAttacker.gameObject != null && selectedCardAttacker.gameObject.activeInHierarchy) { 
                selectedCardAttacker.ToggleAttackerHighlight(false); 
            } 
            selectedCardAttacker = null; 
        } 
    }
    
    public void EndGameByDeckOut(PlayerStats playerWhoDeckedOut) { 
        if (_isGameOver) return; 
        Debug.Log($"¡FIN DEL JUEGO! {playerWhoDeckedOut.name} intentó robar de un mazo vacío y pierde.");
        DeclararPerdedor(playerWhoDeckedOut);
    }

    public void DeclararPerdedor(PlayerStats perdedor) { 
        if (_isGameOver) return; 
        PlayerStats ganador = (perdedor == jugador1) ? jugador2 : jugador1;
        DeclararGanadorYPerdedor(ganador, perdedor, $"El Dinero de {perdedor.name} llegó a 0 o se quedó sin cartas."); 
    }

    private void DeclararGanadorYPerdedor(PlayerStats ganador, PlayerStats perdedor, string razon) { 
        if (_isGameOver) return; 
        _isGameOver = true; 
        string mensaje = $"¡FIN DE LA PARTIDA!\nGanador: {ganador?.name ?? "Desconocido"}\nPerdedor: {perdedor?.name ?? "Desconocido"}\nRazón: {razon}"; 
        Debug.Log(mensaje.Replace("\n", " ")); 
        if (UIManager.Instance != null) UIManager.Instance.ShowStatusMessage(mensaje, 30f); 
        Time.timeScale = 0f; 
        Debug.LogWarning("Juego Pausado. Fin de la partida."); 
    }

    private void DeclareDraw(string razon) { 
        if (_isGameOver) return; 
        _isGameOver = true; 
        string mensaje = $"¡FIN DE LA PARTIDA!\n¡EMPATE!\nRazón: {razon}"; 
        Debug.Log(mensaje.Replace("\n"," ")); 
        if (UIManager.Instance != null) UIManager.Instance.ShowStatusMessage(mensaje, 30f); 
        Time.timeScale = 0f; 
        Debug.LogWarning("Juego Pausado. Empate."); 
    }
}