using NUnit.Framework;
using System.Collections.Generic;
using System.Net.Sockets;
using Unity.VisualScripting.Antlr3.Runtime.Tree;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using static Unity.Collections.Unicode;
using static UnityEditor.FilePathAttribute;
using static UnityEngine.ParticleSystem;
using UnityEngine.InputSystem.HID;

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
    [SerializeField] private float trapEffectiveness = 0.5f;    // 1 is immediate stopping 

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
    public int CurrentNote = 0;
    public List<string> NoteList = new List<string>()
    {
        "This is a note to anyone who may find this. I’m trapped. I don’t know how, why, or where I am. For that matter, I must have been taken in my sleep. Why, I don’t know; sorry, not important. I tend to ramble. I’ll be slipping this out via the sewer. If you can trace this back to me, I will be most....",
        "[Note is torned] ...Twisting, turning, reeling. Bones break, blood curdles, flesh ripped. They’re all around me. How did I get here? Who is here? What is here? Can I hear? Can I see? Nothing. Nothing but the masks. The masks help me see.\r\n\r\nDays and days they try to break me. ME? HA! I’m better than that, right? They stop, but I see. I see it—the fellow who stalks the hallways here at North Folk. He oozes but leaves no trails. My friend, a pound of immovable flesh that keeps getting closer, setting up his tricks and traps. He is a clever one.\r\n\r\nBUT, BUT, BUT... I know his clever little secret. These eyes don’t work, so get new eyes. A new face by becoming unrecognizable. You recognize it? He comes closer now. I’m ready for him. Are you?",
        "With you and with me, E [the name is burned out] w  June 11th 2005 ",
        "Yo what up ima do poetry that pretty obous think that bit in bendy with the weird alter thing in chapter 1   Gather unto thee the following artifacts of your gaze To escape the confines of this maze    -Childs doll -A knife -A pair of dice -A bar of gold -The ink of an esteemed man  ",
        "Northfolk medical center\r\n\r\nName of patient: gordon ramsey \r\n\r\nDate of admission June 11th 2005 \r\n\r\nAdmit Order signed by the honerable judge nick bolton\r\n\r\nLocation of last admittance: First Admitance \r\n\r\nDOB 12/20/1994\r\n\r\nDangerous to self: yes\r\n\r\nDangrous to others: yes \r\n\r\nReason for admittance: On the 6t of march 2005 patchent entered the big yak club in las vagas and shocker he blew all his money long story short wife tried to leave him so he killed em witha knife \r\n\r\nPatchent has so far forsfully rejected all attempts at medication\r\n"
    };


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

        bool stunned = stunTime < maxStunTime;
        if (stunned)
        {
            // Restrict / Disable movement not just input
            rb.linearVelocity -= rb.linearVelocity * (trapEffectiveness * Time.fixedDeltaTime);
            stunTime += Time.fixedDeltaTime;
            return;
        }
        
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

        AudioManager.Instance.PlayerIsMoving = rb.linearVelocity.magnitude > 1f;
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

    public bool isOnGround()
    {
        return isGrounded;
    }

    public bool isInSprint()
    {
        return isSprinting;
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
