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
        [SerializeField] public GameObject body;
        [SerializeField] public GameObject foot;
        [SerializeField] public GameObject beans;

        private const float planeSize = 100f; // radius of the playable area
        private const float inputVelocityNormalizationFactor = 0.06f; // factor to normalize input velocity
        private const float inputAngularVelocityNormalizationFactor = 0.06f; // factor to normalize input angular velocity

        private const float torqueForce = 30000f; // max force output
        private const float torqueBaseSlope = 5000f; // slope of force curve for model output = 0
        private const float torquePenaltyFactor = -0.125f; // penalty to apply to normalized torque magnitude
        private const float beanCollectReward = 1f; // reward for reaching target
        private const float beanCollectRadius = 2f; // radius for a target to be reached
        private const float yMax = 20f; // max y value to not be punished
        private const float yPenalty = -1f; // penalty for exceeding yMax
        private const float yRewardFactor = 0.05f; // reward factor for being higher
        private const float xzRewardFactor = 0.5f; // reward factor for distance to target on xz plane
        private const float xzRewardRadius = 20f; // radius of gaussian of xz reward
        private const float uprightnessRewardFactor = 0.125f; // reward for being upright
        private const float fallPenalty = -1f; // penalty for falling over
        private const float angularVelocityPenaltyFactor = -0.005f; // penalty for high angular velocity
        private const float jumpPenalty = -0.01f; // penalty for changing jump state
        private const float jumpThresh = 0.5f; // threshold to switch between jump states


        private InputAction _jumpAction;
        private Vector3 _startPos;
        private int _stepSinceFall = 0;
        private bool _wasJumping = false;
        private float _prevDist;

        private Vector3 _targetPos;
        
        private void Start()
        {
            _jumpAction = InputSystem.actions.FindAction("Jump");
            _startPos = _articulationBody.transform.position;
            Physics.IgnoreCollision(body.GetComponent<Collider>(), foot.GetComponent<Collider>());
            Physics.IgnoreCollision(body.GetComponent<Collider>(), pogo.GetComponent<Collider>());
            Physics.IgnoreCollision(pogo.GetComponent<Collider>(), foot.GetComponent<Collider>());
        }

        public override void CollectObservations(VectorSensor sensor)
        {
            // Rotational position (four inputs)
            Quaternion rotation = _articulationBody.transform.rotation.normalized;
            sensor.AddObservation(rotation);
            Debug.Log("ModelIO: Input 1: rotation: " + rotation.ToString());

            // Angular velocity (three inputs)
            Vector3 angularVelocity = _articulationBody.angularVelocity;
            sensor.AddObservation(angularVelocity * inputAngularVelocityNormalizationFactor);
            Debug.Log("ModelIO: Input 2: angular velocity: " + angularVelocity.ToString());

            // Position (three inputs)
            Vector3 position = _articulationBody.transform.position;
            sensor.AddObservation(position / planeSize);
            Debug.Log("ModelIO: Input 3: position: " + position.ToString());

            // Target position delta (three inputs)
            Vector3 target = _targetPos - position;
            sensor.AddObservation(target / planeSize);
            Debug.Log("ModelIO: Input 4: targetdelta: " + target.ToString());

            // Translational velocity (three inputs)
            Vector3 linearV = _articulationBody.linearVelocity;
            sensor.AddObservation(linearV * inputVelocityNormalizationFactor);
            Debug.Log("ModelIO: Input 5: linear velocity: " + linearV.ToString());

            // Was jumping last frame
            sensor.AddObservation(_wasJumping);
        }

        public override void OnActionReceived(ActionBuffers actions)
        {

            float reward = 0.0f;

            // weighted to try to help the model jump less frequently
            bool is_jumping = actions.ContinuousActions[3] > jumpThresh;
            Debug.Log("ModelIO: Output 1: jump: " + is_jumping.ToString());
            pogo.GetComponent<ArticulationBody>().SetDriveTarget(ArticulationDriveAxis.X, is_jumping ? 0.5f : 0);

            // add a small penalty for changing jumping state
            if (is_jumping != _wasJumping)
            {
                reward += jumpPenalty;
            }

            Vector3 baseTorque = new Vector3(
                actions.ContinuousActions[0], 
                actions.ContinuousActions[1], 
                actions.ContinuousActions[2]
            );
            
            // Penalty for excess torque
            reward += torquePenaltyFactor * Logistic(baseTorque.sqrMagnitude);

            Vector3 torque = new Vector3(TorqueCurve(baseTorque.x), TorqueCurve(baseTorque.y), TorqueCurve(baseTorque.z));
            Debug.Log("ModelIO: Output 2: torque: " + torque.ToString());
            _articulationBody.AddRelativeTorque(torque);

            Vector3 position = _articulationBody.transform.position;

            // Penalty for angular velocity
            float avpenalty = Logistic(_articulationBody.angularVelocity.sqrMagnitude);
            reward += avpenalty * angularVelocityPenaltyFactor;

            // Rewards / penalties for y value
            reward += (-0.1f <= position.y && position.y <= yMax) ? position.y * yRewardFactor : yPenalty;

            // Reward for uprightness
            float uprightness = Vector3.Dot(_articulationBody.transform.up, Vector3.up);
            reward += uprightness * uprightnessRewardFactor;

            // Reward for distance to target
            float distSqr = (position - _targetPos).sqrMagnitude;
            reward += xzRewardFactor * Mathf.Exp(-distSqr/(xzRewardRadius*xzRewardRadius));

            if (distSqr < beanCollectRadius)
            {
                OnCollectBeans();
            }

            // apply reward
            AddReward(reward);

            Debug.Log("Adding reward: " + reward.ToString() + ".");

            _stepSinceFall++;
            _wasJumping = is_jumping;
            _prevDist = distSqr;
        }

        private float TorqueCurve(float x)
        {
            return Mathf.Clamp(((torqueForce - torqueBaseSlope)*x*x + torqueBaseSlope)*x, -torqueForce, torqueForce);
        }

        // Logistic from 0 to 1
        private float Logistic(float x)
        {
            return 1/(1 + Mathf.Exp(-x));
        }

        // logistic from -1 to 1 on 3 axies
        private Vector3 Logistic3(Vector3 v)
        {
            return new Vector3(2/(1 + Mathf.Exp(-v.x)) - 1, 2/(1 + Mathf.Exp(-v.y)) - 1, 2/(1 + Mathf.Exp(-v.z)) - 1);
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
            Debug.Log("Steps since last fall: " + _stepSinceFall + ".");
            _stepSinceFall = 0;
            Debug.Log("Adding reward for fall: " + fallPenalty.ToString() + ".");
            AddReward(fallPenalty);
            EndEpisode();
        }

        public override void OnEpisodeBegin()
        {
            base.OnEpisodeBegin();

            SetTarget();

            _prevDist = (_articulationBody.transform.position - _targetPos).sqrMagnitude;

            RecoverToUpright();
        }

        private void SetTarget()
        {
            // choose a new random point to target
            _targetPos = new Vector3(UnityEngine.Random.Range(-planeSize, planeSize), 1f, UnityEngine.Random.Range(-planeSize, planeSize));
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
    }
}