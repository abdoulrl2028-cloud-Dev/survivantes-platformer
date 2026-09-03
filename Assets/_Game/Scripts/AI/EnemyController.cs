using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using BlackHorizon.Core;
using BlackHorizon.Systems;

namespace BlackHorizon.AI
{
    /// <summary>
    /// Military enemy AI driven by a finite state machine (Idle/Patrol/
    /// Investigate/Alert/Combat/Search/Retreat/Dead). Uses NavMeshAgent for
    /// pathfinding and PerceptionConfig for detection (vision + hearing).
    ///
    /// To keep draw calls and Updates low, this uses a modest polling cadence
    /// rather than per-frame full perception in large squads.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(Health))]
    [RequireComponent(typeof(Collider))]
    public class EnemyController : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] private PerceptionConfig perception;

        [Header("Patrol")]
        [SerializeField] private Transform[] patrolPoints;
        [SerializeField] private float waitAtPoint = 1.5f;

        [Header("Visual (placeholder)")]
        [SerializeField] private Transform body;
        [SerializeField] private float damage = 15f;

        [Header("Debug")]
        [SerializeField] private bool showGizmos = true;

        private NavMeshAgent _agent;
        private Health _health;
        private Transform _transform;
        private Transform _player;

        private AIState _state = AIState.Idle;
        private int _patrolIndex;

        private float _awareness;                  // 0..1 perception of player
        private float _lastSeenPlayerTime;
        private float _stateEnterTime;
        private float _attackTimer;

        private Coroutine _attackRoutine;
        private Vector3 _investigateTarget;

        public AIState State => _state;
        public float Damage => damage;

        /// <summary>Set patrol route from an EnemyGroup after spawn.</summary>
        public void AssignPatrolPoints(Transform[] points)
        {
            patrolPoints = points;
        }

        private void Awake()
        {
            _transform = transform;
            _agent = GetComponent<NavMeshAgent>();
            _health = GetComponent<Health>();

            if (perception == null)
            {
                perception = CreateDefaultPerception();
            }
            if (perception.lineOfSightMask == 0) perception.lineOfSightMask = GameLayers.EnvironmentMask;
        }

        private void Start()
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) _player = player.transform;

            _health.OnDamaged += HandleDamaged;
            _health.OnDied += HandleDeath;

            EventBus.OnShotFired += HandleShotFired;

            if (_state == AIState.Idle)
            {
                EnterState(AIState.Patrol);
            }
        }

        private PerceptionConfig CreateDefaultPerception()
        {
            var cfg = ScriptableObject.CreateInstance<PerceptionConfig>();
            cfg.sightRange = 30f;
            cfg.fieldOfView = 90f;
            cfg.hearingRange = 25f;
            cfg.lineOfSightMask = GameLayers.EnvironmentMask;
            return cfg;
        }

        private void OnDestroy()
        {
            if (_health != null)
            {
                _health.OnDamaged -= HandleDamaged;
                _health.OnDied -= HandleDeath;
            }
            EventBus.OnShotFired -= HandleShotFired;
        }

        private void Update()
        {
            if (_state == AIState.Dead) return;

            float dt = Time.deltaTime;
            UpdatePerception();
            UpdateState(dt);
        }

        // ---------- Perception ----------

        private void UpdatePerception()
        {
            if (_player == null || _state == AIState.Dead) return;

            float dist = Vector3.Distance(_transform.position, _player.position);
            bool canSee = dist <= perception.sightRange && HasLineOfSight(_player.position) && InFieldOfView(_player.position);

            if (canSee)
            {
                _awareness = Mathf.MoveTowards(_awareness, 1f, perception.detectSpeed * Time.deltaTime);
                _lastSeenPlayerTime = Time.time;
            }
            else
            {
                _awareness = Mathf.MoveTowards(_awareness, 0f, 0.5f * Time.deltaTime);
            }

            if (_awareness >= 0.6f)
            {
                TransitionToCombat();
            }
        }

        private bool InFieldOfView(Vector3 point)
        {
            Vector3 dir = (point - _transform.position).normalized;
            float angle = Vector3.Angle(_transform.forward, dir);
            return angle <= perception.fieldOfView * 0.5f;
        }

        private bool HasLineOfSight(Vector3 point)
        {
            Vector3 origin = _transform.position + Vector3.up * 1.5f;
            if (Physics.Linecast(origin, point, out _, perception.lineOfSightMask))
            {
                return false;
            }
            return true;
        }

        private void HandleShotFired(Vector3 shotPos)
        {
            if (_state == AIState.Dead || _state == AIState.Combat) return;
            if (Vector3.Distance(_transform.position, shotPos) <= perception.hearingRange)
            {
                _investigateTarget = shotPos;
                if (_state == AIState.Patrol || _state == AIState.Idle)
                {
                    EnterState(AIState.Investigate);
                }
            }
        }

        private void HandleDamaged(Health.DamageInfo damage)
        {
            if (_state == AIState.Dead) return;
            if (damage.Source != null)
            {
                var player = damage.Source.transform.root;
                _player = player;
                _awareness = 1f;
                EnterState(AIState.Combat);
            }
        }

        private void HandleDeath(GameObject source)
        {
            EnterState(AIState.Dead);
            _agent.isStopped = true;
            if (body != null) Destroy(body, 2f);
            if (_attackRoutine != null) StopCoroutine(_attackRoutine);
            Destroy(gameObject, 3f);
        }

        // ---------- State Machine ----------

        private void EnterState(AIState newState)
        {
            if (_state == newState) return;
            _state = newState;
            _stateEnterTime = Time.time;

            switch (newState)
            {
                case AIState.Patrol:
                    _agent.speed = perception.patrolSpeed;
                    _agent.isStopped = false;
                    MoveToNextPatrolPoint();
                    break;
                case AIState.Investigate:
                    _agent.speed = perception.patrolSpeed;
                    _agent.isStopped = false;
                    _agent.SetDestination(_investigateTarget);
                    break;
                case AIState.Combat:
                    _agent.speed = perception.combatSpeed;
                    UpdateCombatDestination();
                    break;
                case AIState.Search:
                    _agent.speed = perception.patrolSpeed;
                    _agent.isStopped = false;
                    _agent.SetDestination(RandomSearchPoint());
                    break;
                case AIState.Dead:
                    _agent.isStopped = true;
                    break;
            }
        }

        private void UpdateState(float dt)
        {
            switch (_state)
            {
                case AIState.Patrol:
                    UpdatePatrol();
                    break;
                case AIState.Investigate:
                    UpdateInvestigate();
                    break;
                case AIState.Combat:
                    UpdateCombat(dt);
                    break;
                case AIState.Search:
                    UpdateSearch();
                    break;
            }
        }

        private void UpdatePatrol()
        {
            if (patrolPoints == null || patrolPoints.Length == 0)
            {
                if (_agent.remainingDistance <= _agent.stoppingDistance)
                {
                    EnterState(AIState.Idle);
                }
                return;
            }

            if (_agent.remainingDistance <= _agent.stoppingDistance && !_agent.pathPending)
            {
                MoveToNextPatrolPoint();
            }
        }

        private void MoveToNextPatrolPoint()
        {
            if (patrolPoints == null || patrolPoints.Length == 0) return;
            _agent.SetDestination(patrolPoints[_patrolIndex].position);
            _patrolIndex = (_patrolIndex + 1) % patrolPoints.Length;
        }

        private void UpdateInvestigate()
        {
            if (_agent.remainingDistance <= _agent.stoppingDistance && !_agent.pathPending)
            {
                EnterState(AIState.Search);
            }
        }

        private void UpdateSearch()
        {
            if (Time.time - _stateEnterTime > perception.searchDuration)
            {
                EnterState(AIState.Patrol);
            }
            else if (_agent.remainingDistance <= _agent.stoppingDistance && !_agent.pathPending)
            {
                _agent.SetDestination(RandomSearchPoint());
            }
        }

        private Vector3 RandomSearchPoint()
        {
            Vector2 r = Random.insideUnitCircle * perception.searchRadius;
            Vector3 target = _investigateTarget + new Vector3(r.x, 0f, r.y);
            if (NavMesh.SamplePosition(target, out var hit, 5f, NavMesh.AllAreas))
            {
                return hit.position;
            }
            return _transform.position;
        }

        private void TransitionToCombat()
        {
            if (_state != AIState.Combat) EnterState(AIState.Combat);
        }

        private void UpdateCombatDestination()
        {
            if (_player == null) return;
            _agent.isStopped = false;
            _agent.SetDestination(_player.position);
        }

        private void UpdateCombat(float dt)
        {
            if (_player == null) { EnterState(AIState.Patrol); return; }

            // If we lost sight for too long, switch to search.
            float sinceSeen = Time.time - _lastSeenPlayerTime;
            if (sinceSeen > perception.loseSightTime)
            {
                _investigateTarget = _player.position;
                EnterState(AIState.Search);
                return;
            }

            UpdateCombatDestination();
            FacePlayer(dt);

            // Attack if within range and cooldown ready.
            float dist = Vector3.Distance(_transform.position, _player.position);
            _attackTimer -= dt;
            if (dist <= perception.attackRange && _attackTimer <= 0f)
            {
                _attackTimer = perception.attackCooldown;
                StartAttack();
            }
        }

        private void FacePlayer(float dt)
        {
            if (_player == null) return;
            Vector3 dir = (_player.position - _transform.position).normalized;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.01f) return;
            var target = Quaternion.LookRotation(dir);
            _transform.rotation = Quaternion.RotateTowards(_transform.rotation, target, perception.turnSpeed * dt);
        }

        private void StartAttack()
        {
            if (_player == null) return;
            if (_attackRoutine != null) StopCoroutine(_attackRoutine);
            _attackRoutine = StartCoroutine(AttackRoutine());
        }

        private IEnumerator AttackRoutine()
        {
            yield return new WaitForSeconds(0.15f);

            if (_player == null) yield break;
            float dist = Vector3.Distance(_transform.position, _player.position);
            if (dist > perception.attackRange * 1.2f) yield break;

            var dir = (_player.position + Vector3.up * 1.4f - (_transform.position + Vector3.up * 1.5f)).normalized;
            RaycastHit hit;
            if (Physics.Raycast(_transform.position + Vector3.up * 1.5f, dir, out hit, perception.attackRange, GameLayers.ShootMask))
            {
                var damageable = hit.collider.GetComponentInParent<IDamageable>();
                if (damageable != null)
                {
                    damageable.TakeDamage(damage, hit.point, hit.normal, gameObject);
                }
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (!showGizmos || perception == null) return;
            Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.2f);
            Gizmos.DrawWireSphere(transform.position, perception.sightRange);
            Gizmos.color = new Color(1f, 1f, 0.2f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, perception.hearingRange);
        }
#endif
    }
}
