using KINEMATION.FPSAnimationPack.Scripts.Player;
using KINEMATION.FPSAnimationPack.Scripts.Weapon;
using KINEMATION.KAnimationCore.Runtime.Core;
using KINEMATION.ProceduralRecoilAnimationSystem.Runtime;
using System.Collections.Generic;
using UnityEngine;

public interface IPlayerWeaponInfoProvider
{
    bool GetTriggerState();
    float GetAimSpeed();
    float GetGaitSmoothing();

    WeaponBase GetActiveWeapon();
}
public class PlayerWeaponController : MonoBehaviour, IPlayerWeaponInfoProvider
{
    [Header("Ref")]
    private FPSPlayerSettings playerSettings;
    [SerializeField] private KTransform armsRoot;
    private WeaponRigBinder rigBinder;

    private KTransform _localCameraPoint;
    [Header("Providers")]
    private IWeaponRigInfoProvider weaponRigInfoProvider;

    [Header("WeaponsList")]
    private List<WeaponBase> weapons = new List<WeaponBase>();
    private List<WeaponBase> prefabComponents = new List<WeaponBase>();
    private int activeWeaponIndex = 0;

    bool triggerAllowed;


    private void Awake()
    {
        rigBinder = GetComponent<WeaponRigBinder>();
        if(rigBinder == null )
        {
            Debug.LogWarning("[PlayerWeaponController] rigBinder is NULL");
        }

        weaponRigInfoProvider = rigBinder as IWeaponRigInfoProvider;
        if(weaponRigInfoProvider == null )
        {
            Debug.LogWarning("[PlayerWeaponController] weaponRigInfoProvider is NULL");
        }
    }
    private void Start()
    {
        _localCameraPoint = armsRoot.GetRelativeTransform(new KTransform(weaponRigInfoProvider.GetCameraPoint()), false);

        foreach (var prefab in playerSettings.weaponPrefabs)
        {
            var wPrefab = prefab.GetComponent<WeaponBase>();
            if (wPrefab == null) continue;

            prefabComponents.Add(wPrefab);

            var instance = Instantiate(prefab, weaponRigInfoProvider.GetWeaponBone(), false);
            instance.SetActive(false);

            var component = instance.GetComponent<WeaponBase>();
            component.Initialize(gameObject);

            KTransform weaponT = new KTransform(weaponRigInfoProvider.GetWeaponBone());
            component.rightHandPose = new KTransform(weaponRigInfoProvider.GetRightHand().tip).GetRelativeTransform(weaponT, false);

            var localWeapon = armsRoot.GetRelativeTransform(weaponT, false);

            localWeapon.rotation *= ANIMATED_OFFSET;

            component.adsPose.position = _localCameraPoint.position - localWeapon.position;
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
    public void OnReload()
    {
        GetActiveWeapon().OnReload();
    }

    private void SetWeaponVisible()
    {
        GetActiveWeapon().gameObject.SetActive(true);
    }

    public WeaponBase GetActiveWeapon()
    {
        return weapons[activeWeaponIndex];
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
    private void Update()
    {
        triggerAllowed = GetActiveWeapon().weaponSettings.useSprintTriggerDiscipline;
    }
}
