using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    public float walkSpeed = 5f;
    public float jumpForce = 4f;
    public float sprintSpeed = 8f;
    public bool canMove = true;

    [Header("Ground")]
    [SerializeField] Transform groundCheck;
    [SerializeField] float groundDistance = 0.2f;
    [SerializeField] LayerMask groundMask;

    private bool isGrounded;
    private Vector2 inputDir;
    private bool shouldJump;

    private bool isSprinting;
    private float stamina = 0f;
    private float maxStamina = 5f;
    [SerializeField] float degenRate = 3f;
    [SerializeField] float regenRate = 0.5f;

    private Rigidbody rb;
    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction sprintAction;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        moveAction = InputSystem.actions.FindAction("Move");
        jumpAction = InputSystem.actions.FindAction("Jump");
        sprintAction = InputSystem.actions.FindAction("Sprint");
    }

    private void Update()
    {
        
        // Handle player being stunned (Critical nonmovement logics should be before this)
        if (stunTime < maxStunTime)
        {
            // Can't move, jump, etc.
            stunTime += Time.deltaTime;
            return;
        }


        inputDir = moveAction.ReadValue<Vector2>();

        UpdateGround();

        if (jumpAction.WasPerformedThisFrame() && isGrounded)
        {
            shouldJump = true;
        }
        else if (sprintAction.WasPressedThisFrame())
        {
            isSprinting = true;
        }
        else if (sprintAction.WasReleasedThisFrame())
        {
            isSprinting = false;
        }
    }

    void FixedUpdate()
    {
        // movement in physics call
        Move();
        Jump();
    }

    void Jump() 
    {
        if (shouldJump)
        {
            shouldJump = false;
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            Debug.Log("Jumping" + rb.linearVelocity.y);
        }
    }

    void Move()
    {
        if (!canMove || !isGrounded) return;
        
        Vector3 moveDir = transform.right * inputDir.x + transform.forward * inputDir.y;
        Vector3 velocity;
        if (isSprinting)
        {
            velocity = moveDir * sprintSpeed;
            stamina = Mathf.Max(0f, stamina - Time.fixedDeltaTime * degenRate);
            if (stamina <= 0f)
            {
                isSprinting = false;
            }
        } else 
        { 
            velocity = moveDir * walkSpeed;
            if (stamina < maxStamina)
            {
                stamina = Mathf.Clamp(stamina + regenRate * Time.fixedDeltaTime, 0f, maxStamina);
            }
        }
        rb.linearVelocity = new Vector3(velocity.x, rb.linearVelocity.y, velocity.z);
        // Debug.Log(stamina + ", " + isSprinting);
    }

    void UpdateGround()
    {
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask, QueryTriggerInteraction.Ignore);
    }

    private float maxStunTime = 3f;
    private float stunTime = 3f;
    public void StunLock(float newStunTime)
    {
        Debug.Log("Stunned");
        
        float stunTimeLeft = maxStunTime - stunTime;
        // Keep higher stuntime
        if (stunTimeLeft < newStunTime)
        {
            maxStunTime = newStunTime;
            stunTime = 0;
        }
    }
}
