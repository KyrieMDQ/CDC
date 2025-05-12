using DG.Tweening;
using UnityEngine;

public class AttackEffects : MonoBehaviour
{
    [Header("References")]
    public ParticleSystem slashEffect;
    public SpriteRenderer hitSprite;
    
    [Header("Settings")]
    public float hitShowDuration = 0.3f;

    public void PlaySlashEffect(Vector3 position, Quaternion rotation) {
        ParticleSystem instance = Instantiate(slashEffect, position, rotation);
        instance.Play();
        Destroy(instance.gameObject, 2f);
    }

    public void PlayHitEffect(Vector3 position) {
        SpriteRenderer hit = Instantiate(hitSprite, position, Quaternion.identity);
        hit.transform.localScale = Vector3.zero;
        
        hit.transform.DOScale(Vector3.one, hitShowDuration * 0.3f)
            .SetEase(Ease.OutBack);
            
        hit.DOFade(0, hitShowDuration)
            .OnComplete(() => Destroy(hit.gameObject));
    }
}