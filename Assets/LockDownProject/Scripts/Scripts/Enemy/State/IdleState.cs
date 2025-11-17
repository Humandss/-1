using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class IdleState : IState
{
    [Header("Refs")]
    private EnemyController enemy;
    private EnemyStateMachine fsm;

    [Header("Idle Stats")]
    [SerializeField] private float turnAngle = 90.0f;
    [SerializeField] private float turnSpeed = 0.5f;
    [SerializeField] private float turnInterval = 2.5f;
    private bool isTurning = false;
    private float turnStartTime;
    private float lastTurnEndTime;
    private Quaternion startRot;
    private Quaternion targetRot;

  
    public IdleState(EnemyController enenmy, EnemyStateMachine fsm)
    {
        this.enemy = enenmy;
        this.fsm = fsm;

    }
    public void Enter()
    {
        if (enemy.agent != null)
        {
            Debug.LogWarning("[IdleState] enemy.agent is NULL");
        }

        isTurning = true;
        lastTurnEndTime = Time.time;
    }
    public void Execute()
    {
        //시야에 발견한다면 바로 공격 모드
        if (enemy.IsPlayerInEnemySight())
        {
            Debug.Log("플레이어 발견 공격!");
            fsm.ChangeState(enemy.attackState); 
            return;
        
        }

        //회전을 하면서 플레이어 탐색
        if (isTurning)
        {
            UpdateTurn();
        }
        else
        {
            //턴인터벌이 지나면 다음 회전각 세팅
            if(Time.time -  lastTurnEndTime > turnInterval)
            {
                StartTurn();
            }
        }
    }

    private void StartTurn()
    {
        isTurning=true;
        turnStartTime = Time.time;

        startRot = enemy.transform.rotation;
        //목표 각도 세팅
        targetRot = Quaternion.AngleAxis(turnAngle, Vector3.up) * startRot;
    }

    private void UpdateTurn()
    {
        float time = (Time.time - turnStartTime) / turnSpeed;

        //다돌았으면 ->lastTurnEndTime 초기화, 아니면 천천히 회전
        if (time >= 1.0f)
        {
            enemy.transform.rotation = targetRot;
            isTurning = false;
            lastTurnEndTime= Time.time;
        }
        else
        {
            enemy.transform.rotation = Quaternion.Slerp(startRot, targetRot, time);
        }
    }
    public void Exit() 
    {
        isTurning = false;
    }
}
