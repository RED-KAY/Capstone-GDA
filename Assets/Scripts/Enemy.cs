using UnityEngine;
using UnityEngine.AI;

public enum StateId { Chase = 3, Attack = 4, Death = 5 }

[RequireComponent(typeof(NavMeshAgent))]
public class Enemy : MonoBehaviour
{
    NavMeshAgent agent;
    Animator anim;
    Damageable hp;
    FIMSpace.FProceduralAnimation.LegsAnimator legs;

    [SerializeField] Transform player;
    Transform target;
    Collider targetCol;

    [SerializeField] HitTrigger hit;
    [SerializeField, Range(0.1f, 4f)] float minDistToHit = 1f;
    [SerializeField] float hitTime = 4.6f;
    [SerializeField] float hitArm  = 0f;

    float atkTimer;
    float navTimer;
    [SerializeField] float navStep = 0.5f;

    public Vector3 cohesion, separation, alignment;
    public int mates;

    [SerializeField] float flockRange = 25f;
    [SerializeField] float biasFar    = 1.5f;
    [SerializeField] float biasNear   = 0.2f;
    [SerializeField, Range(0f,1f)] float inertiaW = 0.85f;
    Vector3 steer;

    [SerializeField] float nearNoFlockMul = 1.15f;
    [SerializeField] float attackInMul  = 1.00f;
    [SerializeField] float attackOutMul = 1.40f;
    [SerializeField] float sepNearScaleMin = 0.35f;

    [SerializeField] float lookAhead = 3f;
    [SerializeField] float minStep   = 1f;
    [SerializeField] float sampleRad = 1f;
    [SerializeField] float retarget  = 0.5f;
    [SerializeField] float stuckMax  = 0.8f;
    float stuck;

    [SerializeField] float navSampleRadius = 2f;

    public int id;
    public int type;
    public int m_Type { get { return type; } }

    [Header("Ragdoll")]
    [SerializeField] ZombieRagdoll RagdollPrefab;   // direct instantiate
    [SerializeField] float ragdollLife = 5f;

    IState cur;
    public StateId state { get; private set; }
    public Vector3 Velocity => agent ? agent.velocity : Vector3.zero;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        anim  = GetComponent<Animator>();
        hp    = GetComponent<Damageable>();
        legs  = GetComponent<FIMSpace.FProceduralAnimation.LegsAnimator>();
        if (agent) agent.autoRepath = true;

        if (hp)
        {
            hp.m_OnDeath  += OnDie;
            hp.m_OnDamage += OnHit;
        }
    }

    void OnEnable() { EnsureOnNavMesh(); }

    void Start()
    {
        if (!player)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) player = p.transform;
        }
        SetTarget(player);
        EnsureOnNavMesh();
        Set(new ChaseState(this), StateId.Chase);
    }

    void Update() { cur?.Tick(); }

    public interface IState { void Enter(); void Tick(); void Exit(); }

    void Set(IState next, StateId idNew)
    {
        cur?.Exit();
        state = idNew;
        cur = next;
        cur?.Enter();
        ZombiesManager.Instance.OnStateUpdate(this);
    }

    public void Chase(Transform t) { SetTarget(t); Set(new ChaseState(this), StateId.Chase); }
    public void Attack()           { Set(new AttackState(this), StateId.Attack); }
    public void Die()              { Set(new DeathState(this),  StateId.Death); }

    void OnHit(Transform from) { if (state != StateId.Death) Chase(player ? player : from); }
    void OnDie()               { Die(); }

    void SetTarget(Transform t) { target = t; targetCol = t ? t.GetComponent<Collider>() : null; }

    public Vector3 ClosestTarget()
    {
        if (!target) return transform.position;
        if (targetCol) return targetCol.ClosestPoint(transform.position);
        return target.position;
    }

    public void Walk(bool on)
    {
        if (anim) { anim.SetBool("Walking", on); anim.SetBool("Attack", false); }
        if (agent && agent.enabled && agent.isOnNavMesh) agent.isStopped = !on;
        if (!on && agent) agent.velocity = Vector3.zero;
    }

    public void FaceTarget() { if (target) transform.LookAt(target.position); }

    public void Hit(int v)
    {
        if (v == 1) hit.ResetHitApplied();
        else if (v == 2) hit.gameObject.SetActive(true);
        else if (v == 3) hit.gameObject.SetActive(false);
    }

    bool EnsureOnNavMesh()
    {
        if (!agent || !agent.enabled) return false;
        if (agent.isOnNavMesh) return true;
        if (NavMesh.SamplePosition(transform.position, out var hit, navSampleRadius, NavMesh.AllAreas))
            return agent.Warp(hit.position);
        return false;
    }

    public void UpdateBoid()
    {
        if (!agent || !agent.enabled || !target) return;
        if (!agent.isOnNavMesh && !EnsureOnNavMesh()) return;

        float dist = Vector3.Distance(agent.transform.position, target.position);

        if (dist > flockRange)
        {
            if ((target.position - agent.destination).sqrMagnitude > retarget * retarget)
                agent.SetDestination(target.position);
            return;
        }

        float nearCut = minDistToHit * nearNoFlockMul;
        if (dist <= nearCut)
        {
            if ((target.position - agent.destination).sqrMagnitude > retarget * retarget)
                agent.SetDestination(target.position);
            return;
        }

        if (mates != 0) cohesion /= mates;

        Vector3 toT = (target.position - agent.transform.position).normalized;
        var m = ZombiesManager.Instance;

        float tNear = Mathf.Clamp01(dist / flockRange);
        Vector3 sepScaled = separation * Mathf.Lerp(sepNearScaleMin, 1f, tNear);

        Vector3 dir =
            (cohesion   * m.m_CohesionFactor +
             sepScaled  * m.m_SeparationFactor +
             alignment  * m.m_AlignmentFactor +
             toT        * m.m_PlayerFollowFactor).normalized;

        float bias = Mathf.Lerp(biasFar, biasNear, tNear);
        dir = (dir + toT * bias).normalized;

        float dot = Vector3.Dot(toT, dir);
        if (dot <= m.m_MaxDeviationFromTarget) dir = (dir + toT).normalized;

        if (steer == Vector3.zero) steer = dir;
        steer = Vector3.Slerp(steer, dir, 1f - Mathf.Clamp01(inertiaW));
        if (steer.sqrMagnitude < 1e-4f) return;

        float step = Mathf.Max(lookAhead, minStep, agent.radius + agent.stoppingDistance * 0.5f);
        Vector3 dst = agent.transform.position + steer.normalized * step;

        if (NavMesh.SamplePosition(dst, out var hitPos, sampleRad, NavMesh.AllAreas))
            dst = hitPos.position;

        if ((dst - agent.destination).sqrMagnitude > retarget * retarget)
            agent.SetDestination(dst);

        bool slow = agent.velocity.sqrMagnitude < 0.01f;
        bool tinyGoal = agent.remainingDistance < 0.2f;
        bool playerFar = dist > minDistToHit;

        if (slow && tinyGoal && playerFar)
        {
            stuck += Time.deltaTime;
            if (stuck > stuckMax)
            {
                agent.SetDestination(target.position);
                stuck = 0f;
            }
        }
        else stuck = 0f;
    }

    class ChaseState : IState
    {
        Enemy e; float enter2;
        public ChaseState(Enemy e){ this.e = e; enter2 = e.minDistToHit * e.attackInMul; enter2 *= enter2; }
        public void Enter()
        {
            e.Walk(true);
            e.navTimer = 0f;
            if (e.agent && e.agent.enabled && e.agent.isOnNavMesh && e.target)
                e.agent.SetDestination(e.target.position);
        }
        public void Tick()
        {
            e.navTimer += Time.deltaTime;
            if (e.navTimer >= e.navStep)
            {
                e.navTimer = 0f;
                if (e.agent && e.agent.enabled && e.agent.isOnNavMesh && e.target)
                    e.agent.SetDestination(e.target.position);
            }
            Vector3 d = e.ClosestTarget() - e.transform.position;
            if (d.sqrMagnitude <= enter2) e.Attack();
        }
        public void Exit(){ }
    }

    class AttackState : IState
    {
        Enemy e; float exit2; Damageable other;
        public AttackState(Enemy e){ this.e = e; float r = e.minDistToHit * e.attackOutMul; exit2 = r * r; }
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
                e.Hit(2);

            if (e.atkTimer >= e.hitTime)
            {
                e.atkTimer = 0f;
                e.Hit(1);
                e.Hit(3);
            }

            Vector3 d = e.ClosestTarget() - e.transform.position;
            if (d.sqrMagnitude > exit2) { e.Chase(e.player ? e.player : e.target); return; }
            if (other && other.Dead)     { e.Chase(e.player ? e.player : e.target); return; }
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
            if (e.agent && e.agent.enabled && e.agent.isOnNavMesh)
            {
                e.agent.isStopped = true;
                e.agent.velocity  = Vector3.zero;
            }
            if (e.anim) e.anim.enabled = false;

            // Instantiate ragdoll (no pooling)
            if (e.RagdollPrefab)
            {
                var r = GameObject.Instantiate(e.RagdollPrefab);
                r.transform.position = e.transform.position;
                r.transform.rotation = e.transform.rotation;
                r.gameObject.SetActive(true);
                r.AddForce(-e.transform.forward, 100f);

                // auto-destroy to avoid scene buildup
                var auto = r.gameObject.GetComponent<RagdollAutoDestroy>();
                if (!auto) auto = r.gameObject.AddComponent<RagdollAutoDestroy>();
                auto.life = e.ragdollLife;
            }

            // destroy this enemy (no pooling)
            GameObject.Destroy(e.gameObject);
        }
        public void Tick(){ }
        public void Exit(){ }
    }
}
