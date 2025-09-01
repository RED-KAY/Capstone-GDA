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

    [SerializeField] private ParticleSystem m_BulletHitPrefab;

    [SerializeField] private bool m_ShowHealthbar = true;
    [SerializeField] private Canvas m_HealthBarCanvasPrefab;
    [SerializeField] private DecalProjector bloodDecal;
    
    // private Slider m_HealthBar;
    private Collider m_Collider;
    
    public Action m_OnDeath;

    public Action<Transform> m_OnDamage;

    [SerializeField] private float m_YOffset = 2f; 
    
    private void Start()
    {
        m_MaxHealth = 150;
        m_Health = m_MaxHealth;
        m_Collider = GetComponent<Collider>();

        // Vector3 healthBarPosition = new Vector3(m_Collider.bounds.center.x, m_Collider.bounds.center.y + m_YOffset, m_Collider.bounds.center.z);
        //
        // Canvas healthBar = Instantiate(m_HealthBarCanvasPrefab, this.transform);
        // healthBar.transform.position = healthBarPosition;
        // m_HealthBar = healthBar.GetComponentInChildren<Slider>();
        // m_HealthBar.maxValue = m_MaxHealth;
        // m_HealthBar.value = m_Health;
        //
        // healthBar.gameObject.SetActive(m_ShowHealthbar);
    }

    public bool Damage(float damage)
    {
        if (Dead) return false;
        m_Health -= damage * (1 - m_DamageAbsorption);
        if (bloodDecal)
        {
            float healthPercentage = m_Health / m_MaxHealth;
            bloodDecal.fadeFactor = (1f - healthPercentage);
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

            //TODO: Object pool this!
            ParticleSystem hitEffect = Instantiate(m_BulletHitPrefab, position, Quaternion.LookRotation(hitNormal), this.transform);
            hitEffect.Play();
        }
    }

    private IEnumerator Die()
    {
        m_Health = 0;
        m_OnDeath?.Invoke();
        // m_HealthBar.transform.parent.gameObject.SetActive(false);
        
        yield return new WaitForSeconds(0f);
        
        this.gameObject.SetActive(false);
    }

    private void Heal(float amount)
    {
        if (Dead) return;
        m_Health += amount;
        if(m_Health > m_MaxHealth)
            m_Health = m_MaxHealth;
        
        UpdateUI();
    }

    private void UpdateUI()
    {
        // m_HealthBar.value = m_Health;
    }
}
