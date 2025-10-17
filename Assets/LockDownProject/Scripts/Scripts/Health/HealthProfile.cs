using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Health/HP")]
public class HealthProfile : ScriptableObject
{

    [Header("Head Health")]
    public float headHP = 35.0f;
    public bool headLightBleeding = false;

    [Header("Thorax Health")]
    public float thoraxHP = 85.0f;
    public bool thoraxLightBleeding = false;
    public bool thoraxFracture = false;

    [Header("Stomach Health")]
    public float stomachHP = 70.0f;
    public bool stomachLightBleeding = false;
    public bool stomachFracture = false;

    [Header("Right arm Health")]
    public float rightArmHP = 60.0f;
    public bool rightArmLightBleeding = false;
    public bool rightArmHeavyBleeding = false;
    public bool rightArmFracture = false;

    [Header("Left arm Health")]
    public float leftArmHP = 60.0f;
    public bool leftArmLightBleeding = false;
    public bool leftArmHeavyBleeding = false;
    public bool leftArmFracture = false;

    [Header("Right leg Health")]
    public float rightLegHP = 65.0f;
    public bool leftLegLightBleeding = false;
    public bool leftLegHeavyBleeding = false;
    public bool leftLegFracture = false;

    [Header("Left leg Health")]
    public float leftLegHP = 65.0f;
    public bool leftRightLightBleeding = false;
    public bool leftRightHeavyBleeding = false;
    public bool leftRightFracture = false;
}
