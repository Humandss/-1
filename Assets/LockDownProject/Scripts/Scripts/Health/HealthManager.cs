using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

public interface ICheckBodyHit
{
    void CheckBodyHit(Collider col,float ammoDamage, float ammoCriticalChance, float ammoCriticalDamMul);
}
public enum BodyParts
{
    Head,
    Thorax,
    Stomach,
    LeftArm,
    RightArm,
    LeftLeg,
    RightLeg,
}
public class HealthManager : MonoBehaviour, ICheckBodyHit
{
    [Header("Refs")]
    [SerializeField] private HealthProfile health;

    [Header("HPs")]
    private Dictionary<BodyParts, float> hp;
    private float headHP;
    private float thoraxHP;
    private float stomachHP;
    private float leftArmHP;
    private float rightArmHP;
    private float leftLegHP;
    private float rightLegHP;

    private void Awake()
    {
        InitializeHP();
    }
    private void InitializeHP()
    {
        headHP = health.headHP;
        thoraxHP = health.thoraxHP;
        stomachHP = health.stomachHP;
        leftArmHP = health.leftArmHP;
        rightArmHP = health.rightArmHP;
        leftLegHP = health.leftLegHP;
        rightLegHP = health.rightLegHP;

        hp = new Dictionary<BodyParts, float>()
        {
            {BodyParts.Head, headHP},
            {BodyParts.Thorax, thoraxHP},
            {BodyParts.Stomach, stomachHP},
            {BodyParts.LeftArm, leftArmHP},
            {BodyParts.RightArm, rightArmHP},
            {BodyParts.LeftLeg, leftLegHP},
            {BodyParts.RightLeg, rightLegHP},

        };
    }
    private void FixedUpdate()
    {
        
        CheckHP();
    }
    public void CheckBodyHit(Collider col, float ammoDamage, float ammoCriticalChance, float ammoCriticalDamMul)
    {
        Debug.Log($"[HIT] {col.name} layer={LayerMask.LayerToName(col.gameObject.layer)}");

        float totalDamage = CalculateDamage(ammoDamage, ammoCriticalChance, ammoCriticalDamMul);
       
        
        if (col.name == "head")
        {
            Debug.Log("머리에 맞음!");       
            hp[BodyParts.Head] -= totalDamage;
            
         
        }

        if(col.name == "thorax" || col.name == "thorax_back"|| col.name == "thorax_back_neck")
        {
            Debug.Log("흉부에 맞음!");
            hp[BodyParts.Thorax] -= totalDamage;
  
        }

        if (col.name == "stomach")
        {
            Debug.Log("복부에 맞음!");
            if(totalDamage > hp[BodyParts.Stomach])
            {
                float restDamage = totalDamage - hp[BodyParts.Stomach];
                hp[BodyParts.Stomach] = 0.0f;
                DistributeDamageToOtherParts(restDamage);
                
            }
            else hp[BodyParts.Stomach] -= totalDamage;

           
        }

        if (col.name == "left_arm" || col.name == "left_forearm" || col.name == "left_hand")
        {
            Debug.Log("왼팔에 맞음!");
            if (totalDamage > hp[BodyParts.LeftArm])
            {
                float restDamage = totalDamage - hp[BodyParts.LeftArm];
                hp[BodyParts.LeftArm] = 0.0f;
                DistributeDamageToOtherParts(restDamage);

            }
            else hp[BodyParts.LeftArm] -= totalDamage;
        }

        if (col.name == "right_arm" || col.name == "right_forearm" || col.name == "right_hand")
        {
            Debug.Log("오른팔에 맞음!");
            if (totalDamage > hp[BodyParts.RightArm])
            {
                float restDamage = totalDamage - hp[BodyParts.RightArm];
                hp[BodyParts.RightArm] = 0.0f;
                DistributeDamageToOtherParts(restDamage);

            }
            else hp[BodyParts.RightArm] -= totalDamage;
        }

        if (col.name == "left_thigh" || col.name == "left_shin" || col.name == "left_foot")
        {
            Debug.Log("왼다리에 맞음!");
            if (totalDamage > hp[BodyParts.LeftLeg])
            {
                float restDamage = totalDamage - hp[BodyParts.LeftLeg];
                hp[BodyParts.LeftLeg] = 0.0f;
                DistributeDamageToOtherParts(restDamage);

            }
            else hp[BodyParts.LeftLeg] -= totalDamage;
        }

        if (col.name == "right_thigh" || col.name == "right_shin" || col.name == "right_foot")
        {
            Debug.Log("오른다리에 맞음!");
            if (totalDamage > hp[BodyParts.RightLeg])
            {
                float restDamage = totalDamage - hp[BodyParts.RightLeg];
                hp[BodyParts.RightLeg] = 0.0f;
                DistributeDamageToOtherParts(restDamage);

            }
            else hp[BodyParts.LeftLeg] -= totalDamage;
        }
    }


    private void DistributeDamageToOtherParts(float damage)
    {
       
        float remaining = damage;
        int maxRound = 0;
        //잔여 피해 x, 최대 5번까지만 진행
        while (remaining > 0.0f && maxRound < 5) 
        {
            List<BodyParts> aliveParts = new List<BodyParts>();
 
            //살아 있는 부분만 계산
            foreach (var parts in hp)
            {
                if (parts.Value > 0.0f) aliveParts.Add(parts.Key);
               // Debug.Log($"살아있는 부위 ={parts}, 해당 부위 체력 ={parts.Value}");
 
            }
            if (aliveParts.Count == 0) return;

            float distributeDamage = remaining / aliveParts.Count;
            float overflowDam = 0.0f;
            // 분배 대미지 각 부분에 적용, 만약 분배 대미지 얻다가 부위 사망시 => 반복적으로 호출
            foreach (var parts in aliveParts)
            {
                if (distributeDamage > hp[parts])
                {
                    overflowDam += distributeDamage - hp[parts];
                    hp[parts] = 0.0f;
                   
                }
                else
                {
                    //부위별 대미지 적용
                    if (parts == BodyParts.Head)
                    {
                        hp[parts] -= distributeDamage * 0.3f;
                    }
                    else if(parts == BodyParts.Thorax)
                    {
                        hp[parts] -= distributeDamage * 1.0f;
                    }
                    else
                    {
                        hp[parts] -= distributeDamage*0.8f;
                    }
                       
                }

                
            }
            //테스트문
            foreach (var parts in hp)
            {
                if (parts.Value > 0.0f) Debug.Log($"살아있는 부위 ={parts.Key}, 해당 부위 체력 ={parts.Value}");

            }
            Debug.Log($"토탈 대미지= {remaining}, 각 부위별 분산 대미지 = {distributeDamage}, 잔여 대미지 ={overflowDam}");

            maxRound++;
            remaining = overflowDam;
        }
       
     
    }
    private void CheckHP()
    {
        if (hp[BodyParts.Head] <= 0.0f || hp[BodyParts.Thorax] <= 0.0f)
        {
            Debug.Log("사망");
        }
        
    }
    private float CalculateDamage(float ammoDamage, float ammoCriticalChance, float ammoCriticalDamMul)
    {
        float critcalChance = Mathf.Clamp01(ammoCriticalChance);

        bool isCritical = (UnityEngine.Random.value <= critcalChance);

        return ammoDamage + (isCritical ? ammoDamage * ammoCriticalDamMul : 0.0f);

    }
}
