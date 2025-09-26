using KINEMATION.FPSAnimationPack.Scripts.Player;
using KINEMATION.FPSAnimationPack.Scripts.Weapon;
using System.Collections.Generic;
using UnityEngine;

namespace KINEMATION.FPSAnimationPack.Scripts.Player
{
    public interface IActiveWeaponProvider
    {
        FPSWeapon ActiveWeapon { get; }
        event System.Action<FPSWeapon> OnActiveWeaponChanged;
    }
}
public class PlayerWeaponController : MonoBehaviour, IActiveWeaponProvider
{
  
    [Header("Ref")]
    [SerializeField] private FPSPlayerSettings playerSettings;
    [SerializeField] private readonly Transform weaponBone;

    [Header("WeaponList")]
    [SerializeField] private readonly List<FPSWeapon> weapons = new ();
  
    public FPSWeapon ActiveWeapon => weapons.Count > 0 ? weapons[activeWeaponIndex] : null;
    public event System.Action<FPSWeapon> OnActiveWeaponChanged;

    private int activeWeaponIndex = 0;


    public void Start()
    {
        InitializeWeapon();
        EquippedWeapon();

    }
    private void InitializeWeapon()
    {
        weapons.Clear();

        if(playerSettings == null)
        {
            Debug.LogWarning("[PlayerWeaponController] playerSetting is NULL");
            return;
        }
        if(playerSettings.weaponPrefabs == null)
        {
            Debug.LogWarning("[PlayerWeaponController] playerSettings.weaponPrefabs is NULL");
            return;
        }
        if(weaponBone == null)
        {
            Debug.LogWarning("[PlayerWeaponController]weaponBone is NULL");
            return;
        }

        foreach (var prefab in playerSettings.weaponPrefabs)
        {
            var hasWeapon = prefab.GetComponent<FPSWeapon>();
            if (hasWeapon == null) continue;

            var instance = Instantiate(prefab, weaponBone, false);
            instance.SetActive(false);

            var weapon = instance.GetComponent<FPSWeapon>();
            weapon.Initialize(gameObject);

            weapons.Add(weapon);
        }
   
    }
    private void EquippedWeapon()
    {   
        ActiveWeapon.gameObject.SetActive(true);
        ActiveWeapon.OnEquipped();
        OnActiveWeaponChanged?.Invoke(ActiveWeapon);
    }
}
