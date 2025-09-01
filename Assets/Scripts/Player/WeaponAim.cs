using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponAim : MonoBehaviour
{
    private Plane m_PlayerPlane;

    private Vector3 m_MousePosition;
    private Vector3 m_TargetPosition;
    private Vector3 m_ReticlePosition;
    private Vector3 m_LastMousePosition;

    [SerializeField] private float m_MouseDeadZoneRadius;

    [SerializeField] private Transform m_Owner;
    [SerializeField] private OrientationController orientationController;
    
    public Vector3 CurrentAim {
        get { return m_CurrentAim; }
    }

    public Vector3 WeaponAimCurrentAim {
        get { return m_WeaponAimCurrentAim; }
    }

    [SerializeField] private float m_MinAngle;
    [SerializeField] private float m_MaxAngle;

    private Vector3 m_CurrentAim;
    private Vector3 m_WeaponAimCurrentAim;
    [SerializeField] private float m_RotationSpeed;
    [SerializeField] private Reticle m_ReticleToUpdate;

    [SerializeField] private LayerMask m_ShotLayerMask;
    
    [Header("Effects")]
    [SerializeField] private ParticleSystem m_MuzzleFlashEffect;

    [SerializeField] private Transform m_BarrelPosition;
    [SerializeField] private LineRenderer m_BulletRay;
    [SerializeField] private Light m_MuzzleFlashLight;
    
    private void Start()
    {
        m_MuzzleFlashEffect.Stop();
        m_MuzzleFlashLight.enabled = false;
        
        m_PlayerPlane = new Plane(Vector3.up, Vector3.zero);
        orientationController?.Init(m_Owner, this);
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Shoot();
        }
        GetMouseAim();
        Rotate();
    }

    private void FixedUpdate()
    {
        UpdatePlayerPlane();
    }

    private void UpdatePlayerPlane()
    {
        m_PlayerPlane.SetNormalAndPosition(Vector3.up, transform.position);
    }

    private void GetMouseAim()
    {
        m_MousePosition = Input.mousePosition;
        Ray ray = Camera.main.ScreenPointToRay(m_MousePosition);
        Debug.DrawRay(ray.origin, ray.direction * 100, Color.yellow);
        float distance;
        if (m_PlayerPlane.Raycast(ray, out distance))
        {
            m_TargetPosition = ray.GetPoint(distance);
        }

        m_ReticlePosition = m_TargetPosition;

        if(m_ReticleToUpdate != null) { m_ReticleToUpdate.SetPosition(m_ReticlePosition); }

        if (Vector3.Distance(m_TargetPosition, transform.position) < m_MouseDeadZoneRadius)
        {
            m_TargetPosition = m_LastMousePosition;
        }
        else
        {
            m_LastMousePosition = m_TargetPosition;
        }

        m_TargetPosition.y = transform.position.y;
        m_CurrentAim = m_TargetPosition - m_Owner.position;
        m_WeaponAimCurrentAim = m_TargetPosition - this.transform.position;
    }

    private void Rotate()
    {
        float currentAngle = Mathf.Atan2(m_WeaponAimCurrentAim.z, m_WeaponAimCurrentAim.x) * Mathf.Rad2Deg;

        currentAngle = Mathf.Clamp(currentAngle, m_MinAngle, m_MaxAngle);
        currentAngle = -currentAngle + 90f;

        Quaternion rotation = Quaternion.Euler(currentAngle * Vector3.up);

        transform.rotation = Quaternion.Lerp(transform.rotation, rotation, Time.deltaTime * m_RotationSpeed);
    }

    public void Shoot()
    {
        m_MuzzleFlashEffect.Play();
        m_MuzzleFlashLight.enabled = true;

        Invoke(nameof(DisableMuzzleFlash), m_MuzzleFlashEffect.main.duration); 

        Vector3 shotDirection = (m_ReticlePosition - m_BarrelPosition.position).normalized;
        Ray ray = new Ray(m_BarrelPosition.position, shotDirection);
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit, float.PositiveInfinity, m_ShotLayerMask))
        {
            Debug.Log(hit.collider.gameObject.name);
            StartCoroutine(ShowBulletRay(m_BarrelPosition.position, hit.point));
            
            Damageable damageable = hit.collider.GetComponent<Damageable>();
            if (damageable == null)
                return;

            damageable.Damage(10, GameManager.Instance.PlayerTransform, hit.point, hit.normal);
        }
        else
        {
            Vector3 endPosition;
            endPosition = m_BarrelPosition.position + m_BarrelPosition.forward * 100f;
            //endPosition = m_ReticlePosition;
            StartCoroutine(ShowBulletRay(m_BarrelPosition.position, endPosition));
        }
    }

    private void DisableMuzzleFlash()
    {
        m_MuzzleFlashLight.enabled = false;
        m_MuzzleFlashEffect.Stop();
    }

    private IEnumerator ShowBulletRay(Vector3 start, Vector3 end)
    {
        m_BulletRay.SetPosition(0, start);
        m_BulletRay.SetPosition(1, end);
        m_BulletRay.gameObject.SetActive(true);

        yield return new WaitForSeconds(0.05f);

        m_BulletRay.gameObject.SetActive(false);
    }

    public void Initialize(Transform owner, Reticle reticle)
    {
        m_Owner = owner;
        m_ReticleToUpdate = reticle;
    }
}
