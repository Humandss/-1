using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackState : IState
{
    [Header("Refs")]
    private EnemyController enemy;
    private EnemyStateMachine fsm;

    [Header("Attack Stats")]
    [SerializeField] private float turnSpeed = 3.5f;
    [SerializeField] private float aimAngleAllow = 5.0f;
    bool isAimed = false;
    private Quaternion startRot;
    private Quaternion targetRot;

    public AttackState(EnemyController enemy, EnemyStateMachine fsm)
    {
        this.enemy = enemy;
        this.fsm = fsm;
    }

    public void Enter()
    {
         
    }
    public void Execute()
    {
        if (enemy.GetPlayerLocation() == null || enemy.GetEnemyEyeLocation() == null)
        {
            fsm.ChangeState(enemy.idleState); 
            return;
        }
        startRot = enemy.transform.rotation;

        Vector3 toPlayer = enemy.GetVectorBetweenPlayerAndEnemy();
        float distanceToPlayer = toPlayer.magnitude;
        //거리가 멀거나 시야에서 놓칠경우 -> 추격상태
        if (distanceToPlayer > enemy.GetDetectionRange() || !enemy.IsPlayerInEnemySight())
        {
           //fsm.ChangeState(enemy.idleState);
            return;
        }

        isAimed = RotateToPlayer();

        if(isAimed)
        {
            enemy.Fire();
        }
     

    }
  
    private bool RotateToPlayer()
    {
        Transform enemyBody = enemy.transform;
        Transform target = enemy.GetPlayerLocation();

        //y축만 고정
        Vector3 dirToPlayer = enemy.GetVectorBetweenPlayerAndEnemy().normalized;
        dirToPlayer.y = 0.0f;

        Vector3 forward = enemy.GetEnemyEyeLocation().forward;
        forward.y = 0.0f;

        float angle = Vector3.Angle(forward, dirToPlayer);

        targetRot = Quaternion.LookRotation(dirToPlayer);
        enemy.transform.rotation = Quaternion.Slerp(startRot, targetRot, turnSpeed);

        return angle <= aimAngleAllow;
    }
    public void Exit()
    {

    }
}
