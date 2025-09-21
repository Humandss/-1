using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;


public class PlayerMovementController : MonoBehaviour
{
    [SerializeField] private float gravity = 18f;

    private CharacterController cc;
    private float jumpValue;
    
    public void Awake()
    {
        cc = GetComponent<CharacterController>();
        
    }
    public void UpdateMovement(Vector2 moveInput, float moveSpeed, bool isJumped, float h)
    {
        
        Vector3 dir = transform.right * moveInput.x + transform.forward * moveInput.y;

        Vector3 totalDir = dir * moveSpeed;

        if (isJumped && cc.isGrounded) jumpValue = Mathf.Sqrt(2.0f * gravity * h);

        jumpValue += (-gravity) * Time.deltaTime;

        cc.Move(totalDir * Time.deltaTime + Vector3.up * jumpValue * Time.deltaTime);

    }
    

}
