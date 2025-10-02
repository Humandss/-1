
using UnityEngine;
using KINEMATION.KAnimationCore.Runtime.Core;
using System;
using KINEMATION.FPSAnimationPack.Scripts.Player;


[Serializable]
public struct IKTransforms
{
    public Transform tip;
    public Transform mid;
    public Transform root;
}
public interface IWeaponRigInfoProvider
{
    Transform GetWeaponBone();
    Transform GetCameraPoint();
    IKTransforms GetRightHand();

    Quaternion GetAnimatedOffset();
}
public class WeaponRigBinder : MonoBehaviour, IWeaponRigInfoProvider
{
    [Header("Ref")]
    private PlayerWeaponController weaponController;
    private PlayerAnimationController playerAnimationController;
    private WeaponBase weaponBase;

    [Header("Providers")]
    private IPlayerWeaponInfoProvider playerWeaponInfoProvider;
    private IPlayerAnimationGetFloatWeight playerAnimationWeightProvider;
    private IWeaponRecoilInfoProvider weaponRecoilInfoProvider;

    [Header("Skeleton")]
    [SerializeField] private Transform skeletonRoot;
    [SerializeField] private Transform weaponBone;
    [SerializeField] private Transform weaponBoneAdditive;
    [SerializeField] private Transform cameraPoint;
    [SerializeField] private IKTransforms rightHand;
    [SerializeField] private IKTransforms leftHand;

    private KTwoBoneIkData rightHandIk;
    private KTwoBoneIkData leftHandIk;

    private float ikMotionPlayBack;
    private KTransform ikMotion = KTransform.Identity;
    private KTransform cachedIkMotion = KTransform.Identity;
    private IKMotion activeMotion;

    private static Quaternion ANIMATED_OFFSET = Quaternion.Euler(90f, 0f, 0f);
    /*
    public void BindRecoilProvider(IWeaponRecoilInfoProvider provider)
    {
        weaponRecoilInfoProvider = provider;
        if(weaponRecoilInfoProvider == null)
        {
            Debug.Log("문제");
        }
        // 여기서 한 번 캐시 끝. 이후 매 프레임 GetComponentInChildren() 돌릴 필요 없음.
    }

    public void UnbindRecoilProvider(IWeaponRecoilInfoProvider provider)
    {
        if (weaponRecoilInfoProvider == provider) weaponRecoilInfoProvider = null;
    }*/

    private void Awake()
    {
        weaponController = GetComponent<PlayerWeaponController>();
        if(weaponController == null )
        {
            Debug.LogWarning("[WeaponRigBinder] weaponController is NULL");
        }

        playerAnimationController = GetComponent<PlayerAnimationController>();
        if (playerAnimationController == null)
        {
            Debug.LogWarning("[WeaponRigBinder]playerAnimationController is NULL");
        }

        playerWeaponInfoProvider = weaponController as IPlayerWeaponInfoProvider;
        if (playerWeaponInfoProvider == null)
        {
            Debug.LogWarning("[WeaponRigBinder] playerWeaponInfoProvider is NULL");
        }

        playerAnimationWeightProvider = playerAnimationController as IPlayerAnimationGetFloatWeight;
        if (playerAnimationWeightProvider == null)
        {
            Debug.LogWarning("[WeaponRigBinder] playerAnimationWeightProvider is NULL");
        }

     
    }
    private void Start()
    {
        weaponBase = GetComponentInChildren<WeaponBase>(true);
        if (weaponBase == null)
        {
            Debug.LogWarning("[WeaponRigBinder]weaponBase is NULL");
        }

        weaponRecoilInfoProvider = weaponBase as IWeaponRecoilInfoProvider;
        if (playerAnimationWeightProvider == null)
        {
            Debug.LogWarning("[WeaponRigBinder] weaponRecoilInfoProvider is NULL");
        }
    }
    private void LateUpdate()
    {
        KAnimationMath.RotateInSpace(transform, rightHand.tip,
                playerWeaponInfoProvider.GetActiveWeapon().weaponSettings.rightHandSprintOffset, playerAnimationWeightProvider.GetFloatTacSprintWeight());

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

        SetupIkData(ref rightHandIk, rightHandTarget, rightHand, playerWeaponInfoProvider.GetIKWeight());
        SetupIkData(ref leftHandIk, leftHandTarget, leftHand, playerWeaponInfoProvider.GetIKWeight());

        KTwoBoneIK.Solve(ref rightHandIk);
        KTwoBoneIK.Solve(ref leftHandIk);

        ApplyIkData(rightHandIk, rightHand);
        ApplyIkData(leftHandIk, leftHand);
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
        var weaponOffset = playerWeaponInfoProvider.GetActiveWeapon().weaponSettings.ikOffset;

        float mask = 1f - playerAnimationWeightProvider.GetFloatTacSprintWeight();
        weaponT.position = KAnimationMath.MoveInSpace(rootT, weaponT, weaponOffset, mask);

        var settings = playerWeaponInfoProvider.GetActiveWeapon().weaponSettings;
        KAnimationMath.MoveInSpace(root, rightHand.root, settings.rightClavicleOffset, mask);
        KAnimationMath.MoveInSpace(root, leftHand.root, settings.leftClavicleOffset, mask);
    }

    private void ProcessAdditives(ref KTransform weaponT)
    {
        KTransform rootT = new KTransform(skeletonRoot);
        KTransform additive = rootT.GetRelativeTransform(new KTransform(weaponBoneAdditive), false);

        float weight = Mathf.Lerp(1f, 0.3f, playerAnimationWeightProvider.GetFloatADSWeight()) * (1f - playerAnimationWeightProvider.GetFloatGrenadeWeight());

        weaponT.position = KAnimationMath.MoveInSpace(rootT, weaponT, additive.position, weight);
        weaponT.rotation = KAnimationMath.RotateInSpace(rootT, weaponT, additive.rotation, weight);
    }

    private void ProcessRecoil(ref KTransform weaponT)
    {
        KTransform recoil = new KTransform()
        {
            rotation = weaponBase.GetRecoilOutRot(),
            position = weaponBase.GetRecoilOutLoc(),
        };

        KTransform root = new KTransform(transform);
        weaponT.position = KAnimationMath.MoveInSpace(root, weaponT, recoil.position, 1f);
        weaponT.rotation = KAnimationMath.RotateInSpace(root, weaponT, recoil.rotation, 1f);
    }

    private void ProcessAds(ref KTransform weaponT)
    {
        var weaponOffset = playerWeaponInfoProvider.GetActiveWeapon().weaponSettings.ikOffset;
        var adsPose = weaponT;

        KTransform aimPoint = KTransform.Identity;

        aimPoint.position = -weaponBone.InverseTransformPoint(playerWeaponInfoProvider.GetActiveWeapon().aimPoint.position);
        aimPoint.position -= playerWeaponInfoProvider.GetActiveWeapon().weaponSettings.aimPointOffset;
        aimPoint.rotation = Quaternion.Inverse(weaponBone.rotation) * playerWeaponInfoProvider.GetActiveWeapon().aimPoint.rotation;

        KTransform root = new KTransform(transform);
        adsPose.position = KAnimationMath.MoveInSpace(root, adsPose,
            playerWeaponInfoProvider.GetActiveWeapon().adsPose.position - weaponOffset, 1f);
        adsPose.rotation =
            KAnimationMath.RotateInSpace(root, adsPose,
                playerWeaponInfoProvider.GetActiveWeapon().adsPose.rotation, 1f);

        KTransform cameraPose = root.GetWorldTransform(playerWeaponInfoProvider.GetLocalCameraPoint(), false);

        float adsBlendWeight = playerWeaponInfoProvider.GetActiveWeapon().weaponSettings.adsBlend;
        adsPose.position = Vector3.Lerp(cameraPose.position, adsPose.position, adsBlendWeight);
        adsPose.rotation = Quaternion.Slerp(cameraPose.rotation, adsPose.rotation, adsBlendWeight);

        adsPose.position = KAnimationMath.MoveInSpace(root, adsPose, aimPoint.rotation * aimPoint.position, 1f);
        adsPose.rotation = KAnimationMath.RotateInSpace(root, adsPose, aimPoint.rotation, 1f);

        float weight = KCurves.EaseSine(0f, 1f, playerAnimationWeightProvider.GetFloatADSWeight());

        weaponT.position = Vector3.Lerp(weaponT.position, adsPose.position, weight);
        weaponT.rotation = Quaternion.Slerp(weaponT.rotation, adsPose.rotation, weight);
    }

    private KTransform GetWeaponPose()
    {
        KTransform defaultWorldPose =
            new KTransform(rightHand.tip).GetWorldTransform(playerWeaponInfoProvider.GetActiveWeapon().rightHandPose, false);
        float weight = playerAnimationWeightProvider.GetFloatRightHandWeight();

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
    public Transform GetWeaponBone()
    {
        return weaponBone;
    }
    public Transform GetCameraPoint()
    {
        return cameraPoint;
    }
    public IKTransforms GetRightHand()
    {
        return rightHand;
    }
    public Quaternion GetAnimatedOffset()
    {
        return ANIMATED_OFFSET;
    }
    /*
    [Header("Refs")]
    [SerializeField] private PlayerWeaponController weaponControllerRef;  
    [SerializeField] private PlayerAnimationController animationControllerRef;

    [Header("Skeleton / Mounts")]
    [SerializeField] private Transform skeletonRoot;
    [SerializeField] private Transform weaponBone;
    [SerializeField] private Transform weaponBoneAdditive;
    [SerializeField] private Transform cameraPoint;
    [SerializeField] private IKTransforms rightHand;
    [SerializeField] private IKTransforms leftHand;

    [Header("Providers")]
    private WeaponSettings currentWeapon;

    [Header("IKData")]
    private KTwoBoneIkData rightHandIk;
    private KTwoBoneIkData leftHandIk;

    [Header("Motions")]
    private float ikMotionPlayBack;
    private KTransform ikMotion = KTransform.Identity;
    private KTransform cachedIkMotion = KTransform.Identity;
    private IKMotion activeMotion;

    [Header("Tuning")]
    [SerializeField] private float handPoseWeight = 1.0f;  
    [SerializeField] private float ikPosWeight = 1.0f;
    [SerializeField] private float ikRotWeight = 1.0f;
    private static Quaternion ANIMATED_OFFSET = Quaternion.Euler(90f, 0f, 0f);

    private KTransform localCameraPoint;
   
    private void Awake()
    {

        activeWeaponProvider = weaponControllerRef as IActiveWeaponProvider;

        KTransform root = new KTransform(transform);
        localCameraPoint = root.GetRelativeTransform(new KTransform(cameraPoint), false);


    }
    
    public void UpdateWeaponRig(WeaponSettings weapon)
    {
        currentWeapon = weapon;

        if (currentWeapon == null || weaponBone == null || rightHand.tip == null) return;

        // 오른손 그립 포즈(무기 로컬)
        currentWeapon.rightHandPose = new KTransform(rightHand.tip).GetRelativeTransform(new KTransform(weaponBone), false);

        // ADS 포즈(카메라 로컬 기준)
        var root = new KTransform(transform);
        var weaponT = new KTransform(weaponBone);
        var localWeapon = root.GetRelativeTransform(weaponT, false);
        localWeapon.rotation *= ANIMATED_OFFSET;

        currentWeapon.adsPose.position = localCameraPoint.position - localWeapon.position;
        currentWeapon.adsPose.rotation = Quaternion.Inverse(localWeapon.rotation);
    }
    private void LateUpdate()
    { 
        KTransform weaponTransform = GetWeaponPose();

        weaponTransform.rotation = KAnimationMath.RotateInSpace(weaponTransform, weaponTransform,
            ANIMATED_OFFSET, 1f);

        KTransform rightHandTarget = weaponTransform.GetRelativeTransform(new KTransform(rightHand.tip), false);
        KTransform leftHandTarget = weaponTransform.GetRelativeTransform(new KTransform(leftHand.tip), false);

        weaponBone.position = weaponTransform.position;
        weaponBone.rotation = weaponTransform.rotation;

        rightHandTarget = weaponTransform.GetWorldTransform(rightHandTarget, false);
        leftHandTarget = weaponTransform.GetWorldTransform(leftHandTarget, false);

        SetupIkData(ref rightHandIk, rightHandTarget, rightHand);
        SetupIkData(ref leftHandIk, leftHandTarget, leftHand);

        KTwoBoneIK.Solve(ref rightHandIk);
        KTwoBoneIK.Solve(ref leftHandIk);

        ApplyIkData(rightHandIk, rightHand);
        ApplyIkData(leftHandIk, leftHand);
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
    private void PlayIkMotion(IKMotion newMotion)
    {
        ikMotionPlayBack = 0f;
        cachedIkMotion = ikMotion;
        activeMotion = newMotion;
    }
    private KTransform GetWeaponPose()
    {
        if (weaponBone == null)
        {
            Debug.LogError("[WeaponRigBinder] weaponBone is NULL (GetWeaponPose)", this);
            return new KTransform(transform);
        }
        if (rightHand.tip == null)
        {
            Debug.LogError("[WeaponRigBinder] rightHand.tip is NULL (GetWeaponPose)", this);
            return new KTransform(weaponBone);
        }
        if (activeWeaponProvider == null)
        {
            Debug.LogWarning("[WeaponRigBinder] activeWeaponProvider is NULL (GetWeaponPose)", this);
            return new KTransform(weaponBone);
        }
        var activeWeapon = activeWeaponProvider.GetActiveWeapon();
        if (activeWeapon == null)
        {
            Debug.LogWarning("[WeaponRigBinder] GetActiveWeapon() returned NULL (GetWeaponPose)", this);
            return new KTransform(weaponBone);
        }
        KTransform defaultWorldPose = new KTransform(rightHand.tip).
            GetWorldTransform(activeWeaponProvider.GetActiveWeapon().rightHandPose, false);
        float weight = animationWeightProvider.RightHandWeight();
       
        return KTransform.Lerp(new KTransform(weaponBone), defaultWorldPose, weight);
    }
   */

}
