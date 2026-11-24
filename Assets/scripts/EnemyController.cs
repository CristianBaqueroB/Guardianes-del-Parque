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

    [Header("Contaminacion Activa")]
    [SerializeField] private float litterInterval = 18f;
    private float _litterTimer = 12f;

    private NavMeshAgent _agent;
    private Animator _animator;
    private AudioSource _audioSource;
    private static AudioClip _alertClip;
    private EnemyState _state = EnemyState.Patrolling;
    private bool _isHitting;
    private float _attackTimer;
    private float _patrolWaitTimer;
    private Vector3 _patrolTarget;
    private bool _waitingAtPatrolPoint = false;
    private bool _showDangerWarning = false;

    private static readonly int SpeedHash        = Animator.StringToHash("Speed");
    private static readonly int IsFollowingHash  = Animator.StringToHash("IsFollowing");

    private void Awake()
    {
        _agent    = GetComponent<NavMeshAgent>();
        _animator = GetComponent<Animator>();
        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null)
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.playOnAwake = false;
            _audioSource.spatialBlend = 0.4f;
        }
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
        if (GameManager.instance != null &&
            (!GameManager.instance.IsGameStarted() || GameManager.instance.isPaused ||
             GameManager.instance.isGameOver || GameManager.instance.isVictory))
        {
            _agent.isStopped = true;
            _showDangerWarning = false;
            return;
        }

        if (player == null)
        {
            var playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) player = playerObj.transform;
            else { UpdateAnimations(); return; }
        }

        float distToPlayer = Vector3.Distance(player.position, transform.position);
        _showDangerWarning = (_state == EnemyState.Chasing && distToPlayer <= 12f);

        switch (_state)
        {
            case EnemyState.Patrolling:
                DoPatrol();
                HandleLittering();
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

    private void HandleLittering()
    {
        _litterTimer -= Time.deltaTime;
        if (_litterTimer <= 0f)
        {
            _litterTimer = litterInterval;
            if (RandomTrashGenerator.Instance != null)
            {
                RandomTrashGenerator.Instance.SpawnTrashAt(transform.position);
            }
        }
    }

    private void ShowDetectionAlert()
    {
        if (InteractionHUD.Instance != null)
        {
            InteractionHUD.Instance.ShowNotification(
                "Cuidado! El contaminador te detecto.", null, false, 2f);
        }

        if (_audioSource != null)
        {
            if (_alertClip == null)
            {
                int sr = 22050;
                int samples = (int)(sr * 0.25f);
                float[] data = new float[samples];
                for (int i = 0; i < samples; i++)
                {
                    float t = (float)i / samples;
                    float freq = (t < 0.5f) ? 620f : 440f;
                    float env = 1f - t;
                    data[i] = Mathf.Sin(2f * Mathf.PI * freq * ((float)i / sr)) * env * 0.45f;
                }
                _alertClip = AudioClip.Create("EnemyAlert", samples, 1, sr, false);
                _alertClip.SetData(data, 0);
            }
            float vol = (AmbientAudioManager.Instance != null) ? AmbientAudioManager.Instance.masterVolume * 0.7f : 0.4f;
            _audioSource.PlayOneShot(_alertClip, vol);
        }
    }

    private void OnGUI()
    {
        if (!_showDangerWarning) return;
        float pulse = 0.35f + 0.35f * Mathf.Sin(Time.realtimeSinceStartup * 7f);
        GUI.color = new Color(1f, 0.2f, 0.15f, pulse * 0.25f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, 10f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(0, Screen.height - 10f, Screen.width, 10f), Texture2D.whiteTexture);
        GUI.color = Color.white;
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
