using UnityEngine;
using TMPro;
using System.Collections;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Mensajes de Estado")]
    public TextMeshProUGUI statusMessageText;
    public float statusMessageDuration = 2.5f;
    private Coroutine activeMessageCoroutine;
    private CanvasGroup statusMessageCG; // Cache CanvasGroup para mensajes

    [Header("Efectos de Daño al Jugador")]
    public CanvasGroup damageSplashP1_CanvasGroup;
    public CanvasGroup damageSplashP2_CanvasGroup;
    public float damageSplashFadeInDuration = 0.1f;
    public float damageSplashHoldDuration = 0.3f;
    public float damageSplashFadeOutDuration = 0.5f;
    private Coroutine p1SplashCoroutine;
    private Coroutine p2SplashCoroutine;

    [Header("Contadores de Cripta")]
    public TextMeshProUGUI discardCountP1_Text; // Asignar en Inspector
    public TextMeshProUGUI discardCountP2_Text; // Asignar en Inspector

    private void Awake()
    {
        if (Instance == null) { Instance = this; }
        else { Destroy(gameObject); return; }

        // Inicializar Mensajes de Estado
        if (statusMessageText != null) {
            statusMessageCG = statusMessageText.GetComponent<CanvasGroup>();
            if (statusMessageCG == null) statusMessageCG = statusMessageText.gameObject.AddComponent<CanvasGroup>();
            statusMessageCG.alpha = 0f;
            statusMessageCG.blocksRaycasts = false;
            statusMessageText.gameObject.SetActive(true);
        } else { Debug.LogError("UIManager: statusMessageText no asignado."); }

        // Inicializar Splashes de Daño
        InitializeSplash(damageSplashP1_CanvasGroup, "damageSplashP1_CanvasGroup");
        InitializeSplash(damageSplashP2_CanvasGroup, "damageSplashP2_CanvasGroup");

        // Inicializar Contadores de Cripta
        InitializeDiscardCounter(discardCountP1_Text, "discardCountP1_Text");
        InitializeDiscardCounter(discardCountP2_Text, "discardCountP2_Text");
    }

     private void InitializeSplash(CanvasGroup splashCG, string debugName)
    {
        if (splashCG != null) {
            splashCG.alpha = 0;
            splashCG.blocksRaycasts = false;
            splashCG.gameObject.SetActive(true);
        } else { Debug.LogError($"UIManager: {debugName} no asignado."); }
    }

     private void InitializeDiscardCounter(TextMeshProUGUI counterText, string debugName)
    {
        if (counterText != null) {
            counterText.text = "Cripta: 0";
            counterText.gameObject.SetActive(true);
        } else { Debug.LogWarning($"UIManager: {debugName} no asignado."); } // Warning en lugar de Error
    }


    public void ShowStatusMessage(string message) { ShowStatusMessage(message, statusMessageDuration); }
    public void ShowStatusMessage(string message, float duration) {
        if (statusMessageText == null || statusMessageCG == null) { Debug.LogError("UIManager: statusMessageText o su CanvasGroup no están listos."); return; }
        if (activeMessageCoroutine != null) { StopCoroutine(activeMessageCoroutine); LeanTween.cancel(statusMessageCG.gameObject); }
        activeMessageCoroutine = StartCoroutine(ShowMessageCoroutine(message, duration, statusMessageCG));
    }
    private IEnumerator ShowMessageCoroutine(string message, float duration, CanvasGroup cg) {
        statusMessageText.text = message;
        cg.alpha = 0f;
        cg.blocksRaycasts = true; // Bloquear si el mensaje está encima de algo clickeable
        LeanTween.alphaCanvas(cg, 1f, 0.2f);
        yield return new WaitForSeconds(duration);
        LeanTween.alphaCanvas(cg, 0f, 0.3f).setOnComplete(() => { if (cg != null) cg.blocksRaycasts = false; });
        activeMessageCoroutine = null;
    }

    public void TriggerDamageSplash(PlayerStats targetPlayer) {
        if (targetPlayer == null || GameManager.Instance == null) return;
        CanvasGroup targetSplashGroup = null; bool isP1 = false;
        if (targetPlayer == GameManager.Instance.jugador1) { targetSplashGroup = damageSplashP1_CanvasGroup; isP1 = true; if (p1SplashCoroutine != null) { StopCoroutine(p1SplashCoroutine); LeanTween.cancel(targetSplashGroup.gameObject); } }
        else if (targetPlayer == GameManager.Instance.jugador2) { targetSplashGroup = damageSplashP2_CanvasGroup; if (p2SplashCoroutine != null) { StopCoroutine(p2SplashCoroutine); LeanTween.cancel(targetSplashGroup.gameObject); } }
        if (targetSplashGroup != null) { if (isP1) p1SplashCoroutine = StartCoroutine(DamageSplashCoroutine(targetSplashGroup)); else p2SplashCoroutine = StartCoroutine(DamageSplashCoroutine(targetSplashGroup)); }
    }
    private IEnumerator DamageSplashCoroutine(CanvasGroup splashCanvasGroup) {
        if(!splashCanvasGroup.gameObject.activeSelf) splashCanvasGroup.gameObject.SetActive(true);
        splashCanvasGroup.alpha = 0f; splashCanvasGroup.blocksRaycasts = true;
        LeanTween.alphaCanvas(splashCanvasGroup, 1f, damageSplashFadeInDuration).setEaseOutQuad();
        yield return new WaitForSeconds(damageSplashFadeInDuration + damageSplashHoldDuration);
        LeanTween.alphaCanvas(splashCanvasGroup, 0f, damageSplashFadeOutDuration).setEaseInQuad().setOnComplete(() => { if (splashCanvasGroup != null) splashCanvasGroup.blocksRaycasts = false; });
    }

    // --- NUEVO MÉTODO para actualizar UI de Cripta ---
    public void UpdateDiscardCountUI(DeckManager deckManagerOwner, int count)
    {
        if (GameManager.Instance == null || deckManagerOwner == null) return;

        TextMeshProUGUI targetText = null;
        if (deckManagerOwner == GameManager.Instance.deckManagerJugador1) targetText = discardCountP1_Text;
        else if (deckManagerOwner == GameManager.Instance.deckManagerJugador2) targetText = discardCountP2_Text;

        if (targetText != null) { targetText.text = $"Cripta: {count}"; }
        // else { Debug.LogWarning($"UIManager: No se encontró el texto de Cripta para el DeckManager de {deckManagerOwner.gameObject.name}"); }
    }
}