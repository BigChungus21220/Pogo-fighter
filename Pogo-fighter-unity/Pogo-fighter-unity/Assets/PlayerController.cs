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
    }
}
