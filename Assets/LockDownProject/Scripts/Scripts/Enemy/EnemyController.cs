using UnityEngine;
using UnityEngine.AI;


public class EnemyController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Transform playerLocation;
    [SerializeField] private Transform enemyEyes;
    [SerializeField] private Weapon weapon;
    public NavMeshAgent agent;
    private HealthManager healthManager;
    private EnemyStateMachine fsm;

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

        InitializeStates();
        weapon.EnemeyWeaponInitialize(gameObject);
        // 레이케스트에서 제외할 부분들(적 본인몸에 씹히는 경우 제외하기 위헤서)
        layerMask = LayerMask.GetMask("Head", "Thorax", "Stomach", "Left_arm", "Right_arm", "Left_leg", "Right_leg", "Armor");
    }
    private void InitializeStates()
    {
        idleState = new IdleState(this, fsm);
        attackState= new AttackState(this, fsm);
    }

    private void Start()
    {
        fsm.ChangeState(idleState);
    }

    private void Update()
    {
        fsm.Tick();

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

            if (hit.transform != playerLocation && hit.transform.root != playerLocation)
            {
               // Debug.Log($"SIGHT FAIL: {hit.transform.name} 에 막힘");
                return false;
            }
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
            if (hit.transform != playerLocation && hit.transform.root != playerLocation)
            {
                //Debug.Log($"SIGHT FAIL: {hit.transform.name} 에 막힘");
                return false;
            }
        }
        //Debug.Log("SIGHT SUCCESS");
        return true;
    }
    public void Fire()
    {
        if(weapon == null) return;

        weapon.EnemyFirePressed();

    }
    private void ReloadAmmo()
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