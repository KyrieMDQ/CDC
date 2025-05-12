// IAController.cs
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class IAController : MonoBehaviour
{
    public DeckManager deckManagerIA;
    public PlayerStats playerStatsIA;
    public BoardPlayZone boardPlayZoneIA;

    public float tiempoPensarEntreAcciones = 0.75f;
    public float tiempoEsperaAtaque = 0.75f;

    public IEnumerator TomarTurno()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver) yield break;
        // Debug.Log($"[IA - {playerStatsIA.name}] >>> INICIANDO TURNO. Poder: {playerStatsIA.poderActual}/{playerStatsIA.maxPoderEsteTurno}");
        yield return new WaitForSeconds(tiempoPensarEntreAcciones);

        // Debug.Log($"[IA - {playerStatsIA.name}] Fase Jugar Cartas. Mano: {deckManagerIA.playerHand.Count}, Poder: {playerStatsIA.poderActual}");
        bool pudoJugarAlgunaCartaLoop;
        int safetyBreakJugarCartas = 20;

        do
{
    pudoJugarAlgunaCartaLoop = false;
    safetyBreakJugarCartas--;

    // EncontrarCartaJugable ya debería considerar si la IA puede pagar
    // y si hay espacio para Políticos/Personajes.
    ScriptableCard cartaAJugar = deckManagerIA.EncontrarCartaJugable(playerStatsIA.poderActual, boardPlayZoneIA);

    if (cartaAJugar == null) {
        // Debug.Log($"[IA - {playerStatsIA.name}] No encontró carta jugable o no puede pagar/colocar.");
        pudoJugarAlgunaCartaLoop = false;
        continue; // Intenta en la siguiente iteración del loop o termina si no hay más opciones
    }

    // No es necesario llamar a playerStatsIA.PuedePagar aquí si EncontrarCartaJugable ya lo hizo.
    // Y no llames a playerStatsIA.Pagar aquí; deja que los métodos de juego específicos lo hagan.

    string tipoCartaNormalizado = cartaAJugar.TipoCarta?.Trim().ToLowerInvariant() ?? "desconocido";
    Debug.Log($"[IAController] Consideranto jugar: '{cartaAJugar.NombreCarta}' (Tipo: {tipoCartaNormalizado})");

    if (tipoCartaNormalizado == "político" || tipoCartaNormalizado == "politico" || 
        tipoCartaNormalizado == "apoyo" || tipoCartaNormalizado == "personaje") // Tipos que van al tablero
    {
        // La validación de slot ya debería estar en EncontrarCartaJugable o dentro de TryPlayCardFromHandToBoard
        // Si EncontrarCartaJugable ya lo validó (incluyendo slots), podemos proceder.

        GameObject cardGOInHand = deckManagerIA.GetHandCardObject(cartaAJugar);

        if (cardGOInHand != null)
        {
            Debug.Log($"[IAController] Intentando jugar '{cartaAJugar.NombreCarta}' al tablero. GO: {cardGOInHand.name}, Zona: {boardPlayZoneIA.name}, Stats: {playerStatsIA.name}");
            // TryPlayCardFromHandToBoard se encargará de verificar costo, pagar, remover de mano e instanciar
            if (deckManagerIA.TryPlayCardFromHandToBoard(cardGOInHand, boardPlayZoneIA, playerStatsIA))
            {
                // El log de pago "[PlayerStats - Player2] Pagó..." ahora vendrá desde DENTRO de TryPlayCardFromHandToBoard
                Debug.Log($"[IAController] Éxito al jugar '{cartaAJugar.NombreCarta}' al tablero.");
                pudoJugarAlgunaCartaLoop = true;
                yield return new WaitForSeconds(tiempoPensarEntreAcciones); // Pausa después de jugar
            }
            else
            {
                Debug.LogError($"[IAController] Falló TryPlayCardFromHandToBoard para '{cartaAJugar.NombreCarta}' (razón debería estar en log de DeckManager).");
                pudoJugarAlgunaCartaLoop = false; 
                // Si falla aquí, es probable que no pueda pagar o un error interno.
                // EncontrarCartaJugable debería haber prevenido esto si el costo era el problema.
                // Considera no hacer 'break' para que la IA intente otras cartas si una falla inesperadamente.
                // break; // Opcional: si un fallo es crítico para el turno de la IA.
            }
        }
        else
        {
            Debug.LogError($"[IAController] ERROR CRÍTICO: No se encontró el GameObject en mano para '{cartaAJugar.NombreCarta}' que EncontrarCartaJugable dijo que era jugable.");
            // Esto indica un problema de sincronización entre la lista lógica de mano y la visual.
            pudoJugarAlgunaCartaLoop = false;
            break; // Salir del loop de jugar cartas si hay un problema grave de sincronización
        }
    }
    else if (tipoCartaNormalizado == "accion" || tipoCartaNormalizado == "acción" || tipoCartaNormalizado == "evento")
    {
        Debug.Log($"[IAController] '{cartaAJugar.NombreCarta}' es Acción/Evento. Verificando pago...");
        // Para Acciones/Eventos, la IA debe manejar el pago aquí si ActionEffectManager no lo hace.
        if (playerStatsIA.PuedePagar(cartaAJugar))
        {
            playerStatsIA.Pagar(cartaAJugar); // Paga la acción
            Debug.Log($"[IAController] Costo pagado por '{cartaAJugar.NombreCarta}'. Ejecutando efecto...");

            if (ActionEffectManager.Instance != null)
            {
                ActionEffectManager.Instance.ExecuteEffect(
                    cartaAJugar,
                    playerStatsIA,
                    GameManager.Instance.GetOpponentPlayer(),
                    boardPlayZoneIA, // Caster's board zone
                    GameManager.Instance.GetOpponentPlayerBoardZone() // Opponent's board zone
                );
            }
            else
            {
                Debug.LogError("[IAController] ActionEffectManager.Instance es null.");
                // Aquí deberías considerar reembolsar el costo, ya que se pagó pero el efecto no se pudo ejecutar.
                // playerStatsIA.poderActual += playerStatsIA.GetCostoRealCarta(cartaAJugar); // Concepto
                // playerStatsIA.ActualizarPoderUI();
                pudoJugarAlgunaCartaLoop = false;
                break; 
            }
            deckManagerIA.AddToDiscard(cartaAJugar);
            deckManagerIA.RemoveCardFromHand(cartaAJugar); // Remueve el GO de la mano
            pudoJugarAlgunaCartaLoop = true;
            yield return new WaitForSeconds(tiempoPensarEntreAcciones + 0.2f); // Pausa después de acción
        }
        else
        {
            Debug.LogWarning($"[IAController] No pudo pagar la acción '{cartaAJugar.NombreCarta}' (esto no debería pasar si EncontrarCartaJugable funciona bien).");
            pudoJugarAlgunaCartaLoop = false;
            continue; // Intenta otra carta
        }
    }
    else
    {
        Debug.LogWarning($"[IAController] Tipo carta '{tipoCartaNormalizado}' ('{cartaAJugar.NombreCarta}') no manejado por la IA.");
        pudoJugarAlgunaCartaLoop = false;
        // No hacer break aquí necesariamente, podría haber otras cartas que sí sepa jugar.
        // break;
    }

} while (pudoJugarAlgunaCartaLoop && safetyBreakJugarCartas > 0 && playerStatsIA.poderActual > 0 && deckManagerIA.playerHand.Count > 0);
        
        List<BoardCardController> misAtacantes = new List<BoardCardController>();
        foreach (GameObject cGO in boardPlayZoneIA.CartasEnZona()) {
            if(cGO != null) {
                var bcc = cGO.GetComponent<BoardCardController>();
                if(bcc != null && bcc.PuedeAtacar()) misAtacantes.Add(bcc);
            }
        }

        PlayerStats jugadorOponente = GameManager.Instance.GetOpponentPlayer();
        BoardPlayZone zonaOponente = GameManager.Instance.GetOpponentPlayerBoardZone();

        if (jugadorOponente == null || zonaOponente == null) { Debug.LogError("IAController: Oponente o su zona es null en ataque."); yield break; }

        foreach (BoardCardController miAtacante in misAtacantes) {
            if (miAtacante == null || !miAtacante.gameObject.activeInHierarchy || !miAtacante.PuedeAtacar()) continue;
            BoardCardController objetivoCartaEnemiga = null;
            bool hayFuerosEnOponente = zonaOponente.HayCartasConFuerosActivas();
            List<BoardCardController> posiblesObjetivosCartas = new List<BoardCardController>();
            List<BoardCardController> objetivosConFueros = new List<BoardCardController>();

            foreach (GameObject ego in zonaOponente.CartasEnZona()) {
                if(ego != null) {
                    var ebcc = ego.GetComponent<BoardCardController>();
                    if(ebcc != null && !ebcc.isDying && ebcc.resistencia > 0) {
                        string objetivoTipoNorm = ebcc.cardData?.TipoCarta?.Trim().ToLowerInvariant() ?? "";
                        string atacanteTipoNorm = miAtacante.cardData?.TipoCarta?.Trim().ToLowerInvariant() ?? "";
                        bool puedeAtacarEsteObjetivo = !((atacanteTipoNorm == "político" || atacanteTipoNorm == "politico") && objetivoTipoNorm == "apoyo");
                        if(puedeAtacarEsteObjetivo) {
                            posiblesObjetivosCartas.Add(ebcc);
                            if(hayFuerosEnOponente && ebcc.cardData != null && ebcc.cardData.TieneFueros) {
                                objetivosConFueros.Add(ebcc);
                            }
                        }
                    }
                }
            }

            if (hayFuerosEnOponente) {
                if (objetivosConFueros.Count > 0) {
                    objetivoCartaEnemiga = objetivosConFueros[0]; 
                    // Debug.Log($"[IA Attack] '{miAtacante.cardData.NombreCarta}' atacará a '{objetivoCartaEnemiga.cardData.NombreCarta}' (Tiene Fueros).");
                } else {
                    Debug.LogError("IAController: hayFuerosEnOponente es true pero objetivosConFueros está vacía.");
                    continue; 
                }
            } else {
                if (posiblesObjetivosCartas.Count > 0) {
                    objetivoCartaEnemiga = posiblesObjetivosCartas[0];
                    // Debug.Log($"[IA Attack] '{miAtacante.cardData.NombreCarta}' atacará a '{objetivoCartaEnemiga.cardData.NombreCarta}'.");
                } else {
                    // Debug.Log($"[IA Attack] '{miAtacante.cardData.NombreCarta}' atacará al jugador: {jugadorOponente.name}.");
                    miAtacante.AtacarJugador(jugadorOponente); 
                    yield return new WaitForSeconds(tiempoEsperaAtaque); 
                    continue; 
                }
            }
            if (objetivoCartaEnemiga != null) {
                miAtacante.Atacar(objetivoCartaEnemiga); 
                yield return new WaitForSeconds(tiempoEsperaAtaque); 
            }
            yield return new WaitForSeconds(0.2f); 
        }

        // Debug.Log($"[IA - {playerStatsIA.name}] >>> FINALIZANDO TURNO.");
        yield return new WaitForSeconds(tiempoPensarEntreAcciones); 
        GameManager.Instance.FinalizarTurno();
    }
}