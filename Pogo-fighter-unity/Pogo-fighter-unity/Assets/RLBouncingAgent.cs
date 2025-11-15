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
        [SerializeField] public float torqueForce = 30000f;
        [SerializeField] public float beanCollectReward = 1f;
        [SerializeField] public float yDeltaPenaltyFactor = 1f;
        [SerializeField] public float xzDeltaRewardFactor = 0.05f;
        [SerializeField] public float xzRewardFactor = 0.15f;
        [SerializeField] public float xzRewardRadius = 20f;
        [SerializeField] public float c3 = 0.1f;
        private InputAction _jumpAction;
        private Vector3 _startPos;
        private int _stepSinceFall = 0;
        private bool _wasJumping = false;
        private float _prevDist;

        private Vector3 _targetPos;

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
            // need to normalize everything on [-1,1]

            // Rotational position (four inputs)
            // already normalized
            Quaternion rotation = _articulationBody.transform.rotation;
            sensor.AddObservation(rotation);
            Debug.Log("ModelIO: Input 1: rotation: " + rotation.ToString());

            // Angular velocity (three inputs)
            // requires normalization
            Vector3 angularVelocity = _articulationBody.angularVelocity;
            sensor.AddObservation(Logistic3(angularVelocity/Mathf.PI));
            Debug.Log("ModelIO: Input 2: angular velocity: " + angularVelocity.ToString());

            // Position (three inputs)
            // requires normalization
            Vector3 position = _articulationBody.transform.position;
            sensor.AddObservation(Logistic3(position/100));
            Debug.Log("ModelIO: Input 3: y: " + position.ToString());

            // Target position delta (three inputs)
            // requires normalization
            Vector3 target = _targetPos - position;
            sensor.AddObservation(Logistic3(target/100));
            Debug.Log("ModelIO: Input 3: y: " + target.ToString());

            // Translational velocity (three inputs)
            // requires normalization
            Vector3 linearV = _articulationBody.linearVelocity;
            sensor.AddObservation(Logistic3(linearV));
            Debug.Log("ModelIO: Input 4: linear velocity: " + linearV.ToString());

            // Was jumping last frame
            sensor.AddObservation(_wasJumping);
        }

        public override void OnActionReceived(ActionBuffers actions)
        {

            float reward = 0.0f;

            // weighted to try to help the model jump less frequently
            bool is_jumping = actions.ContinuousActions[3] > 0.5;
            Debug.Log("ModelIO: Output 1: jump: " + is_jumping.ToString());
            SetJump(is_jumping);

            // add a small penalty for changing jumping state
            if (is_jumping != _wasJumping)
            {
                reward -= 0.05f;
            }

            Vector3 torque = new Vector3(
                TorqueCurve(actions.ContinuousActions[0]), 
                TorqueCurve(actions.ContinuousActions[1]), 
                TorqueCurve(actions.ContinuousActions[2])
            );
            
            // Penalty for excess torque
            reward -= 0.25f*(Logistic(torque.sqrMagnitude/torqueForce)*2 - 1);
            Debug.Log("ModelIO: Output 2: torque: " + torque.ToString());

            _articulationBody.AddRelativeTorque(torque);

            // Penalty for angular velocity
            float avpenalty = Logistic(_articulationBody.angularVelocity.sqrMagnitude)*2 - 1;
            reward -= avpenalty*0.125f;

            // Penalty for out of bounds Y
            reward += (0f <= transform.position.y && transform.position.y <= 20f) ? transform.position.y*0.05f : -yDeltaPenaltyFactor;

            // Reward for uprightness
            float uprightness = Vector3.Dot(_articulationBody.transform.up, Vector3.up);
            reward += uprightness*0.5f;

            // reward for change in distance to target
            float dist = (transform.position - _targetPos).sqrMagnitude;
            //float dist_reward = 2*(Logistic(-5f*(dist - _prevDist))*2 - 1);
            float dist_reward = xzRewardFactor*Mathf.Exp(-dist*dist/(xzRewardRadius*xzRewardRadius));
            reward += dist_reward;

            if (dist < 2)
            {
                OnCollectBeans();
            }

            // apply reward
            AddReward(reward);

            Debug.Log("Adding reward: " + reward.ToString() + ".");

            _stepSinceFall++;
            _wasJumping = is_jumping;
            _prevDist = dist;
        }

        private float TorqueCurve(float x)
        {
            const float base_slope = 5f;
            float curve = -base_slope*Mathf.Log(2/(x + 1) - 1);
            return Mathf.Max(Mathf.Min(curve, torqueForce),-torqueForce);
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

            _prevDist = (transform.position - _targetPos).sqrMagnitude;

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
                pogo.GetComponent<ArticulationBody>().SetDriveTarget(ArticulationDriveAxis.X, 0.5f);
            }
            else
            {
                pogo.GetComponent<ArticulationBody>().SetDriveTarget(ArticulationDriveAxis.X, 0.0f);
            }
        }
    }
}