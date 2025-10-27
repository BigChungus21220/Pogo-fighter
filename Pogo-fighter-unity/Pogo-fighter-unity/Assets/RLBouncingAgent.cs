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
    public class RLBouncingAgent : Agent
    {
        [SerializeField] private ArticulationBody _articulationBody;
        [SerializeField] public GameObject pogo;
        [SerializeField] public int torqueForce;
        private InputAction _jumpAction;
        private Vector3 _startPos;
        private int _stepSinceFall = 0;

        private Vector3 _targetPos;

        private float Logistic(float x)
        {
            return 1/(1 + Mathf.Exp(-x));
        }
        
        private void Start()
        {
            _jumpAction = InputSystem.actions.FindAction("Jump");
            _startPos = _articulationBody.transform.position;
        }

        public override void CollectObservations(VectorSensor sensor)
        {
            // Rotational position (four inputs)
            // Keep in mind: rotation is a quaternion, so four inputs for the model, not one
            Quaternion rotation = _articulationBody.transform.rotation;
            sensor.AddObservation(rotation);
            Debug.Log("ModelIO: Input 1: rotation: " + rotation.ToString());

            // Angular velocity (three inputs)
            Vector3 angularVelocity = _articulationBody.angularVelocity;
            sensor.AddObservation(angularVelocity);
            Debug.Log("ModelIO: Input 2: angular velocity: " + angularVelocity.ToString());

            // Position (three inputs)
            Vector3 position = _articulationBody.transform.position;
            sensor.AddObservation(position);
            Debug.Log("ModelIO: Input 3: y: " + position.ToString());

            // Target position delta (three inputs)
            Vector3 target = _targetPos - position;
            sensor.AddObservation(target);
            Debug.Log("ModelIO: Input 3: y: " + target.ToString());

            // Directional velocity (three inputs)
            Vector3 linearV = _articulationBody.linearVelocity;
            sensor.AddObservation(linearV);
            Debug.Log("ModelIO: Input 4: linear velocity: " + linearV.ToString());
        }

        public override void OnActionReceived(ActionBuffers actions)
        {
            bool is_jumping = actions.DiscreteActions[0] > 0;
            Debug.Log("ModelIO: Output 1: jump: " + is_jumping.ToString());
            SetJump(is_jumping);

            float reward = 0.0f;

            Vector3 torque = new Vector3(actions.ContinuousActions[0], actions.ContinuousActions[1], actions.ContinuousActions[2]) * torqueForce;
            //Vector3 torque = Vector3.zero;
            // penalty for applying torque
            reward -= Logistic(torque.sqrMagnitude/torqueForce)*2 - 1;
            Debug.Log("ModelIO: Output 2: torque: " + torque.ToString());

            _articulationBody.AddRelativeTorque(torque);

            // Perfectly upright: uprighness    = 1
            // Horizontal: uprightness          = 0
            // Upside Down: uprightness         = -1
            float uprightness = Vector3.Dot(_articulationBody.transform.up, Vector3.up);
            reward += uprightness*2;
            float avpenalty = Logistic(_articulationBody.angularVelocity.sqrMagnitude)*2 - 1;
            reward -= avpenalty*0.125f;
            AddReward(reward); // Small reward each frame for being upright

            _stepSinceFall++;

            Debug.Log("Adding reward: " + reward.ToString() + ".");

            // todo: add target distance penalty (will need special weighting on y axis)

            //float dummyReward = actions.ContinuousActions[3];
            //AddReward(dummyReward);
            //Debug.Log("Adding dummy reward: " + dummyReward.ToString() + ".");
        }
        
        // Called by Sphere game object
        public void OnFall()
        {
            float fallReward = -5f;
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

            SetTarget();

            RecoverToUpright();
        }

        private void SetTarget()
        {
            // choose a new random point to target
            _targetPos = new Vector3(UnityEngine.Random.Range(-100f, 100f), 0, UnityEngine.Random.Range(-100f, 100f));
            // move beans to target for visualization
        }

        private void RecoverToUpright()
        {
            _articulationBody.linearVelocity = Vector3.zero;
            _articulationBody.angularVelocity = Vector3.zero;
            Quaternion newRot = Quaternion.Euler(0, 0, 0);
            _articulationBody.TeleportRoot(_startPos, newRot);
            Debug.Log("Recovered position");
        }

        private void SetJump(bool is_jumping)
        {
            if (is_jumping)
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