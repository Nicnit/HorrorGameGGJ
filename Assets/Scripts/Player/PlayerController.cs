using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float sprintSpeed = 8f;
    [SerializeField] private float jumpForce = 4f;
    [SerializeField] public bool canMove = true;

    [Header("Ground")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundDistance = 0.2f;
    [SerializeField] private LayerMask groundMask;

    [Header("Stamina")]
    [SerializeField] private float maxStamina = 5f;
    [SerializeField] private float degenRate = 3f;
    [SerializeField] private float regenRate = 0.5f;

    [Header("Wall Slide")]
    [SerializeField, Tooltip("How 'wall-like' a surface must be. Lower = more likely to count as wall.")]
    private float wallNormalYThreshold = 0.2f;

    [SerializeField, Tooltip("Small outward nudge to reduce sticky wall contact. 0 disables.")]
    private float wallDepenetrationBias = 0.0f;

    private Rigidbody rb;

    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction sprintAction;

    private bool isGrounded;
    private Vector2 inputDir;
    private bool shouldJump;

    private bool isSprinting;
    private float stamina;
    
    private bool hasWallContact;
    private Vector3 bestWallNormal; 
    private float bestWallOpposition; 

    private float maxStunTime = 3f;
    private float stunTime = 3f;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate; 

        moveAction = InputSystem.actions.FindAction("Move");
        jumpAction = InputSystem.actions.FindAction("Jump");
        sprintAction = InputSystem.actions.FindAction("Sprint");

        stamina = maxStamina;
    }

    private void Update()
    {
        bool stunned = stunTime < maxStunTime;
        if (stunned)
        {
            stunTime += Time.deltaTime;
            return;
        }

        inputDir = moveAction.ReadValue<Vector2>();
        UpdateGround();

        if (jumpAction.WasPerformedThisFrame() && isGrounded)
            shouldJump = true;

        // held sprint
        isSprinting = sprintAction.IsPressed();
    }

    private void FixedUpdate()
    {
        hasWallContact = false;
        bestWallNormal = Vector3.zero;
        bestWallOpposition = 0f;

        Move();
        Jump();
    }

    private void Move()
    {
        if (!canMove) return;
        if (!isGrounded) return;

        Vector3 wishDir = (transform.right * inputDir.x + transform.forward * inputDir.y);

        float inputMagnitude = Mathf.Clamp01(wishDir.magnitude);
        if (wishDir.sqrMagnitude > 1f) wishDir.Normalize();
        else if (wishDir.sqrMagnitude > 0f) wishDir.Normalize();

        float targetSpeed = walkSpeed;

        if (isSprinting && stamina > 0f && inputMagnitude > 0.01f)
        {
            targetSpeed = sprintSpeed;
            stamina = Mathf.Max(0f, stamina - Time.fixedDeltaTime * degenRate);
        }
        else
        {
            stamina = Mathf.Clamp(stamina + regenRate * Time.fixedDeltaTime, 0f, maxStamina);
        }

        Vector3 desiredVelocity = wishDir * (targetSpeed * inputMagnitude);
        
        if (hasWallContact && desiredVelocity.sqrMagnitude > 0.0001f)
        {
            float intoWall = Vector3.Dot(desiredVelocity, bestWallNormal);

            if (intoWall < 0f)
            {
                desiredVelocity = desiredVelocity - bestWallNormal * intoWall;
                
                if (wallDepenetrationBias > 0f)
                    desiredVelocity += bestWallNormal * wallDepenetrationBias;
            }
        }

        rb.linearVelocity = new Vector3(desiredVelocity.x, rb.linearVelocity.y, desiredVelocity.z);
    }

    private void Jump()
    {
        if (!shouldJump) return;

        shouldJump = false;
        
        Vector3 v = rb.linearVelocity;
        if (v.y < 0f) v.y = 0f;
        rb.linearVelocity = v;

        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
    }

    private void UpdateGround()
    {
        isGrounded = Physics.CheckSphere(
            groundCheck.position,
            groundDistance,
            groundMask,
            QueryTriggerInteraction.Ignore
        );
    }

    private void OnCollisionStay(Collision collision)
    {
        Vector3 intendedMove = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        if (intendedMove.sqrMagnitude < 0.0001f)
        {
            return;
        }

        intendedMove.Normalize();

        foreach (var contact in collision.contacts)
        {
            Vector3 n = contact.normal;
            
            if (Mathf.Abs(n.y) >= wallNormalYThreshold)
                continue;
            
            float opposition = -Vector3.Dot(intendedMove, n);

            if (opposition > bestWallOpposition)
            {
                bestWallOpposition = opposition;
                bestWallNormal = n;
                hasWallContact = true;
            }
        }
    }

    public void StunLock(float newStunTime)
    {
        Debug.Log("Stunned");
        
        float stunTimeLeft = maxStunTime - stunTime;
        if (stunTimeLeft < newStunTime)
        {
            maxStunTime = newStunTime;
            stunTime = 0f;
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawWireSphere(groundCheck.position, groundDistance);
    }
#endif
}
