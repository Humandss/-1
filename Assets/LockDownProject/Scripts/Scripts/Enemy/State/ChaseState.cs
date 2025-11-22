using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChaseState : IState
{ 

    EnemyController enemy;
    EnemyStateMachine fsm;

    private Vector3 lastPlayerPosition;
    private float searchTime;
    private float sprintTime = 0.25f;
    private float nextSprint;

    public ChaseState(EnemyController enemy, EnemyStateMachine fsm)
    {
        this.enemy = enemy;
        this.fsm = fsm;
    }

    public void Enter()
    {
        enemy.PlayChaseDialogueSound();

        lastPlayerPosition = enemy.GetPlayerLocation().position;
        searchTime = enemy.GetChasingTime();

       // Debug.Log($"[Chase] Enter, lastPos = {lastPlayerPosition}");

        enemy.SetWalkspeed(false);
        enemy.MoveTo(lastPlayerPosition); 
    }
    public void Execute()
    {
        if (enemy.IsMoving())
        {
            enemy.AlignDirection();
        }

        if (Time.time > nextSprint && enemy.IsMoving())
        {
            nextSprint = Time.time + sprintTime;
            enemy.PlayWalkSound(false);
        }

        //해당 마지막 위치에서 회전하면서 판단-> 시간 동안 못찾을 경우 순찰 모드 전환
        if (enemy.ReachedDestination())
        {
            searchTime -= Time.deltaTime;

            enemy.SlowRotateSearch();

            if (searchTime <= 0) fsm.ChangeState(enemy.patrolState);
         
        }

    }

    public void Exit()
    {
        enemy.StopMove();
    }
}
