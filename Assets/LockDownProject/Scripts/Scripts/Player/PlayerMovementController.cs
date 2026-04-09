using UnityEngine;


public class PlayerMovementController : MonoBehaviour
{
    [SerializeField] private float gravity = 18f;
    [SerializeField] private float groundedGravity = -2f;
    [Header("Height Settings")]
    private float standHeight = 1.8f;
    private float crouchHeight = 1.0f;
    private float proneHeight = 0.1f;


    [Header("Radius Settings")]
    private float defaultRadius = 0.5f;
    private float proneRadius = 0.2f;


    private CharacterController cc;

    private float jumpValue;


    public void Awake()
    {
        cc = GetComponent<CharacterController>();
    }
    public void UpdateMovement(Vector2 moveInput, float moveSpeed, bool isJumped, float h)
    {
        if (cc == null) return;

        Vector3 dir = transform.right * moveInput.x + transform.forward * moveInput.y;

        Vector3 totalDir = dir * moveSpeed;

        if (cc.isGrounded && jumpValue < 0.0f)
        {
            jumpValue = groundedGravity;
        }

        if (isJumped)
        {
            jumpValue = Mathf.Sqrt(2.0f * gravity * h);
        }

        jumpValue -= gravity * Time.deltaTime;

        cc.Move(totalDir * Time.deltaTime + Vector3.up * jumpValue * Time.deltaTime);

    }
    public void UpdateCCHeight(MovementMode mode)
    {
        if (cc == null) return;

        if (mode.crouch)
        {
            cc.height = crouchHeight;
            cc.radius = defaultRadius;
            cc.center = new Vector3(0.0f, crouchHeight * 0.5f, 0.0f);
        }

        else if (mode.prone)
        {
            cc.height = proneHeight;
            cc.radius = proneRadius;
            cc.center = new Vector3(0.0f, proneHeight * 0.5f, 0.0f);
        }

        else
        {
            cc.height = standHeight;
            cc.radius = defaultRadius;
            cc.center = new Vector3(0.0f, standHeight * 0.5f, 0.0f);
        }
    }
    public bool IsGrounded()
    {
        if(cc.isGrounded) return true;
        else return false;
    }

}
