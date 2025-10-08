
using KINEMATION.FPSAnimationPack.Scripts.Camera;
using KINEMATION.FPSAnimationPack.Scripts.Weapon;
using KINEMATION.KAnimationCore.Runtime.Core;
using KINEMATION.ProceduralRecoilAnimationSystem.Runtime;
using UnityEditor;
using UnityEngine;

public interface IAnimatorControllerProvider
{
    RuntimeAnimatorController SetCharacterController();
}
public interface IWeaponRecoilInfoProvider
{
    Quaternion GetRecoilOutRot();
    Vector3 GetRecoilOutLoc();
}

public class WeaponBase : MonoBehaviour, IAnimatorControllerProvider, IWeaponRecoilInfoProvider
{
    [Header("Ref")]
    [SerializeField]public FPSWeaponSettings weaponSettings;
    private WeaponAnimationController weaponAnimation;
    private PlayerAnimationController playerAnimation;
    private WeaponSoundController weaponSound;
    private PlayerLookController playerLookController;
    private RecoilAnimation recoilAnimation;
    private WeaponAnimationClip weaponAnimationClip;
    protected GameObject ownerPlayer;
    public Transform aimPoint;

    [Header("Providers")]
    private IWeaponAnimator weaponAnimator;
    private IPlayerAnimator playerAnimator;
    private ICameraAnimation cameraAnimation;
    [Header("CheckState")]
    private bool isReloading;
    private bool isFiring;
    [Header("Delay")]
    private float unEquipDelay;
    private float emptyReloadDelay;
    private float tacReloadDelay;

    private int activeAmmo;

    [SerializeField] protected FireMode fireMode = FireMode.Semi;
    public FireMode ActiveFireMode => fireMode;
    [HideInInspector] public KTransform rightHandPose;
    [HideInInspector] public KTransform adsPose;

    
    private void Awake()
    {
       
    }
    public virtual void Initialize(GameObject owner)
    {
        playerAnimation = owner.GetComponent<PlayerAnimationController>();
        if (playerAnimation == null)
        {
            Debug.LogWarning("[WeaponBase] playerAnimation is NULL");
        }

        playerAnimator = playerAnimation as PlayerAnimationController;
        if (playerAnimation == null)
        {
            Debug.LogWarning("[WeaponBase] playerAnimator is NULL");
        }

        recoilAnimation = owner.GetComponent<RecoilAnimation>();
        if (recoilAnimation == null)
        {
            Debug.LogWarning("[WeaponBase] recoilAnimation is NULL");
        }

        weaponAnimation = GetComponent<WeaponAnimationController>();
        if (weaponAnimation == null)
        {
            Debug.LogWarning("[WeaponBase]weaponAnimation is NULL");
        }

        weaponAnimator = weaponAnimation as IWeaponAnimator;
        if (weaponAnimator == null)
        {
            Debug.LogWarning("[WeaponBase] weaponAnimator is NULL");
        }

        weaponSound = GetComponentInChildren<WeaponSoundController>();
        if(weaponSound==null)
        {
            Debug.LogWarning("[WeaponBase] weaponSound is NULL");
        }

        playerLookController = GetComponentInParent<PlayerLookController>();
        if (playerLookController == null)
        {
            Debug.LogWarning("[PlayerWeaponController]playerLookController is NULL");
        }
        cameraAnimation = playerLookController as ICameraAnimation;
        if (cameraAnimation == null)
        {
            Debug.LogWarning("[PlayerWeaponController] cameraAnimation is NULL");
        }

        activeAmmo = weaponSettings.ammo;

        if (weaponSettings == null || weaponSettings.characterController == null) return;

        AnimationClip idlePose = null;

        foreach (var clip in weaponSettings.characterController.animationClips)
        {
            var name = clip.name.ToLowerInvariant();

            if (name.Contains("reload"))
            {
                if (name.Contains("empty")) emptyReloadDelay = clip.length;
                else if (name.Contains("tac")) tacReloadDelay = clip.length;
                continue;
            }

            if (name.Contains("unequip"))
            {
                unEquipDelay = clip.length;
                continue;
            }

            if (idlePose == null && (name.Contains("idle") || name.Contains("pose")))
                idlePose = clip;
        }

        // �����ڼ� ���ø�
        if (idlePose != null && ownerPlayer != null)
            idlePose.SampleAnimation(ownerPlayer, 0f);
    }

    public RuntimeAnimatorController SetCharacterController()
    {
        if (weaponSettings == null || weaponSettings.characterController == null)
            Debug.LogError("[WeaponBase] weaponSettings or characterController is NULL");
        return weaponSettings.characterController;
    }
    public Quaternion GetRecoilOutRot()
    {
        return recoilAnimation.OutRot;
    }
    public Vector3 GetRecoilOutLoc()
    {
        return recoilAnimation.OutLoc;
    }
    public void OnEquipped(bool fastEquip = false)
    {
        playerAnimation.SetCharacterController(this);
        recoilAnimation.Init(weaponSettings.recoilAnimData, weaponSettings.fireRate, fireMode);

        playerAnimator.PlayIdle();

        if(weaponSettings.hasEquipOverride)
        {
            playerAnimator.PlayEquippedOverride();
            return;
        }

        playerAnimator.PlayEquipped();
    }
    public void OnFirePressed()
    {
        isFiring = true;
        OnFire();
    }
    public void OnFireReleased()
    {
        isFiring = false;
        recoilAnimation.Stop();
    }
    public float OnUnEquipped()
    {
        playerAnimator.PlayUnEquipped();
        return unEquipDelay + 0.05f;
    }
    public void OnFire()
    {
       
        if (!isFiring || isReloading) return;

        bool lastBullet = (activeAmmo == 1);

        if (activeAmmo == 0)
        {
            OnFireReleased();
            return;
        }

        recoilAnimation.Play();
        if (weaponSound != null) weaponSound.PlayFireSound();
        cameraAnimation.PlayCameraShake(weaponSettings.cameraShake);
        weaponAnimator.Fire(lastBullet);

        activeAmmo--;

        //Debug.Log(activeAmmo);
        if (fireMode == FireMode.Auto && weaponSettings)
        {
            Invoke(nameof(OnFire), 60f / Mathf.Max(1f, weaponSettings.fireRate));
        }
    }
    public void OnAim(bool isAiming)
    {
        recoilAnimation.isAiming = isAiming;
       
    }
    public void OnFireModeChange()
    {
        fireMode = fireMode == FireMode.Auto ? FireMode.Semi : weaponSettings.fullAuto ? FireMode.Auto : FireMode.Semi;
        recoilAnimation.fireMode = fireMode;
    }
    public void OnReload()
    {
        if(activeAmmo == weaponSettings.ammo) return;

        if(isReloading) return;

        if(activeAmmo == 0)
        {
            weaponAnimator.TacticalReload();
            playerAnimator.PlayTacticalReload();
        }
        else
        {
            weaponAnimator.Reload();
            playerAnimator.PlayReload();
        }

        float delay = activeAmmo == 0 ? emptyReloadDelay : tacReloadDelay;

        Invoke(nameof(ResetActiveAmmo), delay * weaponSettings.ammoResetTimeScale);

        isReloading = true;

    }
   
    protected void ResetActiveAmmo()
    {
        activeAmmo = GetMaxAmmo();
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
}
