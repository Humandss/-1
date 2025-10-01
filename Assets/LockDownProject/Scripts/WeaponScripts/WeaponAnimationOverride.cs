using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponAnimationOverride
{
    public static AnimatorOverrideController CreateOverride(RuntimeAnimatorController baseCtrl, WeaponAnimationClip p)
    {
        var aoc = new AnimatorOverrideController(baseCtrl);
        var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>(aoc.overridesCount);

        // 이름 매핑(상태명 == 클립명 기준)
        var map = new Dictionary<string, AnimationClip> {
            {"Idle",     p.Idle},
            {"AimIn",    p.AimIn    ? p.AimIn  : p.Idle},
            {"AimOut",   p.AimOut   ? p.AimOut : p.Idle},
            {"Fire",     p.Fire},
            {"Reload",   p.Reload},
            {"Equip",    p.Equip    ? p.Equip  : p.Idle},
            {"Unequip",  p.Unequip  ? p.Unequip: p.Idle},
        };

        foreach (var baseClip in aoc.animationClips)
        {
            var key = baseClip.name; // 베이스 컨트롤러의 state clip 이름
            map.TryGetValue(key, out var repl);
            overrides.Add(new KeyValuePair<AnimationClip, AnimationClip>(baseClip, repl ? repl : baseClip));
        }

        aoc.ApplyOverrides(overrides);
        return aoc;
    }
}
