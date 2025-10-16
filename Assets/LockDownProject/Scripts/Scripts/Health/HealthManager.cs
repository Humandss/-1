using System;
using UnityEngine;

public interface ICheckBodyHit
{
    void CheckBodyHit(Collider col,float ammoDamage, float ammoCriticalChance, float ammoCriticalDamMul);
}
public class HealthManager : MonoBehaviour, ICheckBodyHit
{
    [Header("Refs")]
    [SerializeField] private HealthProfile health;

    [Header("HPs")]
    private float totalHP = 0.0f;
    private float headHP = 0.0f;
    private float thoraxHP = 0.0f;
    private float stomachHP = 0.0f;
    private float leftArmHP = 0.0f;
    private float rightArmHP = 0.0f;
    private float leftLegHP = 0.0f;
    private float rightLegHP = 0.0f;

    private void Awake()
    {
        InitializeHP();
    }
    private void InitializeHP()
    {
        headHP = health.headHP;
        thoraxHP= health.thoraxHP;
        stomachHP= health.stomachHP;
        leftArmHP= health.leftArmHP;
        rightArmHP= health.rightArmHP;
        leftLegHP= health.leftLegHP;
        rightLegHP= health.rightLegHP;
        totalHP = headHP + thoraxHP + stomachHP + leftArmHP + rightArmHP + leftLegHP + rightLegHP;

    }
    private void FixedUpdate()
    {
        //totalHP = headHP + thoraxHP + stomachHP + leftArmHP + rightArmHP + leftLegHP + rightLegHP;
       
    }
    public void CheckBodyHit(Collider col, float ammoDamage, float ammoCriticalChance, float ammoCriticalDamMul)
    {
        Debug.Log($"[HIT] {col.name} layer={LayerMask.LayerToName(col.gameObject.layer)}");

        float totalDamage = CalculateDamage(ammoDamage, ammoCriticalChance, ammoCriticalDamMul);
       
        if (col.name == "head")
        {
            Debug.Log("머리에 맞음!");
            headHP -= totalDamage;
            
        }
        if(col.name == "thorax" || col.name == "thorax_back"|| col.name == "thorax_back_neck")
        {
            Debug.Log("흉부에 맞음!");
            thoraxHP -= totalDamage;
        }
        if (col.name == "stomach")
        {
            Debug.Log("복부에 맞음!");
            stomachHP -= totalDamage;
            Debug.Log(stomachHP);
        }
        if (col.name == "left_arm" || col.name == "left_forearm" || col.name == "left_hand")
        {
            Debug.Log("왼팔에 맞음!");
            leftArmHP -= totalDamage;
        }
        if (col.name == "right_arm" || col.name == "right_forearm" || col.name == "right_hand")
        {
            Debug.Log("오른팔에 맞음!");
            rightArmHP -= totalDamage;
        }
        if (col.name == "left_thigh" || col.name == "left_shin" || col.name == "left_foot")
        {
            Debug.Log("왼다리에 맞음!");
            leftLegHP -= totalDamage;
        }
        if (col.name == "right_thigh" || col.name == "right_shin" || col.name == "right_foot")
        {
            Debug.Log("오른다리에 맞음!");
            rightLegHP -= totalDamage;
        }
    }

    private float CalculateDamage(float ammoDamage, float ammoCriticalChance, float ammoCriticalDamMul)
    {
        float critcalChance = Mathf.Clamp01(ammoCriticalChance);

        bool isCritical = (UnityEngine.Random.value <= critcalChance);

        return ammoDamage + (isCritical ? ammoDamage * ammoCriticalDamMul : 0.0f);

    }
}
