using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace LockDown.Ballistic.Job
{
    /// <summary>
    /// HitEvent를 메인 스레드에서 받아 기존 BallisticProjectile.HandleImpact의 분기
    /// (Default / Armor / Body)를 그대로 재현한다. 매니지드 매니저(Health/Armor/Material)와의
    /// 모든 상호작용은 여기서만 발생. BulletState 갱신 + 슬롯 해제 책임.
    /// </summary>
    public class BulletHitProcessor
    {
        private const float ExitOffset = 0.004f;
        private const float EnterOffset = 0.002f;
        private const float ProbeDist = 8.0f;
        private const float MaxDist = 20.0f;
        private const float EnergyLostPerM = 100.0f;

        private readonly BulletSlotAllocator allocator;
        private readonly BulletInfoTable infoTable;

        public BulletHitProcessor(BulletSlotAllocator allocator, BulletInfoTable infoTable)
        {
            this.allocator = allocator;
            this.infoTable = infoTable;
        }

        public void Process(HitEvent ev)
        {
            if (!ColliderRegistry.TryGet(ev.hitColliderInstanceID, out Collider col)) return;
            if (col == null) return;

            var states = allocator.States;
            var s = states[ev.slotIndex];
            if (s.isAlive == 0) return;

            var info = infoTable.Infos[s.bulletInfoIndex];

            float3 segDir = math.normalizesafe(s.pos - s.prevPos, s.dir);
            float3 normal = math.normalizesafe(ev.hitNormal, new float3(0f, 1f, 0f));
            float cosToNormal = math.clamp(math.dot(-segDir, normal), -1f, 1f);
            float incAngleToPlane = 90f - math.degrees(math.acos(cosToNormal));

            ColliderMaterialRegistry.TryGet(ev.hitColliderInstanceID, out MaterialManager matMan);
            float matRicochetFactor = (matMan != null) ? matMan.GetMaterialRicochetFactor() : 0.5f;
            float compensateAngle = info.baseRicochetAngleDeg * matRicochetFactor;

            string layerName = LayerMask.LayerToName(col.gameObject.layer);
            switch (layerName)
            {
                case "Default":
                    HandleDefault(ref s, ev, col, segDir, normal, compensateAngle, incAngleToPlane, info, matMan);
                    break;
                case "Armor":
                    HandleArmor(ref s, ev, col, segDir, normal, compensateAngle, incAngleToPlane, info, matMan);
                    break;
                default:
                    HandleBody(ref s, ev, col, info);
                    break;
            }

            states[ev.slotIndex] = s;
            if (s.isAlive == 0)
                allocator.Release(ev.slotIndex);
        }

        // ---------- Default 레이어 (벽/지형) ----------

        private void HandleDefault(
            ref BulletState s, HitEvent ev, Collider col,
            float3 segDir, float3 normal,
            float compensateAngle, float incAngleToPlane,
            BlittableBulletInfo info, MaterialManager matMan)
        {
            if (s.isPenetratingTerrain == 1) return;

            if (compensateAngle >= incAngleToPlane && s.ricochetChance < 1)
            {
                ApplyRicochet(ref s, ev, normal, info);
                SoundUtility.PlayRicochet(ev.hitPoint);
            }
            else
            {
                s.isPenetratingTerrain = 1;
                PlayDefaultImpactSound(matMan, ev.hitPoint);
                if (!TryTerrainPenetration(ref s, ev, col, segDir, matMan))
                    return;
            }

            SpawnImpactVfx(matMan, ev.hitPoint, normal);
            SpawnImpactSmoke(ev.hitPoint, normal);
        }

        private bool TryTerrainPenetration(ref BulletState s, HitEvent ev, Collider col, float3 segDir, MaterialManager matMan)
        {
            if (matMan == null) { s.isAlive = 0; return false; }
            if (matMan.GetMaterialType() == MaterialType.Floor) { s.isAlive = 0; return false; }
            if (!matMan.GetIsPentrable()) { s.isAlive = 0; return false; }

            Vector3 dirN = math.normalizesafe(segDir);
            Vector3 hitPoint = ev.hitPoint;
            Vector3 enterPoint = hitPoint + dirN * EnterOffset;

            // 정방향 탐침 → 역방향 fallback (기존 BallisticProjectile 동일)
            if (!col.Raycast(new Ray(enterPoint, dirN), out RaycastHit exitHit, MaxDist))
            {
                if (!col.Raycast(new Ray(hitPoint + dirN * ProbeDist, -dirN), out exitHit, ProbeDist * 1.5f))
                {
                    s.isAlive = 0;
                    return false;
                }
            }

            float thicknessM = ((Vector3)exitHit.point - hitPoint).magnitude;
            float matMul = math.max(0.01f, matMan.GetMaterialPenetrationFactor());
            float totalCost = thicknessM * EnergyLostPerM * matMul;
            s.pen -= totalCost;

            if (s.pen <= 0f) { s.isAlive = 0; return false; }

            float speedLoss = math.clamp(totalCost * 0.002f, 0f, 1f);
            s.speed *= (1f - speedLoss);
            s.velocity = dirN * s.speed;
            s.pos = (float3)exitHit.point + (float3)dirN * ExitOffset;
            s.isPenetratingTerrain = 0;
            return true;
        }

        // ---------- Armor 레이어 (방탄판) ----------

        private void HandleArmor(
            ref BulletState s, HitEvent ev, Collider col,
            float3 segDir, float3 normal,
            float compensateAngle, float incAngleToPlane,
            BlittableBulletInfo info, MaterialManager matMan)
        {
            var armorMan = col.GetComponent<ArmorManager>();
            if (armorMan == null) { s.isAlive = 0; return; }
            var healthMan = col.GetComponentInParent<HealthManager>();

            if (compensateAngle >= incAngleToPlane && s.ricochetChance < 1)
            {
                ApplyRicochet(ref s, ev, normal, info);
                SoundUtility.PlayRicochet(ev.hitPoint);
                armorMan.HandleArmorDurabilityAfterRicochet(s.armorDam);
            }
            else
            {
                PlayDefaultImpactSound(matMan, ev.hitPoint);
                ApplyArmorPenetration(ref s, ev, armorMan, healthMan, info);
            }

            SpawnImpactVfx(matMan, ev.hitPoint, normal);
            SpawnImpactSmoke(ev.hitPoint, normal);
        }

        private void ApplyArmorPenetration(
            ref BulletState s, HitEvent ev, ArmorManager armorMan, HealthManager healthMan, BlittableBulletInfo info)
        {
            float remainingPenPower = s.pen - armorMan.GetArmorClass();
            float penPower01 = 1f;

            if (remainingPenPower <= 0f)
            {
                if (healthMan != null) healthMan.GetBluntDamage(info.bluntDamage);
                armorMan.HandleArmorDurabilityAfterHit(s.armorDam, false);
                s.isAlive = 0;
                return;
            }

            if (remainingPenPower < 10f)
            {
                penPower01 = math.clamp(remainingPenPower / 10f, 0f, 1f);
                if (s.rng.NextFloat() > penPower01)
                {
                    if (healthMan != null) healthMan.GetBluntDamage(info.bluntDamage);
                    armorMan.HandleArmorDurabilityAfterHit(s.armorDam, false);
                    s.isAlive = 0;
                    return;
                }
            }

            armorMan.HandleArmorDurabilityAfterHit(s.armorDam, true);
            s.pen *= penPower01;
            s.speed *= penPower01;
            s.pos = ev.hitPoint + s.dir * ExitOffset;
            s.velocity = s.dir * s.speed;
        }

        // ---------- Body 레이어 (인체) ----------

        private void HandleBody(ref BulletState s, HitEvent ev, Collider col, BlittableBulletInfo info)
        {
            int targetID = col.GetInstanceID();
            // 한 발이 같은 콜라이더를 두 번 데미지 주지 않도록 (관통 후 동일 부위 재히트 방지)
            for (int i = 0; i < s.hitTargets.Length; i++)
                if (s.hitTargets[i] == targetID) return;
            if (s.hitTargets.Length < s.hitTargets.Capacity)
                s.hitTargets.Add(targetID);

            var healthMan = col.GetComponentInParent<HealthManager>();
            if (healthMan != null)
            {
                healthMan.CheckBodyHit(col, info.damage, info.criticalChance, info.criticalDamMultiplier, s.speed, s.pen);
                healthMan.CheckEffectTrigger(col, info.lightBleedingChance, info.heavyBleedingChance, info.fractureChance);
            }

            PlayBodyImpactSound(col, ev.hitPoint);

            // 인체 관통 후 잔여 속도/관통력
            if (healthMan != null)
            {
                float newPen = math.max(0f, healthMan.GetPenetrationAfterPenBody());
                float newSpeed = math.max(0f, healthMan.GetSpeedAfterPenBody());
                s.pen = newPen;
                s.speed = newSpeed;
                if (s.pen <= 0f || s.speed <= 0f) { s.isAlive = 0; return; }
                s.pos = ev.hitPoint + s.dir * ExitOffset;
                s.velocity = s.dir * s.speed;
            }
            else
            {
                // HealthManager 없으면 그냥 소멸
                s.isAlive = 0;
                return;
            }

            // 적이 쏜 총알이 플레이어 콜라이더에 닿았을 때 서프레션
            if (s.isPlayerShot == 0)
            {
                var supp = col.GetComponentInParent<PlayerSuppressionController>();
                if (supp != null) supp.AddHitSuppression(ev.hitPoint);
            }
        }

        // ---------- 사운드 / VFX 헬퍼 ----------

        private void ApplyRicochet(ref BulletState s, HitEvent ev, float3 normal, BlittableBulletInfo info)
        {
            float3 incoming = math.normalizesafe(s.pos - s.prevPos, s.dir);
            float3 reflected = math.normalize(math.reflect(incoming, normal));
            float3 axis = math.normalizesafe(math.cross(normal, reflected), new float3(0f, 1f, 0f));
            float maxAngle = math.lerp(0f, 6f, math.clamp(info.randomRicochetAngle, 0f, 1f));
            float angle = s.rng.NextFloat(-maxAngle, maxAngle);
            quaternion q = quaternion.AxisAngle(axis, math.radians(angle));
            float3 finalDir = math.mul(q, reflected);

            float afterSpeed = s.speed * info.afterRicochetEnergyPercent;
            s.velocity = finalDir * afterSpeed;
            s.pos = ev.hitPoint + normal * ExitOffset;
            s.dir = math.normalizesafe(finalDir, s.dir);
            s.ricochetChance++;
        }

        private static void PlayDefaultImpactSound(MaterialManager matMan, Vector3 pos)
        {
            if (matMan == null) return;
            string mat = matMan.GetMaterialName();
            if (mat == "Metal" || mat == "Steel_Plate")
                SoundUtility.PlayMetalImpact(pos);
            else if (mat == "Floor" || mat == "Concrete" || mat == "Kevlar" || mat == "Compsite_Armor")
                SoundUtility.PlayDefaultImpact(pos);
        }

        private static void PlayBodyImpactSound(Collider col, Vector3 pos)
        {
            // 인체 사운드는 Material로 분기 (Body / Head)
            ColliderMaterialRegistry.TryGet(col.GetInstanceID(), out var matMan);
            if (matMan == null) return;
            string name = matMan.GetMaterialName();
            if (name == "Body") SoundUtility.PlayBodyImpact(pos);
            else if (name == "Head") SoundUtility.PlayHeadImpact(pos);
        }

        private static void SpawnImpactVfx(MaterialManager matMan, Vector3 pos, Vector3 normal)
        {
            if (matMan == null) return;
            string mat = matMan.GetMaterialName();
            if (mat != "Metal" && mat != "Steel_Plate") return;
            var prefab = BulletEffectsRegistry.MetalImpactVfx;
            if (prefab == null) return;
            if (PoolManager.Instance == null) return;
            if (!EffectsBudget.TryConsumeMetal(pos)) return;
            var rot = Quaternion.LookRotation(normal);
            PoolManager.Instance.Spawn(prefab, pos + normal * ExitOffset, rot);
        }

        private static void SpawnImpactSmoke(Vector3 pos, Vector3 normal)
        {
            var prefab = BulletEffectsRegistry.HitSmoke;
            if (prefab == null) return;
            if (PoolManager.Instance == null) return;
            if (!EffectsBudget.TryConsumeSmoke(pos)) return;
            var rot = Quaternion.LookRotation(normal);
            PoolManager.Instance.Spawn(prefab, pos + normal * ExitOffset, rot);
        }
    }
}
