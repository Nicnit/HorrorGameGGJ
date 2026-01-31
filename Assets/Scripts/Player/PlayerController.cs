using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    public float walkSpeed = 5f;
    public float jumpForce = 5f;

    public float sprintSpeed = 5f;

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
        Vector3 moveDir = transform.right * inputDir.x + transform.forward * inputDir.y;
        Vector3 velocity = moveDir * walkSpeed;
        rb.linearVelocity = new Vector3(velocity.x, rb.linearVelocity.y, velocity.z);
    }

    void UpdateGround()
    {
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask, QueryTriggerInteraction.Ignore);
    }
}
