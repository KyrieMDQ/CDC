// ScriptableCard.cs
using UnityEngine;

public enum ActionEffectType
{
    None,
    DamageBothPlayers,
    DamageTargetPlayer,
    HealCasterPlayer,
    HealTargetPlayer,
    DrawCards,
    BlockEnemyPoliticians,
    DrawAndReduceCost
    // Añade aquí más tipos si los necesitas para tu panel y ActionEffectManager
}

[CreateAssetMenu(fileName = "Nueva Carta", menuName = "Cards/Nueva Carta")]
public class ScriptableCard : ScriptableObject
{
    [Header("Identificación")]
    public string IdUnico;
    public string NombreCarta;

    [Header("Estadísticas")]
    public int CostoPoderPolitico;
    public int Influencia;
    public int Resistencia;

    [Header("Características")]
    public string Faccion;
    public string TipoCarta; // "Político", "Acción", "Apoyo", "Evento", "Reacción"
    [TextArea]
    public string Descripcion;
    public string Rareza;

    [Header("Palabras Clave / Estados Iniciales")]
    public bool TieneFueros;
    public bool AccionInmediata;
    public bool TieneArrollar; // Para la mecánica de Arrollar (Trample)

    [Header("Visual")]
    public Sprite Ilustracion;

    [Header("Configuración de Efecto (para Tipos Acción/Evento)")]
    public ActionEffectType effectType = ActionEffectType.None;

    [Tooltip("Cantidad genérica para efectos MUY simples. Priorizar Params específicos.")]
    public int effectAmount = 0; 

    [Header("Parámetros Detallados del Efecto")]
    public int ParamCartasARobar = 0;
    public int ParamDaño = 0; // <--- AÑADIDO/VERIFICADO
    public int ParamCuracion = 0; // <--- AÑADIDO/VERIFICADO
    public int ParamReduccionDeCosto = 0;
    public int ParamDuracionTurnos = 0;
}