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

    public Vector2 Move;
    public Vector2 Look;
    public bool Jump;
    public bool Sprint; 
    public bool TacSprint;
    public bool Crouch;
    public bool Prone;
    public bool FreeLook;
    public bool Fire;
    public bool Aim;
    public bool Reload;
    public bool ChangeFireMode;
    public bool ChangeWeapon;
    public bool UIClick;
    public bool EquipMainWeapon;
    public bool EquipSubWeapon;
    public bool UseIFAK;
    public bool UseTourniquet;
    public bool UseSplint;
    public bool UseSurgeryKit;
    public bool CheckAmmo;

    private void OnMove(InputValue value) => Move = value.Get<Vector2>();
    private void OnLook(InputValue value)=> Look = value.Get<Vector2>();

    private void OnJump(InputValue value)
    {
        if (value.isPressed)
        {
            Jump = true;
        }
    }

    private void OnSprint(InputValue value) => Sprint=value.isPressed;

    private void OnTacSprint(InputValue value) => TacSprint = value.isPressed;

    private void OnCrouch(InputValue value)
    {
        Crouch = value.isPressed;
        Prone = false;
    }
    private void OnProne(InputValue value)
    {
        if (value.isPressed)
        {
            Prone = !Prone;

        }

    }
    private void OnFreeLook(InputValue value)
    {
        if(value.isPressed)
        {
            FreeLook = !FreeLook;
        }
        
    }

    private void OnFire(InputValue value)
    {

        if (value.isPressed && !UIClick)
        {
            stateProvider.OnFire(true);
            Fire = true;
            return;

        }
        else
        {
            stateProvider.OnFire(false);
            Fire = false;
        } 

    }

    private void OnAim(InputValue value)=>Aim = value.isPressed;
    private void OnReload(InputValue value)
    {
        if (value.isPressed)
        {
            Reload = true;
        }
    }

    private void OnChangeFireMode(InputValue value)
    {
        if (value.isPressed)
        {
            ChangeFireMode = true;
        }
    }
    /*
    private void OnChangeWeapon(InputValue value)
    {
        if (value.isPressed)
        {
            ChangeWeapon = true;
        }
    }*/
    private void OnUIClick(InputValue value)
    {
        if (value.isPressed)
        {
            UIClick = !UIClick;
        }
        
    }
    private void OnEquipMainWeapon(InputValue value)
    {
        if (value.isPressed && !EquipMainWeapon) 
        {
            ChangeWeapon = true;
            EquipMainWeapon = true;
            EquipSubWeapon = false;

        }
    }
    private void OnEquipSubWeapon(InputValue value)
    {
        if (value.isPressed && !EquipSubWeapon)
        {
            ChangeWeapon = true;
            EquipMainWeapon = false;
            EquipSubWeapon = true;
        }
    }
    private void OnUseIFAK(InputValue value)
    {
        if (value.isPressed)
        {
            UseIFAK = true;
            UseTourniquet = false;
            UseSplint = false;
            UseSurgeryKit = false;
        } 

    }
    private void OnUseTourniquet(InputValue value)
    {
        if (value.isPressed)
        {
            UseIFAK = false;
            UseTourniquet = true;
            UseSplint = false;
            UseSurgeryKit = false;
        }

    }
    private void OnUseSplint(InputValue value)
    {
        if (value.isPressed)
        {
            UseIFAK = false;
            UseTourniquet = false;
            UseSplint = true;
            UseSurgeryKit = false;
        }

    }
    private void OnUseSurgeryKit(InputValue value)
    {
        if (value.isPressed)
        {
            UseIFAK = false;
            UseTourniquet = false;
            UseSplint = false;
            UseSurgeryKit = true;
        }

    }
    private void OnCheckLeftAmmo(InputValue value)
    {
        if (value.isPressed) CheckAmmo = true;
       
    }
    private void LateUpdate()
    {
        Jump = false;
        Reload = false;
        ChangeFireMode = false;
        ChangeWeapon = false;
        UseIFAK = false;
        UseTourniquet = false;
        UseSplint = false;
        UseSurgeryKit = false;
        CheckAmmo = false;
    }
}
