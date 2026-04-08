using KINEMATION.FPSAnimationPack.Scripts.Camera;
using KINEMATION.FPSAnimationPack.Scripts.Weapon;
using KINEMATION.KAnimationCore.Runtime.Core;
using KINEMATION.ProceduralRecoilAnimationSystem.Runtime;
using UnityEngine;

public interface IGetWeaponAmmoInfoProvider
{
    int GetActiveAmmo();
    int GetMaxAmmo();
    string GetAmmoName();
}

public enum WeaponOwnerType
{
    Player,
    Enemy,
    TutorialDummy
}

public class Weapon : MonoBehaviour, IGetWeaponAmmoInfoProvider, IWeaponFireContext
{
    public float UnEquipDelay => unEquipDelay;
    public FireMode ActiveFireMode => fireMode;
    public Sprite icon_gun;

    [Header("Refs")]
    public FPSWeaponSettings weaponSettings;
    protected RecoilAnimation recoilAnimation;
    protected WeaponSound weaponSound;
    protected Animator characterAnimator;
    protected Animator weaponAnimator;
    private PlayerLookController playerLookController;
    private PlayerManager playerManager;
    private EnemyController enemyController;
    private FireController fireController;
    private IGetBulletDirection enemyBulletDirection;
    private WeaponOwnerType ownerType = WeaponOwnerType.Player;

    [Header("Transform")]
    public Transform aimPoint;
    protected GameObject ownerPlayer;

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

    [Header("Shell")]
    [SerializeField] private Transform shellEjectPoint;
    [SerializeField] private GameObject shell;
    [SerializeField] private float shellEjectDelay = 0.3f;
    [SerializeField] private float shellEjectForce = 2.0f;
    [SerializeField] private float shellEjectUpwardForce = 1.5f;
    [SerializeField] private float shellEjectTorque = 2.0f;

    [Header("Delay")]
    protected float unEquipDelay;
    protected float emptyReloadDelay;
    protected float tacReloadDelay;

    [Header("Sound")]
    [SerializeField] private int emptyFireSoundIndex = 2;
    [SerializeField] private float emptyFireSoundCooldown = 0.08f;
    private float lastEmptyFireSoundTime = float.NegativeInfinity;

    [Header("State")]
    protected bool isReloading;
    protected bool isFiring;

    [Header("Ammo")]
    protected int activeAmmo;

    [SerializeField] protected FireMode fireMode = FireMode.Semi;

    [HideInInspector] public KTransform rightHandPose;
    [HideInInspector] public KTransform adsPose;

    public virtual void Initialize(GameObject owner)
    {
        InitializeForOwner(owner, WeaponOwnerType.Player);
    }

    public void EnemeyWeaponInitialize(GameObject owner)
    {
        InitializeForOwner(owner, WeaponOwnerType.Enemy);
    }

    public void TutorialDummyInitialize(GameObject owner)
    {
        InitializeForOwner(owner, WeaponOwnerType.TutorialDummy);
    }

    private void InitializeForOwner(GameObject owner, WeaponOwnerType ownerType)
    {
        this.ownerType = ownerType;
        ownerPlayer = owner;
        activeAmmo = weaponSettings.ammo;

        if (owner == null)
        {
            Debug.LogWarning("[Weapon] owner is NULL!");
        }

        weaponAnimator = GetComponentInChildren<Animator>();
        if (weaponAnimator == null)
        {
            Debug.LogWarning("[Weapon] weaponAnimator is NULL!");
        }

        weaponSound = GetComponentInChildren<WeaponSound>();
        if (weaponSound == null)
        {
            Debug.LogWarning("[Weapon] weaponSound is NULL!");
        }

        fireController = GetComponent<FireController>();
        if (fireController == null)
        {
            fireController = GetComponentInChildren<FireController>(true);
        }
        if (fireController == null)
        {
            Debug.LogWarning($"[Weapon] fireController is NULL! ownerType={ownerType}, weapon={name}");
        }

        fireBulletProvider = fireController as IFireBulletProvider;
        if (fireBulletProvider == null)
        {
            Debug.LogWarning("[Weapon] fireBulletProvider is NULL!");
        }
        else if (fireController is WeaponFireController unifiedFireController)
        {
            unifiedFireController.Configure(this);
        }

        if (Mathf.Approximately(weaponSettings.fireRate, 0f))
        {
            Debug.LogWarning("[Weapon] Fire Rate is ZERO, setting it to default 600.");
            weaponSettings.fireRate = 600f;
        }

        CacheAnimationDelays();

        if (ownerType == WeaponOwnerType.Player)
        {
            InitializePlayerContext(owner);
            SampleIdlePose(owner);
        }
        else if (ownerType == WeaponOwnerType.Enemy)
        {
            InitializeEnemyContext(owner);
        }
    }

    private void InitializePlayerContext(GameObject owner)
    {
        if (owner == null) return;

        recoilAnimation = owner.GetComponent<RecoilAnimation>();
        if (recoilAnimation == null)
        {
            Debug.LogWarning("[Weapon] recoilAnimation is NULL!");
        }

        characterAnimator = owner.GetComponent<Animator>();
        if (characterAnimator == null)
        {
            Debug.LogWarning("[Weapon] characterAnimator is NULL!");
        }

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

        playerManager = owner.GetComponentInParent<PlayerManager>();
        if (playerManager == null)
        {
            Debug.LogWarning("[Weapon] playerManager is NULL!");
        }

        canFireCheckProvider = playerManager as IPlayerCanFireCheckProvider;
        if (canFireCheckProvider == null)
        {
            Debug.LogWarning("[Weapon] canFireCheckProvider is NULL!");
        }
    }

    private void InitializeEnemyContext(GameObject owner)
    {
        enemyController = owner != null ? owner.GetComponentInParent<EnemyController>() : null;
        if (enemyController == null)
        {
            Debug.LogWarning("[Weapon] enemyController is NULL!");
            enemyBulletDirection = null;
            return;
        }

        enemyBulletDirection = enemyController as IGetBulletDirection;
        if (enemyBulletDirection == null)
        {
            Debug.LogWarning("[Weapon] enemyBulletDirection is NULL!");
        }
    }

    private void CacheAnimationDelays()
    {
        tacReloadDelay = 0f;
        emptyReloadDelay = 0f;
        unEquipDelay = 0f;

        if (weaponSettings == null || weaponSettings.characterController == null) return;

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
            }
        }
    }

    private void SampleIdlePose(GameObject owner)
    {
        if (owner == null || weaponSettings == null || weaponSettings.characterController == null) return;

        AnimationClip idlePose = null;
        foreach (var clip in weaponSettings.characterController.animationClips)
        {
            if (clip.name.Contains("Idle") || clip.name.Contains("Pose"))
            {
                idlePose = clip;
                break;
            }
        }

        if (idlePose != null)
        {
            idlePose.SampleAnimation(owner, 0f);
        }
    }

    public virtual void OnReload()
    {
        if (characterAnimator == null || weaponAnimator == null) return;
        if (activeAmmo == weaponSettings.ammo) return;
        if (isReloading) return;

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
        if (recoilAnimation != null)
        {
            recoilAnimation.fireMode = fireMode;
        }
    }

    public void OnEquipped_Immediate()
    {
        if (characterAnimator == null || weaponAnimator == null || recoilAnimation == null) return;

        characterAnimator.runtimeAnimatorController = weaponSettings.characterController;
        weaponAnimator.Play(IDLE, -1, 0f);
        recoilAnimation.Init(weaponSettings.recoilAnimData, weaponSettings.fireRate, fireMode);
    }

    public void OnEquipped(bool fastEquip = false)
    {
        if (characterAnimator == null || recoilAnimation == null) return;

        characterAnimator.runtimeAnimatorController = weaponSettings.characterController;
        recoilAnimation.Init(weaponSettings.recoilAnimData, weaponSettings.fireRate, fireMode);

        characterAnimator.Play(IDLE, -1, 0f);

        if (weaponSettings.hasEquipOverride)
        {
            characterAnimator.Play("IKMovement", -1, 0f);
            characterAnimator.Play(fastEquip ? EQUIP : EQUIP_OVERRIDE, -1, 0f);
            return;
        }

        characterAnimator.Play(EQUIP, -1, 0f);
    }

    public float OnUnEquipped()
    {
        if (characterAnimator != null)
        {
            characterAnimator.SetTrigger(UNEQUIP);
        }

        return unEquipDelay + 0.05f;
    }

    public void OnFirePressed()
    {
        if (canFireCheckProvider == null || !canFireCheckProvider.CanPlayerFire()) return;

        isFiring = true;
        OnFire();
    }

    public void OnFireReleased()
    {
        isFiring = false;
        if (recoilAnimation != null)
        {
            recoilAnimation.Stop();
        }
    }

    private void OnFire()
    {
        if (!isFiring || isReloading) return;

        if (activeAmmo == 0)
        {
            TryPlayEmptyFireSound();
            OnFireReleased();
            return;
        }

        if (recoilAnimation != null) recoilAnimation.Play();
        if (weaponSound != null) weaponSound.PlayFireSound();
        if (cameraAnimation != null) cameraAnimation.PlayCameraShake(weaponSettings.cameraShake);

        if (weaponSettings.useFireClip && characterAnimator != null)
        {
            characterAnimator.Play(FIRE, -1, 0f);
        }

        if (weaponAnimator != null)
        {
            weaponAnimator.Play(weaponSettings.hasFireOut && activeAmmo == 1 ? FIREOUT : FIRE, -1, 0f);
        }

        fireBulletProvider?.FireBullet();
        Invoke(nameof(ShellEject), shellEjectDelay);
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
        return fireController != null ? fireController.GetAmmoName() : string.Empty;
    }

    public WeaponOwnerType GetOwnerType()
    {
        return ownerType;
    }

    public bool TryGetEnemyFireContext(out IGetBulletDirection directionProvider)
    {
        directionProvider = enemyBulletDirection;
        return directionProvider != null;
    }

    public void EnemyFirePressed()
    {
        isFiring = true;
        EnemyFire();
    }

    private void EnemyFireReleased()
    {
        isFiring = false;
    }

    private void EnemyFire()
    {
        if (!isFiring || isReloading) return;

        if (activeAmmo == 0)
        {
            TryPlayEmptyFireSound();
            EnemyFireReleased();
            return;
        }

        if (weaponSound != null) weaponSound.PlayFireSound();

        fireBulletProvider?.FireBullet();
        Invoke(nameof(ShellEject), shellEjectDelay);
        activeAmmo--;
    }

    public void TutorialDummyPressed()
    {
        isFiring = true;
        TutorialDummyFire();
    }

    private void TutorialDummyFire()
    {
        if (!isFiring || isReloading) return;

        if (activeAmmo == 0)
        {
            TryPlayEmptyFireSound();
            EnemyReload();
            return;
        }

        if (weaponSound != null) weaponSound.PlayFireSound();

        fireBulletProvider?.FireBullet();
        Invoke(nameof(ShellEject), shellEjectDelay);
        activeAmmo--;
    }

    public virtual void EnemyReload()
    {
        if (activeAmmo == weaponSettings.ammo) return;
        if (isReloading) return;

        float delay = activeAmmo == 0 ? emptyReloadDelay : tacReloadDelay;

        if (weaponSound != null)
        {
            if (activeAmmo == 0) weaponSound.PlayWeaponSound(1);
            else weaponSound.PlayWeaponSound(0);
        }

        Invoke(nameof(ResetActiveAmmo), delay * weaponSettings.ammoResetTimeScale);
        isReloading = true;
    }

    private void ShellEject()
    {
        if (shell == null || shellEjectPoint == null) return;

        GameObject shellObj = PoolManager.Instance.Spawn(shell, shellEjectPoint.position, shellEjectPoint.rotation);
        if (shellObj == null) return;

        Rigidbody rb = shellObj.GetComponent<Rigidbody>();
        if (rb == null) return;

        Vector3 ejectDir = shellEjectPoint.right;
        Vector3 force = ejectDir * shellEjectForce + Vector3.up * shellEjectUpwardForce;

        rb.AddForce(force, ForceMode.Impulse);

        Vector3 randomTorque = Random.insideUnitSphere * shellEjectTorque;
        rb.AddTorque(randomTorque, ForceMode.Impulse);
    }

    private void TryPlayEmptyFireSound()
    {
        if (weaponSound == null) return;
        if (Time.time - lastEmptyFireSoundTime < emptyFireSoundCooldown) return;

        weaponSound.PlayWeaponSound(emptyFireSoundIndex);
        lastEmptyFireSoundTime = Time.time;
    }
}
