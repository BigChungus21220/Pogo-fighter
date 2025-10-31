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
        [SerializeField] public float beanCollectReward = 10.0f;
        [SerializeField] public float yDeltaPenaltyFactor = 0.1f;
        [SerializeField] public float xzDeltaRewardFactor = 0.1f;
        [SerializeField] public float c3 = 0.1f;
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
            float reward = 0.0f;

            bool is_jumping = actions.DiscreteActions[0] > 0;
            Debug.Log("ModelIO: Output 1: jump: " + is_jumping.ToString());
            SetJump(is_jumping);

            // todo: add a small penalty for changing jumping state

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

            // todo: add target distance penalty / reward (will need special weighting on y axis)
            // y penalty = C1*(Logistic(-e^(y - 9) - e^(-10y))*2 - 1) -> absolutely no going to space or the nether
            // xz reward = C2*(1 - Logistic(C3*|a.xz - b.xz|^2))
        }

        private float CalculateDistancePenalty(Vector3 current, Vector3 target)
        {
            Vector3 delta = current - target;
            float yPen = yDeltaPenaltyFactor * (Mathf.Log(-Mathf.Exp(delta.y - 9) - Mathf.Exp(-10 * current.y)) * 2 - 1);
            float xzRew = xzDeltaRewardFactor * (1 - Mathf.Log(c3 * Mathf.Pow(Mathf.Abs(delta.x - delta.x), 2)));
            xzRew += xzDeltaRewardFactor * (1 - Mathf.Log(c3 * Mathf.Pow(Mathf.Abs(delta.z - delta.z), 2)));

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
            _targetPos = new Vector3(UnityEngine.Random.Range(-100f, 100f), 5f, UnityEngine.Random.Range(-100f, 100f));
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