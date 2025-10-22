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
        return armorInfo.armorClass;
    }
}
