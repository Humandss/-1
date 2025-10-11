using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
public class PlayerInputController : MonoBehaviour
{
    private Player player;

    private IStateProvider stateProvider;
   
    private void Awake()
    {
        //총쪽 부분만 디테일한 인풋을 요구하기 때문에 따로 뺌
        player = GetComponentInChildren<Player>();
        if (player == null)
        {
            Debug.LogWarning("[PlayerInputController]  player is NULL");
        }

        stateProvider = player as IStateProvider;
        if (stateProvider == null)
        {
            Debug.LogWarning("[PlayerInputController] stateProvider is NULL");
        }
    }

    public Vector2 Move { get; private set; }
    public Vector2 Look { get; private set; }
    public bool Jump { get; private set; }
    public bool Sprint { get; private set; }
    public bool TacSprint { get; private set; }
    public bool Crouch { get; private set; }
    public bool Prone { get; private set; }
    public bool FreeLook { get; private set; }
    public bool Fire { get; private set; }
    public bool Aim { get; private set; }
    public bool Reload { get; private set; }
    public bool ChangeFireMode { get; private set; }
    public bool ChangeWeapon { get; private set; }


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

    public void OnTacSprint(InputValue value) => TacSprint = value.isPressed;

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
            stateProvider.OnFire(true);
            return;
            
        }
        else stateProvider.OnFire(false);

    }
   
    public void OnAim(InputValue value)=>Aim = value.isPressed;
    public void OnReload(InputValue value)
    {
        if (value.isPressed)
        {
            Reload = true;
        }
    }

    public void OnChangeFireMode(InputValue value)
    {
        if (value.isPressed)
        {
            ChangeFireMode = true;
        }
    }

    public void OnChangeWeapon(InputValue value)
    {
        if (value.isPressed)
        {
            ChangeWeapon = true;
        }
    }
    private void LateUpdate()
    {
        Jump = false;
        //Fire = false;
        Reload = false;
        ChangeFireMode = false;
        ChangeWeapon = false;
    }
}
