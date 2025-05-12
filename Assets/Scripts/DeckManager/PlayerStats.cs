// PlayerStats.cs
using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class PlayerStats : MonoBehaviour
{
    [Header("Estadísticas Principales")]
    public int vida = 30;
    public int poderActual;
    public int maxPoderEsteTurno = 0;
    public int maxPoderGlobal = 10;

    [Header("Referencias UI")]
    public TextMeshProUGUI vidaUIText;
    public TextMeshProUGUI poderUIText;

    [Header("Efectos de Daño")]
    public GameObject floatingDamageTextPrefab;
    public Transform uiDamageTextSpawnPoint;

    [Header("Componentes del Jugador")]
    public IAController IAController; // Será null para el jugador humano

    [Header("Referencias Asociadas")]
    public DeckManager myDeckManager; // ASIGNAR EN EL INSPECTOR

    // --- PARA MODIFICADORES DE COSTO TEMPORALES ---
    public class CostoModificadoInfo
    {
        public ScriptableCard cartaOriginal;
        public int reduccionOriginal;
        public int turnosRestantes; // Turnos del *dueño* de este PlayerStats
    }
    public List<CostoModificadoInfo> modificadoresDeCostoActivos = new List<CostoModificadoInfo>();
    // --- FIN MODIFICADORES DE COSTO ---

    void Awake()
    {
        ActualizarVidaUI();
        ActualizarPoderUI();
        if (myDeckManager == null)
        {
            Debug.LogError($"PlayerStats ({gameObject.name}): ¡La referencia a 'myDeckManager' no está asignada en el Inspector!");
        }
    }

    public void IniciarPreparativosDeTurno()
    {
        IncrementarMaxPoderTurno(1);
        RestaurarPoderActualAlMaximo();
        // ProcesarFinDeTurnoParaModificadores() se llama desde GameManager ANTES de iniciar el nuevo turno del oponente,
        // o al final del turno de ESTE jugador.
    }

    public void IncrementarMaxPoderTurno(int cantidad = 1)
    {
        maxPoderEsteTurno = Mathf.Min(maxPoderEsteTurno + cantidad, maxPoderGlobal);
    }

    public void RestaurarPoderActualAlMaximo()
    {
        poderActual = maxPoderEsteTurno;
        ActualizarPoderUI();
    }

    public void ActualizarPoderUI()
    {
        if (poderUIText != null)
        {
            poderUIText.text = $"Poder: {poderActual}/{maxPoderEsteTurno}";
        }
    }

    public void ActualizarVidaUI()
    {
        if (vidaUIText != null)
        {
            vidaUIText.text = $"Dinero: {vida}";
        }
    }

    public void RecibirDaño(int cantidad)
    {
        if (cantidad <= 0) return;
        vida -= cantidad;
        ActualizarVidaUI();

        if (UIManager.Instance != null) UIManager.Instance.TriggerDamageSplash(this);
        if (floatingDamageTextPrefab != null)
        {
            GameObject textGO = Instantiate(floatingDamageTextPrefab);
            Transform canvasTransform = FindFirstObjectByType<Canvas>()?.transform;
            if (canvasTransform != null) textGO.transform.SetParent(canvasTransform, false);
            if (uiDamageTextSpawnPoint != null) textGO.transform.position = uiDamageTextSpawnPoint.position;
            else if (canvasTransform != null) textGO.transform.localPosition = Vector3.zero;
            TextMeshProUGUI tmpro = textGO.GetComponent<TextMeshProUGUI>();
            if (tmpro != null) tmpro.text = $"-{cantidad}";
        }
        if (vida <= 0)
        {
            vida = 0;
            ActualizarVidaUI();
            Debug.Log($"PlayerStats ({name}): ¡HA SIDO DERROTADO!");
            if (GameManager.Instance != null) GameManager.Instance.DeclararPerdedor(this);
        }
    }
    
    // --- MÉTODOS PARA MODIFICADORES DE COSTO ---
    public void AplicarModificadorDeCostoTemporalALista(List<ScriptableCard> cartas, int cantidadReduccion, int duracionEnTurnosDUEÑO)
    {
        if (cartas == null || cantidadReduccion <= 0 || duracionEnTurnosDUEÑO <= 0) return;

        foreach (ScriptableCard carta in cartas)
        {
            if (carta == null) continue;
            // Quitar modificador viejo si existe para esta carta, para evitar apilamiento incorrecto o aplicar uno nuevo
            modificadoresDeCostoActivos.RemoveAll(mod => mod.cartaOriginal != null && mod.cartaOriginal.IdUnico == carta.IdUnico); 
            
            modificadoresDeCostoActivos.Add(new CostoModificadoInfo { 
                cartaOriginal = carta, 
                reduccionOriginal = cantidadReduccion, 
                turnosRestantes = duracionEnTurnosDUEÑO // Duración en "turnos de este jugador"
            });
            Debug.Log($"[PlayerStats - {this.name}] Modificador de costo aplicado a '{carta.NombreCarta}': -{cantidadReduccion} por {duracionEnTurnosDUEÑO} turno(s) de este jugador.");
        }
        // Llamar para actualizar visuales de la mano
        if (myDeckManager != null) myDeckManager.ActualizarCostosVisualesEnMano();
    }

    public int GetCostoRealCarta(ScriptableCard carta)
    {
        if (carta == null) return 999; // Un costo alto para cartas nulas para que no se puedan jugar
        int costoActual = carta.CostoPoderPolitico;
        // Busca un modificador activo para esta carta
        CostoModificadoInfo mod = modificadoresDeCostoActivos.FirstOrDefault(m => m.cartaOriginal != null && m.cartaOriginal.IdUnico == carta.IdUnico && m.turnosRestantes > 0);
        
        if (mod != null)
        {
            costoActual = Mathf.Max(0, costoActual - mod.reduccionOriginal); // El costo no puede ser menor que 0
        }
        return costoActual;
    }

    // Llamar desde GameManager al FINAL del turno de ESTE jugador
    public void ProcesarFinDeTurnoParaModificadores() 
    {
        bool algunaModificacionCambio = false;
        // Iterar al revés para poder remover elementos de la lista de forma segura
        for (int i = modificadoresDeCostoActivos.Count - 1; i >= 0; i--)
        {
            modificadoresDeCostoActivos[i].turnosRestantes--; // Decrementar duración
            if (modificadoresDeCostoActivos[i].turnosRestantes <= 0)
            {
                if(modificadoresDeCostoActivos[i].cartaOriginal != null)
                    Debug.Log($"[PlayerStats - {this.name}] Expiró modificador de costo para '{modificadoresDeCostoActivos[i].cartaOriginal.NombreCarta}'.");
                else
                    Debug.LogWarning($"[PlayerStats - {this.name}] Expiró modificador de costo para una carta que ya no tiene referencia válida.");
                modificadoresDeCostoActivos.RemoveAt(i);
                algunaModificacionCambio = true;
            }
        }
        // Si algún modificador expiró, actualizar los visuales de la mano
        if (algunaModificacionCambio && myDeckManager != null)
        {
            myDeckManager.ActualizarCostosVisualesEnMano();
        }
    }

    // --- MÉTODOS DE PAGO MODIFICADOS PARA USAR ScriptableCard ---
    public bool PuedePagar(ScriptableCard carta) 
    {
        if (carta == null) {
            Debug.LogError($"[PlayerStats - {this.name}] PuedePagar fue llamado con carta null.");
            return false;
        }
        return poderActual >= GetCostoRealCarta(carta);
    }

    public void Pagar(ScriptableCard carta) 
    {
        if (carta == null) {
            Debug.LogError($"[PlayerStats - {this.name}] Pagar fue llamado con carta null.");
            return;
        }
        int costoReal = GetCostoRealCarta(carta);

        // La validación de si se puede pagar ya debería haberse hecho ANTES de llamar a Pagar.
        // Este es un chequeo de seguridad extra.
        if (poderActual >= costoReal)
        {
            poderActual -= costoReal;
            ActualizarPoderUI();
            Debug.Log($"[PlayerStats - {this.name}] Pagó {costoReal} por '{carta.NombreCarta}'. Poder restante: {poderActual}. (Costo original: {carta.CostoPoderPolitico})");
        }
        else
        {
            // Esto no debería pasar si PuedePagar se llamó antes.
            Debug.LogWarning($"PlayerStats ({name}): Intento de pagar {costoReal} por '{carta.NombreCarta}', pero no puede (Poder: {poderActual}).");
        }
    }
}