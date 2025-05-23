// NodeObject.cs
using UnityEngine;
using System.Collections.Generic;

public class NodeObject : MonoBehaviour
{
    public enum NodeType { Room, Hallway, Elevator }
    public string NodeName; // Keep this distinct, as Node class will have 'Name'
    public int Floor;
    public NodeType Type;
    public bool AllowGeneratorSpawn = true;
    public List<NodeObject> Neighbors = new List<NodeObject>(); // Initialized

    private void Awake() // Changed from Start to ensure AllowGeneratorSpawn is set before MapDataManager might access it
    {
        if (Type != NodeType.Room)
        {
            AllowGeneratorSpawn = false;
        }
    }

    /// <summary>
    /// Creates a logical Node data object from this NodeObject.
    /// </summary>
    /// <returns>A new Node instance.</returns>
    public Node GetNodeData()
    {
        // The AllowGeneratorSpawn value will be correctly set by Awake() before this is called.
        return new Node(string.IsNullOrEmpty(NodeName) ? gameObject.name : NodeName, Floor, Type, AllowGeneratorSpawn, this);
    }
}