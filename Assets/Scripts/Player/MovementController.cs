using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.UIElements;

namespace top_down
{
    public class MovementController : MonoBehaviour
    {

        [SerializeField, Range(0f, 100f)] private float m_DefaultMaxSpeed;
        [SerializeField, Range(0f, 100f)] private float m_DefaultMaxAcceleration;

        [SerializeField, Range(0f, 100f)] private float m_PlacingMaxSpeed;
        [SerializeField, Range(0f, 100f)] private float m_PlacingMaxAcceleration;

        public float m_PushForce;

        private float m_MaxSpeed, m_MaxAcceleration;

        private Rigidbody m_Rigidbody;
        private Vector3 m_Velocity;

        private Vector3 m_PlayerInput;

        private Animator m_Animator;

        private Vector3 m_PositionLastFrame;

        [SerializeField] private Transform m_ForwardDirection;
        private float m_ForwardDot;
        private float m_LateralDot;
        [Range(0f, 1f)] public float m_Drag;

        private bool m_DashPressed;
        [SerializeField, Range(0f, 40f)] private float m_DashForce;

        private Vector3 m_InputDirection;
        [SerializeField] private float m_RotationSpeed;
        private Quaternion m_DesiredDirection;

        public Vector3 m_NewForwardDir;

        public Vector3 m_Forward { get => transform.forward; }

        private bool ThereIsAxesInput
        {
            get { return m_PlayerInput.magnitude != 0f; }
        }

        void Start()
        {
            m_Rigidbody = GetComponent<Rigidbody>();
            m_Animator = GetComponent<Animator>();

            m_MaxSpeed = m_DefaultMaxSpeed;
            m_MaxAcceleration = m_DefaultMaxAcceleration;

            m_PositionLastFrame = transform.position;
        }

        void Update()
        {
            GetAxis();
            GetDash();
            //GetSetPush();
        }

        private void GetDash()
        {
            if (Input.GetButtonDown("Jump"))
            {
                m_DashPressed = true;
            }
        }

        private void GetSetPush()
        {
            if (Input.GetButtonDown("Fire2"))
            {
                bool value = m_Animator.GetBool("Pushing");
                Debug.Log(value ? "pushing" : "stop pushing");

                m_Animator.SetBool("Pushing", !value);
            }
        }

        private void FixedUpdate()
        {
            if (ThereIsAxesInput)
            {
                MoveTypeA_Rigidbody();
                //MoveTypeB_Rigidbody();

                if (m_DashPressed)
                {
                    PerformDash();
                }
            }
            else
            {
                //transform.rotation = Quaternion.Lerp(transform.rotation, m_DesiredDirection, Time.deltaTime * m_RotationSpeed);
                m_NewForwardDir = Vector3.zero;
                ApplyDrag();
                m_Animator.SetBool("IsMoving", false);
                m_Animator.SetFloat("ForwardSpeedNormalized", 0);
                m_Animator.SetFloat("LateralSpeedNormalized", 0);
            }


            m_DashPressed = false;
        }

        private void PerformDash()
        {
            m_Rigidbody.AddForce(m_InputDirection * m_DashForce, ForceMode.Impulse);

        }

        private void GetAxis()
        {
            m_PlayerInput.x = Input.GetAxis("Horizontal");
            m_PlayerInput.y = Input.GetAxis("Vertical");
            m_PlayerInput = Vector2.ClampMagnitude(m_PlayerInput, 1f);

            m_InputDirection = new Vector3(m_PlayerInput.x, 0f, m_PlayerInput.y);

        }

        private void MoveTypeA_Rigidbody()
        {
            if (Time.deltaTime == 0) return;

            // Use the scene-defined forward/right from m_ForwardDirection
            Vector3 fwd = m_ForwardDirection ? m_ForwardDirection.forward : Vector3.forward;
            Vector3 right = m_ForwardDirection ? m_ForwardDirection.right : Vector3.right;
            fwd.y = 0f; right.y = 0f; // keep movement on XZ
            fwd.Normalize(); right.Normalize();

            // Build desired velocity in that basis (input.y = forward, input.x = right)
            Vector3 desiredVelocity = (right * m_PlayerInput.x + fwd * m_PlayerInput.y) * m_MaxSpeed;

            // Accelerate towards desired (preserve Y)
            m_Velocity = m_Rigidbody.linearVelocity;
            float maxSpeedChange = m_MaxAcceleration * Time.deltaTime;
            m_Velocity.x = Mathf.MoveTowards(m_Velocity.x, desiredVelocity.x, maxSpeedChange);
            m_Velocity.z = Mathf.MoveTowards(m_Velocity.z, desiredVelocity.z, maxSpeedChange);
            m_Rigidbody.linearVelocity = m_Velocity;

            // Anim flags
            m_Animator.SetBool("IsMoving", true);

            // Compute forward/lateral speeds in the same space for clean animation drive
            Vector3 newSpeed = (transform.position - m_PositionLastFrame) / Time.deltaTime;
            float forwardNorm = Vector3.Dot(newSpeed.normalized, fwd);   // -1..1 along custom forward
            float lateralNorm = Vector3.Dot(newSpeed.normalized, right); // -1..1 along custom right
            m_Animator.SetFloat("ForwardSpeedNormalized", forwardNorm);
            m_Animator.SetFloat("LateralSpeedNormalized", lateralNorm);

            m_PositionLastFrame = transform.position;
        }


        private void MoveTypeB_Rigidbody()
        {
            if (Time.deltaTime == 0) return;

            m_NewForwardDir = (m_ForwardDirection.forward * m_InputDirection.z) + (m_ForwardDirection.right * m_InputDirection.x);


            Quaternion newRotation = Quaternion.LookRotation(m_NewForwardDir);
            transform.rotation = Quaternion.Lerp(transform.rotation, newRotation, Time.deltaTime * m_RotationSpeed);
            m_DesiredDirection = newRotation;


            Vector3 movementDir = Vector3.ClampMagnitude(m_NewForwardDir, 1f);
            Vector3 desiredVelocity = movementDir * m_MaxSpeed;

            m_Velocity = m_Rigidbody.linearVelocity;
            float maxSpeedChange = m_MaxAcceleration * Time.deltaTime;
            m_Velocity.x =
                Mathf.MoveTowards(m_Velocity.x, desiredVelocity.x, maxSpeedChange);
            m_Velocity.z =
                Mathf.MoveTowards(m_Velocity.z, desiredVelocity.z, maxSpeedChange);
            m_Rigidbody.linearVelocity = m_Velocity;

            m_Animator.SetBool("IsMoving", true);

            Vector3 newSpeed = (transform.position - m_PositionLastFrame) / Time.deltaTime;
            Vector3 relativeDirection = newSpeed.normalized;

            m_ForwardDot = Vector3.Dot(m_ForwardDirection.forward, transform.forward);
            m_LateralDot = Vector3.Dot(m_ForwardDirection.right, transform.right);

            m_Animator.SetFloat("ForwardSpeedNormalized", relativeDirection.z * m_ForwardDot);
            m_Animator.SetFloat("LateralSpeedNormalized", relativeDirection.x * m_LateralDot);

            m_PositionLastFrame = transform.position;
        }

        private void ApplyDrag()
        {
            //if (ThereIsAxesInput) return;
            m_Rigidbody.linearVelocity *= m_Drag;
        }

        public void StartedPlacing()
        {
            m_MaxSpeed = m_PlacingMaxSpeed;
            m_MaxAcceleration = m_PlacingMaxAcceleration;
        }

        public void StoppedPlacing()
        {
            m_MaxSpeed = m_DefaultMaxSpeed;
            m_MaxAcceleration = m_DefaultMaxAcceleration;
        }
    }

}
