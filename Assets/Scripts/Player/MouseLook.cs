using UnityEngine;
using UnityEngine.InputSystem;
public class MouseLook : MonoBehaviour
{
    public float minPitch = -90f;
    public float maxPitch = 90f;
    public float sensitivity = 0.08f;

    // public Transform neck;
    public Transform playerBody;

    private float pitch = 0f;

    private InputAction lookAction;

    private void Start()
    {
        LockCursor(true);
        lookAction = InputSystem.actions.FindAction("Look");
    }

  
    void LateUpdate()
    {
        Vector2 rawInput = lookAction.ReadValue<Vector2>();
        Vector2 targetDir =  rawInput * sensitivity; // could optionally smooth here instead

        pitch -= targetDir.y;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        // apply rotations
        playerBody.Rotate(Vector3.up * targetDir.x);
        transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    void LockCursor(bool shouldLock) 
    { 
        Cursor.lockState = shouldLock ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !shouldLock;
    }
}
