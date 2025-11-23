using System;
using Unity.Mathematics;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

namespace Assets
{
    public class RLBouncingAgent : Agent
    {
        [SerializeField] private ArticulationBody _articulationBody;
        [SerializeField] public GameObject pogo;
        [SerializeField] public GameObject body;
        [SerializeField] public GameObject foot;
        [SerializeField] public GameObject beans;

        private Bounds bounds = new Bounds(new Vector3(0,10,0), new Vector3(200,30,200));

        private const float initialTargetRadius = 16f; // initial area the target can spawn in
        private const float targetRadiusGrowthFactor = 1.2f; // factor to multiply targetRadius by when accuracyThresh is hit
        private const float accuracyThresh = 0.8f; // threshold to increase the target radius
        private const int trialCount = 20; // number of attempts to avg the accuracy over

        private const float planeSize = 100f; // radius of the playable area
        private const float inputVelocityNormalizationFactor = 0.06f; // factor to normalize input velocity
        private const float inputAngularVelocityNormalizationFactor = 0.06f; // factor to normalize input angular velocity

        private const float torqueForce = 30000f; // max force output
        private const float torqueBaseSlope = 2000f; // slope of force curve for model output = 0
        private const float torquePenaltyFactor = -0.0005f; // penalty to apply to normalized torque magnitude
        private const float beanCollectReward = 5f; // reward for reaching target, start at 0.25 to train jumping
        private const float beanCollectRadius = 1f; // radius for a target to be reached
        private const float yMax = 20f; // max y value to not be punished
        private const float yPenalty = -1f; // penalty for exceeding yMax
        private const float yRewardFactor = 0.0001f; // reward factor for being higher
        private const float xzPenaltyFactor = -0.0001f; // penalty factor for distance to target on xz plane
        private const float xzDistanceFactor = 0.0f; // falloff factor for distance to target on xz plane
        private const float velocityTargetRewardFactor = 0.0f; // factor for reward for velocity in direction of target
        private const float uprightnessRewardFactor = 0.005f; // reward for being upright
        private const float fallPenalty = -1f; // penalty for falling over
        private const float angularVelocityPenaltyFactor = -0.0f; // penalty for high angular velocity
        private const float jumpPenalty = -0.000001f; // penalty for changing jump state
        private const float jumpThresh = 0.0f; // threshold to switch between jump states
        private const float jumpMoveDist = 1f; // amount to move the pogo by when jumping


        private int _num_collected = 0;
        private int _attempt_count = 0;
        private float _targetRadius = initialTargetRadius;
        private Vector3 _startPos;
        private int _stepSinceFall = 0;
        private bool _wasJumping = false;
        private float _prevDist;

        private Vector3 _targetPos;
        
        private void Start()
        {
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
            //Debug.Log("ModelIO: Input 1: rotation: " + rotation.ToString());

            // Angular velocity (three inputs)
            Vector3 angularVelocity = _articulationBody.angularVelocity;
            sensor.AddObservation(angularVelocity * inputAngularVelocityNormalizationFactor);
            //Debug.Log("ModelIO: Input 2: angular velocity: " + angularVelocity.ToString());

            // Position (three inputs)
            Vector3 position = _articulationBody.worldCenterOfMass;
            sensor.AddObservation(position / planeSize);
            //Debug.Log("ModelIO: Input 3: position: " + position.ToString());

            // Target position delta (three inputs)
            Vector3 target = _targetPos - position;
            sensor.AddObservation(target / planeSize);
            //Debug.Log("ModelIO: Input 4: targetdelta: " + target.ToString());

            // Translational velocity (three inputs)
            Vector3 linearV = _articulationBody.linearVelocity;
            sensor.AddObservation(linearV * inputVelocityNormalizationFactor);
            //Debug.Log("ModelIO: Input 5: linear velocity: " + linearV.ToString());

            // Was jumping last frame
            sensor.AddObservation(_wasJumping);
        }

        public override void OnActionReceived(ActionBuffers actions)
        {

            float reward = 0.0f;

            Vector3 position = _articulationBody.worldCenterOfMass;

            if (!bounds.Contains(position))
            {
                AddReward(-1);
                EndEpisode();
                return;
            }

            // weighted to try to help the model jump less frequently
            bool is_jumping = actions.ContinuousActions[3] > jumpThresh;
            //Debug.Log("ModelIO: Output 1: jump: " + is_jumping.ToString());
            pogo.GetComponent<ArticulationBody>().SetDriveTarget(ArticulationDriveAxis.X, is_jumping ? jumpMoveDist : 0);

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
            reward += Saturate(torquePenaltyFactor * baseTorque.sqrMagnitude);

            Vector3 torque = new Vector3(TorqueCurve(baseTorque.x), TorqueCurve(baseTorque.y), TorqueCurve(baseTorque.z));
            //Debug.Log("ModelIO: Output 2: torque: " + torque.ToString());
            _articulationBody.AddRelativeTorque(torque);

            // Penalty for angular velocity
            reward += Saturate(_articulationBody.angularVelocity.sqrMagnitude * angularVelocityPenaltyFactor);

            // Rewards / penalties for y value
            reward += (position.y <= yMax) ? Saturate(position.y * yRewardFactor) : yPenalty;

            // Reward for uprightness
            float uprightness = Vector3.Dot(_articulationBody.transform.up, Vector3.up);
            reward += Mathf.Min(uprightness,0.7f) * uprightnessRewardFactor;

            Vector3 not_up = new Vector3(1,0,1);

            // Reward for distance to target
            Vector3 targetdelta = Vector3.Scale(_targetPos, not_up) - Vector3.Scale(position, not_up);
            float distSqr = targetdelta.sqrMagnitude;
            reward += Saturate(xzPenaltyFactor * distSqr);

            // Reward for velocity in target direction
            Vector3 targetDir = targetdelta.normalized;
            Vector3 vel = Vector3.Scale(_articulationBody.linearVelocity, not_up);
            reward += Saturate(Vector3.Dot(targetDir, vel)*velocityTargetRewardFactor);

            if (distSqr < beanCollectRadius*beanCollectRadius)
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

        private float Saturate(float x)
        {
            return Mathf.Clamp(x,-1,1);
        }

        private float TorqueCurve(float x)
        {
            return Mathf.Clamp(((torqueForce - torqueBaseSlope)*x*x + torqueBaseSlope)*x, -torqueForce, torqueForce);
        }

        public void OnCollectBeans()
        {
            // add large reward (so it actually wants to collect it, not just hang around it)
            SetTarget();
            AddReward(beanCollectReward);
            //Debug.Log("Beans collected, Adding reward: " + beanCollectReward.ToString() + ".");
            _num_collected++;
        }
        
        // Called by Sphere game object
        public void OnFall()
        {
            //Debug.Log("Steps since last fall: " + _stepSinceFall + ".");
            _stepSinceFall = 0;
            //Debug.Log("Adding reward for fall: " + fallPenalty.ToString() + ".");
            AddReward(fallPenalty);
            EndEpisode();
        }

        public override void OnEpisodeBegin()
        {
            base.OnEpisodeBegin();

            SetTarget();

            _prevDist = (_articulationBody.worldCenterOfMass - _targetPos).sqrMagnitude;

            RecoverToUpright();

            _attempt_count++;
            //Debug.Log(_attempt_count);
            if (_attempt_count >= trialCount)
            {
                //Debug.Log("Collection accuracy: " + _num_collected/((float)_attempt_count) + ".");
                if (_num_collected/((float)_attempt_count) > accuracyThresh)
                {
                    Debug.Log("Increased target radius");
                    _targetRadius *= targetRadiusGrowthFactor;
                }
                _attempt_count = 0;
                _num_collected = 0;
            }
        }

        private void SetTarget()
        {
            // choose a new random point to target
            _targetPos = new Vector3(UnityEngine.Random.Range(-_targetRadius, _targetRadius), 1f, UnityEngine.Random.Range(-_targetRadius, _targetRadius));
            // move beans to target for visualization
            beans.transform.position = _targetPos;
        }

        private void RecoverToUpright()
        {
            _articulationBody.linearVelocity = Vector3.zero;
            _articulationBody.angularVelocity = Vector3.zero;
            Quaternion newRot = Quaternion.Euler(0, 0, 0);
            _articulationBody.TeleportRoot(_startPos, newRot);
            //Debug.Log("Recovered position");
        }
    }
}