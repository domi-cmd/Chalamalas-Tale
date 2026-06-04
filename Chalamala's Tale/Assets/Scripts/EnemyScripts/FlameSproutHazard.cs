using UnityEngine;

/// Ground flame hazard: stays inactive for a warning period, then remains dangerous
/// for its active duration. It can damage the player only once.

public class FlameSproutHazard : MonoBehaviour
{
    [Header("Timing")]
    [SerializeField] private float warningDelaySeconds = 2f;
    [SerializeField] private float destroyAfterSeconds = 2.5f;

    [Header("Damage")]
    [SerializeField] private float damageRadius = 1.5f;
    [SerializeField] private float damageAmount = 1f;
    [SerializeField] private LayerMask playerLayerMask;

    [Header("Visuals")]
    [Tooltip("Sprite shown during the 2-second warning phase.")]
    [SerializeField] private Sprite warningSprite;
    [Tooltip("Sprite shown when the flame erupts.")]
    [SerializeField] private Sprite eruptionSprite;
    [Tooltip("If true, disables renderers during the warning period, then enables them on eruption.")]
    [SerializeField] private bool hideDuringWarning = false;
    [Tooltip("If true, pauses particles during warning and resumes on eruption.")]
    [SerializeField] private bool pauseParticlesDuringWarning = false;

    private bool hasErupted;
    private bool hasDamagedPlayer;

    private SpriteRenderer cachedSpriteRenderer;
    private Collider2D[] cachedColliders;
    private Renderer[] cachedRenderers;
    private ParticleSystem[] cachedParticleSystems;

    public void Initialize(float warningDelay, float radius, float damage, float lifetimeSeconds, LayerMask playerMask)
    {
        warningDelaySeconds = Mathf.Max(0f, warningDelay);
        damageRadius = Mathf.Max(0f, radius);
        damageAmount = Mathf.Max(0f, damage);
        destroyAfterSeconds = Mathf.Max(0.01f, lifetimeSeconds);
        playerLayerMask = playerMask;

        StopAllCoroutines();
        if (isActiveAndEnabled)
        {
            StartCoroutine(EruptSequence());
        }
    }

    private void Awake()
    {
        cachedSpriteRenderer = GetComponentInChildren<SpriteRenderer>();
        cachedColliders = GetComponentsInChildren<Collider2D>(includeInactive: true);
        cachedRenderers = GetComponentsInChildren<Renderer>(includeInactive: true);
        cachedParticleSystems = GetComponentsInChildren<ParticleSystem>(includeInactive: true);

        SetWarningState();
    }

    private void OnEnable()
    {
        StartCoroutine(EruptSequence());
    }

    private void SetWarningState()
    {
        hasErupted = false;
        hasDamagedPlayer = false;

        if (cachedColliders != null)
        {
            for (int i = 0; i < cachedColliders.Length; i++)
            {
                if (cachedColliders[i] != null)
                {
                    cachedColliders[i].enabled = false;
                }
            }
        }

        if (cachedSpriteRenderer != null && warningSprite != null)
        {
            cachedSpriteRenderer.sprite = warningSprite;
        }

        if (hideDuringWarning && cachedRenderers != null)
        {
            for (int i = 0; i < cachedRenderers.Length; i++)
            {
                if (cachedRenderers[i] != null)
                {
                    cachedRenderers[i].enabled = false;
                }
            }
        }

        if (pauseParticlesDuringWarning && cachedParticleSystems != null)
        {
            for (int i = 0; i < cachedParticleSystems.Length; i++)
            {
                if (cachedParticleSystems[i] != null)
                {
                    cachedParticleSystems[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                }
            }
        }
    }

    private void SetEruptState()
    {
        // Colliders are intentionally left disabled — damage is handled via OverlapCircleAll.

        if (cachedSpriteRenderer != null && eruptionSprite != null)
        {
            cachedSpriteRenderer.sprite = eruptionSprite;
        }

        if (hideDuringWarning && cachedRenderers != null)
        {
            for (int i = 0; i < cachedRenderers.Length; i++)
            {
                if (cachedRenderers[i] != null)
                {
                    cachedRenderers[i].enabled = true;
                }
            }
        }

        if (pauseParticlesDuringWarning && cachedParticleSystems != null)
        {
            for (int i = 0; i < cachedParticleSystems.Length; i++)
            {
                if (cachedParticleSystems[i] != null)
                {
                    cachedParticleSystems[i].Play(true);
                }
            }
        }
    }

    private System.Collections.IEnumerator EruptSequence()
    {
        SetWarningState();
        yield return new WaitForSeconds(Mathf.Max(0f, warningDelaySeconds));

        if (hasErupted)
        {
            yield break;
        }

        hasErupted = true;
        SetEruptState();

        float activeEndTime = Time.time + Mathf.Max(0.01f, destroyAfterSeconds);
        while (Time.time < activeEndTime)
        {
            if (!hasDamagedPlayer)
            {
                TryDealDamageOnce();
            }

            yield return null;
        }

        Destroy(gameObject);
    }

    private void TryDealDamageOnce()
    {
        if (hasDamagedPlayer || damageAmount <= 0f || damageRadius <= 0f)
        {
            return;
        }

        Collider2D[] hits = playerLayerMask.value != 0
            ? Physics2D.OverlapCircleAll(transform.position, damageRadius, playerLayerMask)
            : Physics2D.OverlapCircleAll(transform.position, damageRadius);

        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i] == null) continue;

            PlayerHealth ph = hits[i].GetComponentInParent<PlayerHealth>();
            if (ph != null)
            {
                ph.TakeDamage(damageAmount);
                hasDamagedPlayer = true;
                break;
            }
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.5f, 0.1f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, damageRadius);
    }
#endif
}
