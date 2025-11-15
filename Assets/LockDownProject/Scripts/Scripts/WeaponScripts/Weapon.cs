
using KINEMATION.FPSAnimationPack.Scripts.Camera;
using KINEMATION.FPSAnimationPack.Scripts.Weapon;
using KINEMATION.KAnimationCore.Runtime.Core;
using KINEMATION.ProceduralRecoilAnimationSystem.Runtime;
using UnityEngine;
using UnityEngine.UI;

public interface IGetWeaponAmmoInfoProvider
{
    int GetActiveAmmo();
    int GetMaxAmmo();
    string GetAmmoName();
}
public class Weapon : MonoBehaviour, IGetWeaponAmmoInfoProvider
{
    public float UnEquipDelay => unEquipDelay;
    public FireMode ActiveFireMode => fireMode;
    public Sprite icon_gun;

    [Header("Refs")]
    public FPSWeaponSettings weaponSettings;
    public Transform aimPoint;
    protected GameObject ownerPlayer;
    protected RecoilAnimation recoilAnimation;
    protected WeaponSound weaponSound;
    protected Animator characterAnimator;
    protected Animator weaponAnimator;
    private PlayerLookController playerLookController;
    private WeaponFireController weaponFireController;
    private PlayerManager playerManager;
    private WeaponFireController fireController;

    [Header("Providers")]
    private ICameraAnimation cameraAnimation;
    private IFireBulletProvider fireBulletProvider;
    private IPlayerCanFireCheckProvider canFireCheckProvider;

    [Header("Animator Hash")]
    protected static int RELOAD_EMPTY = Animator.StringToHash("Reload_Empty");
    protected static int RELOAD_TAC = Animator.StringToHash("Reload_Tac");
    protected static int FIRE = Animator.StringToHash("Fire");
    protected static int FIREOUT = Animator.StringToHash("FireOut");
    protected static int EQUIP = Animator.StringToHash("Equip");
    protected static int EQUIP_OVERRIDE = Animator.StringToHash("Equip_Override");
    protected static int UNEQUIP = Animator.StringToHash("UnEquip");
    protected static int IDLE = Animator.StringToHash("Idle");

    [Header("Delay")]
    protected float unEquipDelay;
    protected float emptyReloadDelay;
    protected float tacReloadDelay;

    [Header("State")]
    protected bool isReloading;
    protected bool isFiring;
    [Header("etc")]
    protected int activeAmmo;

    [SerializeField] protected FireMode fireMode = FireMode.Semi;

    [HideInInspector] public KTransform rightHandPose;
    [HideInInspector] public KTransform adsPose;

 
    public virtual void Initialize(GameObject owner)
    {
        ownerPlayer = owner;
        if (ownerPlayer == null)
        {
            Debug.LogWarning("[Weapon] ownerPlayer not found!");
        }

        recoilAnimation = owner.GetComponent<RecoilAnimation>();
        if (recoilAnimation == null)
        {
            Debug.LogWarning("[Weapon] recoilAnimation  is NULL!");
        }

        characterAnimator = owner.GetComponent<Animator>();
        if (characterAnimator == null)
        {
            Debug.LogWarning("[Weapon] characterAnimator is NULL!");
        }

        activeAmmo = weaponSettings.ammo;

         playerLookController = owner.GetComponentInParent<PlayerLookController>();
         if (playerLookController == null)
         {
             Debug.LogWarning("[Weapon] playerLookController is NULL!");
         }
    
        cameraAnimation = playerLookController as ICameraAnimation;
        if (cameraAnimation == null)
        {
            Debug.LogWarning("[Weapon] cameraAnimation is NULL!");
        }

        weaponAnimator = GetComponentInChildren<Animator>();
        if (weaponAnimator == null)
        {
            Debug.LogWarning("[Weapon] Animator is NULL!");
        }

        weaponSound = GetComponentInChildren<WeaponSound>();
        if (weaponSound == null)
        {
            Debug.LogWarning("[Weapon] WeaponSound is NULL!");
        }

        weaponFireController = GetComponent<WeaponFireController>();
        if (weaponFireController == null)
        {
            Debug.LogWarning("[Weapon] weaponFireController is NULL!");
        }

        fireBulletProvider = weaponFireController as IFireBulletProvider;
        if (fireBulletProvider == null)
        {
            Debug.LogWarning("[Weapon]  fireBulletProvider is NULL!");
        }
        playerManager = owner.GetComponentInParent<PlayerManager>();
        if (playerManager == null)
        {
            Debug.LogWarning("[Weapon] playerManager is NULL");
        }

        canFireCheckProvider = playerManager as IPlayerCanFireCheckProvider;
        if (canFireCheckProvider == null)
        {
            Debug.LogWarning("[Weapon]  canFireCheckProvider is NULL");
        }

        if (Mathf.Approximately(weaponSettings.fireRate, 0f))
        {
            Debug.LogWarning("[Weapon] Fire Rate is ZERO, setting it to default 600.");
            weaponSettings.fireRate = 600f;
        }

        AnimationClip idlePose = null;

        foreach (var clip in weaponSettings.characterController.animationClips)
        {
            if (clip.name.Contains("Reload"))
            {
                if (clip.name.Contains("Tac")) tacReloadDelay = clip.length;
                if (clip.name.Contains("Empty")) emptyReloadDelay = clip.length;
                continue;
            }

            if (clip.name.ToLower().Contains("unequip"))
            {
                unEquipDelay = clip.length;
                continue;
            }

            if (idlePose != null) continue;
            if (clip.name.Contains("Idle") || clip.name.Contains("Pose")) idlePose = clip;
        }

        if (idlePose != null)
        {
            idlePose.SampleAnimation(ownerPlayer, 0f);
        }
       
    }

    public virtual void OnReload()
    {
        if (activeAmmo == weaponSettings.ammo) return;
        if(isReloading) return;    

        var reloadHash = activeAmmo == 0 ? RELOAD_EMPTY : RELOAD_TAC;
        characterAnimator.Play(reloadHash, -1, 0f);
        weaponAnimator.Play(reloadHash, -1, 0f);

        float delay = activeAmmo == 0 ? emptyReloadDelay : tacReloadDelay;
        Invoke(nameof(ResetActiveAmmo), delay * weaponSettings.ammoResetTimeScale);
        isReloading = true;
    }

    public void OnFireModeChange()
    {
        fireMode = fireMode == FireMode.Auto ? FireMode.Semi : weaponSettings.fullAuto ? FireMode.Auto : FireMode.Semi;
        recoilAnimation.fireMode = fireMode;
    
    }

    public void OnEquipped_Immediate()
    {
        characterAnimator.runtimeAnimatorController = weaponSettings.characterController;
        weaponAnimator.Play(IDLE, -1, 0f);
        recoilAnimation.Init(weaponSettings.recoilAnimData, weaponSettings.fireRate, fireMode);
    }

    public void OnEquipped(bool fastEquip = false)
    {
        characterAnimator.runtimeAnimatorController = weaponSettings.characterController;
        recoilAnimation.Init(weaponSettings.recoilAnimData, weaponSettings.fireRate, fireMode);
        
        // Reset the default pose to idle.
        characterAnimator.Play(IDLE, -1, 0f);

        // Play the equip animation.
        if (weaponSettings.hasEquipOverride)
        {
            characterAnimator.Play("IKMovement", -1, 0f);
            characterAnimator.Play(fastEquip ? EQUIP : EQUIP_OVERRIDE, -1, 0f);
            return;
        }

        // Play the curve-based equipping animation.
        characterAnimator.Play(EQUIP, -1, 0f);
    }

    public float OnUnEquipped()
    {
        characterAnimator.SetTrigger(UNEQUIP);
        return unEquipDelay + 0.05f;
    }

    public void OnFirePressed()
    {
        if (canFireCheckProvider.CanPlayerFire())
        {
            isFiring = true;
            OnFire();
        }
        else return;
        
    }

    public void OnFireReleased()
    {
        isFiring = false;
        recoilAnimation.Stop();
    }

    private void OnFire()
    {
        if (!isFiring || isReloading) return;

        if (activeAmmo == 0)
        {
            OnFireReleased();
            return;
        }

        recoilAnimation.Play();
        if (weaponSound != null) weaponSound.PlayFireSound();

        cameraAnimation.PlayCameraShake(weaponSettings.cameraShake);

        if (weaponSettings.useFireClip) characterAnimator.Play(FIRE, -1, 0f);
        weaponAnimator.Play(weaponSettings.hasFireOut && activeAmmo == 1
            ? FIREOUT
            : FIRE, -1, 0f);
        fireBulletProvider.FireBullet();
        activeAmmo--;

        if (fireMode == FireMode.Semi) return;
        Invoke(nameof(OnFire), 60f / weaponSettings.fireRate);
    }

    protected void ResetActiveAmmo()
    {
        activeAmmo = weaponSettings.ammo;
        isReloading = false;
    }

    public int GetActiveAmmo()
    {
        return activeAmmo;
    }

    public int GetMaxAmmo()
    {
        return weaponSettings.ammo;
    }
    public string GetAmmoName()
    {
        return weaponFireController.GetAmmoName();
    }
}
