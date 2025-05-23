using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;
using UnityEditor;
using static Unity.Burst.Intrinsics.X86.Avx;

public class NodeBasedMovement : MonoBehaviour
{
    public class Node
    {
        public Vector3 Position;
        public List<Node> Neighbors;
        public NodeObject.NodeType Type; // Updated to use NodeType
        public int Floor;
        public string Name;


        public Node(Vector3 position, int floor, NodeObject.NodeType type, string name = "")
        {
            Position = position;
            Neighbors = new List<Node>();
            Floor = floor;
            Type = type;
            Name = name;
        }
    }

    public float moveSpeed = 2f;
    public float neighborRadius = 4f;
    public float separationDistance = 0.8f;
    public float cohesionWeight = 0.8f;
    public float separationWeight = 2f;
    public float alignmentWeight = 1f;
    public float nodeWeight = 3f;
    public float smoothingFactor = 0.1f;
    public float targetRadius = 1.5f;
    private List<Node> nodes = new List<Node>();
    private Dictionary<int, List<Node>> floors = new Dictionary<int, List<Node>>();
    private Node currentNode;
    private List<Node> currentPath;
    private int pathIndex;
    private EmployeeState state = EmployeeState.Idle;

    private Node targetNode;
    private float elevatorTransitionTime = 1f;
    private Vector3 velocity;
    private Vector3 targetPosition;
    private static List<NodeBasedMovement> selectedCharacters;
    private EmployeeActions employeeActions; // Reference to action component
    private Action onMovementComplete;
    public enum EmployeeState { Idle, Moving, InElevator }

    public EmployeeState GetState()
    {
        return state;
    }

    public void SetState(EmployeeState newState)
    {
        state = newState;
    }

    void Start()
    {
        InitializeMapFromScene();
        currentNode = nodes.Find(n => n.Name == "Room1_F1") ?? nodes[0];
        transform.position = currentNode.Position;
        velocity = Vector3.zero;

        if (selectedCharacters == null)
        {
            selectedCharacters = new List<NodeBasedMovement>();
        }

        // Get or add EmployeeActions component
        employeeActions = GetComponent<EmployeeActions>();
        if (employeeActions == null)
        {
            employeeActions = gameObject.AddComponent<EmployeeActions>();
        }
    }

    void Update()
    {
        if (state == EmployeeState.Moving)
        {
            MoveAlongPath();
        }
        else if (state == EmployeeState.InElevator)
        {
            // Elevator transition, no changes needed
        }
        else if (state == EmployeeState.Idle)
        {
            // Let EmployeeActions handle idle state actions
        }
    }

    void InitializeMapFromScene()
    {
        NodeObject[] nodeObjects = FindObjectsOfType<NodeObject>();
        Dictionary<NodeObject, Node> nodeObjectToNode = new Dictionary<NodeObject, Node>();
        //foreach (NodeObject nodeObj in nodeObjects)
        //{
        //    Node node = nodeObj.GetNode();
        //    nodes.Add(node);
        //    nodeObjectToNode[nodeObj] = node;
        //    if (!floors.ContainsKey(node.Floor))
        //    {
        //        floors[node.Floor] = new List<Node>();
        //    }
        //    floors[node.Floor].Add(node);
        //}

        foreach (NodeObject nodeObj in nodeObjects)
        {
            Node node = nodeObjectToNode[nodeObj];
            foreach (NodeObject neighborObj in nodeObj.Neighbors)
            {
                if (nodeObjectToNode.ContainsKey(neighborObj))
                {
                    node.Neighbors.Add(nodeObjectToNode[neighborObj]);
                }
            }
        }

        Debug.Log($"Initialized {nodes.Count} nodes across {floors.Count} floors.");
    }

    public void MoveToNode(string targetNodeName, Action onCompleteCallback = null) // 콜백 파라미터 추가
    {
        employeeActions.CancelAction(); // 이동 시작 전 현재 액션 취소

        targetNode = nodes.Find(n => n.Name == targetNodeName);
        if (targetNode == null)
        {
            Debug.LogError("Target node not found: " + targetNodeName);
            onCompleteCallback?.Invoke(); // 타겟 노드가 없으면 즉시 콜백 호출 (실패 처리)
            return;
        }

        currentPath = FindPath(currentNode, targetNode);
        if (currentPath != null && currentPath.Count > 0)
        {
            pathIndex = 0;
            state = EmployeeState.Moving;
            this.onMovementComplete = onCompleteCallback; // 콜백 저장
            SetRandomTargetPosition();
            Debug.Log($"{gameObject.name} starting move to {targetNodeName}. Callback set: {onMovementComplete != null}");
        }
        else
        {
            Debug.LogWarning($"{gameObject.name} could not find path to {targetNodeName}.");
            this.onMovementComplete = null; // 경로 없으면 콜백도 없음
            onCompleteCallback?.Invoke(); // 경로가 없으면 즉시 콜백 호출 (실패 처리)
            state = EmployeeState.Idle; // 경로 없으면 Idle 상태로
        }
    }

    public List<Node> GetNodes()
    {
        return nodes;
    }

    public void SetSelected(bool isSelected)
    {
        if (isSelected && !selectedCharacters.Contains(this))
        {
            selectedCharacters.Add(this);
        }
        else if (!isSelected && selectedCharacters.Contains(this))
        {
            selectedCharacters.Remove(this);
        }
    }

    // Expose method to set state to Idle (used by EmployeeActions)
    public void SetIdle()
    {
        state = EmployeeState.Idle;
    }

    // Expose current node for EmployeeActions to check node type
    public Node GetCurrentNode()
    {
        return currentNode;
    }

    List<Node> FindPath(Node start, Node goal)
    {
        var openSet = new List<Node> { start };
        var cameFrom = new Dictionary<Node, Node>();
        var gScore = new Dictionary<Node, float> { { start, 0 } };
        var fScore = new Dictionary<Node, float> { { start, Vector3.Distance(start.Position, goal.Position) } };

        while (openSet.Count > 0)
        {
            Node current = openSet.OrderBy(n => fScore.GetValueOrDefault(n, float.MaxValue)).First();
            if (current == goal)
            {
                List<Node> path = new List<Node>();
                while (cameFrom.ContainsKey(current))
                {
                    path.Add(current);
                    current = cameFrom[current];
                }
                path.Add(start);
                path.Reverse();
                return path;
            }

            openSet.Remove(current);
            foreach (var neighbor in current.Neighbors)
            {
                float tentativeGScore = gScore[current] + Vector3.Distance(current.Position, neighbor.Position);
                if (tentativeGScore < gScore.GetValueOrDefault(neighbor, float.MaxValue))
                {
                    cameFrom[neighbor] = current;
                    gScore[neighbor] = tentativeGScore;
                    fScore[neighbor] = tentativeGScore + Vector3.Distance(neighbor.Position, goal.Position);
                    if (!openSet.Contains(neighbor))
                    {
                        openSet.Add(neighbor);
                    }
                }
            }
        }

        return new List<Node>();
    }

    void MoveAlongPath()
    {
        if (state != EmployeeState.Moving || currentPath == null || pathIndex >= currentPath.Count)
        {
            // 이동이 완료되었거나, 경로가 없거나, 이미 끝에 도달한 경우
            if (state == EmployeeState.Moving) // 정상적으로 경로 끝에 도달한 경우
            {
                state = EmployeeState.Idle;
                currentNode = targetNode; // 실제 목표 노드로 현재 노드 업데이트
                velocity = Vector3.zero;
                Debug.Log($"{gameObject.name} reached destination {currentNode?.Name}. Invoking callback.");
                onMovementComplete?.Invoke(); // 저장된 콜백 호출
                onMovementComplete = null;    // 콜백 사용 후 초기화
            }
            return;
        }

        Node nextNodeInPath = currentPath[pathIndex]; // nextNode 변수명 변경 (targetNode와 혼동 방지)
        if (nextNodeInPath.Type == NodeObject.NodeType.Elevator && currentNode.Floor != nextNodeInPath.Floor)
        {
            StartElevatorTransition(nextNodeInPath);
            return;
        }

        // ... (기존 이동 로직: CalculateFlockVelocity, velocity 계산, transform.position 업데이트)
        Vector3 flockVelocity = CalculateFlockVelocity();
        Vector3 nodeDirection = (targetPosition - transform.position).normalized;
        Vector3 targetVelocity = (nodeWeight * nodeDirection + flockVelocity).normalized * moveSpeed;

        velocity = Vector3.Lerp(velocity, targetVelocity, smoothingFactor);
        transform.position += velocity * Time.deltaTime;


        Vector3 toTargetPos = targetPosition - transform.position; // targetPosition은 SetRandomTargetPosition에서 설정된 노드 내 랜덤 위치
        // 현재 경로의 다음 노드(nextNodeInPath)에 충분히 가까워졌는지 확인하는 것이 아니라,
        // 노드 내의 랜덤 목표 지점(targetPosition)에 도달했는지 확인
        if (Vector3.Distance(transform.position, targetPosition) < 0.1f) // 도달 판정 거리 (조절 가능)
        {
            transform.position = new Vector3(targetPosition.x, targetPosition.y, targetPosition.z); // 정확한 위치로 보정
            currentNode = nextNodeInPath; // 현재 노드를 경로상의 다음 노드로 업데이트
            pathIndex++;

            if (pathIndex >= currentPath.Count) // 경로의 끝에 도달했다면
            {
                state = EmployeeState.Idle; // Idle 상태로 변경
                // currentNode는 이미 마지막 노드로 설정됨
                Debug.Log($"{gameObject.name} reached FINAL destination {currentNode?.Name}. Invoking callback.");
                onMovementComplete?.Invoke(); // 콜백 호출
                onMovementComplete = null;    // 콜백 초기화
            }
            else // 아직 경로가 남았다면
            {
                SetRandomTargetPosition(); // 다음 경로의 노드 내 랜덤 위치 설정
            }
        }
    }

    void SetRandomTargetPosition()
    {
        if (pathIndex >= currentPath.Count) return;
        Node nextNode = currentPath[pathIndex];
        Vector2 randomOffset = UnityEngine.Random.insideUnitCircle * targetRadius;
        targetPosition = nextNode.Position + new Vector3(randomOffset.x, 0, randomOffset.y);
    }

    void OnDestroy()
    {
        // Remove this instance from selectedCharacters when destroyed
        if (selectedCharacters != null && selectedCharacters.Contains(this))
        {
            selectedCharacters.Remove(this);
            Debug.Log($"{gameObject.name} removed from selectedCharacters on destroy.");
        }
    }

    Vector3 CalculateFlockVelocity()
    {
        Vector3 cohesion = Vector3.zero;
        Vector3 separation = Vector3.zero;
        Vector3 alignment = Vector3.zero;
        int neighborCount = 0;

        Vector3 toTarget = targetPosition - transform.position;
        float flockWeight = Mathf.Clamp01((Mathf.Max(Mathf.Abs(toTarget.x), Mathf.Abs(toTarget.z)) / targetRadius));

        foreach (NodeBasedMovement other in selectedCharacters)
        {
            if (other == this || other.state != EmployeeState.Moving || other.currentNode.Floor != currentNode.Floor)
                continue;

            float distance = Vector3.Distance(transform.position, other.transform.position);
            if (distance < neighborRadius && distance > 0)
            {
                neighborCount++;
                cohesion += other.transform.position;
                if (distance < separationDistance)
                {
                    Vector3 away = transform.position - other.transform.position;
                    separation += away.normalized / distance;
                }
                alignment += other.velocity;
            }
        }

        if (neighborCount == 0)
            return Vector3.zero;

        cohesion = (cohesion / neighborCount - transform.position).normalized * cohesionWeight * flockWeight;
        separation = separation.normalized * separationWeight * flockWeight;
        alignment = (alignment / neighborCount).normalized * alignmentWeight * flockWeight;

        return cohesion + separation + alignment;
    }

    void StartElevatorTransition(Node elevatorNode)
    {
        state = EmployeeState.InElevator;
        velocity = Vector3.zero;
        StartCoroutine(ElevatorTransition(elevatorNode));
    }

    System.Collections.IEnumerator ElevatorTransition(Node targetElevator)
    {
        Debug.Log("Entering elevator on floor " + currentNode.Floor);
        yield return new WaitForSeconds(elevatorTransitionTime);
        transform.position = targetElevator.Position;
        currentNode = targetElevator;
        pathIndex++;
        if (pathIndex < currentPath.Count)
        {
            SetRandomTargetPosition();
        }
        state = EmployeeState.Moving;
        Debug.Log("Arrived at floor " + targetElevator.Floor);
    }


}