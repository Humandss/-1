using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    [Header("Refs")]
    private PlayerInputController inputController;
    private PlayerMovementController movementController;
    private PlayerLookController lookController;
    private MovementSettings movementSettings;
    private LookSettings lookSettings;
    private Player player;

    [Header("PlayerControllerClassComponent")]
    [SerializeField] private PlayerActionManager actionManager;

    [Header("Providers")]
    private IStateProvider stateProvider;
    private ICameraAnimation camSettings;

    [Header("StateBools")]
    bool isForward;
    bool canFire;
    bool canAim;
    bool canReload;
    bool canJump;

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
        canFire = actionManager.CanFire(movementInfo, inputController.Fire);
        canAim = actionManager.CanAim(movementInfo, inputController.Aim);
        canReload = actionManager.CanReload(movementInfo, inputController.Reload);

        PlayFire(canFire);
        PlayReload(canReload);
        PlayAim(canAim);

        PlayJump(canJump);

        //movementSettings.CheckDesiredGait(inputController.Move, movementInfo, speed);

        movementController.UpdateMovement(inputController.Move, speed, canJump, h);
        lookController.UpdateLook(inputController.Look, rotationSpeed, cameraPosition,
                                  cameraChangeSpeed, mSensitivity, isFreeLook);


        //camSettings.UpdateFOVandCameraShake();

    }
    private void PlayJump(bool canJump)
    {

        if (canJump)
        {
            stateProvider.OnJump();
        }
        else return;
    }

    private void PlayFire(bool canFire)
    {
        if (canFire)
        {
            stateProvider.OnFire();
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
}
[System.Serializable]
public class PlayerActionController
{
    public bool CanFire(in MovementMode mode, bool isFire)
    {
        if (isFire)
        {
            if (mode.tacticalSprint) return false;

            return true;
        }

        return false;
    }
    public bool CanAim(in MovementMode mode, bool isAim)
    {
        if (isAim)
        {
            if (mode.sprint) return false;

            if (mode.tacticalSprint) return false;

            return true;
        }

        return false;


    }
    public bool CanReload(in MovementMode mode, bool isReload)
    {
        if (isReload)
        {
            if (mode.tacticalSprint) return false;

            return true;
        }


        return false;
    }
}
