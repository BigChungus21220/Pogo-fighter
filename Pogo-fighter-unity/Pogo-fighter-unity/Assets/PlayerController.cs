using UnityEngine;
using UnityEngine.InputSystem; 

public class PlayerController : MonoBehaviour
{
    [SerializeField] public GameObject body;
    [SerializeField] public GameObject foot;
    [SerializeField] public GameObject stick;
    [SerializeField] public GameObject pogo;
    InputAction moveAction;
    InputAction jumpAction;
    InputAction lookAction;

    float mouseSensitivity = 1.0f;
    float targetYaw = 0;
    float targetPitch = 0;
    
    //////////////////// REWARD VARIABLES & CONSTANTS ///////////////////////// 


    [SerializeField] private ArticulationBody _articulationBody;

    private Bounds bounds = new Bounds(new Vector3(0,10,0), new Vector3(200,30,200));

    // private const float initialTargetRadius = 30f; // initial area the target can spawn in
    private const float targetRadiusGrowthFactor = 1.0f; // factor to multiply targetRadius by when accuracyThresh is hit
    private const float accuracyThresh = 0.8f; // threshold to increase the target radius
    private const int trialCount = 20; // number of attempts to avg the accuracy over

    private const float planeSize = 100f; // radius of the playable area
    private const float inputVelocityNormalizationFactor = 0.06f; // factor to normalize input velocity
    private const float inputAngularVelocityNormalizationFactor = 0.06f; // factor to normalize input angular velocity

    private const float torqueForce = 30000f; // max force output
    private const float torqueBaseSlope = 2000f; // slope of force curve for model output = 0
    private const float torquePenaltyFactor = -0.00005f; // penalty to apply to normalized torque magnitude
    private const float beanCollectReward = 1f; // reward for reaching target, start at 0.25 to train jumping
    private const float beanCollectRadius = 2f; // radius for a target to be reached
    private const float yMax = 20f; // max y value to not be punished
    private const float yPenalty = -1f; // penalty for exceeding yMax
    private const float yRewardFactor = 0.002f; // reward factor for being higher
    private const float xzPenaltyFactor = -0.0f; // penalty factor for distance to target on xz plane
    private const float xzDistanceFactor = 0.0f; // falloff factor for distance to target on xz plane
    private const float velocityTargetRewardFactor = 0.000001f; // factor for reward for velocity in direction of target
    private const float uprightnessRewardFactor = 0.0005f; // reward for being upright
    private const float fallPenalty = -1f; // penalty for falling over
    private const float angularVelocityPenaltyFactor = -0.0f; // penalty for high angular velocity
    private const float jumpPenalty = -0.0000001f; // penalty for changing jump state
    private const float jumpThresh = 0.0f; // threshold to switch between jump states
    private const float jumpMoveDist = 1f; // amount to move the pogo by when jumping


    private int _num_collected = 0;
    // private int _attempt_count = 0;
    // private float _targetRadius = initialTargetRadius;
    private Vector3 _startPos;
    private int _stepSinceFall = 0;
    // private bool _wasJumping = false;
    private float _prevDist;

    private Vector3 _targetPos;
    public float CurrentReward { get; private set; }
    public float CumulativeReward { get; private set; }

    //////////////////// END - REWARD VARIABLES & CONSTANTS /////////////////////////
    private void Start()
    {
        moveAction = InputSystem.actions.FindAction("Move");
        jumpAction = InputSystem.actions.FindAction("Jump");
        lookAction = InputSystem.actions.FindAction("Look");
        Physics.IgnoreCollision(body.GetComponent<Collider>(), foot.GetComponent<Collider>());
        Physics.IgnoreCollision(body.GetComponent<Collider>(), stick.GetComponent<Collider>());
        Physics.IgnoreCollision(stick.GetComponent<Collider>(), foot.GetComponent<Collider>());
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void OnEnable()
    {
        lookAction.Enable();
        jumpAction.Enable();
        moveAction.Enable();
    }

    void OnDisable()
    {
        lookAction.Disable();
        jumpAction.Disable();
        moveAction.Disable();
    }

    // Update is called once per frame
    void Update()
    {
        Vector2 moveValue = moveAction.ReadValue<Vector2>();
        Debug.DrawLine(body.transform.position, body.transform.position + body.transform.right, Color.red);
        Debug.DrawLine(body.transform.position, body.transform.position + body.transform.forward, Color.blue);
        body.GetComponent<ArticulationBody>().AddTorque(body.transform.right * moveValue.y * 10000, ForceMode.Force);
        body.GetComponent<ArticulationBody>().AddTorque(body.transform.forward * moveValue.x * 10000, ForceMode.Force);

        if (jumpAction.IsPressed())
        {
            pogo.GetComponent<ArticulationBody>().SetDriveTarget(ArticulationDriveAxis.X, 0.0f);
            Debug.Log("Player jump pressed");
        }
        else
        {
            pogo.GetComponent<ArticulationBody>().SetDriveTarget(ArticulationDriveAxis.X, 0.5f);
        }

        Vector2 lookInput = lookAction.ReadValue<Vector2>();

        float mouseX = lookInput.x * mouseSensitivity * Time.deltaTime;
        float mouseY = lookInput.y * mouseSensitivity * Time.deltaTime;

        ArticulationBody stickArticulation = stick.GetComponent<ArticulationBody>();
        targetPitch = Mathf.Clamp(targetPitch - mouseY, -90f, 90f);
        stickArticulation.SetDriveTarget(ArticulationDriveAxis.Y, targetPitch);

        /////////////////////// START - CALCULATE REWARD ///////////////////////

        float reward = 0.0f;
        Vector3 position = _articulationBody.worldCenterOfMass;
        if (!bounds.Contains(position))
        {
            CurrentReward = reward;
            CumulativeReward += reward;
            return;
        }
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
        CurrentReward = reward;
        CumulativeReward += reward;

        Debug.Log("Adding reward: " + reward.ToString() + ".");

        _stepSinceFall++;
        _prevDist = distSqr;
    }
    private float Saturate(float x)
    {
        return Mathf.Clamp(x,-1,1);
    }
    public void OnCollectBeans()
    {
        // add large reward (so it actually wants to collect it, not just hang around it)
        CurrentReward = beanCollectReward;
        CumulativeReward += beanCollectReward;
        //Debug.Log("Beans collected, Adding reward: " + beanCollectReward.ToString() + ".");
        _num_collected++;
    }
    /////////////////////// END - CALCULATE REWARD ///////////////////////
}