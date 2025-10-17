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
        private Vector3 _startPos;
        private int _stepSinceFall = 0;
        
        private void Start()
        {
            _jumpAction = InputSystem.actions.FindAction("Jump");
            _startPos = _articulationBody.transform.position;
        }

        public override void CollectObservations(VectorSensor sensor)
        {
            // Rotational position (four inputs)
            // Keep in mind: rotation is a quaternion, so four inputs for the model, not one
            Quaternion rotation = _articulationBody.transform.rotation.normalized;
            sensor.AddObservation(rotation);
            Debug.Log("ModelIO: Input 1: rotation: " + rotation.ToString());

            // Angular velocity (four inputs)
            //Quaternion angularVelocityQ = Quaternion.Euler(_articulationBody.angularVelocity.normalized);
            //sensor.AddObservation(angularVelocityQ);
            //Debug.Log("ModelIO: Input 2: angular velocity: " + angularVelocityQ.ToString());

            // Height (one input)
            // Not passing x or z. Model does not need to know its absolute position
            //float y = _articulationBody.transform.position.y;
            //sensor.AddObservation(y);
            //Debug.Log("ModelIO: Input 3: y: " + y.ToString());

            // Directional velocity (three inputs)
            //Vector3 linearV = _articulationBody.linearVelocity;
            //sensor.AddObservation(linearV);
            //Debug.Log("ModelIO: Input 4: linear velocity: " + linearV.ToString());
        }

        public override void OnActionReceived(ActionBuffers actions)
        {
            //bool jumpThisFrame = actions.DiscreteActions[0] > 0;
            //Debug.Log("ModelIO: Output 1: jump: " + jumpThisFrame.ToString());
            //Jump(jumpThisFrame);

            Vector3 torque = new Vector3(actions.ContinuousActions[0], actions.ContinuousActions[1], actions.ContinuousActions[2]) * torqueForce;
            Debug.Log("ModelIO: Output 2: torque: " + torque.ToString());
            _articulationBody.AddRelativeTorque(torque);

            // Perfectly upright: uprighness    = 1
            // Horizontal: uprightness          = 0
            // Upside Down: uprightness         = -1
            float uprightness = Vector3.Dot(_articulationBody.transform.up, Vector3.up);
            float reward = uprightness * 0.01f;
            float avpenalty = (float)(-0.001 * (Math.Pow(_articulationBody.angularVelocity.x, 2) + Math.Pow(_articulationBody.angularVelocity.y, 2) + Math.Pow(_articulationBody.angularVelocity.z, 2)));
            reward += avpenalty;
            AddReward(reward); // Small reward each frame for being upright

            _stepSinceFall++;

            Debug.Log("Adding reward for uprightness: " + reward.ToString() + ".");

            //float dummyReward = actions.ContinuousActions[3];
            //AddReward(dummyReward);
            //Debug.Log("Adding dummy reward: " + dummyReward.ToString() + ".");
        }
        
        // Called by Sphere game object
        public void OnFall()
        {
            float fallReward = -2.5f;
            Debug.Log("Steps since last fall: " + _stepSinceFall + ".");
            _stepSinceFall = 0;
            Debug.Log("Adding reward for fall: " + fallReward.ToString() + ".");
            AddReward(fallReward);
            //RecoverToUpright();
            EndEpisode();
        }

        public override void OnEpisodeBegin()
        {
            base.OnEpisodeBegin();

            RecoverToUpright();
        }

        private void RecoverToUpright()
        {
            _articulationBody.linearVelocity = Vector3.zero;
            _articulationBody.angularVelocity = Vector3.zero;
            Quaternion newRot = Quaternion.Euler(0, 0, 0);
            _articulationBody.TeleportRoot(_startPos, newRot);
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