using UnityEngine;
using UnityEngine.InputSystem;
public class MouseLook : MonoBehaviour
{
    public float minPitch = -90f;
    public float maxPitch = 90f;
    public float sensitivity = 100f;

    public Rigidbody playerRb;

    private float pitch = 0f;
    private float yawDelta = 0f;

    private InputAction lookAction;

    private void Start()
    {
        LockCursor(true);
        playerRb = transform.parent.GetComponent<Rigidbody>();
        lookAction = InputSystem.actions.FindAction("Look");
    }

    // Update is called once per frame
    void Update()
    {
        Vector2 rawInput = lookAction.ReadValue<Vector2>();
        
        Vector2 targetDir =  rawInput * sensitivity * Time.deltaTime; // could optionally smooth here instead

        pitch -= targetDir.y;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        // apply rotations
        transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        yawDelta = targetDir.x;
    }

    private void FixedUpdate()
    {
        playerRb.MoveRotation(playerRb.rotation * Quaternion.Euler(0f, yawDelta, 0f));
        yawDelta = 0f;
    }

    void LockCursor(bool shouldLock) 
    { 
        Cursor.lockState = shouldLock ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = shouldLock;
    }
}
