using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
public class PlayerInputController : MonoBehaviour
{ 
    public Vector2 Move { get; private set; }
    public Vector2 Look { get; private set; }
    public bool Jump { get; private set; }
    public bool Sprint { get; private set; }
    public bool TacticalSprint { get; private set; }
    public bool Crouch { get; private set; }
    public bool Prone { get; private set; }
    public bool FreeLook { get; private set; }
    public bool Fire { get; private set; }
    public bool Aim { get; private set; }
    public bool Reload { get; private set; }
    
    public void OnMove(InputValue value) => Move = value.Get<Vector2>();
    public void OnLook(InputValue value)=> Look = value.Get<Vector2>();
 
    public void OnJump(InputValue value)
    {
        if (value.isPressed)
        {
            Jump = true;
        }
    }
    
    public void OnSprint(InputValue value) => Sprint=value.isPressed;

    public void OnTacticalSprint(InputValue value) => TacticalSprint = value.isPressed;

    public void OnCrouch(InputValue value)
    {
        Crouch = value.isPressed;
        Prone = false;
    }
    public void OnProne(InputValue value)
    {
        if (value.isPressed)
        {
            Prone = !Prone;

        }

    }
    public void OnFreeLook(InputValue value)
    {
        if(value.isPressed)
        {
            FreeLook = !FreeLook;
        }
        
    }

    public void OnFire(InputValue value)
    {
        if (value.isPressed)
        {
            Fire = true;
        }

    }
   
    public void OnAim(InputValue value)=>Aim = value.isPressed;
    public void OnReload(InputValue value)
    {
        if (value.isPressed)
        {
            Reload = true;
        }
    }


    private void LateUpdate()
    {
        Jump = false;
        Fire = false;
    }
}
