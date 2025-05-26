using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using UnityEngine.UI;
public class Enemy : MonoBehaviour
{
    public enum PatrolType { FixedRoute, RandomRoom }
    private enum MovementState { Moving, InElevator, Chasing, Attacking }

    [SerializeField] private EnemyData stats; // 적 스탯 (ScriptableObject)
    [SerializeField] private PatrolType patrolType; // 패트롤 유형
    [SerializeField] private List<NodeObject> fixedPatrolRoute; // 고정 루트 노드 리스트 (Inspector에서 설정)
    [SerializeField] private float elevatorTransitionTime = 2f; // 엘리베이터 이동 시간 (초)
    [SerializeField] private GameObject damagerPrefab; // Damager 프리팹 (Inspector에서 설정)
    [SerializeField] private int damagerPoolSize = 3; // 각 Enemy별 Damager 오브젝트 풀 크기 (기본값 3)

    private float currentHP; // 현재 체력
    private Node currentNode; // 현재 노드
    private Node targetNode; // 목표 노드
    private List<Node> currentPath; // 현재 경로
    private int currentPathIndex; // 경로 인덱스
    private List<Node> patrolRoute; // 패트롤 루트
    private int currentPatrolIndex; // 패트롤 인덱스
    private float moveTimer; // 이동 타이머
    private MovementState state = MovementState.Moving; // 현재 상태
    private GameObject targetPlayer; // 타겟 플레이어
    private float lastAttackTime; // 마지막 공격 시간
    private Node lastKnownPlayerNode; // 플레이어의 마지막 노드 위치
    private List<GameObject> damagerPool; // 각 Enemy의 Damager 오브젝트 풀
    private int currentPoolIndex; // 풀에서 사용할 다음 인덱스

    [SerializeField] private GameObject hpBarPrefab; // HP 바 프리팹
    private GameObject hpBarInstance;
    private Image hpFillImage;


    private void Awake()
    {
        if (stats == null)
        {
            Debug.LogError($"{gameObject.name}: EnemyStatsSO가 할당되지 않았습니다.");
            return;
        }
        if (damagerPrefab == null)
        {
            Debug.LogError($"{gameObject.name}: Damager 프리팹이 할당되지 않았습니다.");
            return;
        }
        currentHP = stats.MaxHP;
        InitializeDamagerPool();
        InitializePatrol();
    }

    private void Start()
    {
        if (MapManager.Instance == null)
        {
            Debug.LogError($"{gameObject.name}: MapManager.Instance가 null입니다. 씬에 MapManager가 있는지 확인하세요.");
            return;
        }

        // 초기 노드 설정
        NodeObject closestNodeObj = FindObjectsByType<NodeObject>(FindObjectsSortMode.None)
            .OrderBy(n => Vector3.Distance(transform.position, n.transform.position)).FirstOrDefault();
        if (closestNodeObj != null)
        {
            currentNode = MapManager.Instance.GetNodeByName(closestNodeObj.NodeName);
            if (currentNode == null)
            {
                Debug.LogError($"{gameObject.name}: 초기 노드({closestNodeObj.NodeName})를 MapManager에서 찾을 수 없습니다.");
                return;
            }
            SetNewTargetNode();
        }
        else
        {
            Debug.LogError($"{gameObject.name}: 씬에서 NodeObject를 찾을 수 없습니다.");
        }

        if (hpBarPrefab != null)
        {

            hpBarInstance = hpBarPrefab;
            hpFillImage = hpBarInstance.GetComponent<Image>();
        }
        else
        {
            Debug.LogWarning($"{gameObject.name}: HP 바 프리팹이 할당되지 않았습니다.");
        }

    }

    private void InitializeDamagerPool()
    {
        damagerPool = new List<GameObject>();
        currentPoolIndex = 0;

        // 각 Enemy별 Damager 오브젝트 풀 생성
        for (int i = 0; i < damagerPoolSize; i++)
        {
            GameObject damager = Instantiate(damagerPrefab, Vector3.zero, Quaternion.identity);
            damager.SetActive(false);
            damager.transform.SetParent(transform); // Enemy의 자식으로 설정하여 관리 용이
            damagerPool.Add(damager);
        }
    }

    private void InitializePatrol()
    {
        if (MapManager.Instance == null)
        {
            Debug.LogError($"{gameObject.name}: MapManager.Instance가 null입니다. InitializePatrol 실패.");
            patrolType = PatrolType.RandomRoom;
            return;
        }

        if (patrolType == PatrolType.FixedRoute)
        {
            patrolRoute = new List<Node>();

            // Generator가 있는 노드들을 가져옴
            var generatorNodes = MapManager.Instance.GetNodesWithGenerators().ToList();
            if (generatorNodes == null || generatorNodes.Count == 0)
            {
                Debug.LogWarning($"{gameObject.name}: Generator가 있는 노드가 없습니다. 랜덤 룸 패트롤로 전환합니다.");
                patrolType = PatrolType.RandomRoom;
                return;
            }

            // fixedPatrolRoute가 Inspector에서 설정된 경우, 이를 우선적으로 사용
            if (fixedPatrolRoute != null && fixedPatrolRoute.Count > 0)
            {
                fixedPatrolRoute.RemoveAll(n => n == null);
                if (fixedPatrolRoute.Count > 0)
                {
                    foreach (var nodeObj in fixedPatrolRoute)
                    {
                        if (nodeObj == null) continue;
                        Node node = MapManager.Instance.GetNodeByName(nodeObj.NodeName);
                        if (node != null)
                        {
                            patrolRoute.Add(node);
                        }
                        else
                        {
                            Debug.LogWarning($"{gameObject.name}: {nodeObj.NodeName}에 해당하는 노드를 MapManager에서 찾을 수 없습니다.");
                        }
                    }
                }
            }

            // fixedPatrolRoute가 비어있거나 설정되지 않은 경우, Generator 노드를 사용
            if (patrolRoute.Count == 0)
            {
                patrolRoute = generatorNodes;
            }

            if (patrolRoute.Count == 0)
            {
                Debug.LogWarning($"{gameObject.name}: 유효한 패트롤 노드가 없습니다. 랜덤 룸 패트롤로 전환합니다.");
                patrolType = PatrolType.RandomRoom;
            }
        }
    }

    private void Update()
    {
        if (state == MovementState.InElevator) return;

        // 플레이어 감지
        DetectPlayer();

        // 상태에 따른 행동
        if (state == MovementState.Chasing)
        {
            ChasePlayer();
        }
        else if (state == MovementState.Attacking)
        {
            AttackPlayer();
        }
        else if (state == MovementState.Moving)
        {
            if (currentNode == null || targetNode == null || currentPath == null) return;
            MoveAlongPath();
        }
    }

    private void DetectPlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("PlayerMon");
        if (player == null) return;

        Vector3 toPlayer = player.transform.position - transform.position;
        float distanceToPlayer = toPlayer.magnitude;

        // X축 기준 앞/뒤 감지 범위 확인
        bool isInFront = toPlayer.x > 0 && Mathf.Abs(toPlayer.x) <= stats.FrontDetectionRange && Mathf.Abs(toPlayer.y) < 1f;
        bool isInRear = toPlayer.x < 0 && Mathf.Abs(toPlayer.x) <= stats.RearDetectionRange && Mathf.Abs(toPlayer.y) < 1f;

        if (isInFront || isInRear)
        {
            // 플레이어의 현재 노드 확인
            NodeObject playerNodeObj = FindObjectsByType<NodeObject>(FindObjectsSortMode.None)
                .OrderBy(n => Vector3.Distance(player.transform.position, n.transform.position)).FirstOrDefault();
            if (playerNodeObj != null)
            {
                Node playerNode = MapManager.Instance.GetNodeByName(playerNodeObj.NodeName);
                if (playerNode != null)
                {
                    // 타겟 설정
                    if (targetPlayer == null)
                    {
                        targetPlayer = player;
                        lastKnownPlayerNode = playerNode;
                        state = MovementState.Chasing;
                        Debug.Log($"{gameObject.name}: 플레이어 감지, 추적 시작");
                    }
                    else
                    {
                        // 타겟이 2노드 이상 멀어졌는지 확인
                        List<Node> pathToPlayer = AStarPathfinding.FindPath(currentNode, playerNode);
                        if (pathToPlayer == null || pathToPlayer.Count > 2)
                        {
                            ClearTarget();
                        }
                        else
                        {
                            lastKnownPlayerNode = playerNode;
                        }
                    }
                }
            }
        }
        else if (targetPlayer != null)
        {
            // 감지 범위 밖이면 타겟 해제
            ClearTarget();
        }
    }

    private void ChasePlayer()
    {
        if (targetPlayer == null)
        {
            ClearTarget();
            return;
        }

        // 플레이어의 현재 노드 가져오기
        NodeObject playerNodeObj = FindObjectsByType<NodeObject>(FindObjectsSortMode.None)
            .OrderBy(n => Vector3.Distance(targetPlayer.transform.position, n.transform.position)).FirstOrDefault();
        if (playerNodeObj == null)
        {
            ClearTarget();
            return;
        }

        Node playerNode = MapManager.Instance.GetNodeByName(playerNodeObj.NodeName);
        if (playerNode == null)
        {
            ClearTarget();
            return;
        }

        // 플레이어와 2노드 이상 멀어졌는지 확인
        List<Node> pathToPlayer = AStarPathfinding.FindPath(currentNode, playerNode);
        if (pathToPlayer == null || pathToPlayer.Count > 2)
        {
            ClearTarget();
            return;
        }

        // 공격 범위 내인지 확인
        float distanceToPlayer = Vector3.Distance(transform.position, targetPlayer.transform.position);
        if (distanceToPlayer <= stats.AttackRange)
        {
            state = MovementState.Attacking;
            return;
        }

        // 경로 갱신
        if (playerNode != targetNode)
        {
            targetNode = playerNode;
            currentPath = pathToPlayer;
            currentPathIndex = 0;
        }

        // 1.5배 속도로 이동
        if (currentPath != null && currentPathIndex < currentPath.Count)
        {
            Node nextNode = currentPath[currentPathIndex];
            if (currentNode.Type == NodeObject.NodeType.Elevator && nextNode.Type == NodeObject.NodeType.Elevator)
            {
                StartElevatorTransition(nextNode);
                return;
            }

            Vector3 targetPosition = nextNode.Position;
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, stats.MoveSpeed * 1.5f * Time.deltaTime);

            if (Vector3.Distance(transform.position, targetPosition) < 0.1f)
            {
                currentNode = nextNode;
                currentPathIndex++;

                if (currentPathIndex >= currentPath.Count)
                {
                    currentPath = AStarPathfinding.FindPath(currentNode, targetNode);
                    currentPathIndex = 0;
                }
            }
        }
    }

    private void AttackPlayer()
    {
        if (targetPlayer == null)
        {
            ClearTarget();
            return;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, targetPlayer.transform.position);
        if (distanceToPlayer > stats.AttackRange)
        {
            state = MovementState.Chasing;
            return;
        }

        // 공격 쿨타임 확인
        if (Time.time >= lastAttackTime + stats.AttackCooldown)
        {
            // 오브젝트 풀에서 Damager 활성화
            GameObject damager = GetPooledDamager();
            if (damager != null)
            {
                Vector3 direction = (targetPlayer.transform.position - transform.position).normalized;
                Vector3 spawnPosition = transform.position + direction * 0.5f;
                damager.transform.position = spawnPosition;
                damager.SetActive(true);
                lastAttackTime = Time.time;
                //Debug.Log($"{gameObject.name}: Damager 활성화 at {spawnPosition}");
            }
            else
            {
                Debug.LogWarning($"{gameObject.name}: 사용 가능한 Damager 오브젝트가 풀에 없습니다.");
            }
        }
    }

    private GameObject GetPooledDamager()
    {
        // 풀에서 비활성화된 Damager 찾기
        for (int i = 0; i < damagerPoolSize; i++)
        {
            GameObject damager = damagerPool[currentPoolIndex];
            currentPoolIndex = (currentPoolIndex + 1) % damagerPoolSize;
            if (!damager.activeInHierarchy)
            {
                return damager;
            }
        }
        return null; // 사용 가능한 Damager 없음
    }

    private void ClearTarget()
    {
        targetPlayer = null;
        lastKnownPlayerNode = null;
        state = MovementState.Moving;
        SetNewTargetNode();
        Debug.Log($"{gameObject.name}: 타겟 해제, 패트롤로 복귀");
    }

    private void SetNewTargetNode()
    {
        if (MapManager.Instance == null)
        {
            Debug.LogError($"{gameObject.name}: MapManager.Instance가 null입니다. SetNewTargetNode 실패.");
            return;
        }

        if (patrolType == PatrolType.FixedRoute)
        {
            if (patrolRoute == null || patrolRoute.Count == 0)
            {
                Debug.LogWarning($"{gameObject.name}: patrolRoute가 비어 있습니다. 랜덤 룸 패트롤로 전환합니다.");
                patrolType = PatrolType.RandomRoom;
                SetNewTargetNode();
                return;
            }
            currentPatrolIndex = (currentPatrolIndex + 1) % patrolRoute.Count;
            targetNode = patrolRoute[currentPatrolIndex];
        }
        else // RandomRoom
        {
            var roomNodes = MapManager.Instance.AllNodes
                .Where(n => n.Type == NodeObject.NodeType.Room && n != currentNode)
                .ToList();
            if (roomNodes == null || roomNodes.Count == 0)
            {
                Debug.LogWarning($"{gameObject.name}: 유효한 Room 노드가 없습니다.");
                return;
            }
            targetNode = roomNodes[Random.Range(0, roomNodes.Count)];
        }

        if (targetNode != null)
        {
            currentPath = AStarPathfinding.FindPath(currentNode, targetNode);
            if (currentPath == null)
            {
                Debug.LogWarning($"{gameObject.name}: {currentNode.NodeName}에서 {targetNode.NodeName}으로의 경로를 찾을 수 없습니다.");
                return;
            }
            currentPathIndex = 0;
        }
    }

    private void MoveAlongPath()
    {
        if (currentPath == null || currentPathIndex >= currentPath.Count) return;

        Node nextNode = currentPath[currentPathIndex];
        if (currentNode.Type == NodeObject.NodeType.Elevator && nextNode.Type == NodeObject.NodeType.Elevator)
        {
            StartElevatorTransition(nextNode);
            return;
        }

        Vector3 targetPosition = nextNode.Position;
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, stats.MoveSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, targetPosition) < 0.1f)
        {
            currentNode = nextNode;
            currentPathIndex++;

            if (currentPathIndex >= currentPath.Count)
            {
                SetNewTargetNode();
            }
        }
    }

    private void StartElevatorTransition(Node elevatorNode)
    {
        state = MovementState.InElevator;
        StartCoroutine(ElevatorTransition(elevatorNode));
    }

    private IEnumerator ElevatorTransition(Node targetElevator)
    {
        //Debug.Log($"{gameObject.name}: {currentNode.Floor}층에서 엘리베이터 탑승");
        yield return new WaitForSeconds(elevatorTransitionTime);
        transform.position = targetElevator.Position;
        currentNode = targetElevator;
        currentPathIndex++;
        if (currentPathIndex >= currentPath.Count)
        {
            if (state == MovementState.Chasing)
            {
                ChasePlayer();
            }
            else
            {
                SetNewTargetNode();
            }
        }
        state = MovementState.Moving;
        //Debug.Log($"{gameObject.name}: {targetElevator.Floor}층에 도착");
    }

    public void TakeDamage(float damage)
    {
        currentHP -= damage;

        // HP 바 업데이트
        if (hpFillImage != null)
        {
            hpFillImage.fillAmount = currentHP / stats.MaxHP;
        }

        if (currentHP <= 0)
        {
            Destroy(gameObject);
        }
    }
}