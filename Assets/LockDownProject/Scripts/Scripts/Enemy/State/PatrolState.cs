using System.Collections;
using System.Collections.Generic;
using UnityEditor.Search;
using UnityEngine;

public class PatrolState : IState
{
    EnemyController enemy;
    EnemyStateMachine fsm;

    private float patrolTime;
    private float walkTime = 1.2f;
    private float nextWalk;
    private bool isWaiting;
    private float waitTime;
    private Vector3 patrolP;
    public PatrolState(EnemyController enemy, EnemyStateMachine fsm)
    {
        this.enemy = enemy;
        this.fsm = fsm;
    }
    public void Enter()
    {
        patrolTime = enemy.GetPatrolTime();
        waitTime = enemy.GetPatrolWaitTime();

        SetNextPatrolPoint();
    }
    public void Execute()
    {
        if (enemy.GetPlayerLocation() == null || enemy.GetEnemyEyeLocation() == null)
        {
            fsm.ChangeState(enemy.idleState);
            return;
        }

        patrolTime -= Time.deltaTime;
        //시간이 지나서까지 찾지 못한다면 idle상태로 전환
        if (patrolTime <= 0)
        {
            fsm.ChangeState(enemy.idleState);
            return ;
        }

        if (Time.time > nextWalk && enemy.IsMoving())
        {
            nextWalk = Time.time + walkTime;
            enemy.PlayWalkSound(true);
        }

        //시야에 발견한다면 바로 공격 모드
        if (enemy.IsPlayerInEnemySight())
        {
            // Debug.Log("플레이어 발견 공격!");
            fsm.ChangeState(enemy.attackState);
            return;

        }
        //이동중일 때 -> 방향 정렬 후 이동
        if (!isWaiting)
        {
            enemy.AlignDirection();

            if (enemy.ReachedDestination())
            {
                isWaiting = true;
                waitTime = enemy.GetPatrolWaitTime();
                enemy.StopMove();
            }
        }
        if(isWaiting)
        {
            waitTime -= Time.deltaTime;
            enemy.SlowRotateSearch();
            if (waitTime <= 0.0f)
            { 
                isWaiting=false;
                SetNextPatrolPoint(); 
            }
        }

    }

    private void SetNextPatrolPoint()
    {
        if (enemy.GetNextPatrolPosition(out patrolP))
        {
            enemy.MoveTo(patrolP);   
        }
        else
        {
            isWaiting = false;
        }
    }
    public void Exit()
    {
        enemy.StopMove();
    }
}
