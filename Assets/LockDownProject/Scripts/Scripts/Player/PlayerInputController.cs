using UnityEngine;
using UnityEngine.InputSystem;

public enum WeaponSlotRequest
{
    None,
    Main,
    Sub
}

public enum ItemUseRequest
{
    None = -1,
    IFAK = 4,
    Tourniquet = 5,
    Splint = 6,
    SurgeryKit = 7
}

public class PlayerInputController : MonoBehaviour
{
    private Player player;

    private IStateProvider stateProvider;
    private bool jumpRequested;
    private bool reloadRequested;
    private bool changeFireModeRequested;
    private bool checkAmmoRequested;
    private WeaponSlotRequest requestedWeaponSlot = WeaponSlotRequest.None;
    private ItemUseRequest requestedItemUse = ItemUseRequest.None;
   
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
    public bool Jump => jumpRequested;
    public bool Sprint { get; private set; }
    public bool TacSprint { get; private set; }
    public bool Crouch { get; private set; }
    public bool Prone { get; private set; }
    public bool FreeLook { get; private set; }
    public bool Fire { get; private set; }
    public bool Aim { get; private set; }
    public bool Reload => reloadRequested;
    public bool UIClick { get; private set; }

    private void OnMove(InputValue value) => Move = value.Get<Vector2>();
    private void OnLook(InputValue value)=> Look = value.Get<Vector2>();

    private void OnJump(InputValue value)
    {
        if (value.isPressed)
        {
            jumpRequested = true;
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
        if (stateProvider == null) return;

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
            reloadRequested = true;
        }
    }

    private void OnChangeFireMode(InputValue value)
    {
        if (value.isPressed)
        {
            changeFireModeRequested = true;
        }
    }

    private void OnUIClick(InputValue value)
    {
        if (value.isPressed)
        {
            UIClick = !UIClick;

            if (UIClick && stateProvider != null)
            {
                stateProvider.OnFire(false);
                Fire = false;
                Aim = false;
                stateProvider.OnAim(false);
            }
        }
        
    }
    private void OnEquipMainWeapon(InputValue value)
    {
        if (value.isPressed) 
        {
            requestedWeaponSlot = WeaponSlotRequest.Main;
        }
    }
    private void OnEquipSubWeapon(InputValue value)
    {
        if (value.isPressed)
        {
            requestedWeaponSlot = WeaponSlotRequest.Sub;
        }
    }
    private void OnUseIFAK(InputValue value)
    {
        if (value.isPressed)
        {
            requestedItemUse = ItemUseRequest.IFAK;
        } 

    }
    private void OnUseTourniquet(InputValue value)
    {
        if (value.isPressed)
        {
            requestedItemUse = ItemUseRequest.Tourniquet;
        }

    }
    private void OnUseSplint(InputValue value)
    {
        if (value.isPressed)
        {
            requestedItemUse = ItemUseRequest.Splint;
        }

    }
    private void OnUseSurgeryKit(InputValue value)
    {
        if (value.isPressed)
        {
            requestedItemUse = ItemUseRequest.SurgeryKit;
        }

    }
    private void OnCheckLeftAmmo(InputValue value)
    {
        if (value.isPressed) checkAmmoRequested = true;
       
    }

    public bool ConsumeJumpRequest()
    {
        bool requested = jumpRequested;
        jumpRequested = false;
        return requested;
    }

    public bool ConsumeReloadRequest()
    {
        bool requested = reloadRequested;
        reloadRequested = false;
        return requested;
    }

    public bool ConsumeChangeFireModeRequest()
    {
        bool requested = changeFireModeRequested;
        changeFireModeRequested = false;
        return requested;
    }

    public WeaponSlotRequest ConsumeWeaponSlotRequest()
    {
        WeaponSlotRequest requested = requestedWeaponSlot;
        requestedWeaponSlot = WeaponSlotRequest.None;
        return requested;
    }

    public ItemUseRequest ConsumeItemUseRequest()
    {
        ItemUseRequest requested = requestedItemUse;
        requestedItemUse = ItemUseRequest.None;
        return requested;
    }

    public bool ConsumeCheckAmmoRequest()
    {
        bool requested = checkAmmoRequested;
        checkAmmoRequested = false;
        return requested;
    }
}
