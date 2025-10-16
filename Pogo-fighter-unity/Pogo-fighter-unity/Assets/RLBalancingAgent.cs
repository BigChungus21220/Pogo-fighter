using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using UnityEngine.InputSystem;
using Unity.MLAgents.Actuators;
using Unity.Mathematics;
using System;
using UnityEngine.Assertions.Must;

namespace Assets
{
    public class RLBalancingAgent : Agent
    {
        [SerializeField] private ArticulationBody _articulationBody;
        [SerializeField] public GameObject pogo;
        [SerializeField] public int torqueForce;
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

            Vector3 torque = new Vector3(actions.ContinuousActions[0], actions.ContinuousActions[1], actions.ContinuousActions[2]) * torqueForce;
            _articulationBody.AddTorque(torque);

            Debug.Log("Action received");

            // Perfectly upright: uprighness    = 1
            // Horizontal: uprightness          = 0
            // Upside Down: uprightness         = -1
            float uprightness = Vector3.Dot(transform.up, Vector3.up);
            AddReward(uprightness * 0.1f); // Small reward each frame for being upright
        }
        
        // Called by Sphere game object
        public void OnFall()
        {
            Debug.Log("Fall detected");
            AddReward(-100);
            RecoverToUpright();
            //_recover = true;
        }

        private void RecoverToUpright()
        {
            _articulationBody.linearVelocity = Vector3.zero;
            _articulationBody.angularVelocity = Vector3.zero;
            Vector3 newPos = new Vector3(_articulationBody.transform.position.x, 1, _articulationBody.transform.position.z);
            Quaternion newRot = Quaternion.Euler(0, _articulationBody.transform.rotation.eulerAngles.y, 0);
            _articulationBody.TeleportRoot(newPos, newRot);
            Debug.Log("Recovered position");
        }

        private void Jump(bool jumpThisFrame)
        {
            if (jumpThisFrame || true)
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