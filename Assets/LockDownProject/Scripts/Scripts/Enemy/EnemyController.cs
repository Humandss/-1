using KINEMATION.FPSAnimationPack.Scripts.Sounds;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.AI;

public interface IGetBulletDirection
{
    Vector3 GetVectorBetweenPlayerAndEnemy();
    Vector3 GetBulletDirection();
    float GetVerticalOffset();
    float GetHoriontalOffset();

}
public class EnemyController : MonoBehaviour, IGetBulletDirection
{
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
    private Vector3 patrolP;
    private int maxPatrolPointTries = 10;

    private float dx,dy;
    private float fireRate;
    private Vector3 bulletPos;
    private int ammo;
    private bool isPlayerDetected;
 


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

        Initialize();
        weapon.EnemeyWeaponInitialize(gameObject);
        // 레이케스트에서 제외할 부분들(적 본인몸에 씹히는 경우 제외하기 위헤서)
        layerMask = LayerMask.GetMask("Head", "Thorax", "Stomach", "Left_arm", "Right_arm", "Left_leg", "Right_leg", "Armor");
    }
    private void Initialize()
    {
        idleState = new IdleState(this, fsm);
        attackState= new AttackState(this, fsm);
        chaseState = new ChaseState(this, fsm);
        patrolState = new PatrolState(this, fsm);

        dx= horizontalOffset;
        dy= verticalOffset;
        fireRate =fireInterval;
        
    }
    
    private void Start()
    {
        fsm.ChangeState(idleState);
        ammo = weapon.GetActiveAmmo();
    }

    private void Update()
    {
        fsm.Tick();
        //Debug.Log(ammo);
        if (IsPlayerInAbsoluteDetectionRange() || IsPlayerInSight()) isPlayerDetected = true;
        else isPlayerDetected = false;
        
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

        if(isWalk) enemySound.PlayWalkSound();
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
        patrolPoint = patrolP;
        if (agent == null) return false;
        //10번 반복해서 포인트 찾음
        for (int i = 0; i < maxPatrolPointTries; i++)
        {
            //x y축만 범위 내에서 랜덤으로 매핑 -> 그걸 다시 vector3로 전환
            Vector2 rand2D = UnityEngine.Random.insideUnitSphere * patrolRange;
            Vector3 candidatePos = patrolPoint + new Vector3(rand2D.x, rand2D.y);
            if (NavMesh.SamplePosition(candidatePos, out NavMeshHit hit, 1.0f, NavMesh.AllAreas))
            {
                patrolPoint = hit.position;
                return true;
            }
        }

        return false;
    }
    public void FindCover()
    {
        if (playerLocation == null || enemyEyes == null) return;



    }

    public void SetWalkspeed(bool isWalk)
    {
        if (agent == null) return;
        agent.speed = isWalk? walkSpeed : chaseSpeed;
    }
    public bool IsMoving()
    {
        if (agent == null && agent.velocity.sqrMagnitude > 0.1f) return true;
        else return false;
    }
    public void StopMove()
    {
        if (agent == null) return;
        agent.isStopped = true;
        agent.velocity = Vector3.zero;
    }
    public void MoveTo(Vector3 pos)
    {
        if (agent == null) return;
        agent.isStopped = false;
        agent.SetDestination(pos);
    }
    public bool ReachedDestination()
    {
        //경로 계산중이면 false
        if (agent == null || agent.pathPending) return false;

        if (agent.remainingDistance <= agent.stoppingDistance + reachThreshold)
        {
            //도착해서 경로도 없고 속도도 낮으면 도착한 판정
            if (!agent.hasPath || agent.velocity.sqrMagnitude <= 0.01f) return true;
        }

        return false;
          
    }
    public void AlignDirection()
    {
        if (agent == null) return; 

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
 
    public float GetPatrolWaitTime()
    {
        return patrolWaitTime;
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