using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

public class MonMovemont : MonoBehaviour
{
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
    private MonMovemontState state = MonMovemontState.Idle;

    private Node targetNode;
    private float elevatorTransitionTime = 1f;
    private Vector3 velocity;
    private Vector3 targetPosition;
    private static List<MonMovemont> selectedCharacters;
    private MonAction employeeActions; 
    private Action onMovementComplete;
    public enum MonMovemontState { Idle, Moving, InElevator }

    public MonMovemontState GetState()
    {
        return state;
    }

    public void SetState(MonMovemontState newState)
    {
        state = newState;
    }

    void Start()
    {
        if (MapManager.Instance == null)
        {
            Debug.LogError("MapManager instance is null. Ensure MapManager is initialized in the scene.");
            return;
        }

        nodes = new List<Node>(MapManager.Instance.AllNodes);
        floors = new Dictionary<int, List<Node>>(MapManager.Instance.NodesByFloor
            .ToDictionary(kvp => kvp.Key, kvp => new List<Node>(kvp.Value)));

        currentNode = MapManager.Instance.GetNodeByName("Room1_F1") ?? nodes.FirstOrDefault();
        if (currentNode == null)
        {
            Debug.LogError("No nodes available in the map. Check MapManager initialization.");
            return;
        }
        transform.position = currentNode.Position;
        velocity = Vector3.zero;

        if (selectedCharacters == null)
        {
            selectedCharacters = new List<MonMovemont>();
        }

        employeeActions = GetComponent<MonAction>();
        if (employeeActions == null)
        {
            employeeActions = gameObject.AddComponent<MonAction>();
        }
    }

    void Update()
    {
        if (state == MonMovemontState.Moving)
        {
            MoveAlongPath();
        }
        else if (state == MonMovemontState.InElevator)
        {
        }
        else if (state == MonMovemontState.Idle)
        {
        }
    }

    public void MoveToNode(string targetNodeName, Action onCompleteCallback = null)
    {
        employeeActions.CancelAction(); 

        targetNode = MapManager.Instance.GetNodeByName(targetNodeName);
        if (targetNode == null)
        {
            Debug.LogError("Target node not found: " + targetNodeName);
            onCompleteCallback?.Invoke();
            return;
        }

        currentPath = FindPath(currentNode, targetNode);
        if (currentPath != null && currentPath.Count > 0)
        {
            pathIndex = 0;
            state = MonMovemontState.Moving;
            this.onMovementComplete = onCompleteCallback;
            SetRandomTargetPosition();
            Debug.Log($"{gameObject.name} starting move to {targetNodeName}. Callback set: {onMovementComplete != null}");
        }
        else
        {
            Debug.LogWarning($"{gameObject.name} could not find path to {targetNodeName}.");
            this.onMovementComplete = null;
            onCompleteCallback?.Invoke();
            state = MonMovemontState.Idle;
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
        if (state != MonMovemontState.Moving || currentPath == null || pathIndex >= currentPath.Count)
        {
            if (state == MonMovemontState.Moving)
            {
                state = MonMovemontState.Idle;
                currentNode = targetNode;
                velocity = Vector3.zero;
                Debug.Log($"{gameObject.name} reached destination {currentNode?.NodeName}. Invoking callback.");
                onMovementComplete?.Invoke();
                onMovementComplete = null;
            }
            return;
        }

        Node nextNodeInPath = currentPath[pathIndex];
        if (nextNodeInPath.Type == NodeObject.NodeType.Elevator && currentNode.Floor != nextNodeInPath.Floor)
        {
            StartElevatorTransition(nextNodeInPath);
            return;
        }

        Vector3 flockVelocity = CalculateFlockVelocity();
        Vector3 nodeDirection = (targetPosition - transform.position).normalized;
        Vector3 targetVelocity = (nodeWeight * nodeDirection + flockVelocity).normalized * moveSpeed;

        velocity = Vector3.Lerp(velocity, targetVelocity, smoothingFactor);
        transform.position += velocity * Time.deltaTime;

        Vector3 toTargetPos = targetPosition - transform.position;
        if (Vector3.Distance(transform.position, targetPosition) < 0.1f)
        {
            transform.position = new Vector3(targetPosition.x, targetPosition.y, targetPosition.z);
            currentNode = nextNodeInPath;
            pathIndex++;

            if (pathIndex >= currentPath.Count)
            {
                state = MonMovemontState.Idle;
                Debug.Log($"{gameObject.name} reached FINAL destination {currentNode?.NodeName}. Invoking callback.");
                onMovementComplete?.Invoke();
                onMovementComplete = null;
            }
            else
            {
                SetRandomTargetPosition();
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

        foreach (MonMovemont other in selectedCharacters)
        {
            if (other == this || other.state != MonMovemontState.Moving || other.currentNode.Floor != currentNode.Floor)
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
        state = MonMovemontState.InElevator;
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
        state = MonMovemontState.Moving;
        Debug.Log("Arrived at floor " + targetElevator.Floor);
    }
}