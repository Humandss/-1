using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IPlayerCanFireCheckProvider
{
    bool CanPlayerFire();
}
public class PlayerManager : MonoBehaviour, IPlayerCanFireCheckProvider
{
    [Header("Refs")]
    private PlayerInputController inputController;
    private PlayerMovementController movementController;
    private PlayerLookController lookController;
    private MovementSettings movementSettings;
    private LookSettings lookSettings;
    private Player player;


    [Header("Providers")]
    private IStateProvider stateProvider;
    private ICameraAnimation camSettings;

    [Header("StateBools")]
    bool isForward;
    bool canFire;
    bool canAim;
    bool canReload;
    bool canJump;
    bool canChangeFireMode;
    bool canChangeWeapon;

    private void Awake()
    {
        inputController = GetComponent<PlayerInputController>();
        if (inputController == null)
        {
            Debug.LogWarning("[PlayerController] inputController is NULL");
        }

        movementController = GetComponent<PlayerMovementController>();
        if (movementController == null)
        {
            Debug.LogWarning("[PlayerController] movementController is NULL");
        }

        lookController = GetComponent<PlayerLookController>();
        if (lookController == null)
        {
            Debug.LogWarning("[PlayerController] lookController is NULL");
        }

        movementSettings = GetComponent<MovementSettings>();
        if (movementSettings == null)
        {
            Debug.LogWarning("[PlayerController] movementSettings is NULL");
        }

        lookSettings = GetComponent<LookSettings>();
        if (lookSettings == null)
        {
            Debug.LogWarning("[PlayerController] lookSettings is NULL");
        }

        player = GetComponentInChildren<Player>();
        if (player == null)
        {
            Debug.LogWarning("[PlayerController]  player is NULL");
        }

        stateProvider = player as IStateProvider;
        if (stateProvider == null)
        {
            Debug.LogWarning("[PlayerController] stateProvider is NULL");
        }
        camSettings = lookController as ICameraAnimation;
        if (camSettings == null)
        {
            Debug.LogWarning("[PlayerController]  camSettings is NULL");
        }
    }

    private void Update()
    {
        var movementInfo = new MovementMode
        {
            prone = inputController.Prone,
            crouch = inputController.Crouch,
            sprint = inputController.Sprint,
            tacticalSprint = inputController.TacSprint,

        };
        //������
        isForward = movementSettings.IsForward(inputController.Move);
        float speed = movementSettings.GetSpeed(movementInfo, isForward);
        canJump = movementSettings.CanJump(movementInfo, inputController.Jump, movementController.IsGrounded());
       

        //ī�޶� 
        float rotationSpeed = lookSettings.GetRotationSpeed(movementInfo);
        float cameraPosition = lookSettings.GetCameraPosition(movementInfo);
        float cameraChangeSpeed = lookSettings.GetCameraChangeTime(movementInfo);
        bool isFreeLook = inputController.FreeLook;

        //get�Լ�
        float mSensitivity = lookSettings.GetMouseSensitivity();
        float h = movementSettings.GetJumpHeight();
        //��� �� ����
        canFire = movementSettings.CanFire(movementInfo);
        canAim = movementSettings.CanAim(movementInfo, inputController.Aim);
        canReload = movementSettings.CanReload(movementInfo, inputController.Reload);
        canChangeFireMode = movementSettings.CanChangeFireMode(movementInfo, inputController.ChangeFireMode);
        canChangeWeapon = movementSettings.CanChangeWeapon(movementInfo, inputController.ChangeWeapon);

        PlayReload(canReload);
        PlayAim(canAim);
        PlayJump(canJump);
        ChangeWeaponFireMode(canChangeFireMode);
        ChangeWeapon(canChangeWeapon);


        movementSettings.CheckDesiredGait(inputController.Move, movementInfo, speed);

        movementController.UpdateMovement(inputController.Move, speed, canJump, h);
        lookController.UpdateLook(inputController.Look, rotationSpeed, cameraPosition,
                                  cameraChangeSpeed, mSensitivity, isFreeLook);


        camSettings.UpdateFOVandCameraShake();

    }
    private void PlayJump(bool canJump)
    {

        if (canJump)
        {
            stateProvider.OnJump();
        }
        else return;
    }

    private void PlayReload(bool canReload)
    {
        if (canReload)
        {
            stateProvider.OnReload();
        }
        else return;
    }
    private void PlayAim(bool canAim)
    {
        if (canAim)
        {
            stateProvider.OnAim(true);

        }
        else stateProvider.OnAim(false);
    }
    private void ChangeWeaponFireMode(bool canChangeWeaponFireMode)
    {
        if (canChangeWeaponFireMode)
        {
            stateProvider.OnChangeFireMode();
        }
        else return;
   
    }
    private void ChangeWeapon(bool canChangeWeapon)
    {
        if(canChangeWeapon)
        {
            stateProvider.OnChangeWeapon();
        }
        else return;
    }
    public bool CanPlayerFire()
    {
        return canFire;
    }
}

