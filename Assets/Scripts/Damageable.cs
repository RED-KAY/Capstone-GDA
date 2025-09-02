using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

[RequireComponent(typeof(Collider))]
public class Damageable : MonoBehaviour
{
    [SerializeField] private float m_MaxHealth;
    [SerializeField] private float m_Health;

    [Range(0, 0.9f)][SerializeField] private float m_DamageAbsorption;
    public bool Dead => m_Health <= 0;

    [Header("Hit FX")]
    [Tooltip("Factory prefab index for hit FX (FxFactory).")]
    [SerializeField] private int m_BulletHitFxId = 0;
    [Tooltip("Fallback prefab if FxFactory is not registered in Services.")]
    [SerializeField] private ParticleSystem m_BulletHitPrefab;

    [SerializeField] private bool m_ShowHealthbar = true;
    [SerializeField] private Canvas m_HealthBarCanvasPrefab;
    [SerializeField] private DecalProjector bloodDecal;

    private Collider m_Collider;

    public Action m_OnDeath;
    public Action<Transform> m_OnDamage;

    [SerializeField] private float m_YOffset = 2f;

    private void Start()
    {
        m_Health = m_MaxHealth;
        m_Collider = GetComponent<Collider>();
        // healthbar setup removed for brevity (same as your original commented code)
    }

    private void OnEnable()
    {
        m_Health = m_MaxHealth;
        if (bloodDecal)
        {
            float hpPct = m_Health / m_MaxHealth;
            bloodDecal.fadeFactor = 1f - hpPct;
        }
    }

    public bool Damage(float damage)
    {
        if (Dead) return false;

        m_Health -= damage * (1 - m_DamageAbsorption);

        if (bloodDecal)
        {
            float hpPct = m_Health / m_MaxHealth;
            bloodDecal.fadeFactor = 1f - hpPct;
        }

        UpdateUI();

        if (m_Health <= 0)
        {
            StartCoroutine(Die());
            return true;
        }
        return true;
    }

    public void Damage(float damage, Transform source, Vector3 position, Vector3 hitNormal)
    {
        if (Damage(damage))
        {
            m_OnDamage?.Invoke(source);

            // ---- Spawn hit FX via Services + FxFactory (pooled) ----
            var fxFac = Services.Get<FxFactory>();
            if (fxFac)
            {
                var fx = fxFac.Get(m_BulletHitFxId);
                if (fx)
                {
                    fx.transform.SetPositionAndRotation(position, Quaternion.LookRotation(hitNormal));
                    fx.gameObject.SetActive(true);
                    fx.Play(true);
                }
            }
        }
    }

    private IEnumerator Die()
    {
        m_Health = 0;
        m_OnDeath?.Invoke();
        yield return null;
        PoolRecycle.Recycle(gameObject);
        //gameObject.SetActive(false);
    }

    private void Heal(float amount)
    {
        if (Dead) return;
        m_Health += amount;
        if (m_Health > m_MaxHealth) m_Health = m_MaxHealth;
        UpdateUI();
    }

    private void UpdateUI()
    {
        // update healthbar if you re-enable it
    }
}
