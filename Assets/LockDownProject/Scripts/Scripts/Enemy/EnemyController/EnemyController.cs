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
    float GetHorizontalOffset();

}

public partial class EnemyController : MonoBehaviour, IGetBulletDirection
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
    [SerializeField] private LayerMask bulletLayerMask;
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

    [Header("Burst stats")]
    [SerializeField] private bool isBursting = false;      // 지금 3점사 중인지
    [SerializeField] private int burstShotsLeft = 0;      // 이번 버스트에서 남은 탄 수
    [SerializeField] private float burstShotInterval = 0.5f; // 버스트 안에서 총알 사이 간격(초)
    [SerializeField] private float nextBurstShotTime = 2.0f;  // 다음 발사 시각

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

    [Header("Items Slots")]
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
    private bool hasAimSfxState;
    private bool lastAimSfxState;

    [Header("Bullet Awareness")]
    [SerializeField] private float bulletAwarenessRadius = 6.0f;
    [SerializeField] private float bulletAwarenessDuration = 0.5f;
    [SerializeField] private float bulletAwarenessTurnSpeedMultiplier = 1.4f;

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
        if (bulletLayerMask.value == 0)
        {
            bulletLayerMask = LayerMask.GetMask("Bullet");
        }
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
        //기존에 너무 자동차?같은 느낌 없애기 위해서 
        agent.acceleration = 100.0f;   
        agent.angularSpeed = 720.0f;   
        agent.stoppingDistance = 0.05f; 
        agent.autoBraking = false;  
    }
   
    private void Update()
    {
        if (GetPlayerLocation() == null || GetEnemyEyeLocation() == null)
        {
            fsm.ChangeState(idleState);
            return;
        }

        UpdateIncomingBulletAwareness();
        fsm.Tick();
        //적 사망 판단
        isDead = healthManager.CheckIsDead();
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

        ApplyIncomingBulletRotation();
    }
    //Sound APIs
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
    public void PlayWalkSound(bool isWalk)
    {
        if (agent.speed <= 0.0f) return;

        if (isWalk) enemySound.PlayWalkSound();
        else enemySound.PlaySprintSound();
    }

    /// <summary>
    /// Public APIs
    /// </summary>
    /// <returns></returns>
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
    public float GetHorizontalOffset()
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
    private void EnemyDead()
    {
        if (!isDead) return;

        fsm.enabled = false;

        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.ResetPath();
            agent.enabled = false;
        }

        Destroy(gameObject, 0.1f);
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
