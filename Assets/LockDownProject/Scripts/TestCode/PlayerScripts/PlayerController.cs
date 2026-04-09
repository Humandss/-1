using System.Security.Claims;
using UnityEngine;

public struct MovementMode
{
    public bool idle;
    public bool prone;
    public bool crouch;
    public bool sprint;
    public bool tacticalSprint;

}

public class PlayerController : MonoBehaviour
{
    [Header("Refs")]
    private PlayerInputController inputController;
    private PlayerMovementController movementController;
    private PlayerLookController lookController;
    private MovementSettings movementSettings;
    private LookSettings lookSettings;
    private PlayerWeaponController weaponController;
    private PlayerAnimationController animationController;

    [Header("Providers")]
    private IPlayerWeaponStateProvider playerWeaponStateProvider;
    private ICameraAnimation camSettings;
    private IPlayerAnimator playerAnimator;

    [Header("PlayerControllerClassComponent")]
    [SerializeField]private PlayerActionManager actionManager;

    [Header("StateBools")]
    bool isForward;
    bool canFire;
    bool canAim;
    bool canReload;
    bool canJump;
    bool isGrounded;
   // bool wasGrounded = true;

    private void Awake()
    {
        inputController=GetComponent<PlayerInputController>();
        if (inputController == null )
        {
            Debug.LogWarning("[PlayerController] inputController is NULL");
        }

        movementController=GetComponent<PlayerMovementController>();
        if (movementController  == null)
        {
            Debug.LogWarning("[PlayerController] movementController is NULL");
        }

        lookController =GetComponent<PlayerLookController>();
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
        if(lookSettings == null)
        {
            Debug.LogWarning("[PlayerController] lookSettings is NULL");
        }

        animationController = GetComponentInChildren<PlayerAnimationController>();
        if (animationController == null)
        {
            Debug.LogWarning("[ PlayerMovementController] animationController is NULL");
        }

        playerAnimator = animationController as IPlayerAnimator;
        if (playerAnimator == null)
        {
            Debug.LogWarning("[ PlayerMovementController] playerAnimator is NULL");
        }

        camSettings = lookController as ICameraAnimation;
        if (camSettings == null)
        {
            Debug.LogWarning("[PlayerController]  camSettings is NULL");
        }

        weaponController =GetComponentInChildren<PlayerWeaponController>();
        if (weaponController == null)
        {
            Debug.LogWarning("[PlayerController]  weaponController is NULL");
        }

        playerWeaponStateProvider = weaponController as IPlayerWeaponStateProvider;
        if (playerWeaponStateProvider == null)
        {
            Debug.LogWarning("[PlayerController] playerWeaponStateProvider is NULL");
        }
    }

    private void Update()
    {

        //������ ��忡 ���� ���� ��ȭ ����
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
        isGrounded = movementController.IsGrounded();

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

        movementSettings.CheckDesiredGait(inputController.Move, movementInfo, speed);
        

        //���
        PlayFire(canFire);
        PlayReload(canReload);
        PlayAim(canAim);

        PlayJump(canJump);
        movementController.UpdateMovement(inputController.Move, speed, canJump, h);
        lookController.UpdateLook(inputController.Look, rotationSpeed, cameraPosition,
                                  cameraChangeSpeed, mSensitivity, isFreeLook);

        //카메라(반동. fov 변화는 update에서 사격 및 줌인하고 이후 처리하기 때문에 제일 마지막 처리, 추후 문제가 된다면 LateUpdate로 뺄 예정
        camSettings.UpdateFOVandCameraShake();

    }
    private void PlayJump(bool canJump)
    {
       
        if (canJump)
        {
            playerAnimator.OnJump();
         
        }
        else return;
    }

    private void PlayFire(bool canFire)
    {
        if (canFire)
        {
            playerWeaponStateProvider.OnFire();
        }
        else return;
    }
    private void PlayReload(bool canReload)
    {
        if (canReload)
        {
            playerWeaponStateProvider.OnReload();
        }
        else return;
    }
    private void PlayAim(bool canAim)
    {
        if (canAim)
        {
            playerWeaponStateProvider.OnAim(true);

        }
        else playerWeaponStateProvider.OnAim(false);
    }
}
[System.Serializable]
public class PlayerActionManager
{
    public bool CanFire(in MovementMode mode, bool isFire)
    {
        if(isFire)
        {
            if (mode.tacticalSprint) return false;

            return true;
        }

        return false;
    }
    public bool CanAim(in MovementMode mode, bool isAim)
    {
        if(isAim)
        {
            if (mode.sprint) return false;

            if (mode.tacticalSprint) return false;

            return true;
        }

        return false;
   
        
    }
    public bool CanReload(in MovementMode mode, bool isReload)
    {
        if(isReload)
        {
            if (mode.tacticalSprint) return false;

            return true;
        }
       

        return false ;
    }
}


