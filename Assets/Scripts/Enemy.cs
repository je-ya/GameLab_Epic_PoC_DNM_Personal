using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class Enemy : MonoBehaviour
{
    [SerializeField] private float _health = 100f;
    public float health => _health;
    [SerializeField] private float attackPower = 10f;
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float attackCooldown = 1f;

    private List<Generator> generators;
    private int currentGeneratorIndex = 0;
    private NodeObject currentNode;
    private NodeObject targetNode;
    private List<NodeObject> path;
    private int currentPathIndex;
    private GameObject attackTarget;
    private float lastAttackTime;
    private bool isAttacking;

    void Start()
    {
        StartCoroutine(InitializeEnemy());
    }

    private IEnumerator InitializeEnemy()
    {
        // GameManager가 초기화될 때까지 대기
        while (GameManager.Instance == null)
        {
            Debug.LogWarning("GameManager not yet initialized, retrying...");
            yield return new WaitForSeconds(0.1f);
        }

        // 발전기 목록이 채워질 때까지 대기
        int retryCount = 0;
        const int maxRetries = 50; // 최대 5초 대기 (0.1초 * 50)
        while (retryCount < maxRetries)
        {
            generators = GameManager.Instance.GetGenerators();
            if (generators != null && generators.Count > 0)
            {
                Debug.Log($"Found {generators.Count} generators in GameManager.");
                break;
            }
            Debug.LogWarning($"No generators found in GameManager, retrying... (Attempt {retryCount + 1}/{maxRetries})");
            retryCount++;
            yield return new WaitForSeconds(0.1f);
        }

        if (generators == null || generators.Count == 0)
        {
            Debug.LogError("No generators found in GameManager after retries!");
            yield break;
        }

        // 초기 위치에서 가장 가까운 NodeObject 찾기
        currentNode = FindClosestNode();
        if (currentNode == null)
        {
            Debug.LogError("No initial node found for enemy!");
            yield break;
        }

        // 첫 번째 발전기로 경로 설정
        SetPathToGenerator();
    }

    // 나머지 메서드는 동일
    void Update()
    {
        if (generators == null || generators.Count == 0 || currentNode == null) return;

        if (!isAttacking)
        {
            GameObject player = DetectPlayerMon();
            if (player != null)
            {
                attackTarget = player;
                isAttacking = true;
                path = null;
            }
            else
            {
                MoveToTarget();
            }
        }
        else
        {
            HandleAttackState();
        }
    }

    private NodeObject FindClosestNode()
    {
        NodeObject[] allNodes = FindObjectsOfType<NodeObject>();
        NodeObject closest = null;
        float minDistance = Mathf.Infinity;
        Vector3 position = transform.position;

        foreach (NodeObject node in allNodes)
        {
            float distance = Vector3.Distance(position, node.transform.position);
            if (distance < minDistance)
            {
                minDistance = distance;
                closest = node;
            }
        }
        return closest;
    }

    private GameObject DetectPlayerMon()
    {
        Collider[] colliders = Physics.OverlapSphere(currentNode.transform.position, attackRange);
        foreach (Collider col in colliders)
        {
            if (col.CompareTag("PlayerMon"))
            {
                return col.gameObject;
            }
        }
        return null;
    }

    private void HandleAttackState()
    {
        if (attackTarget == null)
        {
            isAttacking = false;
            SetPathToGenerator();
            return;
        }

        NodeObject playerNode = FindClosestNodeTo(attackTarget.transform.position);
        int distance = CalculateNodeDistance(currentNode, playerNode);

        if (distance > 2)
        {
            isAttacking = false;
            attackTarget = null;
            SetPathToGenerator();
            return;
        }

        float distanceToTarget = Vector3.Distance(transform.position, attackTarget.transform.position);
        if (distanceToTarget <= attackRange && Time.time >= lastAttackTime + attackCooldown)
        {
            Attack(attackTarget);
            lastAttackTime = Time.time;
        }
        else if (distanceToTarget > attackRange)
        {
            path = FindPath(currentNode, playerNode);
            currentPathIndex = 0;
            MoveToTarget();
        }
    }

    private void Attack(GameObject target)
    {
        EmployeeActions player = target.GetComponent<EmployeeActions>();
        if (player != null)
        {
            player.TakeDamage(attackPower);
            Debug.Log($"Enemy attacked {target.name}, dealt {attackPower} damage");
        }
    }

    private void SetPathToGenerator()
    {
        if (generators == null || generators.Count == 0)
        {
            Debug.LogWarning("No generators available to set path!");
            return;
        }

        // Cycle back to the first generator if the end is reached
        if (currentGeneratorIndex >= generators.Count)
        {
            currentGeneratorIndex = 0;
            Debug.Log("Cycling back to first generator!");
        }

        NodeObject generatorNode = FindClosestNodeTo(generators[currentGeneratorIndex].transform.position);
        path = FindPath(currentNode, generatorNode);
        currentPathIndex = 0;
        targetNode = path != null && path.Count > 0 ? path[0] : null;
    }

    private void MoveToTarget()
    {
        if (path == null || currentPathIndex >= path.Count)
        {
            if (currentGeneratorIndex < generators.Count &&
                Vector3.Distance(transform.position, generators[currentGeneratorIndex].transform.position) < 0.1f)
            {
                currentGeneratorIndex++;
                SetPathToGenerator();
            }
            return;
        }

        targetNode = path[currentPathIndex];
        Vector3 targetPosition = targetNode.transform.position;
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, targetPosition) < 0.1f)
        {
            currentNode = targetNode;
            currentPathIndex++;
            if (currentPathIndex < path.Count)
            {
                targetNode = path[currentPathIndex];
            }
        }
    }

    private NodeObject FindClosestNodeTo(Vector3 position)
    {
        NodeObject[] allNodes = FindObjectsOfType<NodeObject>();
        NodeObject closest = null;
        float minDistance = Mathf.Infinity;

        foreach (NodeObject node in allNodes)
        {
            float distance = Vector3.Distance(position, node.transform.position);
            if (distance < minDistance)
            {
                minDistance = distance;
                closest = node;
            }
        }
        return closest;
    }

    private int CalculateNodeDistance(NodeObject start, NodeObject end)
    {
        if (start == null || end == null) return int.MaxValue;

        Queue<NodeObject> queue = new Queue<NodeObject>();
        Dictionary<NodeObject, int> distances = new Dictionary<NodeObject, int>();
        queue.Enqueue(start);
        distances[start] = 0;

        while (queue.Count > 0)
        {
            NodeObject current = queue.Dequeue();
            if (current == end) return distances[current];

            foreach (NodeObject neighbor in current.Neighbors)
            {
                if (!distances.ContainsKey(neighbor))
                {
                    distances[neighbor] = distances[current] + 1;
                    queue.Enqueue(neighbor);
                }
            }
        }
        return int.MaxValue;
    }

    private List<NodeObject> FindPath(NodeObject start, NodeObject goal)
    {
        if (start == null || goal == null) return null;

        List<NodeObject> openSet = new List<NodeObject> { start };
        Dictionary<NodeObject, NodeObject> cameFrom = new Dictionary<NodeObject, NodeObject>();
        Dictionary<NodeObject, float> gScore = new Dictionary<NodeObject, float> { { start, 0 } };
        Dictionary<NodeObject, float> fScore = new Dictionary<NodeObject, float> { { start, Vector3.Distance(start.transform.position, goal.transform.position) } };

        while (openSet.Count > 0)
        {
            NodeObject current = openSet.OrderBy(n => fScore.GetValueOrDefault(n, float.MaxValue)).First();
            if (current == goal)
            {
                return ReconstructPath(cameFrom, current);
            }

            openSet.Remove(current);
            foreach (NodeObject neighbor in current.Neighbors)
            {
                float tentativeGScore = gScore[current] + Vector3.Distance(current.transform.position, neighbor.transform.position);
                if (tentativeGScore < gScore.GetValueOrDefault(neighbor, float.MaxValue))
                {
                    cameFrom[neighbor] = current;
                    gScore[neighbor] = tentativeGScore;
                    fScore[neighbor] = gScore[neighbor] + Vector3.Distance(neighbor.transform.position, goal.transform.position);
                    if (!openSet.Contains(neighbor))
                    {
                        openSet.Add(neighbor);
                    }
                }
            }
        }
        return null;
    }

    private List<NodeObject> ReconstructPath(Dictionary<NodeObject, NodeObject> cameFrom, NodeObject current)
    {
        List<NodeObject> path = new List<NodeObject> { current };
        while (cameFrom.ContainsKey(current))
        {
            current = cameFrom[current];
            path.Insert(0, current);
        }
        return path;
    }

    public void TakeDamage(float damage)
    {
        _health -= damage;
        if (_health <= 0)
        {
            Destroy(gameObject);
            Debug.Log("Enemy destroyed!");
        }
    }
}