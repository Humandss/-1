using UnityEngine;

//EnemyController.Detection
public partial class EnemyController
{
    [Header("Bullet Awarness")]
    private Vector3 lastIncomingBulletLookDirection;
    private float lastIncomingBulletTime = float.NegativeInfinity;

    private bool IsPlayerInAbsoluteDetectionRange()
    {
        if (playerLocation == null || enemyEyes == null) return false;

        Vector3 toPlayer = GetVectorBetweenPlayerAndEnemy();
        float distanceToPlayer = toPlayer.magnitude;

        if (distanceToPlayer > absoluteDetectionRange)
        {
            //Debug.Log("SIGHT FAIL: 거리 범위 밖");
            return false;
        }
        Vector3 dirToPlayer = toPlayer.normalized;

        //레이캐스트 시야 플레이어 쪽에 장애물 있는지 판단
        if (Physics.Raycast(enemyEyes.position, dirToPlayer, out var hit, distanceToPlayer, ~layerMask))
        {
            if (!hit.transform.CompareTag("Player")) return false;
        }

        return true;
    }
    private bool IsPlayerInSight()
    {
        if (playerLocation == null || enemyEyes == null)
        {
            // Debug.LogWarning($"[{name}] Sight FAIL: null refs. player={playerLocation}, eye={enemyEyes}", this);
            return false;
        }
        //거리 판단 -> 탐지 거리보다 크면 false
        Vector3 toPlayer = GetVectorBetweenPlayerAndEnemy();
        float distanceToPlayer = toPlayer.magnitude;

        if (distanceToPlayer > detectionRange)
        {
            //Debug.Log($"[{name}] Sight FAIL: dist={distanceToPlayer:F2} > range={detectionRange:F2}", this);
            return false;
        }

        //각도 판단 -> 적군의 시야 위치(가슴)에서 플레이어까지의 거리만큼
        Vector3 dirToPlayer = toPlayer.normalized;
        float angle = Vector3.Angle(enemyEyes.forward, dirToPlayer);
        // Debug.Log($"angle = {angle}");
        // 탐지 각도보다 크면 false
        if (angle > detectionAngle * 0.5f)
        {
            // Debug.Log("SIGHT FAIL: 각도 범위 밖");
            return false;
        }
        //레이캐스트 시야 플레이어 쪽에 장애물 있는지 판단
        if (Physics.Raycast(enemyEyes.position, dirToPlayer, out var hit, distanceToPlayer, ~layerMask))
        {
            // Debug.Log($"[{name}] Sight FAIL: blocked by {hit.transform.name}");
            if (!hit.transform.CompareTag("Player")) return false;
        }
        // Debug.Log("SIGHT SUCCESS");
        return true;
    }

    /// <summary>
    /// 근접 플레이어 총알 인지. PhysX OverlapSphere를 안 쓰고
    /// BulletSimulationSystem이 노출하는 활성 플레이어 총알 NativeArray에 거리 체크.
    /// </summary>
    private void UpdateIncomingBulletAwareness()
    {
        var sys = LockDown.Ballistic.Job.BulletSimulationSystem.Instance;
        if (sys == null) return;

        var bullets = sys.GetActivePlayerBulletsForDetection();
        if (!bullets.IsCreated || bullets.Length == 0) return;

        Transform origin = enemyEyes != null ? enemyEyes : transform;
        Vector3 originPos = origin.position;
        float radiusSq = bulletAwarenessRadius * bulletAwarenessRadius;

        float bestDistanceSq = float.MaxValue;
        Vector3 detectedLookDirection = Vector3.zero;

        for (int i = 0; i < bullets.Length; i++)
        {
            var b = bullets[i];
            Vector3 bulletPos = b.pos;
            Vector3 toBullet = bulletPos - originPos;
            float distSq = toBullet.sqrMagnitude;
            if (distSq > radiusSq) continue;

            Vector3 travelDirection = b.travelDir;
            if (travelDirection.sqrMagnitude < 0.0001f) continue;

            Vector3 lookDirection = -travelDirection;
            lookDirection.y = 0.0f;
            if (lookDirection.sqrMagnitude < 0.0001f) continue;

            if (distSq >= bestDistanceSq) continue;

            bestDistanceSq = distSq;
            detectedLookDirection = lookDirection.normalized;
        }

        if (detectedLookDirection.sqrMagnitude < 0.0001f) return;

        lastIncomingBulletLookDirection = detectedLookDirection;
        lastIncomingBulletTime = Time.time;
    }

    private void ApplyIncomingBulletRotation()
    {
        if (Time.time - lastIncomingBulletTime > bulletAwarenessDuration) return;
        if (fsm.CurrentState is AttackState) return;
        if (CanUseAgent() && !agent.isStopped && agent.desiredVelocity.sqrMagnitude > 0.01f) return;

        Vector3 lookDirection = lastIncomingBulletLookDirection;
        lookDirection.y = 0.0f;
        if (lookDirection.sqrMagnitude < 0.0001f) return;

        Quaternion targetRot = Quaternion.LookRotation(lookDirection.normalized);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, turnSpeed * bulletAwarenessTurnSpeedMultiplier * Time.deltaTime);
    }
}
