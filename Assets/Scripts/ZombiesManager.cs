using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public struct BoidData
{
    public Vector3 position;
    public Vector3 direction;

    public Vector3 cohesion;
    public Vector3 separation;
    public Vector3 alignment;
    public int numberOfHordemates;

    public static int Size => sizeof(float) * 3 * 5 + sizeof(int);
}

[Serializable]
public class ZombieMaterialStates { public Material[] m_States; }

public class ZombiesManager : Singleton<ZombiesManager>
{
    const int threadGroupSize = 1024;
    const int MaxTypes = 3;
    const int MaxBatch = 1023;

    [SerializeField] ComputeShader m_BoidsComputeShader;

    [SerializeField] Mesh[] m_ZombieMeshes;
    public ZombieMaterialStates[] m_ZombieMaterials;

    Transform[] m_ZombieTypes;
    List<Enemy> m_All = new List<Enemy>();
    readonly List<Enemy> m_Boids = new List<Enemy>(); // chasers

    // boid tuning
    [Range(0f, 0.5f), SerializeField] float m_BoidStep = 0.25f;
    public float m_CohesionFactor;
    public float m_SeparationFactor;
    public float m_AlignmentFactor;
    public float m_PlayerFollowFactor = 1f;
    [Range(0, 0.9f)] public float m_MaxDeviationFromTarget = 0.2f;
    public float m_RadiusOfInfluence = 6f;
    public float m_AvoidRadius = 1.5f;

    float m_Timer;

    // draw
    readonly List<Enemy>[] m_Draw = new List<Enemy>[MaxTypes];
    readonly MaterialPropertyBlock[] m_Mpbs = new MaterialPropertyBlock[MaxTypes];
    static readonly Matrix4x4[] s_Mats = new Matrix4x4[MaxBatch];
    static readonly List<float> s_Anim = new List<float>(MaxBatch);
    bool m_DrawDirty;

    // compute scratch
    bool m_InBoid;
    readonly List<Enemy> m_Add = new();
    readonly List<Enemy> m_Rem = new();

    BoidData[] m_Scratch;
    ComputeBuffer m_Buffer;

    void Awake()
    {
        m_ZombieTypes = new Transform[transform.childCount];
        for (int i = 0; i < transform.childCount; i++)
            m_ZombieTypes[i] = transform.GetChild(i);

        for (int t = 0; t < MaxTypes; t++)
        {
            m_Draw[t] = new List<Enemy>(1024);
            m_Mpbs[t] = new MaterialPropertyBlock();
        }
    }

    void Update()
    {
        // flock step for all chasers
        m_Timer += Time.deltaTime;
        if (m_Timer < m_BoidStep) return;
        m_Timer = 0f;

        int n = m_Boids.Count;
        if (n == 0) return;

        m_InBoid = true;

        if (m_Scratch == null || m_Scratch.Length < n) m_Scratch = new BoidData[n];
        if (m_Buffer == null || m_Buffer.count < n)
        {
            m_Buffer?.Dispose();
            m_Buffer = new ComputeBuffer(n, BoidData.Size);
        }

        for (int i = 0; i < n; i++)
        {
            var e = m_Boids[i];
            m_Scratch[i].position  = e.transform.position;
            m_Scratch[i].direction = e.Velocity;
        }

        int k = m_BoidsComputeShader.FindKernel("CSMain");
        m_Buffer.SetData(m_Scratch, 0, 0, n);
        m_BoidsComputeShader.SetBuffer(k, "boids", m_Buffer);
        m_BoidsComputeShader.SetInt("numBoids", n);
        m_BoidsComputeShader.SetFloat("radiusOfInfluence", m_RadiusOfInfluence);
        m_BoidsComputeShader.SetFloat("avoidRadius", m_AvoidRadius);

        int groups = Mathf.CeilToInt(n / (float)threadGroupSize);
        m_BoidsComputeShader.Dispatch(k, groups, 1, 1);
        m_Buffer.GetData(m_Scratch, 0, 0, n);

        for (int i = 0; i < n; i++)
        {
            var e = m_Boids[i];
            e.cohesion   = m_Scratch[i].cohesion;
            e.separation = m_Scratch[i].separation;
            e.alignment  = m_Scratch[i].alignment;
            e.mates      = m_Scratch[i].numberOfHordemates;
            e.UpdateBoid(); // steer with flock variance (now distance-aware)
        }

        m_InBoid = false;

        // apply deferred membership
        if (m_Rem.Count > 0)
        {
            for (int i = 0; i < m_Rem.Count; i++) m_Boids.Remove(m_Rem[i]);
            m_Rem.Clear();
        }
        if (m_Add.Count > 0)
        {
            for (int i = 0; i < m_Add.Count; i++)
            {
                var e = m_Add[i];
                if (!m_Boids.Contains(e)) m_Boids.Add(e);
            }
            m_Add.Clear();
        }

        if (m_DrawDirty) { BuildDraw(); m_DrawDirty = false; }
    }

    void LateUpdate()
    {
        if (m_DrawDirty) { BuildDraw(); m_DrawDirty = false; }
        DrawAll();
    }

    public void AddZombieToGroup(int typeIndex, Enemy z)
    {
        if (typeIndex < 0 || typeIndex >= m_ZombieTypes.Length)
        {
            Debug.LogError("Bad type index");
            return;
        }

        switch (typeIndex)
        {
            case 0: z.name = "ZombieType1"; break;
            case 1: z.name = "ZombieType2"; break;
            case 2: z.name = "ZombieType3"; break;
        }

        int newId = m_All.Count + 1;
        z.name += "_" + newId;
        z.id = newId;
        z.type = typeIndex;

        m_All.Add(z);
        m_Boids.Add(z); // starts in Chase
        m_DrawDirty = true;
    }

    // Enemy calls this when state changes
    public void OnStateUpdate(Enemy e)
    {
        if (m_InBoid)
        {
            if (e.state == StateId.Chase) m_Add.Add(e);
            else m_Rem.Add(e);
        }
        else
        {
            if (e.state == StateId.Chase)
            {
                if (!m_Boids.Contains(e)) m_Boids.Add(e);
            }
            else m_Boids.Remove(e);
        }

        if (e.state == StateId.Death) m_All.Remove(e);

        m_DrawDirty = true;
    }

    void BuildDraw()
    {
        for (int t = 0; t < MaxTypes; t++) m_Draw[t].Clear();

        for (int i = 0; i < m_All.Count; i++)
        {
            var z = m_All[i];
            if (!z) continue;
            if (z.state == StateId.Death) continue;
            if (z.type < 0 || z.type >= MaxTypes) continue;
            m_Draw[z.type].Add(z);
        }
    }

    static int Slice(StateId s)
    {
        switch (s)
        {
            case StateId.Attack: return 1;
            case StateId.Death:  return 2;
            default:             return 0; // chase locomotion
        }
    }

    void DrawAll()
    {
        int types = m_ZombieMeshes.Length;
        for (int t = 0; t < types; t++)
        {
            var mesh = m_ZombieMeshes[t];
            if (!mesh) continue;

            var group = m_Draw[t];
            if (group == null || group.Count == 0) continue;

            var mat = m_ZombieMaterials[t].m_States != null && m_ZombieMaterials[t].m_States.Length > 0
                        ? m_ZombieMaterials[t].m_States[0]
                        : null;
            if (!mat || !mat.enableInstancing) continue;

            DrawGroup(t, mesh, mat, group);
        }
    }

    void DrawGroup(int typeIndex, Mesh mesh, Material mat, List<Enemy> group)
    {
        int total = group.Count;
        var mpb = m_Mpbs[typeIndex];

        for (int start = 0; start < total; start += MaxBatch)
        {
            int n = Mathf.Min(MaxBatch, total - start);

            for (int i = 0; i < n; i++)
            {
                var z = group[start + i];
                if (!z)
                {
                    s_Mats[i] = Matrix4x4.identity;
                    if (i >= s_Anim.Count) s_Anim.Add(0f); else s_Anim[i] = 0f;
                    continue;
                }

                float scale = 1f;
                if (z.type == 1) scale = 1.3f;
                else if (z.type == 2) scale = 2f;

                s_Mats[i] = Matrix4x4.TRS(z.transform.position, z.transform.rotation, Vector3.one * scale);

                int slice = Slice(z.state);
                if (i >= s_Anim.Count) s_Anim.Add(slice); else s_Anim[i] = slice;
            }

            if (s_Anim.Count > n) s_Anim.RemoveRange(n, s_Anim.Count - n);

            mpb.Clear();
            mpb.SetFloatArray("_AnimIndex", s_Anim);

            Graphics.DrawMeshInstanced(mesh, 0, mat, s_Mats, n, mpb, ShadowCastingMode.On, true);
        }
    }

    void OnDisable()
    {
        m_Buffer?.Dispose();
        m_Buffer = null;
    }
}
