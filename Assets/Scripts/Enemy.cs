using UnityEngine;
using UnityEngine.AI;

public enum StateId { Boid = 1, Sheen = 2, Chase = 3, Attack = 4, Death = 5 }

[RequireComponent(typeof(NavMeshAgent))]
public class Enemy : MonoBehaviour
{
    // components
    NavMeshAgent agent;
    Animator anim;
    Damageable hp;
    FIMSpace.FProceduralAnimation.LegsAnimator legs;

    // target
    Transform target;
    Collider targetCol;

    // hit + attack timing
    [SerializeField] HitTrigger hit;
    [SerializeField, Range(0.1f, 4f)] float minDistToHit = 1f;
    [SerializeField] float hitTime = 4.6f;
    [SerializeField] float hitArm = 0f;

    float atkTimer;
    float navTimer;
    [SerializeField] float navStep = 0.5f;

    // boid data (filled by manager)
    public Vector3 cohesion, separation, alignment;
    public int mates;

    // meta
    public int id;
    public int m_Type;

    // ragdoll
    [SerializeField] ZombieRagdoll RagdollPrefab;

    // state
    IState cur;
    public StateId state { get; private set; }
    public int StateIndex => (int)state; // manager uses this

    // velocity (manager reads this)
    public Vector3 Velocity => agent ? agent.velocity : Vector3.zero;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        anim  = GetComponent<Animator>();
        hp    = GetComponent<Damageable>();
        legs  = GetComponent<FIMSpace.FProceduralAnimation.LegsAnimator>();

        if (hp)
        {
            hp.m_OnDeath  += OnDie;
            hp.m_OnDamage += OnHit;
        }
    }

    void Start()
    {
        SetTarget(GameManager.Instance.SheenFacilityT);
        Set(new BoidState(this), StateId.Boid);
    }

    void Update() { cur?.Tick(); }

    // ------------ state host ------------
    public interface IState { void Enter(); void Tick(); void Exit(); }

    void Set(IState next, StateId idNew)
    {
        cur?.Exit();
        state = idNew;
        cur = next;
        cur?.Enter();
        ZombiesManager.Instance.OnStateUpdate(this);
    }

    // wrappers for external triggers
    public void GoSheen()  { Set(new SheenState(this), StateId.Sheen); }
    public void Chase(Transform t) { SetTarget(t); Set(new ChaseState(this), StateId.Chase); }
    public void Attack()   { Set(new AttackState(this), StateId.Attack); }
    public void Die()      { Set(new DeathState(this),  StateId.Death); }

    // ------------ events ------------
    void OnHit(Transform from) { if (state != StateId.Death) Chase(from); }
    void OnDie()               { Die(); }

    // ------------ helpers ------------
    void SetTarget(Transform t)
    {
        target = t;
        targetCol = t ? t.GetComponent<Collider>() : null;
    }

    public Vector3 ClosestTarget()
    {
        if (!target) return transform.position;
        if (targetCol) return targetCol.ClosestPoint(transform.position);
        return target.position;
    }

    public bool Near(float dSqr)
    {
        var p = ClosestTarget() - transform.position;
        return p.sqrMagnitude <= dSqr;
    }

    // called by manager during boid compute step (when in Boid)
    public void UpdateBoid()
    {
        if (mates != 0) cohesion /= mates;

        Vector3 toT = (target.position - agent.transform.position).normalized;

        var m = ZombiesManager.Instance;
        Vector3 dir =
            (cohesion   * m.m_CohesionFactor +
             separation * m.m_SeparationFactor +
             alignment  * m.m_AlignmentFactor +
             toT        * m.m_PlayerFollowFactor).normalized;

        float dot = Vector3.Dot(toT, dir);
        if (dot <= m.m_MaxDeviationFromTarget) dir += toT;

        Vector3 dst = agent.transform.position + dir * 1f;
        if (agent && agent.enabled) agent.SetDestination(dst);
    }

    public void Walk(bool on)
    {
        if (anim) { anim.SetBool("Walking", on); anim.SetBool("Attack", false); }
        if (agent) agent.isStopped = !on;
        if (!on && agent) agent.velocity = Vector3.zero;
    }

    public void FaceTarget() { if (target) transform.LookAt(target.position); }

    public void Hit(int v)
    {
        if (v == 1) hit.ResetHitApplied();
        else if (v == 2) hit.gameObject.SetActive(true);
        else if (v == 3) hit.gameObject.SetActive(false);
    }

    // ------------ states ------------
    class BoidState : IState
    {
        Enemy e; float d2;
        public BoidState(Enemy e){ this.e = e; d2 = e.minDistToHit * e.minDistToHit; }
        public void Enter(){ e.Walk(true); }
        public void Tick(){ if (e.Near(d2)) e.Attack(); }
        public void Exit(){ }
    }

    class SheenState : IState
    {
        Enemy e; float d2;
        public SheenState(Enemy e){ this.e = e; d2 = e.minDistToHit * e.minDistToHit; }
        public void Enter()
        {
            e.Walk(true);
            e.SetTarget(GameManager.Instance.SheenFacilityT);
            if (e.agent && e.agent.enabled) e.agent.SetDestination(GameManager.Instance.SheenFacilityT.position);
        }
        public void Tick(){ if (e.Near(d2)) e.Attack(); }
        public void Exit(){ }
    }

    class ChaseState : IState
    {
        Enemy e; float d2;
        public ChaseState(Enemy e){ this.e = e; d2 = e.minDistToHit * e.minDistToHit; }
        public void Enter()
        {
            e.Walk(true);
            e.navTimer = 0f;
            if (e.agent && e.agent.enabled && e.target) e.agent.SetDestination(e.target.position);
        }
        public void Tick()
        {
            e.navTimer += Time.deltaTime;
            if (e.navTimer >= e.navStep)
            {
                e.navTimer = 0f;
                if (e.agent && e.agent.enabled && e.target) e.agent.SetDestination(e.target.position);
            }
            if (e.Near(d2)) e.Attack();
        }
        public void Exit(){ }
    }

    class AttackState : IState
    {
        Enemy e; float d2; Damageable other;
        public AttackState(Enemy e){ this.e = e; d2 = e.minDistToHit * e.minDistToHit; }
        public void Enter()
        {
            e.Walk(false);
            e.FaceTarget();
            e.atkTimer = 0f;
            if (e.anim) e.anim.SetBool("Attack", true);
            other = e.target ? e.target.GetComponent<Damageable>() : null;
        }
        public void Tick()
        {
            e.atkTimer += Time.deltaTime;

            if (e.atkTimer >= e.hitArm && !e.hit.isActiveAndEnabled && !e.hit.HitApplied)
                e.Hit(2); // enable hit

            if (e.atkTimer >= e.hitTime)
            {
                e.atkTimer = 0f;
                e.Hit(1); // reset
                e.Hit(3); // disable
            }

            if (!e.Near(d2)) { e.Chase(e.target); return; }
            if (other && other.Dead) { e.GoSheen(); return; }
        }
        public void Exit(){ if (e.anim) e.anim.SetBool("Attack", false); e.Hit(3); }
    }

    class DeathState : IState
    {
        Enemy e;
        public DeathState(Enemy e){ this.e = e; }
        public void Enter()
        {
            if (e.legs) e.legs.enabled = false;
            if (e.agent) { e.agent.isStopped = true; e.agent.velocity = Vector3.zero; }
            if (e.anim) e.anim.enabled = false;

            if (e.RagdollPrefab)
            {
                var r = GameObject.Instantiate(e.RagdollPrefab);
                r.gameObject.SetActive(false);
                r.transform.position = e.transform.position;
                r.transform.rotation = e.transform.rotation;
                e.gameObject.SetActive(false);
                r.gameObject.SetActive(true);
                r.AddForce(-e.transform.forward, 100f);
            }
            else e.gameObject.SetActive(false);
        }
        public void Tick(){ }
        public void Exit(){ }
    }
}
