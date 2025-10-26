using UnityEngine;

public interface IArmorInfoProviders
{
    float GetArmorClass();
}
public class ArmorManager : MonoBehaviour,IArmorInfoProviders
{
    [SerializeField] private ArmorInfo armorInfo;

 
  
    public float GetArmorClass()
    {
        if (armorInfo.armorClass == ArmorClass.Level1) return 10.0f;

        if (armorInfo.armorClass == ArmorClass.Level2A) return 20.0f;

        if (armorInfo.armorClass == ArmorClass.Level2) return 30.0f;

        if (armorInfo.armorClass == ArmorClass.Level3A) return 35.0f;

        if (armorInfo.armorClass == ArmorClass.Level3) return 45.0f;

        if (armorInfo.armorClass == ArmorClass.Level4) return 55.0f;

        return 20.0f;
    }
}
