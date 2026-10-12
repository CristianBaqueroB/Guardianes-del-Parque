using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public enum EnemyState
{
    Following,
    Attacking
}

public class EnemyController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;

    [Header("Settings")]
    [SerializeField] private float attackRange = 1.2f;
    [SerializeField] private float attackDamage = 25f; // Cantidad de daño por ataque
    [SerializeField] private float attackDuration = 1.5f; // Duración estimada de la animación de ataque

    [Header("Physics Mask")]
    [Tooltip("Capas que BLOQUEAN la visión (ej: Default, Obstacles). NO incluyas la capa del Player ni del Enemy.")]
    [SerializeField] private LayerMask obstacleLayers;

    [Header("Speed Settings")]
    [SerializeField] private float chaseSpeed = 5f;

    private NavMeshAgent _agent;
    private Animator _animator;
    private EnemyState _state = EnemyState.Following;
    private bool _isHitting;
    private float _attackTimer;

    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int IsFollowingHash = Animator.StringToHash("IsFollowing");

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _animator = GetComponent<Animator>();
    }

    public void Start()
    {
        // Buscar automáticamente al jugador por Tag si no está asignado
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
            }
        }
    }

    public void Update()
    {
        // Reintenta encontrar al jugador por Tag si se perdió la referencia
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
            }
            else
            {
                UpdateAnimations();
                return;
            }
        }

        var distanceToPlayer = Vector3.Distance(player.position, transform.position);

        switch (_state)
        {
            case EnemyState.Following:
                FollowPlayer();

                // Si entra en rango, pasa al estado de ataque
                if (distanceToPlayer <= attackRange)
                {
                    _state = EnemyState.Attacking;
                    StartAttack();
                }
                break;

            case EnemyState.Attacking:
                Attack();

                // Control automático del tiempo de ataque
                _attackTimer -= Time.deltaTime;
                if (_attackTimer <= 0f)
                {
                    _isHitting = false;
                    _agent.isStopped = false;

                    // Vuelve inmediatamente a perseguir al jugador tras terminar el golpe
                    _state = EnemyState.Following;
                }
                break;
        }

        UpdateAnimations();
    }

    private void StartAttack()
    {
        _agent.isStopped = true;
        _isHitting = true;
        _attackTimer = attackDuration;
        _animator.SetTrigger("Hit");
    }

    private void Attack()
    {
        _agent.isStopped = true;

        // Orientar suavemente hacia el jugador durante el ataque
        Vector3 direction = (player.position - transform.position).normalized;
        direction.y = 0f;

        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 10f * Time.deltaTime);
        }
    }

    private void FollowPlayer()
    {
        _agent.speed = chaseSpeed;
        if (_agent.isStopped) _agent.isStopped = false;
        _agent.SetDestination(player.position);
    }

    private void UpdateAnimations()
    {
        if (_animator == null) return;

        float speed = _agent.velocity.magnitude;
        _animator.SetFloat(SpeedHash, speed);

        // Envía true de forma constante porque siempre está persiguiendo o atacando activamente
        _animator.SetBool(IsFollowingHash, true);
    }
}