using System;
using Unity.Mathematics;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using UnityEngine.Assertions.Must;
using UnityEngine.InputSystem;
using static UnityEngine.InputSystem.LowLevel.InputStateHistory;

namespace Assets
{
    public class RLBouncingAgent : Agent
    {
        [SerializeField] private ArticulationBody _articulationBody;
        [SerializeField] public GameObject pogo;
        [SerializeField] public GameObject beans;
        [SerializeField] public int torqueForce;
        [SerializeField] public float beanCollectReward = 50.0f;
        [SerializeField] public float yDeltaPenaltyFactor = 0.1f;
        [SerializeField] public float xzDeltaRewardFactor = 0.25f;
        [SerializeField] public float xzRewardRadius = 20f;
        [SerializeField] public float c3 = 0.1f;
        private InputAction _jumpAction;
        private Vector3 _startPos;
        private int _stepSinceFall = 0;
        private bool was_jumping = false;

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
            float reward = 0.0f;

            bool is_jumping = actions.DiscreteActions[0] > 0;
            Debug.Log("ModelIO: Output 1: jump: " + is_jumping.ToString());
            SetJump(is_jumping);

            // add a small penalty for changing jumping state
            if (is_jumping != was_jumping)
            {
                reward -= 0.05f;
            }

            Vector3 torque = new Vector3(actions.ContinuousActions[0], actions.ContinuousActions[1], actions.ContinuousActions[2]) * torqueForce;
            
            // Penalty for excess torque
            reward -= Logistic(torque.sqrMagnitude/torqueForce)*2 - 1;
            Debug.Log("ModelIO: Output 2: torque: " + torque.ToString());

            _articulationBody.AddRelativeTorque(torque);

            // Penalty for angular velocity
            float avpenalty = Logistic(_articulationBody.angularVelocity.sqrMagnitude)*2 - 1;
            reward -= avpenalty*0.125f;

            // Reward for uprightness
            float uprightness = Vector3.Dot(_articulationBody.transform.up, Vector3.up);
            reward += uprightness*2;

            // reward for position w respect to target
            reward += CalculateDistanceReward(transform.position, _targetPos);

            // apply reward
            AddReward(reward);

            Debug.Log("Adding reward: " + reward.ToString() + ".");

            _stepSinceFall++;
            was_jumping = is_jumping;
        }

        private float CalculateDistanceReward(Vector3 current, Vector3 target)
        {
            float yPen = (0f <= current.y && current.y < 12f) ? 0 : -yDeltaPenaltyFactor;
            // 2d gaussian
            float xz2 = Vector3.Scale(current - target, new Vector3(1,0,1)).sqrMagnitude;
            float xzRew = xzDeltaRewardFactor*Mathf.Exp(-xz2/(xzRewardRadius*xzRewardRadius));

            return xzRew + yPen;
        }


        public void OnCollectBeans()
        {
            // add large reward (so it actually wants to collect it, not just hang around it)
            SetTarget();
            AddReward(beanCollectReward);
            Debug.Log("Adding reward: " + beanCollectReward.ToString() + ".");
        }
        
        // Called by Sphere game object
        public void OnFall()
        {
            float fallReward = -5f;
            Debug.Log("Steps since last fall: " + _stepSinceFall + ".");
            _stepSinceFall = 0;
            Debug.Log("Adding reward for fall: " + fallReward.ToString() + ".");
            AddReward(fallReward);
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
            _targetPos = new Vector3(UnityEngine.Random.Range(-100f, 100f), 1f, UnityEngine.Random.Range(-100f, 100f));
            // move beans to target for visualization
            beans.transform.position = _targetPos;
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