using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;

public interface IHealthStateProvider
{
    void GetBluntDamage(float bluntDam);
    int GetNumberHeavyBleeding();
    int GetNumberLightBleeding();

    float GetMaxHP();
    float GetTotalHP();
    bool GetIsLeftArmFracture();
    bool GetIsRightArmFracture();
    bool GetIsLeftLegFracture();
    bool GetIsRightLegFracture();

    bool GetIsLeftArmBlackout();
    bool GetIsRightArmBlackout();
    bool GetIsLeftLegBlackout();
    bool GetIsRightLegBlackout();
}
public interface ICheckBodyHit
{
    void CheckBodyHit(Collider col,float ammoDamage, float ammoCriticalChance, float ammoCriticalDamMul);
    void CheckEffectTrigger(Collider col, float lightBleedingChance, float heavyBleedingChance, float fractureChance);
}
public class HealthManager : MonoBehaviour, ICheckBodyHit, IHealthStateProvider
{
    [Header("Refs")]
    [SerializeField] private HealthProfile health;

    [Header("Health Component")]
    private float totalHP = 0.0f;
    private Dictionary<BodyParts, float> hp = new();
    private Dictionary<BodyParts, float> damMul = new();
    private Dictionary<BodyParts, InjuryMask> allowedInjury = new();
    private struct LimbStatus { public bool light, heavy, fracture, blackout; }
    private Dictionary<BodyParts, LimbStatus> status = new();

    /*
    private float headHP;
    private float thoraxHP;
    private float stomachHP;
    private float leftArmHP;
    private float rightArmHP;
    private float leftLegHP;
    private float rightLegHP;
    */

    [Header("Time")]
    [SerializeField] float tickInterval =2.5f;
    float nextTick;

    private void Awake()
    {
        InitializeHealthProfile();
        GetMaxHP();
     }
    private void InitializeHealthProfile()
    {
        hp.Clear(); damMul.Clear(); allowedInjury.Clear(); status.Clear();

        foreach (var p in health.parts)
        {
            hp[p.parts] = Mathf.Max(0, p.maxHP);
            damMul[p.parts] = (p.damageDistributeMul <= 0f) ? 1f : p.damageDistributeMul;
            allowedInjury[p.parts] = p.allowed;
            status[p.parts] = new LimbStatus
            {
                light = p.startLight,
                heavy = p.startHeavy,
                fracture = p.startFrac,
                blackout = p.startBlackout
            };
        }
        /*
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

        };*/
    }
    private void FixedUpdate()
    {
        CheckHP();
        if (Time.time >= nextTick)
        {
            
            nextTick = Time.time + tickInterval;
            CheckBleedingEffects();
            CheckBlackoutEffects();
            Debug.Log($"머리 체력 : {hp[BodyParts.Head]}, 흉부 체력 : {hp[BodyParts.Thorax]}");

        }
        
   
    }
    private void CheckBlackoutEffects()
    {
        foreach(var parts in hp)
        {
            if (parts.Value <= 0)
            {
                if (parts.Key == BodyParts.Head)
                {
                    var s = status[BodyParts.Head];
                    s.blackout = true;
                    status[BodyParts.Head] = s;
                }
                if (parts.Key == BodyParts.Thorax)
                {
                    var s = status[BodyParts.Thorax];
                    s.blackout = true;
                    status[BodyParts.Thorax] = s;
                }
                if (parts.Key == BodyParts.Stomach)
                {
                    var s = status[BodyParts.Stomach];
                    s.blackout = true;
                    status[BodyParts.Stomach] = s;
                }
                if (parts.Key == BodyParts.LeftArm)
                {
                    var s = status[BodyParts.LeftArm];
                    s.blackout = true;
                    status[BodyParts.LeftArm] = s;
                }
                if (parts.Key == BodyParts.RightArm)
                {
                    var s = status[BodyParts.RightArm];
                    s.blackout = true;
                    status[BodyParts.RightArm] = s;
                }
                if (parts.Key == BodyParts.LeftLeg)
                {
                    var s = status[BodyParts.LeftLeg];
                    s.blackout = true;
                    status[BodyParts.LeftLeg] = s;
                }
                if (parts.Key == BodyParts.RightLeg)
                {
                    var s = status[BodyParts.RightLeg];
                    s.blackout = true;
                    status[BodyParts.RightLeg] = s;
                }
            }       
           
        }
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

 
    public void CheckEffectTrigger(Collider col, float lightBleedingChance, float heavyBleedingChance, float fractureChance)
    {
        if (col.name == "Head") return;

        if (col.name == "Thorax") return;

        if (col.name == "Stomach") return;

        bool isLightBleeding = (UnityEngine.Random.value <= lightBleedingChance);
        bool isHeavyBleeding = (UnityEngine.Random.value <= heavyBleedingChance);
        bool isFracture = (UnityEngine.Random.value <= fractureChance);
        //확률 안걸리면 스킵
        if (!isLightBleeding && !isHeavyBleeding && !isFracture) return;
        //상호베타, 과다출혈과 일반 출혈 동시 발생시 과다출혈만 인정
        if (isLightBleeding && isHeavyBleeding) isLightBleeding = false; 

        if (col.name == "LeftArm")
        {
            var s = status[BodyParts.LeftArm];

            if(isLightBleeding) s.light = true; 
           
            if(isHeavyBleeding) s.heavy = true; 
           
            if(isFracture) s.fracture = true;

            status[BodyParts.LeftArm] = s;

            return;
        }

        if (col.name == "RightArm")
        {
            var s = status[BodyParts.RightArm];

            if (isLightBleeding) s.light= true; 

            if (isHeavyBleeding) s.heavy = true; 

            if (isFracture) s.fracture = true;

            status[BodyParts.RightArm] = s;

            return;
        }

        if (col.name == "LeftLeg")
        {
            var s = status[BodyParts.LeftLeg];

            if (isLightBleeding) s.light = true; 

            if (isHeavyBleeding) s.heavy = true; 
        
            if (isFracture) s.fracture = true;

            status[BodyParts.LeftLeg] = s;

            return;
        }

        if (col.name == "RightLeg")
        {
            var s = status[BodyParts.RightLeg];

            if (isLightBleeding) s.light = true;
      
            if (isHeavyBleeding) s.heavy = true;
       
            if (isFracture) s.fracture = true;

            status[BodyParts.RightLeg] = s;

            return;
        }
    
    }
    
    private void CheckBleedingEffects()
    {
        var aliveParts = GetAliveParts();
        //왼팔
        if (status[BodyParts.LeftArm].light || status[BodyParts.LeftArm].heavy)
        {
            float tickDam = (status[BodyParts.LeftArm].light? health.lightPerTickDam : health.heavyPerTickDam);
        
            if (hp[BodyParts.LeftArm] <= 0.0f)
            {
                foreach (var part in aliveParts)
                {
                    hp[part] = Mathf.Max(0, hp[part]-(tickDam + health.tickPenaltyMul));
                }
            }
            else
            {
                foreach (var part in aliveParts)
                {
                    
                    if (part == BodyParts.LeftArm)
                    {
                        hp[part] = Mathf.Max(0, hp[part] - (tickDam + 1.0f));
                    }
                    else hp[part] = Mathf.Max(0, hp[part] - tickDam); 
                }
            }
          
        }
       
        //오른팔
        if(status[BodyParts.RightArm].light || status[BodyParts.RightArm].heavy)
        {
            float tickDam = (status[BodyParts.RightArm].light ? health.lightPerTickDam : health.heavyPerTickDam);

            if (hp[BodyParts.RightArm] <= 0.0f)
            {
                foreach (var part in aliveParts)
                {
                    hp[part] = Mathf.Max(0, hp[part] - (tickDam + health.tickPenaltyMul));
                }
            }
            else
            {
                foreach (var part in aliveParts)
                {
                    if (part == BodyParts.RightArm)
                    {
                        hp[part] = Mathf.Max(0, hp[part] - (tickDam + 1.0f));
                    }
                    else hp[part] = Mathf.Max(0, (hp[part] - tickDam));
                }
            }
        }
       
        //왼다리
        if (status[BodyParts.LeftLeg].light || status[BodyParts.LeftLeg].heavy)
        {
            float tickDam = (status[BodyParts.LeftLeg].light ? health.lightPerTickDam : health.heavyPerTickDam);

            if (hp[BodyParts.LeftLeg] <= 0.0f)
            {
                foreach (var part in aliveParts)
                {
                    hp[part] = Mathf.Max(0, hp[part] - (tickDam + health.tickPenaltyMul));
                }
            }
            else
            {
                foreach (var part in aliveParts)
                {
                    if (part == BodyParts.LeftLeg)
                    {
                        hp[part] = Mathf.Max(0, hp[part] - (tickDam + 1.0f));
                    }
                    else hp[part] = Mathf.Max(0, hp[part] - tickDam);
                }
            }
        }
       
        //오른다리
        if (status[BodyParts.RightLeg].light || status[BodyParts.RightLeg].heavy)
        {
            float tickDam = (status[BodyParts.RightLeg].light ? health.lightPerTickDam : health.heavyPerTickDam);

            if (hp[BodyParts.RightLeg] <= 0.0f)
            {
                foreach (var part in aliveParts)
                {
                    hp[part] = Mathf.Max(0, hp[part] - (tickDam + health.tickPenaltyMul));
                }
            }
            else
            {
                foreach (var part in aliveParts)
                {
                    if (part == BodyParts.RightLeg)
                    {
                        hp[part] = Mathf.Max(0, hp[part] - (tickDam + 1.0f));
                    }
                    else hp[part] = Mathf.Max(0, hp[part] - tickDam);
                }
            }
        }

    }

    private List<BodyParts> GetAliveParts()
    {
        List<BodyParts> aliveParts = new List<BodyParts>();

        //살아 있는 부분만 계산
        foreach (var parts in hp)
        {
            if (parts.Value > 0.0f) aliveParts.Add(parts.Key);
            // Debug.Log($"살아있는 부위 ={parts}, 해당 부위 체력 ={parts.Value}");

        }

        return aliveParts;
    }
    private List<BodyParts> GetAllParts()
    {
        List<BodyParts> allParts = new List<BodyParts>();


        foreach (var parts in hp)
        {
            allParts.Add(parts.Key);
            
        }

        return allParts;
    }
    private void DistributeDamageToOtherParts(float damage)
    {
       
        float remaining = damage;
       // Debug.Log(damage);
        int maxRound = 0;
        //잔여 피해 x, 최대 5번까지만 진행
        while (remaining > 1.0f && maxRound < 5) 
        {

            var allParts = GetAllParts();
            if (allParts.Count == 0) return;

            float distributeDamage = remaining / allParts.Count;
            //너무 낮은 분산 대미지는 패스
            if (distributeDamage < 0.05f) break;

            float overflowDam = 0.0f;
            // 분배 대미지 각 부분에 적용, 만약 분배 대미지 얻다가 부위 사망시 => 반복적으로 호출
            foreach (var parts in allParts)
            {
                if (distributeDamage > hp[parts])
                {
                    overflowDam += distributeDamage - hp[parts];
                    //머리와 흉부는 초과 대미지 받을시 -로 => 사망
                    if (parts == BodyParts.Head)
                    {
                        hp[parts] -= 1.0f;

                    }
                    else if (parts == BodyParts.Thorax)
                    {
                        hp[parts] -= 1.0f;
                    }
                    else hp[parts] = 0.0f;

                }
                else
                {
                    //부위별 대미지 적용
                    if (parts == BodyParts.Head)
                    {                      
                        hp[parts] -= distributeDamage * damMul[BodyParts.Head];
                                               
                    }
                    else if(parts == BodyParts.Thorax)
                    {
                        hp[parts] -= distributeDamage * damMul[BodyParts.Thorax];
                    }
                    else
                    {                     
                        hp[parts] -= distributeDamage * health.defaultDamageDistributeMul;  
                    }
                       
                }
                Debug.Log($"현재 부위 ={parts}, 체력 ={hp[parts]} 받은 대미지 ={distributeDamage}");

            }
            /*//테스트문
            foreach (var parts in hp)
            {
                if (parts.Value > 0.0f) Debug.Log($"살아있는 부위 ={parts.Key}, 해당 부위 체력 ={parts.Value}");

            }*/
           // Debug.Log($"토탈 대미지= {remaining}, 각 부위별 분산 대미지 = {distributeDamage}, 잔여 대미지 ={overflowDam}");

            maxRound++;
            remaining = overflowDam;
        }
       
     
    }
    private void CheckHP()
    {
        totalHP = GetTotalHP();
       
        if (hp[BodyParts.Head] < 0.0f || hp[BodyParts.Thorax] < 0.0f || totalHP <= 0)
        {
            Debug.Log("사망");
        }
        
    }
    public void GetBluntDamage(float bluntDam)
    {
        
        float distributeDam = bluntDam / (float)GetAllParts().Count;

        var parts = GetAllParts();
        Debug.Log(distributeDam);
        foreach (var part in parts)
        {
            hp[part] -= distributeDam;
        }
     }
    public float GetTotalHP()
    {
        float totalHP = 0.0f;
        foreach (var part in hp)
        {
            totalHP += part.Value;
        }

        return totalHP;
    }
    public float GetMaxHP()
    {
        float maxHP = 0.0f;
        foreach (var part in hp)
        {
            maxHP += part.Value;
        }

        return maxHP;
    }
    private float CalculateDamage(float ammoDamage, float ammoCriticalChance, float ammoCriticalDamMul)
    {
 
        bool isCritical = (UnityEngine.Random.value <= ammoCriticalChance);

        return ammoDamage + (isCritical ? ammoDamage * ammoCriticalDamMul : 0.0f);

    }
    
  
    public bool GetIsLeftArmFracture()
    {
        return status[BodyParts.LeftArm].fracture;
    }
    public bool GetIsRightArmFracture()
    {
        return status[BodyParts.RightArm].fracture;
    }
    public bool GetIsLeftLegFracture()
    {
        return status[BodyParts.LeftLeg].fracture;
    }
    public bool GetIsRightLegFracture()
    {
        return status[BodyParts.RightLeg].fracture;
    }
    public bool GetIsLeftArmBlackout()
    {
        return status[BodyParts.LeftArm].blackout;
    }
    public bool GetIsRightArmBlackout()
    {
        return status[BodyParts.RightArm].blackout;
    }
    public bool GetIsLeftLegBlackout()
    {
        return status[BodyParts.LeftLeg].blackout;
    }
    public bool GetIsRightLegBlackout()
    {
        return status[BodyParts.RightLeg].blackout;
    }
    public int GetNumberLightBleeding()
    {
        int count = 0;
        foreach(var parts in status)
        {
            if (parts.Value.light) count++;
        }

        return count;
    }
    public int GetNumberHeavyBleeding()
    {
        int count = 0;
        foreach (var parts in status)
        {
            if (parts.Value.heavy) count++;
        }

        return count;
    }
}
