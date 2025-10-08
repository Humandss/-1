using KINEMATION.FPSAnimationPack.Scripts.Player;
using KINEMATION.FPSAnimationPack.Scripts.Weapon;
using KINEMATION.KAnimationCore.Runtime.Core;

using System.Collections.Generic;
using System.Reflection;
using Unity.VisualScripting;
using UnityEngine;

public interface IPlayerWeaponInfoProvider
{
    bool GetTriggerState();
    float GetAimSpeed();
    float GetGaitSmoothing();

    float GetIKWeight();

    bool GetIsAimingState();

    WeaponBase GetActiveWeapon();

    KTransform GetLocalCameraPoint();

    bool GetUseSprintTriggerDiscipline();
}
public interface IPlayerWeaponStateProvider
{
    void OnFire();
    void OnReload();
    void OnAim(bool value);
}
public class PlayerWeaponController : MonoBehaviour, IPlayerWeaponInfoProvider, IPlayerWeaponStateProvider
{
    [Header("Ref")]
    public FPSPlayerSettings playerSettings;
    private PlayerSoundController playerSoundController;
    private KTransform armsRoot;
    private WeaponRigBinder rigBinder;

    private KTransform localCameraPoint;

    [Header("Providers")]
    private IWeaponRigInfoProvider weaponRigInfoProvider;
    private IPlayerSoundProvider soundProvider;


    [Header("WeaponsList")]
    private List<WeaponBase> weapons = new List<WeaponBase>();
    private List<WeaponBase> prefabComponents = new List<WeaponBase>();
    private int activeWeaponIndex = 0;

    bool triggerAllowed;
    private bool isAiming;

    private void Awake()
    {
        rigBinder = GetComponent<WeaponRigBinder>();
        if(rigBinder == null )
        {
            Debug.LogWarning("[PlayerWeaponController] rigBinder is NULL");
        }

        playerSoundController = GetComponentInParent<PlayerSoundController>();
        if (playerSoundController == null)
        {
            Debug.LogWarning("[PlayerWeaponController] playerSoundController is NULL");
        }

        weaponRigInfoProvider = rigBinder as IWeaponRigInfoProvider;
        if(weaponRigInfoProvider == null )
        {
            Debug.LogWarning("[PlayerWeaponController] weaponRigInfoProvider is NULL");
        }

        soundProvider = playerSoundController as IPlayerSoundProvider;
        if (soundProvider == null)
        {
            Debug.LogWarning("[PlayerWeaponController] soundProvider is NULL");
        }

    }
    private void Start()
    {
        /*
        armsRoot = new KTransform(transform);
        localCameraPoint = new KTransform(weaponRigInfoProvider.GetCameraPoint());

        foreach (var prefab in playerSettings.weaponPrefabs)
        {
            var wPrefab = prefab.GetComponent<WeaponBase>();
            if (wPrefab == null) continue;

            prefabComponents.Add(wPrefab);

            var instance = Instantiate(prefab, weaponRigInfoProvider.GetWeaponBone(), false);
            instance.SetActive(false);

            prefabComponents.Add(wPrefab);

            var component = instance.GetComponentInChildren<WeaponBase>();
            component.Initialize(gameObject);
            
            KTransform weaponT = new KTransform(weaponRigInfoProvider.GetWeaponBone());
            component.rightHandPose = new KTransform(weaponRigInfoProvider.GetRightHand().tip).GetRelativeTransform(weaponT, false);

            var localWeapon = armsRoot.GetRelativeTransform(weaponT, false);

            localWeapon.rotation *= rigBinder.GetAnimatedOffset();

           component.adsPose.position = weaponRigInfoProvider.GetCameraPoint().position - localWeapon.position;
           component.adsPose.rotation = Quaternion.Inverse(localWeapon.rotation);

            weapons.Add(component);
        }

        GetActiveWeapon().gameObject.SetActive(true);
  
        GetActiveWeapon().OnEquipped();*/

        armsRoot = new KTransform(transform);

        foreach (var prefab in playerSettings.weaponPrefabs)
        {
            var wPrefab = prefab.GetComponent<WeaponBase>();
            if (wPrefab == null) continue;

            // ❌ prefabComponents.Add(wPrefab); // 이건 굳이 필요 없고, 아래 중복도 제거

            // 무기를 weaponBone 밑에 생성
            var weaponBoneTf = weaponRigInfoProvider.GetWeaponBone();
            var instance = Instantiate(prefab, weaponBoneTf, false);
            instance.SetActive(false);

            var component = instance.GetComponentInChildren<WeaponBase>(true);
            component.Initialize(gameObject);

            // 기준 변환들
            var weaponT = new KTransform(weaponBoneTf);             // weaponBone의 '월드' 변환
            var rightHandTipT = new KTransform(weaponRigInfoProvider.GetRightHand().tip);
            var cameraPointT = new KTransform(weaponRigInfoProvider.GetCameraPoint());
            var rootT = new KTransform(transform);                  // armsRoot와 동일

            // 1) rightHandPose: [손 → 무기] 상대 변환(무기 기준 포즈)
            component.rightHandPose = rightHandTipT.GetRelativeTransform(weaponT, false);

            // 2) ADS 포즈: 카메라와 무기 모두 같은 'root' 기준으로 변환 후 계산
            var localWeapon = rootT.GetRelativeTransform(weaponT, false);
            localWeapon.rotation *= rigBinder.GetAnimatedOffset();  // 네가 런타임에서 항상 더하던 오프셋

            var localCamera = rootT.GetRelativeTransform(cameraPointT, false);

            component.adsPose.position = localCamera.position - localWeapon.position;
            component.adsPose.rotation = Quaternion.Inverse(localWeapon.rotation);

            weapons.Add(component);
        }

        GetActiveWeapon().gameObject.SetActive(true);
        GetActiveWeapon().OnEquipped();
    }
  
    public void OnChangeFireMode()
    {
        var prevFireMode = GetActiveWeapon().ActiveFireMode;
        GetActiveWeapon().OnFireModeChange();

        if (prevFireMode != GetActiveWeapon().ActiveFireMode)
        {
            //_playerSound.PlayFireModeSwitchSound();
           // PlayIkMotion(playerSettings.fireModeMotion);
        }
    }
    
    private void EquipWeapon()
    {
        GetActiveWeapon().gameObject.SetActive(false);
        GetActiveWeapon().OnEquipped(true);
        Invoke(nameof(SetWeaponVisible), 0.05f);
    }
    public void OnFire()
    {
        GetActiveWeapon().OnFirePressed();
        GetActiveWeapon().OnFireReleased();
    }
    public void OnReload()
    {
        GetActiveWeapon().OnReload();
    }
    public void OnAim(bool value)
    {
        bool wasAiming = isAiming;
        isAiming = value;

        GetActiveWeapon().OnAim(isAiming);

        if (wasAiming != isAiming)
        {
            soundProvider.PlayAimSound(isAiming);
            weaponRigInfoProvider.PlayIkMotion(playerSettings.aimingMotion);
            
        }
    }
    private void SetWeaponVisible()
    {
        GetActiveWeapon().gameObject.SetActive(true);
    }

    public WeaponBase GetActiveWeapon()
    {
        return weapons[activeWeaponIndex];
    }
    public bool GetIsAimingState()
    {
        return isAiming;
    }
    public WeaponBase GetActivePrefab()
    {
        return prefabComponents[activeWeaponIndex];
    }
    public bool GetTriggerState()
    {
        return triggerAllowed;
    }
    public float GetAimSpeed()
    {
        return playerSettings.aimSpeed;
    }
    public float GetGaitSmoothing()
    {
        return playerSettings.gaitSmoothing;
    }
    public float GetIKWeight()
    {
        return playerSettings.ikWeight;
    }
    public KTransform GetLocalCameraPoint()
    {
        return localCameraPoint;
    }
    public bool GetUseSprintTriggerDiscipline()
    {
        return GetActiveWeapon().weaponSettings.useSprintTriggerDiscipline;
    }
    private void Update()
    {
      // triggerAllowed = GetActiveWeapon().weaponSettings.useSprintTriggerDiscipline;
    }
}
