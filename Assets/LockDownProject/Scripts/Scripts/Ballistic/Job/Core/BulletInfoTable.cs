using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

namespace LockDown.Ballistic.Job
{
    /// <summary>
    /// BulletInfo (ScriptableObject)들을 Job에서 사용 가능한 blittable 배열로 변환하여 보유한다.
    /// 게임 시작 시 1회 Build, 종료 시 Dispose.
    /// </summary>
    public class BulletInfoTable : System.IDisposable
    {
        private NativeArray<BlittableBulletInfo> infos;
        private readonly Dictionary<BulletInfo, int> indexBySo = new Dictionary<BulletInfo, int>();
        private readonly List<BulletInfo> registeredSos = new List<BulletInfo>();

        public NativeArray<BlittableBulletInfo> Infos => infos;
        public int Count => indexBySo.Count;

        /// <summary>
        /// SO → 인덱스. 등록되지 않은 SO에는 -1.
        /// </summary>
        public int Resolve(BulletInfo so)
        {
            if (so == null) return -1;
            return indexBySo.TryGetValue(so, out int idx) ? idx : -1;
        }

        /// <summary>
        /// 인덱스 → SO. 잘못된 인덱스에는 null.
        /// </summary>
        public BulletInfo GetSo(int index)
        {
            if (index < 0 || index >= registeredSos.Count) return null;
            return registeredSos[index];
        }

        public void Build(IEnumerable<BulletInfo> allBulletInfos)
        {
            Dispose();

            registeredSos.Clear();
            indexBySo.Clear();
            // job이 SO(매니지드 GC Heap)이 직접 참조하지 못하기 때문에
            // 해당 총알을 인덱스화 시켜 정보를 저장함
            foreach (var so in allBulletInfos)
            {
                if (so == null || indexBySo.ContainsKey(so)) continue;
                indexBySo[so] = registeredSos.Count;
                registeredSos.Add(so);
            }

            infos = new NativeArray<BlittableBulletInfo>(registeredSos.Count, Allocator.Persistent);
            for (int i = 0; i < registeredSos.Count; i++)
            {
                infos[i] = ToBlittable(registeredSos[i]);
            }
        }

        private static BlittableBulletInfo ToBlittable(BulletInfo so)
        {
            return new BlittableBulletInfo
            {
                muzzleVelocity = so.muzzleVelocity,
                mass = so.mass,
                caliberMm = so.caliberMm,
                refAreaScale = so.refAreaScale,
                dragCoeff = so.dragCoeff,
                lifeTime = so.lifeTime,
                baseRicochetAngleDeg = so.baseRicochetAngleDeg,
                randomRicochetAngle = so.randomRicochetAngle,
                afterRicochetEnergyPercent = so.afterRicochetEnergyPercent,
                penetrationPower = so.penetrationPower,
                armorDamage = so.armorDamage,
                damage = so.damage,
                criticalChance = so.criticalChance,
                criticalDamMultiplier = so.criticalDamMultiplier,
                lightBleedingChance = so.lightBleedingChance,
                heavyBleedingChance = so.heavyBleedingChance,
                fractureChance = so.fractureChance,
                bluntDamage = so.bluntDamage,
            };
        }

        public void Dispose()
        {
            if (infos.IsCreated) infos.Dispose();
        }
    }
}
