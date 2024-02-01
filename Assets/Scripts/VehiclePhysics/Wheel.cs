using Unity.Mathematics;
using UnityEngine;

namespace VehicleSystems
{
    public class Wheel : MonoBehaviour
    {
        [Header("Wheel")]
        [SerializeField] private float radius = 0.34f;
        [Header("Suspension")]
        [SerializeField] private float suspensionDistance = 0.5f;
        [SerializeField] private float springRate = 50000.0f;
        [SerializeField] private float damperRate = 2500.0f;
        [Header("Collision")]
        [SerializeField] private LayerMask collisionLayers = -1;

        public float motorTorque { get; set; } = 0.0f;
        public float brakeTorque { get; set; } = 0.0f;
        public float steerAngle { get; set; } = 0.0f;
        public float angularVelocity { get; set; } = 0.0f;

        private Rigidbody cachedRigidbody = default;

        private float fixedDeltaTime = 0.0f;

        private bool isGrounded = false;
        private RaycastHit hitResult = new RaycastHit();

        private float3 wheelForward = float3.zero;
        private float3 wheelRight = float3.zero;
        private float3 wheelUp = float3.zero;

        private float currentSuspensionDistance = 0.0f;
        private float previousSuspensionDistance = 0.0f;
        private float suspensionForce = 0.0f;
        private float wheelLoad = 0.0f;

        private float3 groundForward = float3.zero;
        private float3 groundRight = float3.zero;
        //private Rigidbody otherRigidbody = default;

        private float3 pointVelocity = float3.zero;
        private float speedForward = 0.0f;
        private float speedRight = 0.0f;

        private float forceForward = 0.0f;
        private float forceRight = 0.0f;
        private float3 frictionForce = float3.zero;

        private void OnEnable()
        {
            cachedRigidbody = GetComponentInParent<Rigidbody>();

            if (cachedRigidbody == null)
            {
                Debug.LogWarning("Disabling wheel, rigidbody not found.");
                enabled = false;
                return;
            }
        }

        private void FixedUpdate()
        {
            // time
            fixedDeltaTime = Time.fixedDeltaTime;

            // collision
            isGrounded = Physics.Raycast(transform.position, -transform.up, out hitResult, suspensionDistance + radius, collisionLayers, QueryTriggerInteraction.Ignore);

            // wheel directions
            quaternion steeredRotation = quaternion.Euler(0.0f, steerAngle, 0.0f);
            wheelForward = transform.TransformDirection(math.mul(steeredRotation, math.forward())).normalized;
            wheelRight = transform.TransformDirection(math.mul(steeredRotation, math.right())).normalized;
            wheelUp = transform.TransformDirection(math.mul(steeredRotation, math.up())).normalized;

            // suspension
            previousSuspensionDistance = currentSuspensionDistance;
            currentSuspensionDistance = isGrounded ? hitResult.distance - radius : suspensionDistance;
            suspensionForce = ((suspensionDistance - currentSuspensionDistance) * springRate) + ((previousSuspensionDistance - currentSuspensionDistance) / fixedDeltaTime * damperRate);
            cachedRigidbody.AddForceAtPosition(suspensionForce * wheelUp, transform.position);
            wheelLoad = suspensionForce > 0.0f ? suspensionForce : 0.0f;

            // projected forward and right direction
            groundForward = math.normalizesafe(math.cross(hitResult.normal, -wheelRight));
            groundRight = math.normalizesafe(math.cross(hitResult.normal, wheelForward));
            //otherRigidbody = hitResult.collider?.attachedRigidbody;

            // velocity
            pointVelocity = isGrounded ? cachedRigidbody.GetPointVelocity(hitResult.point) : float3.zero;
            //pointVelocity = otherRigidbody == null ? pointVelocity : pointVelocity - (float3)otherRigidbody.GetPointVelocity(hitResult.point);
            speedForward = math.dot(pointVelocity, groundForward);
            speedRight = math.dot(pointVelocity, groundRight);

            // friction
            forceForward = 0.0f;
            forceRight = 0.0f;

            // add friction force
            frictionForce = isGrounded ? ((groundForward * forceForward) + (groundRight * forceRight)) : float3.zero;
            cachedRigidbody.AddForceAtPosition(frictionForce, Vector3.zero);
        }
    }
}