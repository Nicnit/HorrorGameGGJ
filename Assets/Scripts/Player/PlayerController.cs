using System.Collections.Generic;  
using UnityEngine;  
using UnityEngine.InputSystem;  

[RequireComponent(typeof(Rigidbody))]  
public class PlayerController : MonoBehaviour  
{  
    [Header("Movement")]  
    [SerializeField] private float walkSpeed = 5f;  
    [SerializeField] private float sprintSpeed = 8f;  
    [SerializeField] private float jumpForce = 4f;  
    public bool canMove = true;  
  
    [Header("Ground")]  
    [SerializeField] private Transform groundCheck;  
    [SerializeField] private float groundDistance = 0.2f;  
    [SerializeField] private LayerMask groundMask;  
  
    [Header("Stamina")]  
    [SerializeField] private float maxStamina = 5f;  
    [SerializeField] private float degenRate = 3f;  
    [SerializeField] private float regenRate = 0.5f;  
    [SerializeField] private float trapEffectiveness = 0.5f;  
  
    [Header("Wall Slide")]  
    [SerializeField] private float wallNormalYThreshold = 0.2f;  
    [SerializeField] private float wallDepenetrationBias = 0.0f;  
    
    
    private Rigidbody rb;  
    private InputAction moveAction;  
    private InputAction jumpAction;  
    private InputAction sprintAction;  
  
    private bool isGrounded;  
    private Vector2 inputDir;  
    private bool shouldJump;  
    private bool isSprinting;  
    private float stamina;  
  
    // Wall logic  
    private bool hasWallContact;  
    private Vector3 bestWallNormal;  
    private float bestWallOpposition;  
  
    private float maxStunTime = 3f;  
    private float stunTime = 3f;  
  
    private float pendingYRotation = 0f;   
    private void Start()  
    {        
        rb = GetComponent<Rigidbody>();  
        rb.freezeRotation = true;  
        // Questionable interpolation  
        rb.interpolation = RigidbodyInterpolation.Interpolate;  
  
        var map = InputSystem.actions?.FindActionMap("Player");
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
  
        isSprinting = sprintAction.IsPressed();  
    }  
    // Used in MouseLook
    public void RotateBody(float yRotation)  
    {        
        pendingYRotation = yRotation;  
    }  
    private void FixedUpdate()  
    {
	    // stun  
        bool stunned = stunTime < maxStunTime;  
        if (stunned)  
        {
            rb.linearVelocity -= rb.linearVelocity * (trapEffectiveness * Time.fixedDeltaTime);  
            stunTime += Time.fixedDeltaTime;
            hasWallContact = false; 
            return;  
        }
        
        if (Mathf.Abs(pendingYRotation) > 0.001f)
        {            
            Quaternion deltaRotation = Quaternion.Euler(0f, pendingYRotation, 0f);  
            rb.MoveRotation(rb.rotation * deltaRotation);  
            pendingYRotation = 0f; // Reset after applying  
        }  
        Move();  
        Jump();  
  
        // Reset flags so Move() has access to data from collisionstay       
        hasWallContact = false;  
        bestWallNormal = Vector3.zero;  
        bestWallOpposition = 0f;  
    }  
    private void Move()  
    {        
        if (!canMove || !isGrounded) return;  
  
        Vector3 wishDir = (transform.right * inputDir.x + transform.forward * inputDir.y);  
        // Normalizing
        float inputMagnitude = Mathf.Clamp01(wishDir.magnitude);  
        if (wishDir.sqrMagnitude > 1f) wishDir.Normalize();  
  
        float targetSpeed = walkSpeed;  
  
        if (isSprinting && stamina > 0f && inputMagnitude > 0.01f)  
        {            targetSpeed = sprintSpeed;  
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
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask, QueryTriggerInteraction.Ignore);  
    }  
    private void OnCollisionStay(Collision collision)  
    {        
        // Not moving doesnt require  
        Vector3 intendedMove = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);  
        if (intendedMove.sqrMagnitude < 0.001f) return;  
  
        intendedMove.Normalize();  
  
        foreach (var contact in collision.contacts)  
        {            
            Vector3 n = contact.normal;  
            if (Mathf.Abs(n.y) >= wallNormalYThreshold) continue; //was Treated as wall instead of floor  
  
            float opposition = -Vector3.Dot(intendedMove, n);  
            if (opposition > bestWallOpposition)  
            {                
                bestWallOpposition = opposition;  
                bestWallNormal = n;  
                hasWallContact = true;  
                
            }        
        }    
    }
    
    public bool isOnGround()
    {
        return isGrounded;
    }

    public bool IsInSprint()
    {
        return isSprinting;
    }
    
    // Not sure why this is here tbh
    public int CurrentNote = 0;  
    public List<string> NoteList = new List<string>()  
    {  
        "This is a note to anyone who may find this. I�m trapped. I don�t know how, why, or where I am. For that matter, I must have been taken in my sleep. Why, I don�t know; sorry, not important. I tend to ramble. I�ll be slipping this out via the sewer. If you can trace this back to me, I will be most....",  
        "[Note is torned] ...Twisting, turning, reeling. Bones break, blood curdles, flesh ripped. They�re all around me. How did I get here? Who is here? What is here? Can I hear? Can I see? Nothing. Nothing but the masks. The masks help me see.\r\n\r\nDays and days they try to break me. ME? HA! I�m better than that, right? They stop, but I see. I see it�the fellow who stalks the hallways here at North Folk. He oozes but leaves no trails. My friend, a pound of immovable flesh that keeps getting closer, setting up his tricks and traps. He is a clever one.\r\n\r\nBUT, BUT, BUT... I know his clever little secret. These eyes don�t work, so get new eyes. A new face by becoming unrecognizable. You recognize it? He comes closer now. I�m ready for him. Are you?",  
        "With you and with me, E [the name is burned out] w  June 11th 2005 ",  
        "Yo what up ima do poetry that pretty obous think that bit in bendy with the weird alter thing in chapter 1   Gather unto thee the following artifacts of your gaze To escape the confines of this maze    -Childs doll -A knife -A pair of dice -A bar of gold -The ink of an esteemed man  ",  
        "Northfolk medical center\r\n\r\nName of patient: gordon ramsey \r\n\r\nDate of admission June 11th 2005 \r\n\r\nAdmit Order signed by the honerable judge nick bolton\r\n\r\nLocation of last admittance: First Admitance \r\n\r\nDOB 12/20/1994\r\n\r\nDangerous to self: yes\r\n\r\nDangrous to others: yes \r\n\r\nReason for admittance: On the 6t of march 2005 patchent entered the big yak club in las vagas and shocker he blew all his money long story short wife tried to leave him so he killed em witha knife \r\n\r\nPatchent has so far forsfully rejected all attempts at medication\r\n"  
    };
    
}