using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using UnityEngine.InputSystem;
using Unity.MLAgents.Actuators;
using Unity.Mathematics;
using System;

namespace Assets
{
    public class RLBalancingAgent : Agent
    {
        [SerializeField] private ArticulationBody _articulationBody;
        [SerializeField] public GameObject pogo;
        private InputAction _jumpAction;
        
        private void Start()
        {
            _jumpAction = InputSystem.actions.FindAction("Jump");
        }

        public override void CollectObservations(VectorSensor sensor)
        {
            // Rotational position (four inputs)
            // Keep in mind: rotation is a quaternion, so four inputs for the model, not one
            sensor.AddObservation(transform.rotation.normalized);

            // Angular velocity (four inputs)
            Quaternion angularVelocityQ = Quaternion.Euler(_articulationBody.angularVelocity.normalized);
            sensor.AddObservation(angularVelocityQ);

            // Height (one input)
            // Not passing x or z. Model does not need to know its absolute position
            sensor.AddObservation(transform.position.y);

            // Directional velocity (three inputs)
            sensor.AddObservation(_articulationBody.linearVelocity);
        }

        public override void OnActionReceived(ActionBuffers actions)
        {
            bool jumpThisFrame = actions.DiscreteActions[0] > 0;
            Jump(jumpThisFrame);

            Vector3 torque = new Vector3(actions.ContinuousActions[0], actions.ContinuousActions[1], actions.ContinuousActions[2]);
            _articulationBody.AddTorque(torque);

            _articulationBody.AddForce(Vector3.up * 100); // Pickle space program
            //transform.Translate(Vector3.up * 100);

            Debug.Log("Sending to space");

            Debug.Log("Action received");

            // Perfectly upright: uprighness    = 1
            // Horizontal: uprightness          = 0
            // Upside Down: uprightness         = -1
            float uprightness = Vector3.Dot(transform.up, Vector3.up);
            AddReward(uprightness * 0.1f); // Small reward each frame for being upright
        }
        
        // THIS SHOULD BE ATTACHED TO THE "sphere" gameObject
        // Otherwise, the episode will immediately end "pogo" hits "floor"
        // private void OnCollisionEnter(Collision collision)
        // {
        //     if (collision.gameObject.CompareTag("floor"))
        //     {
        //         AddReward(-1.0f);
        //         EndEpisode();
        //     }
        // }
        
        public override void OnEpisodeBegin()
        {
            transform.position = Vector3.zero;
        }


        //private void AccelerateRotationByOutput(Vector3 torque, double deltaTime = 1d) // Just use articulationBody. AddTorque
        //{
        //    for (int i = 0; i < 3; i++) torque[i] = (float)Math.Clamp(torque[i], _torqueMin, _torqueMax);

        //    // Calculate acceleration from torque
        //    // α = (I^(−1))(τ − (ω x (Iω))) 

        //    Vector3 omega = _articulationBody.angularVelocity;
        //    Vector3 inertiaXOmega = _inertia * omega;
        //    Vector3 omegaCross = Vector3.Cross(omega, inertiaXOmega);

        //    Vector3 torqueMinusCross = torque - omegaCross;

        //    Vector3 acceleration = _inertia.inverse * torqueMinusCross;

        //    _articulationBody.angularVelocity += acceleration;
        //}

        private void Jump(bool jumpThisFrame)
        {
            if (jumpThisFrame)
            {
                pogo.GetComponent<ArticulationBody>().SetDriveTarget(ArticulationDriveAxis.X, 0.0f);
            }
            else
            {
                pogo.GetComponent<ArticulationBody>().SetDriveTarget(ArticulationDriveAxis.X, 0.5f);
            }
        }
    }
}