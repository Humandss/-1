using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Health/HP")]
public class HealthProfile : ScriptableObject
{

    [Header("Head Health")]
    public float headHP = 35.0f;

    [Header("Thorax Health")]
    public float thoraxHP = 85.0f;

    [Header("Stomach Health")]
    public float stomachHP = 70.0f;

    [Header("Right arm Health")]
    public float rightArmHP = 60.0f;

    [Header("Left arm Health")]
    public float leftArmHP = 60.0f;

    [Header("Right leg Health")]
    public float rightLegHP = 65.0f;

    [Header("Left leg Health")]
    public float leftLegHP = 65.0f;

}
