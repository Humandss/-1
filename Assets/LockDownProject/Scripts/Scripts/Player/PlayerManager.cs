
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
    private HealthManager healthManager;
    private Player player;
    private UIManager uiManager;

    [Header("Providers")]
    private IStateProvider stateProvider;
    private ICameraAnimation camSettings;
    private IUIStateProvider uIStateProvider;

    [Header("StateBools")]
    bool isForward;
    bool canFire;
    bool canAim;
    bool canReload;
    bool canJump;
    bool canChangeFireMode;
    bool canChangeWeapon;
    bool isUIOn;
    bool isCheckLeftAmmo;
    bool isFreeLook;
    [Header("State Variables")]
    float speed = 0f;
    float rotationSpeed = 0f;
    float cameraPosition = 0f;
    float cameraChangeSpeed = 0f;
    float mSensitivity = 0f;
    float h = 0f;

    private void Start()
    {
        ApplyCursorState(true);
    }

    private void Awake()
    {
        Initialize();
    }
    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus) return;

        ApplyCursorState(!inputController.UIClick);
    }
    private void Update()
    {
        if (!HasRequiredReferences()) return;

        var movementInfo = CheckMovementMode();
        //ui 활성화시 움직임 제한
        CheckUIState();
        if (isUIOn) return;

        WeaponSlotRequest weaponSlotRequest = inputController.ConsumeWeaponSlotRequest();
        ItemUseRequest itemUseRequest = inputController.ConsumeItemUseRequest();
        bool reloadRequested = inputController.ConsumeReloadRequest();
        bool jumpRequested = inputController.ConsumeJumpRequest();
        bool changeFireModeRequested = inputController.ConsumeChangeFireModeRequest();
        bool checkAmmoRequested = inputController.ConsumeCheckAmmoRequest();

        ApplyPlayerInjuryState();
        UpdateFrameVariables(movementInfo, jumpRequested);
        EvaluateActionAvailability(movementInfo, reloadRequested, changeFireModeRequested, weaponSlotRequest, checkAmmoRequested);
        PlayAction(weaponSlotRequest, itemUseRequest);
        UpdateAction(movementInfo); 

    }

    private void Initialize()
    {
        inputController = GetComponent<PlayerInputController>();
        if (inputController == null)
        {
            Debug.LogWarning("[PlayerManager] inputController is NULL");
        }

        movementController = GetComponent<PlayerMovementController>();
        if (movementController == null)
        {
            Debug.LogWarning("[PlayerManager] movementController is NULL");
        }

        lookController = GetComponent<PlayerLookController>();
        if (lookController == null)
        {
            Debug.LogWarning("[PlayerManager] lookController is NULL");
        }

        movementSettings = GetComponent<MovementSettings>();
        if (movementSettings == null)
        {
            Debug.LogWarning("[PlayerManager] movementSettings is NULL");
        }

        lookSettings = GetComponent<LookSettings>();
        if (lookSettings == null)
        {
            Debug.LogWarning("[PlayerManager] lookSettings is NULL");
        }

        healthManager = GetComponent<HealthManager>();
        if (healthManager == null)
        {
            Debug.LogWarning("[PlayerManager] healthManager is NULL");
        }

        player = GetComponentInChildren<Player>();
        if (player == null)
        {
            Debug.LogWarning("[PlayerManager]  player is NULL");
        }

        uiManager = GetComponent<UIManager>();
        if (uiManager == null)
        {
            Debug.LogWarning("[PlayerManager] uiManager is NULL");
        }

        stateProvider = player as IStateProvider;
        if (stateProvider == null)
        {
            Debug.LogWarning("[PlayerManager] stateProvider is NULL");
        }
        camSettings = lookController as ICameraAnimation;
        if (camSettings == null)
        {
            Debug.LogWarning("[PlayerManager]  camSettings is NULL");
        }

        uIStateProvider = uiManager as IUIStateProvider;
        if (uIStateProvider == null)
        {
            Debug.LogWarning("[PlayerManager] uIStateProvider is NULL");
        }
    }

    private MovementMode CheckMovementMode()
    {
        return new MovementMode
        {
            prone = inputController.Prone,
            crouch = inputController.Crouch,
            sprint = inputController.Sprint,
            tacticalSprint = inputController.TacSprint,

        };
    }
    private void CheckUIState()
    {
        isUIOn = inputController.UIClick;
        uIStateProvider.CheckUIPanelOn(isUIOn);
        ApplyCursorState(!isUIOn);
    }
    private void EvaluateActionAvailability(in MovementMode movementInfo, bool reloadRequested, bool changeFireModeRequested, WeaponSlotRequest weaponSlotRequest, bool checkAmmoRequested)
    {
        //플레이어 부상 상태 확인
        canFire = movementSettings.CanFire(movementInfo);
        canAim = movementSettings.CanAim(movementInfo, inputController.Aim);
        canReload = movementSettings.CanReload(movementInfo, reloadRequested);
        canChangeFireMode = movementSettings.CanChangeFireMode(movementInfo, changeFireModeRequested);
        canChangeWeapon = movementSettings.CanChangeWeapon(movementInfo, weaponSlotRequest != WeaponSlotRequest.None);
        isCheckLeftAmmo = checkAmmoRequested;
    }
    private void UpdateFrameVariables(in MovementMode movementInfo, bool jumpRequested)
    {
        isForward = movementSettings.IsForward(inputController.Move);
        speed = movementSettings.GetSpeed(movementInfo, isForward);
        canJump = movementSettings.CanJump(movementInfo, jumpRequested, movementController.IsGrounded());
        rotationSpeed = lookSettings.GetRotationSpeed(movementInfo);
        cameraPosition = lookSettings.GetCameraPosition(movementInfo);
        cameraChangeSpeed = lookSettings.GetCameraChangeTime(movementInfo);
        isFreeLook = inputController.FreeLook;
        mSensitivity = lookSettings.GetMouseSensitivity();
        h = movementSettings.GetJumpHeight();
    }
    private void PlayAction(WeaponSlotRequest weaponSlotRequest, ItemUseRequest itemUseRequest)
    {
        PlayReload(canReload);
        PlayAim(canAim);
        PlayJump(canJump);
        ChangeWeaponFireMode(canChangeFireMode);
        ChangeWeaponByNumKey(canChangeWeapon, weaponSlotRequest);
        HandleItemRequest(itemUseRequest);
        CheckLeftAmmo(isCheckLeftAmmo, speed);
    }
    private void UpdateAction(in MovementMode movementInfo)
    {
        movementSettings.CheckDesiredGait(inputController.Move, movementInfo, speed);
        movementController.UpdateMovement(inputController.Move, speed, canJump, h);
        movementController.UpdateCCHeight(movementInfo);
        lookController.UpdateLook(inputController.Look, rotationSpeed, cameraPosition,
                                  cameraChangeSpeed, mSensitivity, isFreeLook);

        camSettings.UpdateFOVandCameraShake();
    }

    private static void ApplyCursorState(bool lockCursor)
    {
        Cursor.lockState = lockCursor ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !lockCursor;
    }

    private void ApplyPlayerInjuryState()
    {
        if (healthManager == null) return;

        PlayerInjuryState injuryState = healthManager.GetPlayerInjuryState();
        movementSettings.ApplyInjuryState(injuryState);
        lookSettings.ApplyInjuryState(injuryState);
    }

    private bool HasRequiredReferences()
    {
        return inputController != null &&
               movementController != null &&
               lookController != null &&
               movementSettings != null &&
               lookSettings != null &&
               healthManager != null &&
               stateProvider != null &&
               camSettings != null &&
               uIStateProvider != null;
    }

    private void CheckLeftAmmo(bool isCheck, float speed)
    {
        
        if (isCheck && speed <= 5.0f) 
        {
            uIStateProvider.CheckLeftAmmo();
        }
        else return;
    }
    private void HandleItemRequest(ItemUseRequest itemUseRequest)
    {
        if (itemUseRequest == ItemUseRequest.None) return;
        uIStateProvider.UseItem((int)itemUseRequest);
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
    private void ChangeWeaponByNumKey(bool canChangeWeapon, WeaponSlotRequest weaponSlotRequest)
    {
        if (canChangeWeapon)
        {
            stateProvider.OnEquipWeaponByNumberKey(weaponSlotRequest == WeaponSlotRequest.Main);
        }
        else return;
    }
    public bool CanPlayerFire()
    {
        return canFire;
    }
}

