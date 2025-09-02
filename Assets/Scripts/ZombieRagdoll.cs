using System.Collections;
using UnityEngine;

public class ZombieRagdoll : MonoBehaviour
{
    [SerializeField] Rigidbody m_HipRb;

    public void AddForce(Vector3 direction, float force)
    {
        m_HipRb.AddForce(direction * force);
        //StartCoroutine(Hide());
    }

    IEnumerator Hide()
    {
        yield return new WaitForSeconds(15f);


        //TODO: Object pooling : [Id: ZombieRagdoll] Return Back.
        PoolRecycle.Recycle(gameObject);
    }
}
