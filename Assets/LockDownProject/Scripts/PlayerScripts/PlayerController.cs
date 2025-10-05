using UnityEngine;


// �÷��̾� ���� ����ü
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

    [Header("Providers")]
    private IPlayerWeaponStateProvider playerWeaponStateProvider;

    [Header("PlayerControllerClassComponent")]
    [SerializeField]private PlayerActionManager actionManager;

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

       weaponController=GetComponentInChildren<PlayerWeaponController>();
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
        bool isForward = movementSettings.IsForward(inputController.Move);
        float speed = movementSettings.GetSpeed(movementInfo, isForward);
        bool canJump = movementSettings.CanJump(movementInfo, inputController.Jump);
        //ī�޶� 
        float rotationSpeed = lookSettings.GetRotationSpeed(movementInfo);
        float cameraPosition = lookSettings.GetCameraPosition(movementInfo);
        float cameraChangeSpeed = lookSettings.GetCameraChangeTime(movementInfo);
        bool isFreeLook = inputController.FreeLook;

        //get�Լ�
        float mSensitivity = lookSettings.GetMouseSensitivity();
        float h = movementSettings.GetJumpHeight();
        //��� �� ����
        bool canFire = actionManager.CanFire(movementInfo, inputController.Fire);
        bool canAim = actionManager.CanAim(movementInfo, inputController.Aim);
        bool canReload = actionManager.CanReload(movementInfo, inputController.Reload);

        movementSettings.CheckDesiredGait(inputController.Move, movementInfo);
        //���
        movementController.UpdateMovement(inputController.Move, speed, canJump, h);
        lookController.UpdateLook(inputController.Look, rotationSpeed, cameraPosition,
                                  cameraChangeSpeed, mSensitivity, isFreeLook);  

       if(canFire)
        {
            playerWeaponStateProvider.OnFire();
        }
        if (canReload)
        {
            playerWeaponStateProvider.OnReload();
        }
        if(canAim)
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


