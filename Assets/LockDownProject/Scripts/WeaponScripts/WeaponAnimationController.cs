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

    protected static int RELOAD_EMPTY = Animator.StringToHash("Reload_Empty");
    protected static int RELOAD_TAC = Animator.StringToHash("Reload_Tac");
    protected static int FIRE = Animator.StringToHash("Fire");
    protected static int FIREOUT = Animator.StringToHash("FireOut");

    protected static int EQUIP = Animator.StringToHash("Equip");
    protected static int EQUIP_OVERRIDE = Animator.StringToHash("Equip_Override");
    protected static int UNEQUIP = Animator.StringToHash("UnEquip");
    protected static int IDLE = Animator.StringToHash("Idle");
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
        weaponAnimator.Play(lastBullet
                ? FIREOUT
                : FIRE, -1, 0f);
    }
    public void Sprint()
    {

    }
    public void TacticalSprint()
    {

    }
    public void Reload()
    {
        weaponAnimator.Play(RELOAD_TAC, -1, 0f);
    }
    public void TacticalReload()
    {
        weaponAnimator.Play(RELOAD_EMPTY, -1, 0f);
    }
}
