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

    private void Start()
    {
        moveAction = InputSystem.actions.FindAction("Move");
        jumpAction = InputSystem.actions.FindAction("Jump");
        lookAction = InputSystem.actions.FindAction("Look");
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
            pogo.GetComponent<ArticulationBody>().SetDriveTarget(ArticulationDriveAxis.X, 0.5f);
        }
        else
        {
            pogo.GetComponent<ArticulationBody>().SetDriveTarget(ArticulationDriveAxis.X, 0.0f);
        }
    }
}
