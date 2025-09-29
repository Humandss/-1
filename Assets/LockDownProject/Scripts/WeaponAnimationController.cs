using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IWeaponAnimator
{
    void Aim();
    void Fire(bool lastBullet);
    void Reload();
    void TacticalReload();
    void Equip(bool instant);
    void Unequip();
    void Sprint();
    void TacticalSprint();
}
public class WeaponAnimationController : MonoBehaviour, IWeaponAnimator
{
   protected Animator weaponAnimator;

    private void Awake()
    {
        weaponAnimator = GetComponent<Animator>();
    }
    public void Unequip()
    {

    }
    public void Equip(bool instant)
    {

    }
    public void Aim()
    {

    }
    public void Fire(bool lastBullet)
    {

    }
    public void Sprint()
    {

    }
    public void TacticalSprint()
    {

    }
    public void Reload()
    {

    }
    public void TacticalReload()
    {

    }
}
