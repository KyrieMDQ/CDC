using UnityEngine;
using DG.Tweening;
using System;
using System.Collections;
using TMPro;

public class CardAnimator : MonoBehaviour
{
    private RectTransform _rectTransform;
    private CanvasGroup _canvasGroup;
    private BoardCardController _bcc; 

    [Header("Animation Settings")]
    public AttackEffects attackEffects;
    public float hoverPulseScale = 1.06f;
    public float hoverPulseDuration = 0.75f;
    public float deathAnimationDuration = 0.55f;

    private Tween handPulseTween;    
    private Tween boardPulseTween;   
    private Vector3 _boardRestingScale = Vector3.one * 0.4f;
    private Sequence _currentSequence;

    void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null) _canvasGroup = gameObject.AddComponent<CanvasGroup>();
        _bcc = GetComponent<BoardCardController>();
    }

    void OnDestroy()
    {
        StopAllAnimations();
    }

    public void StopAllAnimations()
    {
        if (handPulseTween != null && handPulseTween.IsActive()) handPulseTween.Kill();
        if (boardPulseTween != null && boardPulseTween.IsActive()) boardPulseTween.Kill();
        if (_currentSequence != null && _currentSequence.IsActive()) _currentSequence.Kill();
        
        _rectTransform.DOKill();
    }

    public void SetBoardRestingScale(Vector3 scale)
    {
        if (scale == Vector3.zero) {
            _boardRestingScale = transform.localScale; 
            if (_boardRestingScale == Vector3.zero) _boardRestingScale = Vector3.one * 0.4f; 
        } else {
            _boardRestingScale = scale;
        }
        
        if ((boardPulseTween == null || !boardPulseTween.IsActive()) && _rectTransform != null) {
            if (_rectTransform.localScale != _boardRestingScale) {
                _rectTransform.DOKill(); 
                _rectTransform.localScale = _boardRestingScale;
            }
        }
    }

    public void AnimateBoardCardHoverPulse(bool startPulse)
    {
        if (_rectTransform == null) return;
        if (boardPulseTween != null && boardPulseTween.IsActive()) boardPulseTween.Kill(true); 

        if (startPulse)
        {
            if (_boardRestingScale == Vector3.zero)
            { 
                _boardRestingScale = transform.localScale;
                if (_boardRestingScale == Vector3.zero) _boardRestingScale = Vector3.one * 0.4f; 
            }
            
            boardPulseTween = _rectTransform.DOScale(_boardRestingScale * hoverPulseScale, hoverPulseDuration / 2)
                .SetEase(Ease.OutSine).SetLoops(-1, LoopType.Yoyo);
            boardPulseTween.Play();
        }
        else
        {
            if (_boardRestingScale == Vector3.zero) _boardRestingScale = Vector3.one * 0.4f;
            _rectTransform.DOScale(_boardRestingScale, 0.15f).SetEase(Ease.OutQuad);
        }
    }

    public void AnimateEntryToBoard(Vector3 finalLocalPosition, Vector3 finalScale, Quaternion finalRotation, float duration, Action onCompleteCallback)
    {
        if (_rectTransform == null) { onCompleteCallback?.Invoke(); return; }
        _rectTransform.DOKill(); 

        _rectTransform.localPosition = finalLocalPosition + new Vector3(UnityEngine.Random.Range(-10f, 10f), 120f, 0);
        _rectTransform.localRotation = finalRotation * Quaternion.Euler(UnityEngine.Random.Range(-15f, 15f), 0, UnityEngine.Random.Range(-10f, 10f));
        _rectTransform.localScale = finalScale * 0.5f;
        if (_canvasGroup != null) _canvasGroup.alpha = 0f;

        _currentSequence = DOTween.Sequence();
        _currentSequence.Append(_rectTransform.DOLocalMove(finalLocalPosition, duration).SetEase(Ease.OutCirc))
                     .Join(_rectTransform.DOScale(finalScale, duration * 0.9f).SetEase(Ease.OutBack))
                     .Join(_rectTransform.DOLocalRotateQuaternion(finalRotation, duration).SetEase(Ease.OutQuad));
        
        if (_canvasGroup != null) _currentSequence.Join(_canvasGroup.DOFade(1f, duration * 0.6f).SetEase(Ease.InQuad));
        
        _currentSequence.Append(_rectTransform.DOPunchPosition(new Vector3(0, -UnityEngine.Random.Range(3f,8f), 0), 
                                  duration * 0.5f, vibrato: 3, elasticity: 0.4f).SetDelay(0.05f));
        
        if (onCompleteCallback != null) {
            _currentSequence.OnComplete(() => onCompleteCallback.Invoke());
        }
        _currentSequence.Play();
    }

    public void AnimateToSlot(Vector3 targetLocalPosition, Vector3 targetScale, Quaternion targetRotation, float duration)
    {
        if (_rectTransform == null) return;
        _rectTransform.DOKill();
        SetBoardRestingScale(targetScale); 

        _rectTransform.DOLocalMove(targetLocalPosition, duration).SetEase(Ease.InOutSine);
        _rectTransform.DOScale(targetScale, duration).SetEase(Ease.InOutSine);
        _rectTransform.DOLocalRotateQuaternion(targetRotation, duration).SetEase(Ease.InOutSine);
    }

    public void PlayAttackAnimation(BoardCardController targetBCC, Vector3 originalAttackerPos, Vector3 originalAttackerScale, Action onImpactCallback)
    {
        if (_rectTransform == null || targetBCC == null || (_bcc != null && !_bcc.gameObject.activeInHierarchy)) 
        { 
            onImpactCallback?.Invoke(); 
            return; 
        }
        
        _rectTransform.DOKill();
        int originalSiblingIndex = _rectTransform.GetSiblingIndex();
        _rectTransform.SetAsLastSibling();
        
        Vector3 targetPosition = targetBCC.transform.localPosition;
        float lungeToTargetDuration = 0.22f;
        float impactHoldDuration = 0.1f;
        float returnDuration = 0.3f;
        
        _currentSequence = DOTween.Sequence();
        _currentSequence.Append(_rectTransform.DOLocalMove(targetPosition, lungeToTargetDuration).SetEase(Ease.OutCubic))
                     .Join(_rectTransform.DOScale(originalAttackerScale * 1.1f, lungeToTargetDuration * 0.9f).SetEase(Ease.OutSine));
        
        if (onImpactCallback != null)
        {
            _currentSequence.AppendCallback(() => 
            {
                if (targetBCC != null && targetBCC.gameObject.activeInHierarchy) 
                {
                    if (attackEffects != null) attackEffects.PlayHitEffect(targetBCC.transform.position);
                    targetBCC.transform.DOShakePosition(0.3f, strength: 10f, vibrato: 20, 
                                                      randomnessMode: ShakeRandomnessMode.Harmonic, fadeOut: true);
                }
                onImpactCallback.Invoke();
            });
        }
        
        _currentSequence.AppendInterval(impactHoldDuration)
                     .Append(_rectTransform.DOLocalMove(originalAttackerPos, returnDuration).SetEase(Ease.InCubic))
                     .Join(_rectTransform.DOScale(originalAttackerScale, returnDuration).SetEase(Ease.InSine))
                     .OnComplete(() => 
                     { 
                         if (_rectTransform != null && _rectTransform.parent != null) 
                         { 
                             try { _rectTransform.SetSiblingIndex(originalSiblingIndex); } catch {} 
                         } 
                     })
                     .Play();
    }

    public void PlayAttackPlayerAnimation(PlayerStats targetPlayer, Vector3 originalAttackerPos, Vector3 originalAttackerScale, Action onImpactCallback)
    {
        if (_rectTransform == null || targetPlayer == null || (_bcc != null && !_bcc.gameObject.activeInHierarchy)) 
        { 
            onImpactCallback?.Invoke(); 
            return; 
        }
        
        _rectTransform.DOKill();
        int originalSiblingIndex = _rectTransform.GetSiblingIndex();
        _rectTransform.SetAsLastSibling();
        
        Vector3 lungePos = originalAttackerPos + _rectTransform.up * 100f; 
        
        _currentSequence = DOTween.Sequence();
        _currentSequence.Append(_rectTransform.DOLocalMove(lungePos, 0.3f).SetEase(Ease.OutCubic))
                     .Join(_rectTransform.DOScale(originalAttackerScale * 1.2f, 0.3f).SetEase(Ease.OutCubic))
                     .Join(_rectTransform.DOPunchRotation(new Vector3(0,0,UnityEngine.Random.Range(-12f,12f)), 0.36f, 7, 0.4f));
        
        if (onImpactCallback != null) 
        { 
            _currentSequence.AppendCallback(() => onImpactCallback.Invoke()); 
        }
        
        _currentSequence.AppendInterval(0.1f)
                     .Append(_rectTransform.DOLocalMove(originalAttackerPos, 0.4f).SetEase(Ease.InCubic))
                     .Join(_rectTransform.DOScale(originalAttackerScale, 0.4f).SetEase(Ease.InSine))
                     .OnComplete(() => 
                     {
                         if(_rectTransform != null && _rectTransform.parent != null) 
                         { 
                             try { _rectTransform.SetSiblingIndex(originalSiblingIndex); } catch {} 
                         } 
                     })
                     .Play();
    }

    public void PlayDamageReceivedAnimation(TextMeshProUGUI resistenciaTextToAnimate)
    {
        if (_rectTransform == null) return;
        _rectTransform.DOKill(true); 
        _rectTransform.DOShakePosition(0.35f, strength: new Vector3(10, 5, 0), 
                                     vibrato: 20, randomnessMode: ShakeRandomnessMode.Harmonic, fadeOut: true);
        
        if (resistenciaTextToAnimate != null)
        {
            resistenciaTextToAnimate.transform.DOPunchScale(Vector3.one * 0.25f, 0.3f, vibrato: 6, elasticity: 0.5f);
        }
    }

    public IEnumerator PlayDeathAnimation()
    {
        if (_rectTransform == null || _canvasGroup == null) yield break; 
        
        if (_canvasGroup != null) _canvasGroup.blocksRaycasts = false; 
        _rectTransform.DOKill(); 
        
        _currentSequence = DOTween.Sequence();
        _currentSequence.Append(_rectTransform.DOShakeRotation(deathAnimationDuration, new Vector3(0,0,35), 12, 60, false).SetEase(Ease.OutQuad))
                     .Join(_rectTransform.DOScale(Vector3.one * 0.2f, deathAnimationDuration).SetEase(Ease.InBack))
                     .Join(_canvasGroup.DOFade(0f, deathAnimationDuration * 0.9f).SetEase(Ease.InCirc).SetDelay(0.05f)); 
        
        yield return _currentSequence.WaitForCompletion();
    }
}