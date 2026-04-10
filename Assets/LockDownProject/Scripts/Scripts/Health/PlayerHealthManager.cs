using System.Collections.Generic;

[System.Serializable]
public readonly struct PartSnapshot
{
    public readonly BodyParts part;
    public readonly float hp;
    public readonly float maxHp;
    public readonly bool light;
    public readonly bool heavy;
    public readonly bool fracture;
    public readonly bool blackout;

    public PartSnapshot(BodyParts part, float hp, float maxHp, bool light, bool heavy, bool fracture, bool blackout)
    {
        this.part = part;
        this.hp = hp;
        this.maxHp = maxHp;
        this.light = light;
        this.heavy = heavy;
        this.fracture = fracture;
        this.blackout = blackout;
    }
}

[System.Serializable]
public readonly struct OverallSnapshot
{
    public readonly float totalHp;
    public readonly float totalMaxHp;
    public readonly bool anyLight;
    public readonly bool anyHeavy;
    public readonly bool anyFracture;
    public readonly bool anyBlackout;

    public OverallSnapshot(float totalHp, float totalMaxHp, bool anyLight, bool anyHeavy, bool anyFracture, bool anyBlackout)
    {
        this.totalHp = totalHp;
        this.totalMaxHp = totalMaxHp;
        this.anyLight = anyLight;
        this.anyHeavy = anyHeavy;
        this.anyFracture = anyFracture;
        this.anyBlackout = anyBlackout;
    }
}

public class PlayerHealthManager : HealthManager
{
    private readonly List<BodyParts> changedParts = new(8);
    private readonly List<PartSnapshot> tmpSnapshots = new(8);

    public event System.Action<IReadOnlyList<PartSnapshot>> OnBatchChanged;
    public event System.Action<OverallSnapshot> OnOverallChanged;

    public PartSnapshot GetSnapshot(BodyParts part)
    {
        LimbStatus s = status[part];
        return new PartSnapshot(part, hp[part], maxHp[part], s.light, s.heavy, s.fracture, s.blackout);
    }

    public OverallSnapshot GetOverallSnapshot()
    {
        bool anyLight = false;
        bool anyHeavy = false;
        bool anyFracture = false;
        bool anyBlackout = false;
        float totalHp = 0.0f;
        float totalMaxHp = 0.0f;

        foreach (var part in hp.Keys)
        {
            totalHp += hp[part];
            totalMaxHp += maxHp[part];
        }

        foreach (var part in status.Values)
        {
            anyLight |= part.light;
            anyHeavy |= part.heavy;
            anyFracture |= part.fracture;
            anyBlackout |= part.blackout;
        }

        return new OverallSnapshot(totalHp, totalMaxHp, anyLight, anyHeavy, anyFracture, anyBlackout);
    }

    protected override void OnPartStateChanged(BodyParts part)
    {
        if (!changedParts.Contains(part))
        {
            changedParts.Add(part);
        }
    }

    protected override void FlushTrackedChanges()
    {
        if (changedParts.Count == 0) return;

        tmpSnapshots.Clear();
        for (int i = 0; i < changedParts.Count; i++)
        {
            tmpSnapshots.Add(GetSnapshot(changedParts[i]));
        }

        OnBatchChanged?.Invoke(tmpSnapshots);
        OnOverallChanged?.Invoke(GetOverallSnapshot());
        changedParts.Clear();
    }
}
