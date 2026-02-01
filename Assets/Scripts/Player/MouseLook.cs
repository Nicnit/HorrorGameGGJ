using UnityEngine;
using UnityEngine.InputSystem;

public class MouseLook : MonoBehaviour
{
    [Header("Settings")]
    public float minPitch = -90f;
    public float maxPitch = 90f;
    public float sensitivity = 0.5f;
    
    [Tooltip("Time in seconds to smooth input. Lower = faster.")]
    public float smoothTime = 0.05f; // was smoothspeed
    public bool smooth = true;

    [Header("References")]
    public Transform playerBody;
    private PlayerController playerController; // Reference for pass rotation

    private float pitch = 0f;
    private Vector2 smoothInputVelocity;
    private Vector2 currentInput;

    private InputAction lookAction;

    private void Start()
    {
        LockCursor(true);
        lookAction = InputSystem.actions.FindAction("Look");
        
        if(playerBody) 
            playerController = playerBody.GetComponent<PlayerController>();
    }

    void Update()
    {
        Vector2 rawInput = lookAction.ReadValue<Vector2>();
        Vector2 targetDir = rawInput * sensitivity;

        if (smooth)
        {
            // Using smooth time to fix problem hopefully
            currentInput = Vector2.SmoothDamp(currentInput, targetDir, ref smoothInputVelocity, smoothTime);
            targetDir = currentInput;
        }
        pitch -= targetDir.y;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);

        if (playerController != null)
        {
            playerController.RotateBody(targetDir.x);
        }
        else
        {
            playerBody.Rotate(Vector3.up * targetDir.x);
        }
    }

    public void LockCursor(bool shouldLock)
    {
        Cursor.lockState = shouldLock ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !shouldLock;
    }
}