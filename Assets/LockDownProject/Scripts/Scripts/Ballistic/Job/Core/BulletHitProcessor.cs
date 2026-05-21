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
            // 슬롯 해제는 BulletSimulationSystem.FixedUpdate의 cleanup loop가 일괄 처리.
            // 여기서 Release를 부르면 cleanup loop와 중복 호출되어 freeIndices에 같은 인덱스가
            // 두 번 들어가는 버그가 생김.
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

            // 인체 관통 여부 먼저 판정 (피 방향/위치를 결정하기 위해)
            bool penetrates = false;
            if (healthMan != null)
            {
                float newPen = math.max(0f, healthMan.GetPenetrationAfterPenBody());
                float newSpeed = math.max(0f, healthMan.GetSpeedAfterPenBody());
                s.pen = newPen;
                s.speed = newSpeed;
                penetrates = !(s.pen <= 0f || s.speed <= 0f);
            }

            // 피 VFX 스폰:
            //   - 관통 O: 사출구(반대편)에서 총알 진행 방향(s.dir)으로 분출
            //   - 관통 X: 진입점에서 총알 반대 방향(-s.dir)으로 분출 (사수 쪽으로 튐)
            if (penetrates)
            {
                Vector3 exitPos = ComputeBodyExitPoint(col, ev.hitPoint, (Vector3)s.dir);
                SpawnBloodVfx(col, exitPos, (Vector3)s.dir);
            }
            else
            {
                SpawnBloodVfx(col, ev.hitPoint, (Vector3)(-s.dir));
            }

            // 상태 갱신
            if (penetrates)
            {
                s.pos = ev.hitPoint + s.dir * ExitOffset;
                s.velocity = s.dir * s.speed;
            }
            else
            {
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

        /// <summary>
        /// Default/Armor 레이어 히트 시 임팩트 VFX (Spark1 등) 스폰.
        /// 인체(Body) 히트는 SpawnBloodVfx가 따로 처리하므로 여기서 호출하지 않음.
        /// 재질 무관하게 활성화 (벽/콘크리트/금속 등 모두 동일 VFX). 만약 재질별로
        /// 다른 VFX를 원하면 BulletEffectsRegistry에 추가 슬롯 두고 분기.
        /// </summary>
        private static void SpawnImpactVfx(MaterialManager matMan, Vector3 pos, Vector3 normal)
        {
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

        /// <summary>
        /// 인체 히트 시 피 VFX 스폰.
        ///   - col: 히트한 콜라이더 (head/thorax 등 부위 판정에 사용)
        ///   - pos: 스폰 위치 (관통 시 사출구, 미관통 시 진입점)
        ///   - sprayDirection: 피 분출 방향 (관통 시 총알 진행 방향, 미관통 시 그 반대)
        ///
        /// 머리(Head) 콜라이더는 HeadImpactBloodPrefabs 우선, 없으면 BodyImpactBloodPrefabs.
        /// EffectsBudget으로 프레임당 최대 수 제한 + 거리 컬링.
        /// </summary>
        private static void SpawnBloodVfx(Collider col, Vector3 pos, Vector3 sprayDirection)
        {
            if (PoolManager.Instance == null) return;
            if (!EffectsBudget.TryConsumeBlood(pos)) return;

            // 부위 판정: 머리 콜라이더면 헤드샷 전용 피 우선
            bool isHead = col != null && col.name == "head";
            var list = (isHead && BulletEffectsRegistry.HeadImpactBloodPrefabs != null
                                && BulletEffectsRegistry.HeadImpactBloodPrefabs.Count > 0)
                ? BulletEffectsRegistry.HeadImpactBloodPrefabs
                : BulletEffectsRegistry.BodyImpactBloodPrefabs;

            if (list == null || list.Count == 0) return;

            var prefab = list[UnityEngine.Random.Range(0, list.Count)];
            if (prefab == null) return;

            // sprayDirection 정규화 + Z-fighting 방지로 약간 외부에 스폰
            Vector3 dir = sprayDirection.sqrMagnitude > 1e-6f
                ? sprayDirection.normalized
                : Vector3.forward;
            var rot = Quaternion.LookRotation(dir);
            PoolManager.Instance.Spawn(prefab, pos + dir * ExitOffset, rot);
        }

        /// <summary>
        /// 인체 콜라이더의 사출구 추정. 진입점에서 총알 진행 방향으로 1m 더 들어간 뒤
        /// 콜라이더에 역방향 raycast를 던져서 출구 표면을 찾는다.
        /// 실패 시 진입점 + 0.3m (대략적 두께)로 폴백.
        /// </summary>
        private static Vector3 ComputeBodyExitPoint(Collider col, Vector3 entryPos, Vector3 dir)
        {
            const float probeDistance = 1.0f;
            Vector3 probeStart = entryPos + dir * probeDistance;
            if (col.Raycast(new Ray(probeStart, -dir), out RaycastHit exitHit, probeDistance))
            {
                return exitHit.point;
            }
            // 콜라이더가 작거나 raycast 실패 시 대략적 두께로 폴백
            return entryPos + dir * 0.3f;
        }
    }
}
