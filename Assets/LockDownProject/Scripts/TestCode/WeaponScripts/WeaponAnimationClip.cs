using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponAnimationClip : ScriptableObject
{
    [Header("Clips (Map to base controller states)")]
    public AnimationClip Idle;
    public AnimationClip AimIn;
    public AnimationClip AimOut;
    public AnimationClip Fire;
    public AnimationClip FireOut;
    public AnimationClip Reload;
    public AnimationClip Equip;
    public AnimationClip Unequip;
}
