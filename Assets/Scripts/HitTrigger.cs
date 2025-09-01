using UnityEngine;

public class HitTrigger : MonoBehaviour
{
    private bool m_HitApplied = false;
    public bool HitApplied => m_HitApplied;

    [SerializeField] private float m_DamageAmount = 5f;

    public void ResetHitApplied()
    {
        m_HitApplied = false;
    }

    //    private void OnCollisionStay(Collision collision)
    //    {
    //        if (m_HitApplied) return;
    //
    //        Damageable damageable = collision.gameObject.GetComponent<Damageable>();
    //        if (damageable == null) return;
    //
    //        damageable.Damage(5, collision.contacts[0].point, -collision.gameObject.transform.forward);
    //        m_HitApplied = true;
    //        gameObject.SetActive(false);
    //    }

    private void OnTriggerStay(Collider other)
    {
        if (m_HitApplied) return;

        Damageable damageable = other.gameObject.GetComponent<Damageable>();
        Debug.Log("Collided with: " + other.gameObject.name + " by " + this.transform.parent.name);
        if (damageable == null) return;

        Debug.Log("Damaging: " + other.gameObject.name + " by " + this.transform.parent.name);
        damageable.Damage(m_DamageAmount, transform.parent, other.ClosestPoint(transform.position), -other.gameObject.transform.forward);
        m_HitApplied = true;
        gameObject.SetActive(false);
    }
}
