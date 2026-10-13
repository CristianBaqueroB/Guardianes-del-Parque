using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public enum EnemyState
{
    Patrolling,   // Deambulando por el parque
    Chasing,      // Persiguiendo al jugador
    Attacking     // Atacando al jugador
}

public class EnemyController : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Transform player;

    [Header("Deteccion")]
    [SerializeField] private float detectionRange = 20f;   // Distancia para empezar a perseguir
    [SerializeField] private float loseRange = 35f;        // Distancia para perder al jugador
    [SerializeField] private float attackRange = 1.8f;

    [Header("Dano al Jugador")]
    [SerializeField] private float attackDamage = 25f;
    [SerializeField] private float attackDuration = 1.5f;

    [Header("Velocidades")]
    [SerializeField] private float patrolSpeed = 2.2f;
    [SerializeField] private float chaseSpeed = 5.5f;

    [Header("Patrulla")]
    [SerializeField] private float patrolRadius = 30f;
    [SerializeField] private float patrolWaitMin = 2f;
    [SerializeField] private float patrolWaitMax = 5f;

    [Header("Capas de Obstaculos")]
    [SerializeField] private LayerMask obstacleLayers;

    private NavMeshAgent _agent;
    private Animator _animator;
    private EnemyState _state = EnemyState.Patrolling;
    private bool _isHitting;
    private float _attackTimer;
    private float _patrolWaitTimer;
    private Vector3 _patrolTarget;
    private bool _waitingAtPatrolPoint = false;

    private static readonly int SpeedHash        = Animator.StringToHash("Speed");
    private static readonly int IsFollowingHash  = Animator.StringToHash("IsFollowing");

    private void Awake()
    {
        _agent    = GetComponent<NavMeshAgent>();
        _animator = GetComponent<Animator>();
    }

    public void Start()
    {
        if (player == null)
        {
            var playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) player = playerObj.transform;
        }

        SetNewPatrolTarget();
    }

    public void Update()
    {
        // Si el juego no ha iniciado o esta pausado/terminado, detener al enemigo
        if (GameManager.instance != null && !GameManager.instance.IsGameStarted())
        {
            _agent.isStopped = true;
            return;
        }

        if (player == null)
        {
            var playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) player = playerObj.transform;
            else { UpdateAnimations(); return; }
        }

        float distToPlayer = Vector3.Distance(player.position, transform.position);

        switch (_state)
        {
            case EnemyState.Patrolling:
                DoPatrol();
                if (distToPlayer <= detectionRange && CanSeePlayer())
                {
                    _state = EnemyState.Chasing;
                    ShowDetectionAlert();
                }
                break;

            case EnemyState.Chasing:
                DoChase();
                if (distToPlayer > loseRange)
                {
                    _state = EnemyState.Patrolling;
                    SetNewPatrolTarget();
                }
                else if (distToPlayer <= attackRange)
                {
                    _state = EnemyState.Attacking;
                    StartAttack();
                }
                break;

            case EnemyState.Attacking:
                DoAttack();
                _attackTimer -= Time.deltaTime;
                if (_attackTimer <= 0f)
                {
                    _isHitting = false;
                    _agent.isStopped = false;
                    // Volver a perseguir
                    _state = EnemyState.Chasing;
                }
                break;
        }

        UpdateAnimations();
    }

    // --- IA de Patrulla ---

    private void DoPatrol()
    {
        _agent.speed = patrolSpeed;

        if (_waitingAtPatrolPoint)
        {
            _patrolWaitTimer -= Time.deltaTime;
            if (_patrolWaitTimer <= 0f)
            {
                _waitingAtPatrolPoint = false;
                SetNewPatrolTarget();
            }
            return;
        }

        if (_agent.isStopped) _agent.isStopped = false;

        if (!_agent.pathPending && _agent.remainingDistance < 1.2f)
        {
            // Llegamos al punto de patrulla: esperar un momento
            _waitingAtPatrolPoint = true;
            _patrolWaitTimer = Random.Range(patrolWaitMin, patrolWaitMax);
            _agent.isStopped = true;
        }
    }

    private void SetNewPatrolTarget()
    {
        Vector3 origin = transform.position;

        for (int attempt = 0; attempt < 8; attempt++)
        {
            Vector2 rnd = Random.insideUnitCircle * patrolRadius;
            Vector3 candidate = new Vector3(origin.x + rnd.x, origin.y, origin.z + rnd.y);

            NavMeshHit hit;
            if (NavMesh.SamplePosition(candidate, out hit, 5f, NavMesh.AllAreas))
            {
                _patrolTarget = hit.position;
                if (_agent != null && _agent.isOnNavMesh)
                    _agent.SetDestination(_patrolTarget);
                return;
            }
        }

        // Fallback: quedarse quieto
        _agent.isStopped = true;
    }

    // --- IA de Persecucion ---

    private void DoChase()
    {
        _agent.speed = chaseSpeed;
        if (_agent.isStopped) _agent.isStopped = false;
        _agent.SetDestination(player.position);
    }

    private bool CanSeePlayer()
    {
        if (player == null) return false;
        Vector3 dir = player.position - transform.position;
        float dist = dir.magnitude;

        // Raycast para verificar vision directa (sin obstaculos entre medio)
        if (obstacleLayers.value != 0)
        {
            if (Physics.Raycast(transform.position + Vector3.up * 1.2f, dir.normalized, dist, obstacleLayers))
                return false;
        }
        return true;
    }

    // --- IA de Ataque ---

    private void StartAttack()
    {
        _agent.isStopped = true;
        _isHitting = true;
        _attackTimer = attackDuration;

        if (_animator != null) _animator.SetTrigger("Hit");

        // Dano al jugador via PlayerHealth (si existe)
        var ph = FindPlayerHealth();
        if (ph != null)
        {
            ph.TakeDamage(attackDamage);
        }
        else
        {
            // Fallback: penalizar tiempo (comportamiento anterior)
            if (GameManager.instance != null)
            {
                float timePenalty = Mathf.Clamp(attackDamage * 0.15f, 2f, 5f);
                GameManager.instance.timeRemaining = Mathf.Max(0, GameManager.instance.timeRemaining - timePenalty);
                if (InteractionHUD.Instance != null)
                {
                    InteractionHUD.Instance.ShowNotification(
                        "El contaminador te intercepto! Perdiste tiempo.", null, false, 2.5f);
                    InteractionHUD.Instance.TriggerScreenFlash(false);
                }
            }
        }
    }

    private void DoAttack()
    {
        _agent.isStopped = true;
        if (player == null) return;

        Vector3 direction = (player.position - transform.position).normalized;
        direction.y = 0f;
        if (direction != Vector3.zero)
        {
            Quaternion targetRot = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 10f * Time.deltaTime);
        }
    }

    private void ShowDetectionAlert()
    {
        if (InteractionHUD.Instance != null)
        {
            InteractionHUD.Instance.ShowNotification(
                "Cuidado! El contaminador te detecto.", null, false, 2f);
        }
    }

    private PlayerHealth FindPlayerHealth()
    {
        if (player == null) return null;
        var ph = player.GetComponent<PlayerHealth>();
        if (ph == null) ph = player.GetComponentInChildren<PlayerHealth>();
        return ph;
    }

    private void UpdateAnimations()
    {
        if (_animator == null) return;

        float speed = _agent.velocity.magnitude;
        _animator.SetFloat(SpeedHash, speed);
        _animator.SetBool(IsFollowingHash, _state == EnemyState.Chasing || _state == EnemyState.Attacking);
    }

    public bool IsHitting => _isHitting;
    public float AttackDamage => attackDamage;

    // Gizmos de depuracion en editor
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireSphere(transform.position, loseRange);
    }
}
