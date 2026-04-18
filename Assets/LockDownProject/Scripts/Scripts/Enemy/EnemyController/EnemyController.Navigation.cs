using UnityEngine;
using UnityEngine.AI;

//EnemyController.Navigation
public partial class EnemyController
{
    public bool GetNextPatrolPosition(out Vector3 patrolPoint)
    {
        patrolPoint = transform.position;
        if (agent == null) return false;
        //10�� �ݺ��ؼ� ����Ʈ ã��
        for (int i = 0; i < maxPatrolPointTries; i++)
        {
            //x y�ุ ���� ������ �������� ���� -> �װ� �ٽ� vector3�� ��ȯ
            Vector2 rand2D = UnityEngine.Random.insideUnitSphere * patrolRange;
            Vector3 candidatePos = patrolPoint + new Vector3(rand2D.x, 0.0f, rand2D.y);
            if (NavMesh.SamplePosition(candidatePos, out NavMeshHit hit, 1.0f, NavMesh.AllAreas))
            {
                patrolPoint = hit.position;
                return true;
            }
        }

        return false;
    }
    public bool FindCoverPosition(out Vector3 coverPoint)
    {
        coverPoint = transform.position;
        if (playerLocation == null || enemyEyes == null || agent == null) return false;

        Vector2 rand2D = UnityEngine.Random.insideUnitSphere * patrolRange;
        Vector3 candidatePos = coverPoint + new Vector3(rand2D.x, 0.0f, rand2D.y);

        for (int i = 0; i < maxCoverPointTries; i++)
        {
            if (NavMesh.SamplePosition(candidatePos, out NavMeshHit coverHit, 1.0f, NavMesh.AllAreas))
            {
                Vector3 fromPlayer = coverHit.position - playerLocation.position;
                float distance = fromPlayer.magnitude;

                Vector3 dir = fromPlayer / distance;
                float angle = Vector3.Angle(playerLocation.forward, fromPlayer);
                //�÷��̾� ���� ������ Ŀ���ϱ� ����-> �� �������� ����
                if (angle > 90.0f) continue;

                if (Physics.Raycast(playerLocation.position, dir, out var hit, distance, ~layerMask))
                {
                    if (!hit.transform.CompareTag("Player"))
                    {
                        coverPoint = coverHit.position;
                        return true;
                    }
                }
            }
        }

        return false;

    }

    // AI Moving APIs
    public void SetWalkspeed(bool isWalk)
    {
        if (!CanUseAgent()) return;
        agent.speed = isWalk ? walkSpeed : chaseSpeed;
    }
    public bool IsMoving()
    {
        if (!CanUseAgent()) return false;

        return agent.velocity.sqrMagnitude > 0.1f;
    }
    public void StopMove()
    {
        if (!CanUseAgent()) return;
        agent.isStopped = true;
        agent.velocity = Vector3.zero;
    }
    public void MoveTo(Vector3 pos)
    {
        if (!CanUseAgent()) return;
        agent.isStopped = false;
        agent.SetDestination(pos);
    }
    public bool ReachedDestination()
    {
        //��� ������̸� false
        if (!CanUseAgent() || agent.pathPending) return false;

        if (agent.remainingDistance <= agent.stoppingDistance + reachThreshold)
        {
            //�����ؼ� ��ε� ���� �ӵ��� ������ ������ ����
            if (!agent.hasPath || agent.velocity.sqrMagnitude <= 0.01f) return true;
        }

        return false;

    }
    public void AlignDirection()
    {
        if (!CanUseAgent()) return;

        Vector3 dir = agent.desiredVelocity;
        if (dir.sqrMagnitude < 0.01f) return;

        dir.y = 0.0f;
        dir.Normalize();

        Quaternion targetRot = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, turnSpeed * Time.deltaTime);
    }
    public void SlowRotateSearch()
    {
        Vector3 euler = transform.eulerAngles;
        euler.y += 40.0f * Time.deltaTime;
        transform.rotation = Quaternion.Euler(euler);
    }

}
