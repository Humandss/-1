using System.Reflection;
using UnityEngine;
using UnityEngine.AI;
using System.Collections;

[System.Serializable]
public struct itemInit
{
    public ConsumableItems def;
    public int startRemaining;
}

public interface IGetBulletDirection
{
    Vector3 GetVectorBetweenPlayerAndEnemy();
    Vector3 GetBulletDirection();
    float GetVerticalOffset();
    float GetHoriontalOffset();

}

public class EnemyController : MonoBehaviour, IGetBulletDirection
{
    [Header("Item Init")]
    public itemInit ifakInit, torInit, splintInit, cmsInit;

    [Header("Refs")]
    [SerializeField] private Transform playerLocation;
    [SerializeField] private Transform enemyEyes;
    [SerializeField] private Weapon weapon;
   
    public NavMeshAgent agent;
    private HealthManager healthManager;
    private EnemyStateMachine fsm;
    private EnemySound enemySound;
    
    [Header("LayerMasks")]
    private LayerMask layerMask;
    private IHealthStateProvider healthStateProvider;

    [Header("States")]
    public IdleState idleState;
    public PatrolState patrolState;
    public ChaseState chaseState;
    public AttackState attackState;
    public RetreatState retreatState;

    [Header("Stats")]
    [SerializeField] private float walkSpeed = 2.5f;
    [SerializeField] private float chaseSpeed = 5.5f;
    [SerializeField] private float absoluteDetectionRange = 2.0f; // 절대 탐지 거리
    [SerializeField] private float detectionRange = 30.0f;
    [SerializeField] private float detectionAngle = 120.0f; //탐지 각도
    private bool isDead = false;



    [Header("Attack Stats")]
    [SerializeField] private float turnSpeed = 3.5f;
    [SerializeField] private float aimAngleAllow = 5.0f;
    [SerializeField] private float fireInterval = 0.8f;
    [SerializeField] private float horizontalOffset = 0.3f;
    [SerializeField] private float verticalOffset = 0.3f;
    [SerializeField] private float reloadIntrerval = 3.0f;
    [SerializeField] private float aimDelay = 1.0f;

    [Header("Chase Stats")]
    [SerializeField] private float searchTime = 15.0f;
    [SerializeField] private float reachThreshold = 0.5f;

    [Header("Patrol Stats")]
    [SerializeField] private float patrolRange = 10.0f;
    [SerializeField] private float patrolTime = 15.0f;
    [SerializeField] private float patrolWaitTime = 2.0f;
    private int maxPatrolPointTries = 10;

    [Header("Retreat Stats")]
    [SerializeField] private float retreatAllowRange = 15.0f;
    [SerializeField] private float retreatEnterRatio = 0.5f;
    [SerializeField] private float retreatExitRatio = 0.7f;
    [SerializeField] private int maxRetreatCount = 2;
    [SerializeField] private float retreatEnterInterval = 5.0f;
    private float nextRetreatEnterTick;
    private int retreatCount = 0;
    private int maxCoverPointTries = 10;
  

    [SerializeField] private ConsumableItemManager slot1;
    [SerializeField] private ConsumableItemManager slot2;
    [SerializeField] private ConsumableItemManager slot3;
    [SerializeField] private ConsumableItemManager slot4;

    private float dx, dy;
    private float fireRate;
    private Vector3 bulletPos;
    private int ammo;
    private bool isPlayerDetected;
    private bool isUsing;
    private float lastUseStartTime;


    private void Awake()
    {
        healthManager = GetComponent<HealthManager>();
        if (healthManager == null)
        {
            Debug.LogWarning("[EnemyController] healtManager is NULL");
        }

        fsm = GetComponent<EnemyStateMachine>();
        if (fsm == null)
        {
            Debug.LogWarning("[EnemyController] fsm is NULL");
        }

        enemySound = GetComponent<EnemySound>();
        if (enemySound == null)
        {
            Debug.LogWarning("[EnemyController] enemySound is NULL");
        }

        agent = GetComponent<NavMeshAgent>();
        if (agent == null)
        {
            Debug.LogWarning("[EnemyController] agent is NULL");
        }
        agent.updateRotation = false;

        healthStateProvider = healthManager as IHealthStateProvider;
        if (healthStateProvider == null)
        {
            Debug.LogWarning("[EnemyController] healthStateProvider is NULL");
        }
        Initialize();
        weapon.EnemeyWeaponInitialize(gameObject);
        // 레이케스트에서 제외할 부분들(적 본인몸에 씹히는 경우 제외하기 위헤서)
        layerMask = LayerMask.GetMask("Head", "Thorax", "Stomach", "Left_arm", "Right_arm", "Left_leg", "Right_leg", "Armor");
    }
    private void Initialize()
    {
        idleState = new IdleState(this, fsm);
        attackState = new AttackState(this, fsm);
        chaseState = new ChaseState(this, fsm);
        patrolState = new PatrolState(this, fsm);
        retreatState = new RetreatState(this, fsm);

        dx = horizontalOffset;
        dy = verticalOffset;
        fireRate = fireInterval;

    }

    private void Start()
    {
        fsm.ChangeState(idleState);
        ammo = weapon.GetActiveAmmo();
        InitializeItemsSlot();
    }
    private void InitializeItemsSlot()
    {
        slot1 = InitializeItems(ifakInit);
        slot2 = InitializeItems(torInit);
        slot3 = InitializeItems(splintInit);
        slot4 = InitializeItems(cmsInit);
    }

    private ConsumableItemManager InitializeItems(itemInit init)
    {
        if (!init.def) return null;

        int charges = (init.startRemaining > 0)
       ? init.startRemaining
       : Mathf.Max(0, init.def.remaining);

        var result = new ConsumableItemManager(init.def, charges);
       
        return result;
    }
   
    private void Update()
    {
        if (GetPlayerLocation() == null || GetEnemyEyeLocation() == null)
        {
            fsm.ChangeState(idleState);
            return;
        }

        fsm.Tick();
        //적 사망 판단
        isDead = healthManager.CheckHP();
        EnemyDead();

        //Debug.Log(ammo);
        if (IsPlayerInAbsoluteDetectionRange() || IsPlayerInSight()) isPlayerDetected = true;
        else isPlayerDetected = false;

        float hpRatio = healthStateProvider.GetTotalHP() / healthStateProvider.GetMaxHP();
       // Debug.Log($"[RetreatCheck] state={fsm.CurrentState}, hp={healthStateProvider.GetTotalHP()}/{healthStateProvider.GetMaxHP()}, factor={retreatEnterRatio}");
        //글로벌 상태) 체력이 일정 수준으로 떨어지면 후퇴상태 전이,
        //이때 현재 상태가 후퇴상태가 아니어야 함(무한 후퇴 방지)
        //인터벌을 두어 무한후퇴 방지
        if (!(fsm.CurrentState is RetreatState) &&
            hpRatio <= retreatEnterRatio &&
            Time.time >= nextRetreatEnterTick)
        {
            nextRetreatEnterTick = Time.time + retreatEnterInterval;
           // Debug.Log("[RetreatCheck] >>> RETREAT TRIGGERED <<<");
            fsm.ChangeState(retreatState);
            return;
        }
        if(!(fsm.CurrentState is RetreatState) && IsPlayerInEnemySight())
        {
            fsm.ChangeState(attackState);
            return ;
        }
    }
    private void EnemyDead()
    {
        if(!isDead) return;

        fsm.enabled = false; 

        // NavMeshAgent 정리
        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.ResetPath();
            agent.enabled = false;  
        }

        Destroy(gameObject, 0.1f);
    }
    public bool EnemyUseItem(int index, BodyParts? target = null)
    {
        //사용중일때도 사용중이니깐 true
        if (isUsing) return true;
        var item = GetSlot(index);

        if (item == null || item.remaining <= 0) { Debug.Log("아이템 없음/충전 0"); return false; }
        //적용할 대상 없으면 return
        if (!item.CanApplyAll(healthManager, target)) return false;

        StartCoroutine(CoEnemyUseItem(item, target));
        return true;
    }
    private ConsumableItemManager GetSlot(int idx)
    {
        switch (idx)
        {
            case 1: return slot1;
            case 2: return slot2;
            case 3: return slot3;
            case 4: return slot4;
            default: return null;
        }
    }

    private IEnumerator CoEnemyUseItem(ConsumableItemManager item, BodyParts? target)
    {
        isUsing = true;
        lastUseStartTime = Time.time;

        float dur = Mathf.Max(0.05f, item.def.useTime);

        float time = 0.0f;
        while (time < dur)
        { 
            time += Time.deltaTime;
            yield return null;

        }

        bool ok = item.ApplyAll(healthManager, target);
        isUsing = false;
        yield break;
    }
    //절대 탐지거리
    private bool IsPlayerInAbsoluteDetectionRange()
    {
        if (playerLocation == null || enemyEyes == null) return false;

        Vector3 toPlayer = GetVectorBetweenPlayerAndEnemy();
        float distanceToPlayer = toPlayer.magnitude;

        if (distanceToPlayer > absoluteDetectionRange)
        {
            //Debug.Log("SIGHT FAIL: 각도 범위 밖");
            return false;
        }
        Vector3 dirToPlayer = toPlayer.normalized;

        //레이케스트 쏴서 플레이어 쪽에 장애물 있는지 판단
        if (Physics.Raycast(enemyEyes.position, dirToPlayer, out var hit, distanceToPlayer, ~layerMask))
        {
            if (!hit.transform.CompareTag("Player")) return false;
        }

        return true;
    }
    private bool IsPlayerInSight()
    {
        if (playerLocation == null || enemyEyes == null) return false;

        //거리 판단 -> 탐지 거리보다 크면 false
        Vector3 toPlayer = GetVectorBetweenPlayerAndEnemy();
        float distanceToPlayer = toPlayer.magnitude;

        if (distanceToPlayer > detectionRange) return false;

        //각도 판단 -> 각도는 적 시야 위치(정면)에서 플레이어까지의 거리만큼
        Vector3 dirToPlayer = toPlayer.normalized;
        float angle = Vector3.Angle(enemyEyes.forward, dirToPlayer);
        // Debug.Log($"angle = {angle}");
        // 탐지 각도보다 크면 false
        if (angle > detectionAngle * 0.5f)
        {
            //Debug.Log("SIGHT FAIL: 각도 범위 밖");
            return false;
        }
        //레이케스트 쏴서 플레이어 쪽에 장애물 있는지 판단
        if (Physics.Raycast(enemyEyes.position, dirToPlayer, out var hit, distanceToPlayer, ~layerMask))
        {
            //Debug.Log($"Raycast hit: {hit.transform.name}");
            if (!hit.transform.CompareTag("Player")) return false;
        }
        //Debug.Log("SIGHT SUCCESS");
        return true;
    }
    public void OnFirePressed(Vector3 bulletPos)
    {

        if (weapon == null) return;

        this.bulletPos = bulletPos;


        weapon.EnemyFirePressed();

    }

    public void IsEnemyAim(bool isAiming)
    {
        enemySound.PlayAimSound(isAiming);
    }
    public void PlayWalkSound(bool isWalk)
    {
        if (agent.speed <= 0.0f) return;

        if (isWalk) enemySound.PlayWalkSound();
        else enemySound.PlaySprintSound();
    }
    public void ChangeFireOptionsByPlayerDistance()
    {
        float distance = GetVectorBetweenPlayerAndEnemy().magnitude;

        dx = horizontalOffset;
        dy = verticalOffset;

        if (distance <= detectionRange && distance > detectionRange * 0.8f)
        {
            fireRate = 2.0f;
            dx *= 1.2f;
            dy *= 1.2f;

        }

        else if (distance <= detectionRange * 0.8f && distance > detectionRange * 0.6f)
        {
            fireRate = 1.0f;
            dx *= 1.0f;
            dy *= 1.0f;

        }

        else if (distance <= detectionRange * 0.6f && distance > detectionRange * 0.3f)
        {
            fireRate = 0.8f;
            dx *= 0.8f;
            dy *= 0.8f;

        }

        else if (distance <= detectionRange * 0.3f && distance > detectionRange * 0.0f)
        {
            fireRate = 0.3f;
            dx *= 0.8f;
            dy *= 0.8f;
        }
    }

    public bool GetNextPatrolPosition(out Vector3 patrolPoint)
    {
        patrolPoint = transform.position;
        if (agent == null) return false;
        //10번 반복해서 포인트 찾음
        for (int i = 0; i < maxPatrolPointTries; i++)
        {
            //x y축만 범위 내에서 랜덤으로 매핑 -> 그걸 다시 vector3로 전환
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
                //플레이어 뒤쪽 방향은 커버하기 힘듦-> 이 포지션은 제외
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

    public void SetWalkspeed(bool isWalk)
    {
        if (!CanUseAgent()) return;
        agent.speed = isWalk? walkSpeed : chaseSpeed;
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
        //경로 계산중이면 false
        if (!CanUseAgent() || agent.pathPending) return false;

        if (agent.remainingDistance <= agent.stoppingDistance + reachThreshold)
        {
            //도착해서 경로도 없고 속도도 낮으면 도착한 판정
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
    public void ReloadAmmo()
    {
        if (weapon == null) return;

        weapon.EnemyReload();
    }

    public void PlayAttackDialogueSound()
    {
        enemySound.PlayAttackDialogue();
    }
    public void PlayChaseDialogueSound()
    {
        enemySound.PlayChaseDialogue();
    }
    public void PlayPatrolDialogueSound()
    {
        enemySound.PlayPatrolDialogue();
    }
    public void PlayRetreatDialogueSound()
    {
        enemySound.PlayRetreatDialogue();
    }
    public Vector3 GetVectorBetweenPlayerAndEnemy()
    {
        return playerLocation.position - enemyEyes.position;
    }
    public bool IsPlayerInEnemySight()
    {
        return isPlayerDetected;
    }
    public Transform GetPlayerLocation()
    {
        return playerLocation;
    }
    public Transform GetEnemyEyeLocation()
    {
        return enemyEyes;
    }
    public float GetDetectionRange()
    {
        return detectionRange;
    }

    public float GetAttackAllowAngle()
    {
        return aimAngleAllow;
    }
    public int GetEnemyAmmo()
    {
        return weapon.GetActiveAmmo();
    }
    public int GetEnemyMaxAmmo()
    {
        return weapon.GetMaxAmmo();
    }
    public float GetAttackTurnSpeed()
    {
        return turnSpeed;
    }
    public float GetFireInterval()
    {
        return fireRate;
    }
    public float GetHoriontalOffset()
    {
        return dx;
    }

    public float GetVerticalOffset()
    {
        return dy;
    }
    public Vector3 GetBulletDirection()
    {
        return bulletPos;
    }
    public float GetAimDelay()
    {
        return aimDelay;
    }
    public float GetChasingTime()
    {
        return searchTime;
    }
    public float GetChasingSpeed()
    {
        return chaseSpeed;
    }
    public float GetReachThreshold()
    {
        return reachThreshold;
    }

    public float GetPatrolRange()
    {
        return patrolRange;
    }

    public float GetPatrolTime()
    {
        return patrolTime;
    }
    public float GetTotalHP()
    {
        return healthStateProvider.GetTotalHP();
    }
    public float GetMaxHP()
    {
        return healthStateProvider.GetMaxHP();
    }
    public float GetRetreatExitRatio()
    {
        return retreatExitRatio;
    }
    public float GetPatrolWaitTime()
    {
        return patrolWaitTime;
    }
    private bool CanUseAgent()
    {
        if (isDead) return false;
        if (agent == null) return false;
        if (!agent.enabled) return false;
        if (!agent.isOnNavMesh) return false;
        return true;
    }
    public bool GetEnemyDead()
    {
        return isDead;
    }
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(enemyEyes.position, absoluteDetectionRange);  // 절대 탐지 거리


        Gizmos.color = Color.red;

        Vector3 forward = enemyEyes.forward;
        forward.y = 0f;
        forward.Normalize();

        Quaternion leftRot = Quaternion.AngleAxis(-detectionAngle * 0.5f, Vector3.up);
        Quaternion rightRot = Quaternion.AngleAxis(detectionAngle * 0.5f, Vector3.up);

        Vector3 leftDir = leftRot * forward;
        Vector3 rightDir = rightRot * forward;

        Gizmos.DrawRay(enemyEyes.position, leftDir * detectionRange);
        Gizmos.DrawRay(enemyEyes.position, rightDir * detectionRange);
    }
}