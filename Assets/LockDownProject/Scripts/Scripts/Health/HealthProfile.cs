using UnityEditor;
using UnityEngine;

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
[System.Flags]
public enum InjuryMask
{
    None = 0,
    LightBleed = 1 << 0,
    HeavyBleed = 1 << 1,
    Fracture = 1 << 2,
    Blackout = 1 << 3,
}
[System.Serializable]
public struct BodyPartsDefault
{
    public BodyParts parts;
    public float maxHP;
    public InjuryMask allowed;
    public float damageDistributeMul;
    public float penetrationEnergyDecreaseMul;
    public float armorForBody;
    public bool startLight, startHeavy, startFrac, startBlackout;
};

[CreateAssetMenu(menuName = "Health/HP")]
public class HealthProfile : ScriptableObject
{
    /*
    [Header("Head Health")]
    public float headHP = 35.0f;
    public bool headBlackOut = false;

    [Header("Thorax Health")]
    public float thoraxHP = 85.0f;
    public bool thoraxBlackOut = false;

    [Header("Stomach Health")]
    public float stomachHP = 70.0f;
    public bool stomachBlackOut = false;

    [Header("Left arm Health")]
    public float leftArmHP = 60.0f;
    public bool leftArmLightBleeding = false;
    public bool leftArmHeavyBleeding = false;
    public bool leftArmFracture = false;
    public bool leftArmBlackOut = false;

    [Header("Right arm Health")]
    public float rightArmHP = 60.0f;
    public bool rightArmLightBleeding = false;
    public bool rightArmHeavyBleeding = false;
    public bool rightArmFracture = false;
    public bool rightArmBlackOut = false;

    [Header("Left leg Health")]
    public float leftLegHP = 65.0f;
    public bool leftLegLightBleeding = false;
    public bool leftLegHeavyBleeding = false;
    public bool leftLegFracture = false;
    public bool leftLegBlackOut = false;

    [Header("Right leg Health")]
    public float rightLegHP = 65.0f;
    public bool rightLegLightBleeding = false;
    public bool rightLegHeavyBleeding = false;
    public bool rightLegFracture = false;
    public bool rightLegBlackOut = false;*/

    public BodyPartsDefault[] parts;

    void Reset() => InitializeBodyPartsDefault();
    void OnValidate() { if (parts == null || parts.Length == 0) InitializeBodyPartsDefault(); }

    void InitializeBodyPartsDefault()
    {

        parts = new[]{
            new BodyPartsDefault{ parts=BodyParts.Head,    maxHP=35, damageDistributeMul=0.3f, penetrationEnergyDecreaseMul=0.3f, armorForBody= 25.0f, allowed=InjuryMask.Blackout, startBlackout=false },
            new BodyPartsDefault{ parts=BodyParts.Thorax,  maxHP=85, damageDistributeMul=1.0f, penetrationEnergyDecreaseMul=0.6f, armorForBody= 45.0f, allowed=InjuryMask.Blackout,startBlackout=false},
            new BodyPartsDefault{ parts=BodyParts.Stomach, maxHP=70, damageDistributeMul=0.8f, penetrationEnergyDecreaseMul=0.5f, armorForBody= 35.0f, allowed=InjuryMask.Blackout,startBlackout=false },
            new BodyPartsDefault{ parts=BodyParts.LeftArm, maxHP=60, damageDistributeMul=0.8f, penetrationEnergyDecreaseMul=0.2f, armorForBody= 15.0f, allowed=InjuryMask.LightBleed|InjuryMask.HeavyBleed|InjuryMask.Fracture,startBlackout=false },
            new BodyPartsDefault{ parts=BodyParts.RightArm,maxHP=60, damageDistributeMul=0.8f, penetrationEnergyDecreaseMul=0.2f, armorForBody= 15.0f,  allowed=InjuryMask.LightBleed|InjuryMask.HeavyBleed|InjuryMask.Fracture,startBlackout=false },
            new BodyPartsDefault{ parts=BodyParts.LeftLeg, maxHP=65, damageDistributeMul=0.8f, penetrationEnergyDecreaseMul=0.3f, armorForBody= 25.0f, allowed=InjuryMask.LightBleed|InjuryMask.HeavyBleed|InjuryMask.Fracture ,startBlackout=false},
            new BodyPartsDefault{ parts=BodyParts.RightLeg,maxHP=65, damageDistributeMul=0.8f, penetrationEnergyDecreaseMul=0.3f, armorForBody= 25.0f,  allowed=InjuryMask.LightBleed|InjuryMask.HeavyBleed|InjuryMask.Fracture ,startBlackout=false},
        };
    }

    public float lightPerTickDam = 0.5f;
    public float heavyPerTickDam = 1.0f;
    public float tickPenaltyMul = 2.0f;
    public float defaultDamageDistributeMul = 0.8f;

}
