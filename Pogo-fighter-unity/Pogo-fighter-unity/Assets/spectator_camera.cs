using UnityEngine;
using UnityEngine.InputSystem;

public class SpectatorCamera : MonoBehaviour
{
    public float sensitivity = 1f;
    public float slowSpeed = 2f;
    public float normalSpeed = 5f;
    public float sprintSpeed = 10f;
    float currentSpeed;

    void Update()
    {
        if (Mouse.current.rightButton.isPressed)
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
            Movement();
            Rotation();
        }
        else
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
    }

    void Rotation()
    {
        Vector2 mouseDelta = Mouse.current.delta.ReadValue();
        Vector3 rotation = new Vector3(-mouseDelta.y, mouseDelta.x, 0f) * sensitivity;
        transform.Rotate(rotation);
        Vector3 eulerRotation = transform.rotation.eulerAngles;
        transform.rotation = Quaternion.Euler(eulerRotation.x, eulerRotation.y, 0f);
    }

    void Movement()
    {
        float moveX = 0f;
        float moveY = 0f;
        float moveZ = 0f;

        // XZ movement with WASD
        if (Keyboard.current.aKey.isPressed) moveX = -1f;
        if (Keyboard.current.dKey.isPressed) moveX = 1f;
        if (Keyboard.current.wKey.isPressed) moveZ = 1f;
        if (Keyboard.current.sKey.isPressed) moveZ = -1f;

        // Y movement with Space/Ctrl
        if (Keyboard.current.spaceKey.isPressed) moveY = 1f;
        if (Keyboard.current.leftCtrlKey.isPressed) moveY = -1f;

        // Speed modifiers
        if (Keyboard.current.leftShiftKey.isPressed)
            currentSpeed = sprintSpeed;
        else if (Keyboard.current.leftAltKey.isPressed)
            currentSpeed = slowSpeed;
        else
            currentSpeed = normalSpeed;

        // Apply translation (note: Space.Self keeps it relative to camera rotation)
        Vector3 input = new Vector3(moveX, moveY, moveZ).normalized;
        transform.Translate(input * currentSpeed * Time.deltaTime, Space.Self);
    }
}
