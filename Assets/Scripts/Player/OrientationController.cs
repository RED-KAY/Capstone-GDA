using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OrientationController : MonoBehaviour
{
    [SerializeField] private WeaponAim m_WeaponAim;
    [SerializeField] private float m_RotationSpeed;
    private Transform m_PlayerTransform;

    public void Init(Transform playerTransform, WeaponAim weapon)
    {
        m_PlayerTransform = playerTransform;
        m_WeaponAim = weapon;
    }
    
    private void Update()
    {
        Rotate();
    }

    private void Rotate()
    {
        if(m_WeaponAim == null) return;

        Vector3 aimDirection = new Vector3(m_WeaponAim.CurrentAim.normalized.x, 0 * m_WeaponAim.CurrentAim.normalized.y, m_WeaponAim.CurrentAim.normalized.z);
        Quaternion newRotation = Quaternion.LookRotation(aimDirection);
        m_PlayerTransform.rotation = Quaternion.Lerp(m_PlayerTransform.rotation, newRotation, Time.deltaTime * m_RotationSpeed); 
    }
}
