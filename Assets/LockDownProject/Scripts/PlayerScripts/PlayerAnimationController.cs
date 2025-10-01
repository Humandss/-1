using KINEMATION.KAnimationCore.Runtime.Core;
using Unity.Burst.CompilerServices;
using UnityEditor;
using UnityEngine;

public interface IPlayerAnimator
{
    void SetCharacterController();
    void PlayIdle();
    void PlayEquippedOverride(bool fastEquip = false);
    void PlayEquipped();
    void PlayUnEquipped();

}
public interface IPlayerAnimationGetFloatWeight
{
    float GetFloatTacSprintWeight();
}
public class PlayerAnimationController : MonoBehaviour,IPlayerAnimator, IPlayerAnimationGetFloatWeight
{
    [Header("Ref")]
    [SerializeField] private Animator playerAnimator;
    [SerializeField] private PlayerWeaponController playerWeaponController;
    [SerializeField] private PlayerMovementManager playerMovementManager;
    [SerializeField] private WeaponBase wBase;

    [Header("Providers")]
    private IAnimatorControllerProvider controllerProvider;
    private IPlayerWeaponInfoProvider weaponInfoProvider;
    private IPlayerMoveInfoProvider moveInfoProvider;

    public float AdsWeight => adsWeight;
    private float adsWeight;
    private float smoothGait;

    protected static int RELOAD_EMPTY = Animator.StringToHash("Reload_Empty");
    protected static int RELOAD_TAC = Animator.StringToHash("Reload_Tac");
    protected static int FIRE = Animator.StringToHash("Fire");
    protected static int FIREOUT = Animator.StringToHash("FireOut");

    protected static int EQUIP = Animator.StringToHash("Equip");
    protected static int EQUIP_OVERRIDE = Animator.StringToHash("Equip_Override");
    protected static int UNEQUIP = Animator.StringToHash("UnEquip");
    protected static int IDLE = Animator.StringToHash("Idle");

    private static int RIGHT_HAND_WEIGHT = Animator.StringToHash("RightHandWeight");
    private static int TAC_SPRINT_WEIGHT = Animator.StringToHash("TacSprintWeight");
    private static int GAIT = Animator.StringToHash("Gait");
    private static int IS_IN_AIR = Animator.StringToHash("IsInAir");

    private int tacSprintLayerIndex;
    private int triggerDisciplineLayerIndex;
    private int rightHandLayerIndex;

    private bool isAiming;

    public void Awake()
    {
        playerAnimator = GetComponent<Animator>();
        if( playerAnimator = null )
        {           
             Debug.LogWarning("[PlayerAnimationController] playerAnimator is NULL");          
        }
        controllerProvider = wBase as IAnimatorControllerProvider;
        if (controllerProvider == null)
        {
            Debug.LogWarning("[PlayerAnimationController] controllerProvider is NULL");
        }

        weaponInfoProvider = playerWeaponController as IPlayerWeaponInfoProvider;

        moveInfoProvider = playerMovementManager as IPlayerMoveInfoProvider;
    }
    private void Start()
    {
        
    }
    private void Update()
    {
        adsWeight = Mathf.Clamp01(adsWeight + weaponInfoProvider.GetAimSpeed() * Time.deltaTime * (isAiming ? 1f : -1f));
        smoothGait = Mathf.Lerp(smoothGait, moveInfoProvider.GetDesiredGait(),
            KMath.ExpDecayAlpha(weaponInfoProvider.GetGaitSmoothing(), Time.deltaTime));

        playerAnimator.SetFloat(GAIT, smoothGait);
        playerAnimator.SetLayerWeight(tacSprintLayerIndex, Mathf.Clamp01(smoothGait - 2f));

        playerAnimator.SetLayerWeight(triggerDisciplineLayerIndex,
            weaponInfoProvider.GetTriggerState() ? playerAnimator.GetFloat(TAC_SPRINT_WEIGHT) : 0f);

        playerAnimator.SetLayerWeight(rightHandLayerIndex, playerAnimator.GetFloat(RIGHT_HAND_WEIGHT));
    }
    public float GetFloatTacSprintWeight()
    {
        return playerAnimator.GetFloat(TAC_SPRINT_WEIGHT);
    }
    public void SetCharacterController()
    {
        playerAnimator.runtimeAnimatorController = controllerProvider.SetCharacterController();
    }
    public void PlayIdle()
    {
        playerAnimator.Play(IDLE, -1, 0f);
    }
    public void PlayEquippedOverride(bool fastEquip = false)
    {
        playerAnimator.Play("IKMovement", -1, 0f);
        playerAnimator.Play(fastEquip ? EQUIP : EQUIP_OVERRIDE, -1, 0f);
    }
    public void PlayEquipped()
    {
        playerAnimator.Play(EQUIP, -1, 0f);
    }
    public void PlayUnEquipped()
    {
        playerAnimator.SetTrigger(UNEQUIP);
    }
    public void PlayReload()
    {
        playerAnimator.Play(RELOAD_TAC, -1, 0f);
    }
    public void PlayTacticalReload()
    {
        playerAnimator.Play(RELOAD_EMPTY, -1, 0f);
    }
    public void PlayFire()
    {
        playerAnimator.Play(FIRE, -1, 0f);
    }
    public void OnJump()
    {
        playerAnimator.SetBool(IS_IN_AIR, true);
        Invoke(nameof(OnLand), 0.4f);
    }
    private void OnLand()
    {
        playerAnimator.SetBool(IS_IN_AIR, false);
    }
}
