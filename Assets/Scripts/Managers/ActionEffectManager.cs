using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;

public class ActionEffectManager : MonoBehaviour
{
    private static ActionEffectManager _instance;
    public static ActionEffectManager Instance {
        get {
            if (_instance == null) {
                _instance = FindObjectOfType<ActionEffectManager>();
                if (_instance == null) {
                    GameObject singleton = new GameObject("ActionEffectManager");
                    _instance = singleton.AddComponent<ActionEffectManager>();
                }
            }
            return _instance;
        }
    }

    private void Awake() {
        if (_instance != null && _instance != this) {
            Destroy(gameObject);
        } else {
            _instance = this;
        }
    }

    public void ExecuteEffect(ScriptableCard cardWithEffect, PlayerStats playerCaster, PlayerStats playerTarget, 
                            BoardPlayZone casterBoardZone, BoardPlayZone targetBoardZone)
    {
        if (cardWithEffect == null || playerCaster == null || playerTarget == null) {
            Debug.LogError("Parámetros inválidos en ExecuteEffect");
            return;
        }

        string tipoCarta = cardWithEffect.TipoCarta?.Trim().ToLowerInvariant() ?? "";
        bool isEffectCard = tipoCarta == "accion" || tipoCarta == "acción" || tipoCarta == "evento";

        if (!isEffectCard) return;

        Debug.Log($"Ejecutando efecto: {cardWithEffect.effectType} de {cardWithEffect.NombreCarta}");

        UIManager.Instance?.ShowStatusMessage($"{playerCaster.name} usó {cardWithEffect.NombreCarta}");

        switch (cardWithEffect.effectType)
        {
            case ActionEffectType.DamageBothPlayers:
                ApplyDamageBothPlayers(cardWithEffect, playerCaster, playerTarget);
                break;

            case ActionEffectType.DamageTargetPlayer:
                ApplyDamageToTarget(cardWithEffect, playerTarget);
                break;

            case ActionEffectType.HealCasterPlayer:
                ApplyHealToCaster(cardWithEffect, playerCaster);
                break;

            case ActionEffectType.HealTargetPlayer:
                ApplyHealToTarget(cardWithEffect, playerTarget);
                break;

            case ActionEffectType.DrawCards:
                DrawCards(cardWithEffect, playerCaster);
                break;

            case ActionEffectType.DrawAndReduceCost:
                DrawAndReduceCost(cardWithEffect, playerCaster);
                break;

            case ActionEffectType.BlockEnemyPoliticians:
                BlockEnemyPoliticians(cardWithEffect, playerTarget, targetBoardZone);
                break;

            case ActionEffectType.None:
                Debug.Log($"Carta {cardWithEffect.NombreCarta} sin efecto");
                break;

            default:
                Debug.LogWarning($"Tipo de efecto no implementado: {cardWithEffect.effectType}");
                break;
        }
    }

    private void ApplyDamageBothPlayers(ScriptableCard card, PlayerStats caster, PlayerStats target) {
        int damage = card.ParamDaño > 0 ? card.ParamDaño : card.effectAmount;
        if (damage > 0) {
            caster.RecibirDaño(damage);
            target.RecibirDaño(damage);
        }
    }

    private void ApplyDamageToTarget(ScriptableCard card, PlayerStats target) {
        int damage = card.ParamDaño > 0 ? card.ParamDaño : card.effectAmount;
        if (damage > 0) target.RecibirDaño(damage);
    }

    private void ApplyHealToCaster(ScriptableCard card, PlayerStats caster) {
        int heal = card.ParamCuracion > 0 ? card.ParamCuracion : card.effectAmount;
        if (heal > 0) Debug.LogWarning("Curación no implementada aún");
    }

    private void ApplyHealToTarget(ScriptableCard card, PlayerStats target) {
        int heal = card.ParamCuracion > 0 ? card.ParamCuracion : card.effectAmount;
        if (heal > 0) Debug.LogWarning("Curación no implementada aún");
    }

    private void DrawCards(ScriptableCard card, PlayerStats caster) {
        int cardsToDraw = card.ParamCartasARobar > 0 ? card.ParamCartasARobar : card.effectAmount;
        if (cardsToDraw > 0 && caster.myDeckManager != null) {
            for (int i = 0; i < cardsToDraw; i++) {
                caster.myDeckManager.StartCoroutine(caster.myDeckManager.RobarCartaVisual());
            }
        }
    }

    private void DrawAndReduceCost(ScriptableCard card, PlayerStats caster) {
        if (card.ParamCartasARobar > 0 && caster.myDeckManager != null) {
            StartCoroutine(RobarYReducirCosto(caster, card.ParamCartasARobar, 
                         card.ParamReduccionDeCosto, card.ParamDuracionTurnos));
        }
    }

    private void BlockEnemyPoliticians(ScriptableCard card, PlayerStats target, BoardPlayZone targetZone) {
        if (targetZone != null) {
            StartCoroutine(BlockPoliticiansEffect(target, targetZone, card.ParamDuracionTurnos));
        }
    }

private IEnumerator BlockPoliticiansEffect(PlayerStats enemyPlayer, BoardPlayZone enemyZone, int durationTurns)
{
    if (enemyZone == null || enemyPlayer == null)
    {
        Debug.LogError("BlockPoliticiansEffect: Parámetros inválidos");
        yield break;
    }

    // 1. Bloquear políticos existentes
    List<BoardCardController> blockedPoliticians = new List<BoardCardController>();
    
    var cartasEnZona = enemyZone.CartasEnZona();
    if (cartasEnZona == null || cartasEnZona.Count == 0)
    {
        Debug.LogWarning($"No hay cartas en la zona del oponente: {enemyZone.name}");
        yield break;
    }

    Debug.Log($"Buscando políticos en {cartasEnZona.Count} cartas...");
    
    foreach (GameObject cardObj in cartasEnZona)
    {
        if (cardObj == null) continue;
        
        BoardCardController bcc = cardObj.GetComponent<BoardCardController>();
        if (bcc == null || bcc.cardData == null) continue;

        // Comparación directa sin normalización
        string tipoCarta = bcc.cardData.TipoCarta?.Trim() ?? "";
        
        // Verificación exacta del tipo "Politico" (sin acento)
        bool esPolitico = tipoCarta.Equals("Politico", StringComparison.OrdinalIgnoreCase);
        
        Debug.Log($"Revisando carta: {bcc.cardData.NombreCarta} - Tipo: '{tipoCarta}' - Es político: {esPolitico}");

        if (esPolitico && bcc.resistencia > 0 && !bcc.isDying)
        {
            blockedPoliticians.Add(bcc);
            bcc.SetCanAttack(false);
            Debug.Log($"Político bloqueado: {bcc.cardData.NombreCarta} (Tipo: '{tipoCarta}')");
        }
    }

    if (blockedPoliticians.Count == 0)
    {
        Debug.LogWarning("No se encontraron cartas de tipo 'Politico'. Cartas revisadas:");
        foreach (GameObject cardObj in cartasEnZona)
        {
            if (cardObj == null) continue;
            BoardCardController bcc = cardObj.GetComponent<BoardCardController>();
            if (bcc != null && bcc.cardData != null)
            {
                Debug.LogWarning($"- {bcc.cardData.NombreCarta} (Tipo: '{bcc.cardData.TipoCarta?.Trim()}', Resistencia: {bcc.resistencia}, Dying: {bcc.isDying})");
            }
        }
        yield break;
    
    }

    // 2. Esperar turno del oponente
    int remainingTurns = durationTurns;
    while (remainingTurns > 0)
    {
        yield return new WaitUntil(() => 
            GameManager.Instance != null && 
            GameManager.Instance.GetCurrentPlayer() == enemyPlayer);
        
        Debug.Log($"Turno de bloqueo activo ({remainingTurns} restantes) para {enemyPlayer.name}");
        
        yield return new WaitUntil(() => 
            GameManager.Instance != null && 
            GameManager.Instance.GetCurrentPlayer() != enemyPlayer);
        
        remainingTurns--;
    }

    // 3. Restaurar ataques
    foreach (var politician in blockedPoliticians)
    {
        if (politician != null && politician.gameObject != null)
        {
            politician.SetCanAttack(true);
            Debug.Log($"Ataque restaurado para {politician.cardData?.NombreCarta ?? "POLÍTICO"}");
        }
    }
}

    private IEnumerator RobarYReducirCosto(PlayerStats caster, int drawAmount, int costReduction, int duration) {
        if (caster.myDeckManager == null) yield break;

        HashSet<string> initialHandIds = new HashSet<string>(caster.myDeckManager.playerHand.Select(c => c.IdUnico));
        int drawnCards = 0;

        // Robar cartas
        for (int i = 0; i < drawAmount; i++) {
            if (caster.myDeckManager.mazo.Count == 0) break;
            yield return caster.myDeckManager.StartCoroutine(caster.myDeckManager.RobarCartaVisual());
            drawnCards++;
        }
        yield return new WaitForSeconds(0.1f);

        // Identificar cartas robadas
        List<ScriptableCard> newCards = caster.myDeckManager.playerHand
            .Where(c => !initialHandIds.Contains(c.IdUnico))
            .ToList();

        // Aplicar reducción de costo
        if (newCards.Count > 0 && caster != null) {
            caster.AplicarModificadorDeCostoTemporalALista(newCards, costReduction, duration);
        }
    }
}