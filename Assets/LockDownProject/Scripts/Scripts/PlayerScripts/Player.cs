using KINEMATION.FPSAnimationPack.Scripts.Player;
using KINEMATION.FPSAnimationPack.Scripts.Sounds;
using KINEMATION.KAnimationCore.Runtime.Core;
using KINEMATION.ProceduralRecoilAnimationSystem.Runtime;
using System;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;


public interface IStateProvider
{
    void OnReload();
    void OnJump();
    void OnFire(bool value);
    void OnAim(bool value);
    void OnChangeFireMode();
    void OnChangeWeapon();
    void OnEquipWeaponByNumberKey(bool value);

}
public interface IGetActiveWeaponProvider
{
    Weapon GetActiveWeapon();
}
[Serializable]
public struct IKTransforms
{
    public Transform tip;
    public Transform mid;
    public Transform root;
}
public class Player : MonoBehaviour, IStateProvider
{

    [Header("Refs")]
    public FPSPlayerSettings playerSettings;
    private RecoilAnimation recoilAnimation;
    private FPSPlayerSound playerSound;
    private Animator animator;
    private MovementSettings movementSettings;
   

    [Header("Interface Providers")]
    private IPlayerMoveInfoProvider movementInfoProvider;

    [Header("Skeleton")]
    [SerializeField] private Transform skeletonRoot;
    [SerializeField] private Transform weaponBone;
    [SerializeField] private Transform weaponBoneAdditive;
    [SerializeField] private Transform cameraPoint;
    [SerializeField] private IKTransforms rightHand;
    [SerializeField] private IKTransforms leftHand;
    private KTwoBoneIkData rightHandIk;
    private KTwoBoneIkData leftHandIk;

    [Header("Weapon")]
    private List<Weapon> weapons = new List<Weapon>();
    private List<Weapon> prefabComponents = new List<Weapon>();
    private int activeWeaponIndex = 0;

    [Header("Animator Weight")]
    private static int RIGHT_HAND_WEIGHT = Animator.StringToHash("RightHandWeight");
    private static int TAC_SPRINT_WEIGHT = Animator.StringToHash("TacSprintWeight");
    private static int GRENADE_WEIGHT = Animator.StringToHash("GrenadeWeight");
    private static int THROW_GRENADE = Animator.StringToHash("ThrowGrenade");
    private static int GAIT = Animator.StringToHash("Gait");
    private static int IS_IN_AIR = Animator.StringToHash("IsInAir");
    private static int INSPECT = Animator.StringToHash("Inspect");
    public float AdsWeight => adsWeight;
    private float adsWeight;

    [Header("LayerIndex")]
    private int tacSprintLayerIndex;
    private int triggerDisciplineLayerIndex;
    private int rightHandLayerIndex;

    [Header("IKMotions")]
    private float ikMotionPlayBack;
    private KTransform ikMotion = KTransform.Identity;
    private KTransform cachedIkMotion = KTransform.Identity;
    private IKMotion activeMotion;

    [Header("etc")]
    private KTransform localCameraPoint;
    private bool isAiming;
    private float smoothGait;
    private static Quaternion ANIMATED_OFFSET = Quaternion.Euler(90f, 0f, 0f);

    private void Awake()
    {
        movementSettings = GetComponentInParent<MovementSettings>();
        if (movementSettings == null)
        {
            Debug.LogWarning("[Player]  movementSettings is NULL!");
        }

        movementInfoProvider = movementSettings as IPlayerMoveInfoProvider;
        if (movementInfoProvider == null)
        {
            Debug.LogWarning("[Player]  movementInfoProvider is NULL!");
        }

        
    }

    private void Start()
    {
        animator = GetComponent<Animator>();
        recoilAnimation = GetComponent<RecoilAnimation>();
        playerSound = GetComponent<FPSPlayerSound>();

        triggerDisciplineLayerIndex = animator.GetLayerIndex("TriggerDiscipline");
        rightHandLayerIndex = animator.GetLayerIndex("RightHand");
        tacSprintLayerIndex = animator.GetLayerIndex("TacSprint");

        KTransform root = new KTransform(transform);
        localCameraPoint = root.GetRelativeTransform(new KTransform(cameraPoint), false);

        foreach (var prefab in playerSettings.weaponPrefabs)
        {
            var prefabComponent = prefab.GetComponent<Weapon>();
            if (prefabComponent == null) continue;

            prefabComponents.Add(prefabComponent);

            var instance = Instantiate(prefab, weaponBone, false);
            instance.SetActive(false);

            var component = instance.GetComponent<Weapon>();
            component.Initialize(gameObject);

            KTransform weaponT = new KTransform(weaponBone);
            component.rightHandPose = new KTransform(rightHand.tip).GetRelativeTransform(weaponT, false);

            var localWeapon = root.GetRelativeTransform(weaponT, false);

            localWeapon.rotation *= ANIMATED_OFFSET;

            component.adsPose.position = localCameraPoint.position - localWeapon.position;
            component.adsPose.rotation = Quaternion.Inverse(localWeapon.rotation);

            weapons.Add(component);
        }

        GetActiveWeapon().gameObject.SetActive(true);
        GetActiveWeapon().OnEquipped();
    }

    private void Update()
    {

        adsWeight = Mathf.Clamp01(adsWeight + playerSettings.aimSpeed * Time.deltaTime * (isAiming ? 1f : -1f));

        smoothGait = Mathf.Lerp(smoothGait, movementInfoProvider.GetDesiredGait(),
            KMath.ExpDecayAlpha(playerSettings.gaitSmoothing, Time.deltaTime));

        animator.SetFloat(GAIT, smoothGait);
        animator.SetLayerWeight(tacSprintLayerIndex, Mathf.Clamp01(smoothGait - 2f));

        bool triggerAllowed = GetActiveWeapon().weaponSettings.useSprintTriggerDiscipline;

        animator.SetLayerWeight(triggerDisciplineLayerIndex,
            triggerAllowed ? animator.GetFloat(TAC_SPRINT_WEIGHT) : 0f);

        animator.SetLayerWeight(rightHandLayerIndex, animator.GetFloat(RIGHT_HAND_WEIGHT));
      
    }

    private void SetupIkData(ref KTwoBoneIkData ikData, in KTransform target, in IKTransforms transforms,
        float weight = 1f)
    {
        ikData.target = target;

        ikData.tip = new KTransform(transforms.tip);
        ikData.mid = ikData.hint = new KTransform(transforms.mid);
        ikData.root = new KTransform(transforms.root);

        ikData.hintWeight = weight;
        ikData.posWeight = weight;
        ikData.rotWeight = weight;
    }

    private void ApplyIkData(in KTwoBoneIkData ikData, in IKTransforms transforms)
    {
        transforms.root.rotation = ikData.root.rotation;
        transforms.mid.rotation = ikData.mid.rotation;
        transforms.tip.rotation = ikData.tip.rotation;
    }

    private void ProcessOffsets(ref KTransform weaponT)
    {
        var root = transform;
        KTransform rootT = new KTransform(root);
        var weaponOffset = GetActiveWeapon().weaponSettings.ikOffset;

        float mask = 1f - animator.GetFloat(TAC_SPRINT_WEIGHT);
        weaponT.position = KAnimationMath.MoveInSpace(rootT, weaponT, weaponOffset, mask);

        var settings = GetActiveWeapon().weaponSettings;
        KAnimationMath.MoveInSpace(root, rightHand.root, settings.rightClavicleOffset, mask);
        KAnimationMath.MoveInSpace(root, leftHand.root, settings.leftClavicleOffset, mask);
    }

    private void ProcessAdditives(ref KTransform weaponT)
    {
        KTransform rootT = new KTransform(skeletonRoot);
        KTransform additive = rootT.GetRelativeTransform(new KTransform(weaponBoneAdditive), false);

        float weight = Mathf.Lerp(1f, 0.3f, adsWeight) * (1f - animator.GetFloat(GRENADE_WEIGHT));

        weaponT.position = KAnimationMath.MoveInSpace(rootT, weaponT, additive.position, weight);
        weaponT.rotation = KAnimationMath.RotateInSpace(rootT, weaponT, additive.rotation, weight);
    }

    private void ProcessRecoil(ref KTransform weaponT)
    {
        KTransform recoil = new KTransform()
        {
            rotation = recoilAnimation.OutRot,
            position = recoilAnimation.OutLoc,
        };

        KTransform root = new KTransform(transform);
        weaponT.position = KAnimationMath.MoveInSpace(root, weaponT, recoil.position, 1f);
        weaponT.rotation = KAnimationMath.RotateInSpace(root, weaponT, recoil.rotation, 1f);
    }

    private void ProcessAds(ref KTransform weaponT)
    {
        var weaponOffset = GetActiveWeapon().weaponSettings.ikOffset;
        var adsPose = weaponT;

        KTransform aimPoint = KTransform.Identity;

        aimPoint.position = -weaponBone.InverseTransformPoint(GetActiveWeapon().aimPoint.position);
        aimPoint.position -= GetActiveWeapon().weaponSettings.aimPointOffset;
        aimPoint.rotation = Quaternion.Inverse(weaponBone.rotation) * GetActiveWeapon().aimPoint.rotation;

        KTransform root = new KTransform(transform);
        adsPose.position = KAnimationMath.MoveInSpace(root, adsPose,
            GetActiveWeapon().adsPose.position - weaponOffset, 1f);
        adsPose.rotation =
            KAnimationMath.RotateInSpace(root, adsPose,
                GetActiveWeapon().adsPose.rotation, 1f);

        KTransform cameraPose = root.GetWorldTransform(localCameraPoint, false);

        float adsBlendWeight = GetActiveWeapon().weaponSettings.adsBlend;
        adsPose.position = Vector3.Lerp(cameraPose.position, adsPose.position, adsBlendWeight);
        adsPose.rotation = Quaternion.Slerp(cameraPose.rotation, adsPose.rotation, adsBlendWeight);

        adsPose.position = KAnimationMath.MoveInSpace(root, adsPose, aimPoint.rotation * aimPoint.position, 1f);
        adsPose.rotation = KAnimationMath.RotateInSpace(root, adsPose, aimPoint.rotation, 1f);

        float weight = KCurves.EaseSine(0f, 1f, adsWeight);

        weaponT.position = Vector3.Lerp(weaponT.position, adsPose.position, weight);
        weaponT.rotation = Quaternion.Slerp(weaponT.rotation, adsPose.rotation, weight);
    }

    private KTransform GetWeaponPose()
    {
        KTransform defaultWorldPose =
            new KTransform(rightHand.tip).GetWorldTransform(GetActiveWeapon().rightHandPose, false);
        float weight = animator.GetFloat(RIGHT_HAND_WEIGHT);

        return KTransform.Lerp(new KTransform(weaponBone), defaultWorldPose, weight);
    }

    private void PlayIkMotion(IKMotion newMotion)
    {
        ikMotionPlayBack = 0f;
        cachedIkMotion = ikMotion;
        activeMotion = newMotion;
    }

    private void ProcessIkMotion(ref KTransform weaponT)
    {
        if (activeMotion == null) return;

        ikMotionPlayBack = Mathf.Clamp(ikMotionPlayBack + activeMotion.playRate * Time.deltaTime, 0f,
            activeMotion.GetLength());

        Vector3 positionTarget = activeMotion.translationCurves.GetValue(ikMotionPlayBack);
        positionTarget.x *= activeMotion.translationScale.x;
        positionTarget.y *= activeMotion.translationScale.y;
        positionTarget.z *= activeMotion.translationScale.z;

        Vector3 rotationTarget = activeMotion.rotationCurves.GetValue(ikMotionPlayBack);
        rotationTarget.x *= activeMotion.rotationScale.x;
        rotationTarget.y *= activeMotion.rotationScale.y;
        rotationTarget.z *= activeMotion.rotationScale.z;

        ikMotion.position = positionTarget;
        ikMotion.rotation = Quaternion.Euler(rotationTarget);

        if (!Mathf.Approximately(activeMotion.blendTime, 0f))
        {
            ikMotion = KTransform.Lerp(cachedIkMotion, ikMotion,
                ikMotionPlayBack / activeMotion.blendTime);
        }

        var root = new KTransform(transform);
        weaponT.position = KAnimationMath.MoveInSpace(root, weaponT, ikMotion.position, 1f);
        weaponT.rotation = KAnimationMath.RotateInSpace(root, weaponT, ikMotion.rotation, 1f);
    }

    private void LateUpdate()
    {
        KAnimationMath.RotateInSpace(transform, rightHand.tip,
            GetActiveWeapon().weaponSettings.rightHandSprintOffset, animator.GetFloat(TAC_SPRINT_WEIGHT));

        KTransform weaponTransform = GetWeaponPose();

        weaponTransform.rotation = KAnimationMath.RotateInSpace(weaponTransform, weaponTransform,
            ANIMATED_OFFSET, 1f);

        KTransform rightHandTarget = weaponTransform.GetRelativeTransform(new KTransform(rightHand.tip), false);
        KTransform leftHandTarget = weaponTransform.GetRelativeTransform(new KTransform(leftHand.tip), false);

        ProcessOffsets(ref weaponTransform);
        ProcessAds(ref weaponTransform);
        ProcessAdditives(ref weaponTransform);
        ProcessIkMotion(ref weaponTransform);
        ProcessRecoil(ref weaponTransform);

        weaponBone.position = weaponTransform.position;
        weaponBone.rotation = weaponTransform.rotation;

        rightHandTarget = weaponTransform.GetWorldTransform(rightHandTarget, false);
        leftHandTarget = weaponTransform.GetWorldTransform(leftHandTarget, false);

        SetupIkData(ref rightHandIk, rightHandTarget, rightHand, playerSettings.ikWeight);
        SetupIkData(ref leftHandIk, leftHandTarget, leftHand, playerSettings.ikWeight);

        KTwoBoneIK.Solve(ref rightHandIk);
        KTwoBoneIK.Solve(ref leftHandIk);

        ApplyIkData(rightHandIk, rightHand);
        ApplyIkData(leftHandIk, leftHand);
    }
    private void EquipWeapon_Incremental()
    {
        GetActiveWeapon().gameObject.SetActive(false);
        activeWeaponIndex = activeWeaponIndex + 1 > weapons.Count - 1 ? 0 : activeWeaponIndex + 1;
        GetActiveWeapon().OnEquipped();
        Invoke(nameof(SetWeaponVisible), 0.05f);
    }

    private void EquipWeapon()
    {
        GetActiveWeapon().gameObject.SetActive(false);
        GetActiveWeapon().OnEquipped(true);
        Invoke(nameof(SetWeaponVisible), 0.05f);
    }

    private void ThrowGrenade()
    {
        GetActiveWeapon().gameObject.SetActive(false);
        Invoke(nameof(EquipWeapon), playerSettings.grenadeDelay);
    }

    private void OnLand()
    {
        animator.SetBool(IS_IN_AIR, false);
    }

    public void OnThrowGrenade()
    {
        animator.SetTrigger(THROW_GRENADE);
        Invoke(nameof(ThrowGrenade), GetActiveWeapon().UnEquipDelay);
    }

    public void OnChangeWeapon()
    {
        if (weapons.Count <= 1) return;
        float delay = GetActiveWeapon().OnUnEquipped();
        Invoke(nameof(EquipWeapon_Incremental), delay);
    }
    private void EquipWeapon_IncrementalByNumberKey(bool value)
    {
        GetActiveWeapon().gameObject.SetActive(false);
        activeWeaponIndex = value ? 0 : 1;
        GetActiveWeapon().OnEquipped();
        Invoke(nameof(SetWeaponVisible), 0.05f);
    }
    public void OnEquipWeaponByNumberKey(bool value)
    {
        if (weapons.Count <= 1) return;
        float delay = GetActiveWeapon().OnUnEquipped();
        Invoke(nameof(EquipWeapon_Incremental), delay);

        EquipWeapon_IncrementalByNumberKey(value);

    }
    public void OnChangeFireMode()
    {
        var prevFireMode = GetActiveWeapon().ActiveFireMode;
        GetActiveWeapon().OnFireModeChange();

        if (prevFireMode != GetActiveWeapon().ActiveFireMode)
        {
            playerSound.PlayFireModeSwitchSound();
            PlayIkMotion(playerSettings.fireModeMotion);
        }
    }
    public void OnFire(bool value)
    {
        if(value)
        {
            GetActiveWeapon().OnFirePressed();
            return;
        }
        else GetActiveWeapon().OnFireReleased();

    }

    public void OnAim(bool value)
    {
        bool wasAiming = isAiming;
        isAiming = value;
        recoilAnimation.isAiming = isAiming;

        if (wasAiming != isAiming)
        {
            playerSound.PlayAimSound(isAiming);
            PlayIkMotion(playerSettings.aimingMotion);
        }
    }

    public void OnReload()
    {
        GetActiveWeapon().OnReload();
    }

    public void OnJump()
    {
        animator.SetBool(IS_IN_AIR, true);
        Invoke(nameof(OnLand), 0.4f);
    }

    public void OnInspect()
    {
        animator.CrossFade(INSPECT, 0.1f);
    }

    private void SetWeaponVisible()
    {
        GetActiveWeapon().gameObject.SetActive(true);
    }

    public Weapon GetActiveWeapon()
    {
        return weapons[activeWeaponIndex];
    }

    public Weapon GetActivePrefab()
    {
        return prefabComponents[activeWeaponIndex];
    }
}
