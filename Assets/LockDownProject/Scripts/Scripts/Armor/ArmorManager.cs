using UnityEngine;

public interface IArmorInfoProviders
{
    float GetArmorClass();
    void HandleArmorDurabilityAfterHit(float ammoArmorDamage, bool isPen);

    void HandleArmorDurabilityAfterRicochet(float ammoArmorDamage);
}
public class ArmorManager : MonoBehaviour,IArmorInfoProviders
{
    [SerializeField] private ArmorInfo armorInfo;

    private float durability = 0.0f;
    private static float penArmorDamFactor = 0.5f;
    private static float nonPenArmorDamFactor = 0.25f;
    private static float ricochetArmorDamFactor = 0.1f;

    private void Awake()
    {
        durability = armorInfo.durability;
    }

    private float GetArmorDurabilityDecreaseMulByMaterial()
    {
        if (armorInfo.material == ArmorMaterial.Titanium) return 0.15f;

        if (armorInfo.material == ArmorMaterial.Ceramic) return 0.6f;

        if (armorInfo.material == ArmorMaterial.Composite) return 0.25f;

        if (armorInfo.material == ArmorMaterial.Steel) return 0.2f;

        if (armorInfo.material == ArmorMaterial.Kevlar) return 0.1f;

        return 0.3f;

    }
    public void HandleArmorDurabilityAfterHit(float ammoArmorDamage, bool isPen)
    {
        if (durability <= 0.0f) return;

        float decreaseMul = GetArmorDurabilityDecreaseMulByMaterial();
        float totalCost = decreaseMul * ammoArmorDamage * (isPen ? penArmorDamFactor : nonPenArmorDamFactor);

        durability = Mathf.Max(0.0f, durability - totalCost);

        //Debug.Log($"ArmorDam = {ammoArmorDamage}, totalDam = {totalCost}, dur={durability}");
    }

    public void HandleArmorDurabilityAfterRicochet(float ammoArmorDamage)
    {
        if (durability <= 0.0f) return;

        float decreaseMul = GetArmorDurabilityDecreaseMulByMaterial();
        float totalCost = decreaseMul * ammoArmorDamage * ricochetArmorDamFactor;

        durability = Mathf.Max(0.0f, durability - totalCost);

        //Debug.Log($"ArmorDam = {ammoArmorDamage}, totalDam = {totalCost}, dur={durability}");
    }
    public float GetArmorClass()
    {
        //�������� 0���϶�� ��ȣ ��� x
        if(durability <= 0.0f) return 0.0f;

        if (armorInfo.armorClass == ArmorClass.Level1) return 10.0f;

        if (armorInfo.armorClass == ArmorClass.Level2A) return 20.0f;

        if (armorInfo.armorClass == ArmorClass.Level2) return 30.0f;

        if (armorInfo.armorClass == ArmorClass.Level3A) return 35.0f;

        if (armorInfo.armorClass == ArmorClass.Level3) return 45.0f;

        if (armorInfo.armorClass == ArmorClass.Level4) return 55.0f;

        return 20.0f;
    }
   
}
