using UnityEngine;
using UnityEngine.InputSystem;
public class MouseLook : MonoBehaviour
{
    public float minPitch = -90f;
    public float maxPitch = 90f;
    public float sensitivity = 0.08f;
    public float smoothSpeed = 0.125f;
    public bool smooth = true;

    // public Transform neck;
    public Transform playerBody;

    private float pitch = 0f;
    private Vector2 smoothInputVelocity;
    private Vector2 currentInput;

    private InputAction lookAction;

    private void Start()
    {
        LockCursor(true);
        lookAction = InputSystem.actions.FindAction("Look");
    }

  
    void Update()
    {
        Vector2 rawInput = lookAction.ReadValue<Vector2>();
        Vector2 targetDir =  rawInput * sensitivity;
        
        if (smooth) // smoothing
        {
            // Lerp withoit overshootin
            currentInput = Vector2.SmoothDamp(currentInput, targetDir, ref smoothInputVelocity, 1f / smoothSpeed);
            targetDir = currentInput;
        }

        pitch -= targetDir.y;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        // apply rotations
        playerBody.Rotate(Vector3.up * targetDir.x);
        transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    public void LockCursor(bool shouldLock) 
    { 
        Cursor.lockState = shouldLock ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !shouldLock;
    }
}
