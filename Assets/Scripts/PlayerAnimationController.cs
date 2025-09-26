using KINEMATION.KAnimationCore.Runtime.Core;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

public interface IAnimationWeightProvider
{
    float RightHandWeight { get; }
}
public class PlayerAnimationController : MonoBehaviour, IAnimationWeightProvider
{
    [Header("Ref")]
    [SerializeField] private Animator animator;

    private static int RIGHT_HAND_WEIGHT = Animator.StringToHash("RightHandWeight");
    private static int TAC_SPRINT_WEIGHT = Animator.StringToHash("TacSprintWeight");
    private static int GRENADE_WEIGHT = Animator.StringToHash("GrenadeWeight");
    private static int GAIT = Animator.StringToHash("Gait");
    private static int IS_IN_AIR = Animator.StringToHash("IsInAir");
    private static int INSPECT = Animator.StringToHash("Inspect");

    private static Quaternion ANIMATED_OFFSET = Quaternion.Euler(90f, 0f, 0f);

    private int tacSprintLayerIndex;
    private int triggerDisciplineLayerIndex;
    private int rightHandLayerIndex;

    private bool isAiming;
    private float smoothGait;
    private bool bSprinting;
    private bool bTacSprinting;
    //Event ÇÔ¼ö
    public float RightHandWeight => animator ? animator.GetFloat(RIGHT_HAND_WEIGHT) : 1.0f;
    private void Awake()
    {
        animator = GetComponent<Animator>();

        triggerDisciplineLayerIndex = animator.GetLayerIndex("TriggerDiscipline");
        rightHandLayerIndex = animator.GetLayerIndex("RightHand");
        tacSprintLayerIndex = animator.GetLayerIndex("TacSprint");
    }
    private void Start()
    {
       
    }
  
    private void Update()
    {

    }
}
