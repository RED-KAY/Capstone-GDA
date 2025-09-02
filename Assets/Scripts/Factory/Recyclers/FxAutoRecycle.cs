using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class FxAutoRecycle : MonoBehaviour
{
    ParticleSystem ps;
    void Awake(){ ps = GetComponent<ParticleSystem>(); }
    void OnEnable(){ if (ps && !ps.isPlaying) ps.Play(true); }
    void OnParticleSystemStopped(){ PoolRecycle.Recycle(gameObject); }
}