namespace LockDown.Ballistic.Job
{
    /// <summary>
    /// BulletInfo (ScriptableObject)의 blittable 사본. Burst Job에서 사용.
    /// 메인 스레드에서 BulletInfoTable.Build()로 채워진다.
    /// </summary>
    public struct BlittableBulletInfo
    {
        public float muzzleVelocity;
        public float mass;
        public float caliberMm;
        public float refAreaScale;
        public float dragCoeff;
        public float lifeTime;
        public float baseRicochetAngleDeg;
        public float randomRicochetAngle;
        public float afterRicochetEnergyPercent;
        public float penetrationPower;
        public float armorDamage;
        public float damage;
        public float criticalChance;
        public float criticalDamMultiplier;
        public float lightBleedingChance;
        public float heavyBleedingChance;
        public float fractureChance;
        public float bluntDamage;
    }
}
