using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerController : MonoBehaviour
//Monobehaviour -> Base class from which most of our classes will derive from. 
/*You inherit the following functionalities:
1. Your class becomes a component. This is what allows your class to be assosiated with an object. 
2. Gives access to Event Hooking.
3. 
*/
{
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float jumpForce = 12f;
    [SerializeField] private int startingHealth = 100;

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

    [SerializeField] private float dashVelocity = 14f;
    [SerializeField] private float dashCooldown = 1f;
    [SerializeField] private float dashTime = 0.2f;
    private bool canDash = true;
    private bool isDashing = false;
    private Rigidbody2D rb;
    private Animator anim;
    private float currentSpeed;
    private float horizontalInput;
    private bool jumpRequested;
    private bool isGrounded;
    private bool isfacingRight = true;
    public int CurrentHealth { get; private set; } //get-> make the getter public. priv set-> private setter. C# automatically creates the getters/ setters.

    void Start()
    {
        CurrentHealth = startingHealth;
        rb = GetComponent<Rigidbody2D>(); //returns a pointer to the rigidbody2d component. 
        anim = GetComponent<Animator>();
        defaultGravityScale = rb.gravityScale;
    }

    void Update()
    {
        if (isDashing)
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
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
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
        if (Input.GetKeyDown(KeyCode.Z) && isGrounded)
        {
            jumpRequested = true;
        }
        // Through this Update is listening to the keystrokes of the user. 
        /*
        specifically the A/D, <- -> keys. 
        Raw bypasses Unity's smoothing procedure and instantly returns -1, 1,0, for left, right, and when 
        you let go respectively. 
        You dont use Axes for jumping, because as long as the user presses w, or up arrow, the character will
        keep flying. :)
        */
        if (Input.GetKeyDown(KeyCode.C) && canDash)
        {
            /*
            Coroutines are very simple. They are simply functions with a yield statement inside.

A yield statement stops processing at that point and waits until the next frame before continuing from where it was.

Yield WaitForSeconds does the same thing but waits for a certain number of seconds before continuing.

Where do you use them? Anywhere this functionality proves useful. Imagine you need to generate 1000 objects and doing them all in one frame causes a stutter. Simply create a for loop and put a yield inside it and only one object is created per frame.

Maybe you want to regenerate shields, but instead of doing it once per frame, you can create a coroutine with 'Yield WaitForSeconds(0.25)' and it will only happen once every quarter second.

Need to test something is happening but you don't need to test it every frame? Create a coroutine that checks once per second*/
            StartCoroutine(Dash());
        }
        if (rb.linearVelocity.y < 0.1f)
        {
            rb.gravityScale = defaultGravityScale * fallMultiplier;
        }
        else
        {
            rb.gravityScale = defaultGravityScale;
        }

        anim.SetFloat("Speed", Mathf.Abs(horizontalInput));
        anim.SetBool("IsGrounded", isGrounded);
        anim.SetFloat("yVelocity", rb.linearVelocity.y);
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
        if (isDashing)
        {
            return;
        }
        rb.linearVelocity = new Vector2(horizontalInput * moveSpeed, rb.linearVelocity.y);
        if (jumpRequested)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            jumpRequested = false;
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
    private IEnumerator Dash()
    {
        canDash = false;
        isDashing = true;
        anim.SetBool("IsDashing", true);
        rb.gravityScale = 0f;
        rb.linearVelocity = new Vector2(transform.localScale.x * dashVelocity, 0f);
        yield return new WaitForSeconds(dashTime);

        rb.gravityScale = defaultGravityScale;
        isDashing = false;
        anim.SetBool("IsDashing", false);
        yield return new WaitForSeconds(dashCooldown);
        canDash = true;

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
    public void TakeDamage(int damage)
    {
        CurrentHealth -= damage;
        if (CurrentHealth <= 0)
        {
            Die();
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