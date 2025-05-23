// MapDataManager.cs
using UnityEngine;
using System.Collections.Generic;
using System.Linq; // For FindObjectsOfType

public class MapDataManager : MonoBehaviour
{
    public static MapDataManager Instance { get; private set; }

    // The processed map data
    public List<Node> AllNodes { get; private set; }
    public Dictionary<int, List<Node>> NodesByFloor { get; private set; }

    // Optional: Flag to initialize on Awake, or you can call InitializeMap() manually
    [SerializeField]
    private bool initializeOnAwake = true;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // DontDestroyOnLoad(gameObject); // Uncomment if you need this manager to persist across scene loads
        }
        else
        {
            Debug.LogWarning("Multiple MapDataManager instances detected. Destroying this one.");
            Destroy(gameObject);
            return;
        }

        AllNodes = new List<Node>();
        NodesByFloor = new Dictionary<int, List<Node>>();

        if (initializeOnAwake)
        {
            InitializeMapFromScene();
        }
    }

    public void InitializeMapFromScene()
    {
        AllNodes.Clear();
        NodesByFloor.Clear();

        NodeObject[] nodeObjects = FindObjectsOfType<NodeObject>();
        if (nodeObjects.Length == 0)
        {
            Debug.LogWarning("No NodeObjects found in the scene to initialize map.");
            return;
        }

        Dictionary<NodeObject, Node> nodeObjectToNodeMap = new Dictionary<NodeObject, Node>();

        // First pass: Create all Node data objects and populate basic lists
        foreach (NodeObject nodeObj in nodeObjects)
        {
            Node node = nodeObj.GetNodeData(); // Use the new method
            AllNodes.Add(node);
            nodeObjectToNodeMap[nodeObj] = node;

            if (!NodesByFloor.ContainsKey(node.Floor))
            {
                NodesByFloor[node.Floor] = new List<Node>();
            }
            NodesByFloor[node.Floor].Add(node);
        }

        // Second pass: Connect neighbors
        foreach (NodeObject nodeObj in nodeObjects)
        {
            // It's guaranteed that nodeObj is in nodeObjectToNodeMap from the first pass
            Node currentNode = nodeObjectToNodeMap[nodeObj];

            foreach (NodeObject neighborNodeObject in nodeObj.Neighbors)
            {
                if (neighborNodeObject == null)
                {
                    Debug.LogWarning($"NodeObject '{nodeObj.name}' has a null neighbor reference in its list.", nodeObj);
                    continue;
                }

                if (nodeObjectToNodeMap.TryGetValue(neighborNodeObject, out Node neighborNode))
                {
                    // Check if the neighbor relationship is not already added (to prevent duplicates if bi-directional linking happens in Node)
                    if (!currentNode.Neighbors.Contains(neighborNode))
                    {
                        currentNode.Neighbors.Add(neighborNode);
                    }
                }
                else
                {
                    // This case should ideally not happen if all NodeObjects are processed correctly.
                    // It means a NodeObject was assigned as a neighbor in the Inspector,
                    // but that neighbor NodeObject itself wasn't found or processed.
                    Debug.LogWarning($"Neighbor NodeObject '{neighborNodeObject.name}' for '{nodeObj.name}' not found in the map. " +
                                     "Ensure all linked NodeObjects are active and part of the scene.", neighborNodeObject);
                }
            }
        }

        Debug.Log($"MapDataManager: Initialized {AllNodes.Count} nodes across {NodesByFloor.Count} floors.");

        // Optional: You can print more details for verification
        // foreach(var floorEntry in NodesByFloor)
        // {
        //     Debug.Log($"Floor {floorEntry.Key}: {floorEntry.Value.Count} nodes.");
        //     foreach(Node node in floorEntry.Value)
        //     {
        //         Debug.Log($"  - {node.Name} (Type: {node.Type}), Neighbors: {node.Neighbors.Count}");
        //     }
        // }
    }

    // Example: How to get a specific node
    public Node GetNodeByName(string name)
    {
        return AllNodes.FirstOrDefault(node => node.Name == name);
    }

    // Example: How to get nodes of a specific type on a specific floor
    public List<Node> GetNodesByTypeAndFloor(NodeObject.NodeType type, int floor)
    {
        if (NodesByFloor.TryGetValue(floor, out List<Node> floorNodes))
        {
            return floorNodes.Where(node => node.Type == type).ToList();
        }
        return new List<Node>(); // Return empty list if floor doesn't exist
    }
}