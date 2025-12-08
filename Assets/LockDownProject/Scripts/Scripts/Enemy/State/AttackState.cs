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
    private bool prevAimed; // 에너미가 조준을 하고 있는가
    private Quaternion startRot;
    private Quaternion targetRot;
    private Vector3 bulletPos;
    private float nextFireTime;
    private int leftAmmo;

    [Header("Burst stats")]
    private bool isBursting = false;      // 지금 3점사 중인지
    private int burstShotsLeft = 0;      // 이번 버스트에서 남은 탄 수
    private float burstShotInterval = 0.1f; // 버스트 안에서 총알 사이 간격(초)
    private float nextBurstShotTime = 1.5f;  // 다음 발사 시각
    private float nextBurstReadyTime = 0f; // 다음 버스트 시작 가능 시각 (기존 fireTime 사용)

    public AttackState(EnemyController enemy, EnemyStateMachine fsm)
    {
        this.enemy = enemy;
        this.fsm = fsm;
    }

    public void Enter()
    {

        enemy.PlayAttackDialogueSound();

        fireTime = enemy.GetFireInterval();
        aimAngle = enemy.GetAttackAllowAngle();
        turnSpeed = enemy.GetAttackTurnSpeed();
        dx = enemy.GetHoriontalOffset();
        dy = enemy.GetVerticalOffset();
        leftAmmo = enemy.GetEnemyAmmo();
        prevAimed = false;


        isBursting = false;
        burstShotsLeft = 0;
        nextBurstReadyTime = 0.0f;

    }
    public void Execute()
    {
        if (enemy.GetEnemyDead()) return;

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
        bool wantAim = isAimed;
        enemy.ChangeFireOptionsByPlayerDistance();
        fireTime = enemy.GetFireInterval();
        leftAmmo = enemy.GetEnemyAmmo();
        //총알이 없다면 장전
        if (leftAmmo <= 0)
        {
            enemy.ReloadAmmo();
            return;
        }
     
        if(wantAim && !prevAimed)
        {
            enemy.IsEnemyAim(true);
            nextFireTime = Time.time + enemy.GetAimDelay();
        }
        else if (!wantAim && prevAimed)
        {
            enemy.IsEnemyAim(false);         
        }

        prevAimed = wantAim;
        /*
        if (isAimed && Time.time >= nextFireTime) 
        { 
            nextFireTime = Time.time + fireTime; 
            enemy.OnFirePressed(bulletPos);
            //Debug.Log(leftAmmo);
        }*/

        if (!isBursting)
        {
            // 아직 쿨타임(버스트 사이 시간) 안 지났으면 대기
            if (Time.time < nextBurstReadyTime)
                return;

            // 조준 완료 + 탄약 있음 → 새 버스트 시작
            if (isAimed && leftAmmo > 0)
            {
                isBursting = true;
                burstShotsLeft = Mathf.Min(3, leftAmmo); // 남은 탄이 2발이면 2점사만
                nextBurstShotTime = Time.time;           // 지금 바로 첫 발 발사 가능
            }
        }

        if (isBursting)
        {
            if (Time.time >= nextBurstShotTime)
            {
                // 실제 발사
                enemy.OnFirePressed(bulletPos);
                //Debug.Log(leftAmmo);
                burstShotsLeft--;

                // 다음 발사 시간 설정
                if (burstShotsLeft > 0 && leftAmmo > 0)
                {
                    // 버스트 내 다음 탄
                    nextBurstShotTime = Time.time + burstShotInterval;
                }
                else
                {
                    // 버스트 끝
                    isBursting = false;
                    burstShotsLeft = 0;

                    // 다음 버스트 시작 가능 시간 (기존 fireTime을 쿨타임으로 활용)
                    nextBurstReadyTime = Time.time + fireTime;
                }
            }
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
        prevAimed = false;
        isBursting = false;
    }
}
