using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    public float walkSpeed = 5f;
    public float jumpForce = 5f;
    public float sprintSpeed = 5f;
    public bool canMove = true;

    [Header("Ground")]
    [SerializeField] Transform groundCheck;
    [SerializeField] float groundDistance = 0.2f;
    [SerializeField] LayerMask groundMask;

    private bool isGrounded;
    private Vector2 inputDir;
    private bool shouldJump;

    private Rigidbody rb;
    private InputAction moveAction;
    private InputAction jumpAction;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        moveAction = InputSystem.actions.FindAction("Move");
        jumpAction = InputSystem.actions.FindAction("Jump");
    }

    private void Update()
    {
        
        // Handle player being stunned (Critical nonmovement logics should be before this)
        bool stunned = stunTime < maxStunTime;
        if (stunned)
            // Can't move, jump, etc.
            return;
        else
            stunTime += Time.deltaTime;
        
        
        inputDir = moveAction.ReadValue<Vector2>();

        UpdateGround();

        if (jumpAction.WasPerformedThisFrame() && isGrounded)
        {
            shouldJump = true;
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
        if (!canMove) return;
        
        Vector3 moveDir = transform.right * inputDir.x + transform.forward * inputDir.y;
        Vector3 velocity = moveDir * walkSpeed;
        rb.linearVelocity = new Vector3(velocity.x, rb.linearVelocity.y, velocity.z);
    }

    void UpdateGround()
    {
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask, QueryTriggerInteraction.Ignore);
    }

    private float maxStunTime = 3f;
    private float stunTime = 0;
    public void StunLock(float newStunTime)
    {
        float stunTimeLeft = maxStunTime - stunTime;
        // Keep higher stuntime
        if (stunTimeLeft < newStunTime)
        {
            maxStunTime = newStunTime;
            stunTime = 0;
        }
    }
}
