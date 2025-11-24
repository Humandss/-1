using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RetreatState : IState
{
    EnemyController enemy;
    EnemyStateMachine fsm;

    private Vector3 coverP;
    private float sprintTime = 0.25f;
    private float nextSprint;
    private float maxRetreatTime = 8.0f;
 
    public RetreatState(EnemyController enemy, EnemyStateMachine fsm)
    {
        this.enemy = enemy;
        this.fsm = fsm;
    }

    public void Enter()
    {
        enemy.PlayRetreatDialogueSound();
        enemy.SetWalkspeed(false);
        SetCoverPoint();
        
    }
    public void Execute()
    {
        if (enemy.GetEnemyDead()) return;

        float hpRatio = enemy.GetTotalHP() / enemy.GetMaxHP();

        if (Time.time > nextSprint && enemy.IsMoving())
        {
            nextSprint = Time.time + sprintTime;
            enemy.PlayWalkSound(false);
        }
 
        if (enemy.IsMoving())
        {
            enemy.AlignDirection();
        }
        //도착하면 -> 경계
        if (enemy.ReachedDestination())
        {
            enemy.StopMove();
            enemy.SlowRotateSearch();

            // HP 충분히 회복되었을 경우 -> 플레이어 발견 공격/ 아니라면 순찰
            if (hpRatio >= enemy.GetRetreatExitRatio())
            {
                if (enemy.IsPlayerInEnemySight()) fsm.ChangeState(enemy.attackState);
                else fsm.ChangeState(enemy.patrolState);

                return;
            }
            //체력이 안찼을 경우 -> 체력 회복이 불가할 경우 플레이어 발견시 공격 아님 순찰
            if (hpRatio < enemy.GetRetreatExitRatio())
            {
                // 슬롯 1 아이템 사용 시도, 일단 체력 회복 아이템만
                bool used = enemy.EnemyUseItem(1);

                // 만약 아이템이 없거나 사용 불가하면 → 더 숨을 의미가 없으니 전투/순찰로 전환
                if (!used)
                {
                    if (enemy.IsPlayerInEnemySight()) fsm.ChangeState(enemy.attackState);
                    else fsm.ChangeState(enemy.patrolState);

                }
                //사용중에 적을 본다면 공격 -> 추격 -> 체력회복을 위해 다시 후퇴
                else
                {
                    if (enemy.IsPlayerInEnemySight()) fsm.ChangeState(enemy.attackState);
  
                }
            }
        }
      
    }

    private void SetCoverPoint()
    {
        if(enemy.FindCoverPosition(out coverP))
        {
            enemy.MoveTo(coverP);
        }
        else
        {
            fsm.ChangeState(enemy.patrolState);
            return;
        }
    }
    public void Exit()
    {
        enemy.StopMove();
    }
}
