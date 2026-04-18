using UnityEngine;

//EnemyController.Detection
public partial class EnemyController
{
    [Header("Bullet Awarness")]
    private const int MaxNearbyBullets = 16;
    private readonly Collider[] nearbyBullets = new Collider[MaxNearbyBullets];
    private Vector3 lastIncomingBulletLookDirection;
    private float lastIncomingBulletTime = float.NegativeInfinity;

    private bool IsPlayerInAbsoluteDetectionRange()
    {
        if (playerLocation == null || enemyEyes == null) return false;

        Vector3 toPlayer = GetVectorBetweenPlayerAndEnemy();
        float distanceToPlayer = toPlayer.magnitude;

        if (distanceToPlayer > absoluteDetectionRange)
        {
            //Debug.Log("SIGHT FAIL: ���� ���� ��");
            return false;
        }
        Vector3 dirToPlayer = toPlayer.normalized;

        //�����ɽ�Ʈ ���� �÷��̾� �ʿ� ��ֹ� �ִ��� �Ǵ�
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
        //�Ÿ� �Ǵ� -> Ž�� �Ÿ����� ũ�� false
        Vector3 toPlayer = GetVectorBetweenPlayerAndEnemy();
        float distanceToPlayer = toPlayer.magnitude;

        if (distanceToPlayer > detectionRange)
        {
            //Debug.Log($"[{name}] Sight FAIL: dist={distanceToPlayer:F2} > range={detectionRange:F2}", this);
            return false;
        }

        //���� �Ǵ� -> ������ �� �þ� ��ġ(����)���� �÷��̾������ �Ÿ���ŭ
        Vector3 dirToPlayer = toPlayer.normalized;
        float angle = Vector3.Angle(enemyEyes.forward, dirToPlayer);
        // Debug.Log($"angle = {angle}");
        // Ž�� �������� ũ�� false
        if (angle > detectionAngle * 0.5f)
        {
            // Debug.Log("SIGHT FAIL: ���� ���� ��");
            return false;
        }
        //�����ɽ�Ʈ ���� �÷��̾� �ʿ� ��ֹ� �ִ��� �Ǵ�
        if (Physics.Raycast(enemyEyes.position, dirToPlayer, out var hit, distanceToPlayer, ~layerMask))
        {
            // Debug.Log($"[{name}] Sight FAIL: blocked by {hit.transform.name}");
            if (!hit.transform.CompareTag("Player")) return false;
        }
        // Debug.Log("SIGHT SUCCESS");
        return true;
    }

    private void UpdateIncomingBulletAwareness()
    {
        if (bulletLayerMask.value == 0) return;

        Transform origin = enemyEyes != null ? enemyEyes : transform;
        int hitCount = Physics.OverlapSphereNonAlloc(origin.position, bulletAwarenessRadius, nearbyBullets, bulletLayerMask, QueryTriggerInteraction.Collide);

        float bestDistance = float.MaxValue;
        Vector3 detectedLookDirection = Vector3.zero;

        for (int i = 0; i < hitCount; i++)
        {
            Collider bulletCollider = nearbyBullets[i];
            if (bulletCollider == null) continue;

            BallisticProjectile projectile = bulletCollider.GetComponentInParent<BallisticProjectile>();
            if (projectile == null || !projectile.IsPlayerBullet()) continue;

            Vector3 travelDirection = projectile.GetTravelDirection();
            if (travelDirection.sqrMagnitude < 0.0001f) continue;

            Vector3 lookDirection = -travelDirection;
            lookDirection.y = 0.0f;
            if (lookDirection.sqrMagnitude < 0.0001f) continue;

            float distance = (bulletCollider.transform.position - origin.position).sqrMagnitude;
            if (distance >= bestDistance) continue;

            bestDistance = distance;
            detectedLookDirection = lookDirection.normalized;
        }

        for (int i = 0; i < hitCount; i++)
        {
            nearbyBullets[i] = null;
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
