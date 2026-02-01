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

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Monster"))
        {
            FindFirstObjectByType<Death>().PlayDeath();
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
    
    // Not sure why this is here tbh
    public int CurrentNote = 0;  
    public List<string> NoteList = new List<string>()
    {
        "This is a note to anyone who may find this. I'm trapped, i dont know how, why, or where I am; must have been taken in my sleep from what I gather, I'm at the old Northfolk Hospital, but it closed down, but everythings still running. I see shadows in the window of my cell. I assume they are the staff, but that's impossible, right? Is this a dream, a nightmare, or something else? I'll be slipping this note out if you see this. Please rescue me.\r\n\r\nFind me soon,\r\nEric Winters\r\n",
        "[note is torn]\r\n…Twisting, turning, reeling, bones break, blood curdles, flesh ripped, they're all around me. Who is here? What can hear? Can I hear? Can I see? Nothing, nothing but the masks; the masks help me see what lies in front of me. I know, I see it, the fellow who stalks the hallways here at Northfolk he oozes but leaves no trails, my friend, a pound of immovable flesh that keeps getting closer. Setting up his tricks and traps, he is a clever one, BUT BUT BUT I know his clever little secret. My eyes don't work, so get new eyes, a new face, a mask by becoming unreconiseable you recognize it but it reconises you\r\n\r\n\r\nWith you and with me,\r\nE [the name is burned out] \r\nJune 11th 2005\r\n",
        "Northfolk Medical Center\r\n\r\nName of patient: Eric Winters\r\n\r\nDate of admission: June 11th 2005 \r\n\r\nAdmit Order signed by the honerable judge Nick Bolton\r\n\r\nLocation of last admittance: First Admittance \r\n\r\nDOB 12/20/1994\r\n\r\nDangerous to self: yes\r\n\r\nDangrous to others: yes \r\n\r\nReason for admittance: On the 6t of March 2005 patient entered the Big Yak Club in Las Vegas nevada at 5:00 pm the casino had registered a loss of 50,000 dollars at which point the patient returned to his room at 5:30 poleice received complaints of screams at patients hotel room police arrive a 5:40 pm and breach hotel room door before a shotgun mounted door trap is set off killing one of the responding officers officers report that the whole room was riddled with traps at 4:00 am patient was successfully captured \r\n\r\nThe patient has so far forcefully rejected all attempts at medication\r\n",
    };

}
