using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEngine.GraphicsBuffer;

public class AttackState : IState
{
    [Header("Refs")]
    private EnemyController enemy;
    private EnemyStateMachine fsm;

    [Header("Stats")]
    private float fireTime;
    private float aimAngle;
    private float turnSpeed;
    private float dx, dy;
    private bool isAimed = false; // 에너미가 플레이어를 향하고 있는가
    private bool isAiming = false; // 에너미가 조준을 하고 있는가
    private Quaternion startRot;
    private Quaternion targetRot;
    private Vector3 bulletPos;
    private float nextFireTime;
    private int leftAmmo;
   
    public AttackState(EnemyController enemy, EnemyStateMachine fsm)
    {
        this.enemy = enemy;
        this.fsm = fsm;
    }

    public void Enter()
    {
        fireTime = enemy.GetFireInterval();
        aimAngle = enemy.GetAttackAllowAngle();
        turnSpeed = enemy.GetAttackTurnSpeed();
        dx = enemy.GetHoriontalOffset();
        dy = enemy.GetVerticalOffset();
        leftAmmo = enemy.GetEnemyAmmo();
  
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
            fsm.ChangeState(enemy.chaseState);
            return;
        }

        isAimed = AimToPlayer();
        enemy.ChangeFireOptionsByPlayerDistance();
        fireTime = enemy.GetFireInterval();
        leftAmmo = enemy.GetEnemyAmmo();
        //총알이 없다면 장전
        if (leftAmmo <= 0)
        {
            enemy.ReloadAmmo();
            return;
        }
     
        if(isAimed && !isAiming)
        {
            isAiming = true;
            enemy.IsEnemyAim(isAiming);
            nextFireTime = Time.time + enemy.GetAimDelay();
        }
        if (isAimed && Time.time >= nextFireTime) 
        { 
            nextFireTime = Time.time + fireTime; 
            enemy.OnFirePressed(bulletPos);
            //Debug.Log(leftAmmo);
        }
  
    }

    private bool AimToPlayer()
    {
        Transform enemyBody = enemy.GetEnemyEyeLocation(); 
        Vector3 targetPos = enemy.GetPlayerLocation().position; 

        //float hOffset = Random.Range(-dx, dx); 
        //float vOffset = Random.Range(-dy, dy); 

        //targetPos += (enemyBody.right * hOffset) + (enemyBody.up * vOffset) + (Vector3.down * 0.3f);
        //targetPos += (Vector3.down * 0.3f);

        Vector3 dirToPlayer = (targetPos - enemyBody.position).normalized;
        bulletPos = dirToPlayer;
        dirToPlayer.y = 0.0f;

        if (dirToPlayer.sqrMagnitude < 0.0001f)
        {
            // 너무 가까우면 그냥 현재 보는 방향 유지하고 조준된 걸로 취급
            bulletPos = enemyBody.forward;
            return true;
        }

        Vector3 forward = enemy.GetEnemyEyeLocation().forward;
        forward.y = 0.0f;
        if (forward.sqrMagnitude < 0.0001f)
        {         
            return true;
        }
        float angle = Vector3.Angle(forward, dirToPlayer);
     
        targetRot = Quaternion.LookRotation(dirToPlayer);

        enemy.transform.rotation = Quaternion.Slerp(startRot, targetRot, turnSpeed); 
        //Debug.DrawLine(enemyBody.position, targetPos, Color.red);
        return angle <= aimAngle;
    }
    public void Exit()
    {
        isAiming = false;
        enemy.IsEnemyAim(false);
    }
}
