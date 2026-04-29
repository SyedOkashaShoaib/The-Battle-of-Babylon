using System.Collections;
using System.Runtime.CompilerServices;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerController : MonoBehaviour
//Monobehaviour -> Base class from which most of our classes will derive from. 
/*You inherit the following functionalities:
1. Your class becomes a component. This is what allows your class to be assosiated with an object. 
2. Gives access to Event Hooking.
3. a bunch more stuff i didnt understand :)
*/
{

    [SerializeField] private float doubleJumpForce = 10f; 
    [SerializeField] private bool canDoubleJump = false;
    private bool doubleJumpRequested = false;
    [SerializeField] public bool isAttacking = false;


    [SerializeField] private float slideVelocity = 12f;
    [SerializeField] private float slideTime = 0.4f;
    [SerializeField] private bool isSliding = false;
    [SerializeField] private bool canSlide = true;
    [SerializeField] private float slideCoolDown = 0.5f;
    private BoxCollider2D boxColl;
    [SerializeField] private Vector2 defaultBoxColliderSize;
    [SerializeField] private Vector2 defaultBoxColliderOffset; //distance from knights center to the box's center.
    [SerializeField] private Vector2 slideColliderSize = new Vector2(2.292646f, 0.7163899f);
    [SerializeField] private Vector2 slideColliderOffset = new Vector2(0.7594995f, -1.583324f);
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float jumpForce = 12f;
    [SerializeField] private float startingHealth = 100f;

    [SerializeField] private Transform groundCheck;
    /*Transform is a component that every game object posseses and is undeletable. It holds the following attributes:
    1. Position (x,y) -> This is used to check whether the user's child object (his "feet") are actually touching the ground
    2. Rotation
    3. Scale*/
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] private LayerMask groundLayer;
    /*
    LayerMask is a 32-bit integer. We have assigned the 6th slot in this 32-bit integer the ground layer. 
    during every frame rendering, the engine checks whether the players child object is touching the ground layer(we configure this). 
    */
    [SerializeField] private float fallMultiplier = 2.5f;
    /* Issue: As the player comes down after jumping, it comes down as if it is as light as a feather.
    Reason : jumps at a force of 15 units, gravity pulls him down at 9.8 something m/s, it takes him around 1.5 sec
    for the gravity to cancel the momentum.
    Fix: as the character begins his descent, increase the pull of gravity on the character by a factor of 2.5 (9.8 x 2.5f), so
    that he comes down fast. */
    [SerializeField] private float defaultGravityScale;




    [SerializeField] private float jumpBufferTime = 0.2f;
    private float jumpBufferCounter;

    [SerializeField] private float coyoteTime = 0.2f;
    private float coyoteTimeCounter = 0;

    private Rigidbody2D rb;
    private Animator anim;
    private float currentSpeed;
    private float horizontalInput;
    private bool jumpRequested;
    public bool isGrounded;
    public bool isfacingRight = true;
    public float CurrentHealth { get; private set; } //get-> make the getter public. priv set-> private setter. C# automatically creates the getters/ setters.

    void Start()
    {
        CurrentHealth = startingHealth;
        rb = GetComponent<Rigidbody2D>(); //returns a pointer to the rigidbody2d component. 
        anim = GetComponent<Animator>();
        defaultGravityScale = rb.gravityScale;
        boxColl = GetComponent<BoxCollider2D>();
        defaultBoxColliderSize = boxColl.size;
        defaultBoxColliderOffset = boxColl.offset;
    }

    void Update()
    {
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        anim.SetFloat("Speed", Mathf.Abs(horizontalInput));
        anim.SetBool("IsGrounded", isGrounded);
        anim.SetFloat("yVelocity", rb.linearVelocity.y);
        if (isSliding || isAttacking)
        {
            return;
        }
        //update is an Event Function that comes from MonoBehaviour. 
        /*
        we dont use override for Event Functions even though we are technically overriding the Update
        method. 
        Update() -> Depends on the frame rate of your computer: 144 FPS runs 144 times/sec. 
        since it depends on the Frame rate, you do not add physics calculation in Update. Because
        the computer with faster frame rate will calculate the physics faster as compared to one
        with slower frame rate. 
        */

        /*
        groundCheck.position -> You pass the coordinates.
        groundCheckRadius -> You pass how large the child object is
        groundLayer -> You pass this so that the engine checks whether the child object overlaps with the groundlayer, and does 
        not check against other layers.*/
        horizontalInput = Input.GetAxisRaw("Horizontal");
        if (horizontalInput > 0f && !isfacingRight)
        {
            Flip(); //moving right but not facing right?? FLIIIPPPP
        }
        else if (horizontalInput < 0f && isfacingRight)
        {
            Flip(); //same logic
        }
        if (isGrounded)
        {
            coyoteTimeCounter = coyoteTime;
            canDoubleJump = true;
            if (!jumpRequested && rb.linearVelocity.y <= 0.1f)
            {
                anim.ResetTrigger("Jump"); //idk what this line does lol. 
            }
        }
        else
        {
            coyoteTimeCounter -= Time.deltaTime;
        }
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.W))
        {
            jumpBufferCounter = jumpBufferTime;


        }
        else
        {
            jumpBufferCounter -= Time.deltaTime;
        }
        if (jumpBufferCounter > 0f && coyoteTimeCounter > 0f)
        {
            jumpRequested = true;
            anim.SetTrigger("Jump");
            jumpBufferCounter = 0;
            coyoteTimeCounter = 0;
        }
        else if ((Input.GetKeyDown(KeyCode.W) ||Input.GetKeyDown(KeyCode.Space) ) && coyoteTimeCounter <= 0f && canDoubleJump)
        {

            canDoubleJump = false; // Spend the bullet
            jumpBufferCounter = 0; // Kill the buffer so it doesn't bleed into a ground jump later
            
            doubleJumpRequested = true;
            
            anim.SetTrigger("DoubleJump"); // Send the signal to the Animator
        }
        // Through this Update is listening to the keystrokes of the user. 
        /*
        specifically the A/D, <- -> keys. 
        Raw bypasses Unity's smoothing procedure and instantly returns -1, 1,0, for left, right, and when 
        you let go respectively. 
        You dont use Axes for jumping, because as long as the user presses w, or up arrow, the character will
        keep flying. :)
        */

        if (Input.GetKeyDown(KeyCode.LeftShift) && canSlide && isGrounded)
        {
            // Debug.Log("plz work..");
            StartCoroutine(Slide());
        }

        if (rb.linearVelocity.y < 0.1f)
        {
            rb.gravityScale = defaultGravityScale * fallMultiplier;
        }
        else
        {
            rb.gravityScale = defaultGravityScale;
        }

        // anim.SetFloat("Speed", Mathf.Abs(horizontalInput));
        // anim.SetBool("IsGrounded", isGrounded);
        // anim.SetFloat("yVelocity", rb.linearVelocity.y);
        /*
        the animation occurs using a FSM. Each state -> One animation (Ex running)
        transitions -> conditions
        example:
         The animator component has a hash map. 
         anim.SetFloat("Speed", Mathf.Abs(horizontalInput));
         it goes to the key "Speed" and sets the abs value of horizontalInout. At each frame, it checks a certain conditon like speed > 1
         if speed is greater than 1 then set state idle to state run. */
    }
    void FixedUpdate()
    {
        if (isSliding || isAttacking)
        {
            return;
        }

        rb.linearVelocity = new Vector2(horizontalInput * moveSpeed, rb.linearVelocity.y);
        if (jumpRequested)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x *1.2f, jumpForce);
            jumpRequested = false;
        }
        if (doubleJumpRequested)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, doubleJumpForce);
            doubleJumpRequested = false;
        }
    }
    private void Flip()
    {
        isfacingRight = !isfacingRight;
        Vector3 localScale = transform.localScale;
        /*transform is a pointer to Transform inherited from monobehavoir class. 
         vector3 -> x,y,z. 
          imagine a  character inside a moving car.
          the character's global scale -> constantly changing
          the character's local scale which wold be relative to the car, will stay constant. */
        localScale.x *= -1f;
        transform.localScale = localScale;
        /* you essentially flipped the x-axis: 1 became -1 and vice versa*/

    }
    private IEnumerator Slide()
    {
        canSlide = false;
        isSliding = true;
        anim.SetBool("IsSliding", true);

        boxColl.size = slideColliderSize;
        boxColl.offset = slideColliderOffset;
        rb.linearVelocity = new Vector2(transform.localScale.x * slideVelocity, rb.linearVelocity.y);

        yield return new WaitForSeconds(slideTime);
        boxColl.size = defaultBoxColliderSize;
        boxColl.offset = defaultBoxColliderOffset;

        isSliding = false;
        anim.SetBool("IsSliding", false);
        yield return new WaitForSeconds(slideCoolDown);
        canSlide = true;
    }

    void OnDrawGizmos()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
        //This is just for debugging purposes. it will be scrapped. Hopefully...
    }

    public void TakeDamage(float damage)
    {
        CurrentHealth -= damage;
        Debug.Log("Damege Taken. Current Health : " + CurrentHealth);
        if (CurrentHealth <= 0f)
        {
            // Handle Death
        }
    }

    private void Die()
    {
        //die die die.
    }


}
/*How animation fo sprites works in unity:
Sprite Renderer: Naturally "dumb" -? It renders a single image on the canvas. 
Animator component: AFter every frame, it shoves a new image inside the sprite renderer and the renderer then draws that image.
Animator Component -> new image every frame -> Sprite Renderer -> Renders it on the canvas. */