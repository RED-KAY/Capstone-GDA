using System.Collections;
using UnityEngine;

public class RagdollAutoDestroy : MonoBehaviour
{
    public float life = 5f;
    void OnEnable() { StopAllCoroutines(); StartCoroutine(Co()); }
    IEnumerator Co() { yield return new WaitForSeconds(life);  gameObject.SetActive(false);  }
}
