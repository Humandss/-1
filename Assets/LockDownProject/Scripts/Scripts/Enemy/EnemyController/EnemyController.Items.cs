using System.Collections;
using UnityEngine;

//EnemyController.Items
public partial class EnemyController
{
    private void InitializeItemsSlot()
    {
        slot1 = InitializeItems(ifakInit);
        slot2 = InitializeItems(torInit);
        slot3 = InitializeItems(splintInit);
        slot4 = InitializeItems(cmsInit);
    }

    private ConsumableItemManager InitializeItems(itemInit init)
    {
        if (!init.def) return null;

        int charges = (init.startRemaining > 0)
       ? init.startRemaining
       : Mathf.Max(0, init.def.remaining);

        var result = new ConsumableItemManager(init.def, charges);

        return result;
    }

    public bool EnemyUseItem(int index, BodyParts? target = null)
    {
        //������϶��� ������̴ϱ� true
        if (isUsing) return true;
        var item = GetSlot(index);

        if (item == null || item.remaining <= 0) { Debug.Log("������ ����/���� 0"); return false; }
        //������ ��� ������ return
        if (!item.CanApplyAll(healthManager, target)) return false;

        StartCoroutine(CoEnemyUseItem(item, target));
        return true;
    }
    private ConsumableItemManager GetSlot(int idx)
    {
        switch (idx)
        {
            case 1: return slot1;
            case 2: return slot2;
            case 3: return slot3;
            case 4: return slot4;
            default: return null;
        }
    }

    private IEnumerator CoEnemyUseItem(ConsumableItemManager item, BodyParts? target)
    {
        isUsing = true;
        lastUseStartTime = Time.time;

        float dur = Mathf.Max(0.05f, item.def.useTime);

        float time = 0.0f;
        while (time < dur)
        {
            time += Time.deltaTime;
            yield return null;

        }

        bool ok = item.ApplyAll(healthManager, target);
        isUsing = false;
        yield break;
    }
}
