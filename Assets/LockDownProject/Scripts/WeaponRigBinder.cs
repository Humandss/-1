/*
using UnityEngine;
using KINEMATION.KAnimationCore.Runtime.Core;
using System;
using KINEMATION.FPSAnimationPack.Scripts.Player;
using KINEMATION.FPSAnimationPack.Scripts.Weapon;
using UnityEditor;
using UnityEditor.VersionControl;

[Serializable]
public struct IKTransforms
{
    public Transform tip;
    public Transform mid;
    public Transform root;
}

public class WeaponRigBinder : MonoBehaviour
{
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
   

}
*/