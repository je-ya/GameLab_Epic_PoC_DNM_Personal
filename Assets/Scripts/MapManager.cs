using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class MapManager : MonoBehaviour
{
    public static MapManager Instance { get; private set; }

    // 모든 노드를 저장하는 리스트
    private List<Node> allNodes = new List<Node>();
    // 층별로 노드를 그룹화하여 저장하는 딕셔너리
    private Dictionary<int, List<Node>> nodesByFloor = new Dictionary<int, List<Node>>();
    // Generator 리스트
    private List<Generator> mapGenerators = new List<Generator>();
    // Generator가 있는 Node 매핑
    private Dictionary<Node, Generator> nodeToGeneratorMap = new Dictionary<Node, Generator>();


    // 외부에서 모든 노드에 접근할 수 있도록 프로퍼티 제공 (읽기 전용)
    public IReadOnlyList<Node> AllNodes => allNodes.AsReadOnly();
    // 외부에서 층별 노드에 접근할 수 있도록 프로퍼티 제공 (읽기 전용)
    public IReadOnlyDictionary<int, IReadOnlyList<Node>> NodesByFloor =>
        nodesByFloor.ToDictionary(kvp => kvp.Key, kvp => (IReadOnlyList<Node>)kvp.Value.AsReadOnly());
    // Generator 리스트에 접근할 수 있는 프로퍼티
    public IReadOnlyList<Generator> MapGenerators => mapGenerators.AsReadOnly();
    // Node에서 Generator를 찾을 수 있는 메서드
    public Generator GetGeneratorAtNode(Node node)
    {
        nodeToGeneratorMap.TryGetValue(node, out Generator generator);
        return generator;
    }
    // Generator가 위치한 Node를 찾는 메서드
    public Node GetNodeForGenerator(Generator generator)
    {
        return nodeToGeneratorMap.FirstOrDefault(kvp => kvp.Value == generator).Key;
    }
    // Generator가 있는 Node 리스트 반환
    public IReadOnlyList<Node> GetNodesWithGenerators()
    {
        return nodeToGeneratorMap.Keys.ToList().AsReadOnly();
    }


    private Node currentNode;




    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            InitializeMapFromScene();
        }
        else
        {
            Destroy(gameObject);
        }
    }


    private void InitializeMapFromScene()
    {
        allNodes.Clear();
        nodesByFloor.Clear();
        mapGenerators.Clear();
        nodeToGeneratorMap.Clear(); 

        NodeObject[] nodeObjects = FindObjectsByType<NodeObject>(FindObjectsSortMode.None);
        if (nodeObjects.Length == 0)
        {
            Debug.LogWarning("MapManager: 씬에서 NodeObject를 찾을 수 없습니다.");
        }

        Dictionary<NodeObject, Node> nodeObjectToNodeMap = new Dictionary<NodeObject, Node>();

        // 모든 NodeObject를 Node로 변환하고 기본 목록 및 층별 목록에 추가
        foreach (NodeObject nodeObj in nodeObjects)
        {
            Node node = nodeObj.GetNodeData();
            allNodes.Add(node);
            nodeObjectToNodeMap[nodeObj] = node;

            if (!nodesByFloor.ContainsKey(node.Floor))
            {
                nodesByFloor[node.Floor] = new List<Node>();
            }
            nodesByFloor[node.Floor].Add(node);
        }

        // Node들 간의 이웃 관계 설정
        foreach (NodeObject nodeObj in nodeObjects)
        {
            if (!nodeObjectToNodeMap.TryGetValue(nodeObj, out Node node))
            {
                Debug.LogError($"MapManager: nodeObjectToNodeMap에 {nodeObj.NodeName}이 없습니다. 초기화 오류일 수 있습니다.");
                continue;
            }

            foreach (NodeObject neighborObj in nodeObj.Neighbors)
            {
                if (neighborObj == null)
                {
                    Debug.LogWarning($"MapManager: {nodeObj.NodeName}의 이웃 중 하나가 null입니다. Inspector에서 할당이 누락되었을 수 있습니다.");
                    continue;
                }

                if (nodeObjectToNodeMap.TryGetValue(neighborObj, out Node neighborNode))
                {
                    if (!node.Neighbors.Contains(neighborNode)) // 중복 추가 방지
                    {
                        node.Neighbors.Add(neighborNode);
                    }
                }
                else
                {
                    Debug.LogWarning($"MapManager: {nodeObj.NodeName}의 이웃 {neighborObj.NodeName}을(를) 맵에서 찾을 수 없습니다. 이웃 NodeObject가 씬에 없거나 비활성화 상태일 수 있습니다.");
                }
            }
        }

        Generator[] preExistingGenerators = FindObjectsByType<Generator>(FindObjectsSortMode.None);
        foreach (Generator gen in preExistingGenerators)
        {
            if (!mapGenerators.Contains(gen))
            {
                mapGenerators.Add(gen);
                Debug.Log($"MapManager: Pre-existing generator '{gen.name}' registered, but Node mapping is unknown.");
            }
        }



        Debug.Log($"MapManager: {allNodes.Count}개의 노드를 초기화했고, {nodesByFloor.Count}개의 층으로 구성되었습니다.");
    }

    /// <summary>
    /// 특정 이름의 노드를 찾습니다.
    /// </summary>
    /// <param name="nodeName">찾을 노드의 이름</param>
    /// <returns>해당 이름의 노드. 없으면 null을 반환합니다.</returns>
    public Node GetNodeByName(string nodeName)
    {
        return allNodes.FirstOrDefault(node => node.NodeName == nodeName);
    }

    /// <summary>
    /// 특정 층에 있는 모든 노드를 반환합니다.
    /// </summary>
    /// <param name="floor">찾을 층 번호</param>
    /// <returns>해당 층의 노드 리스트. 해당 층에 노드가 없으면 빈 리스트를 반환합니다.</returns>
    public IReadOnlyList<Node> GetNodesOnFloor(int floor)
    {
        if (nodesByFloor.TryGetValue(floor, out List<Node> floorNodes))
        {
            return floorNodes.AsReadOnly();
        }
        return new List<Node>().AsReadOnly();
    }

    /// <summary>
    /// SpawnRoom 타입의 노드를 찾아 그 Node를 반환
    /// </summary>
    /// <returns>SpawnRoom 노드가 없으면 null를 반환합니다.</returns>
    public Node GetSpawnRoom()
    {
        Node spawnNode = allNodes.FirstOrDefault(node => node.Type == NodeObject.NodeType.SpawnRoom);
        if (spawnNode != null)
        {
            return spawnNode;
        }
        Debug.LogWarning("MapManager: SpawnRoom 노드를 찾을 수 없습니다. 기본 위치(Vector3.zero)를 반환합니다.");
        return null;
    }


    public Node GetCurrentNode()
    {
        return currentNode;
    }

    public void RegisterGenerator(Generator newGenerator, Node node)
    {
        if (newGenerator == null)
        {
            Debug.LogWarning("MapManager: Attempted to register a null Generator.");
            return;
        }

        if (!mapGenerators.Contains(newGenerator))
        {
            mapGenerators.Add(newGenerator);
            if (node != null && allNodes.Contains(node))
            {
                nodeToGeneratorMap[node] = newGenerator;
                Debug.Log($"MapManager: Generator '{newGenerator.name}' registered at Node '{node.NodeName}'. Total generators: {mapGenerators.Count}");
            }
            else
            {
                Debug.LogWarning($"MapManager: Node for Generator '{newGenerator.name}' is invalid or not found in allNodes.");
            }
        }
        else
        {
            Debug.LogWarning($"MapManager: Generator '{newGenerator.name}' is already registered.");
        }
    }

    public void UnregisterGenerator(Generator generator)
    {
        if (mapGenerators.Remove(generator))
        {
            var nodeEntry = nodeToGeneratorMap.FirstOrDefault(kvp => kvp.Value == generator);
            if (nodeEntry.Key != null)
            {
                nodeToGeneratorMap.Remove(nodeEntry.Key);
            }
            Debug.Log($"MapManager: Generator '{generator.name}' unregistered. Total generators: {mapGenerators.Count}");
        }
    }

}