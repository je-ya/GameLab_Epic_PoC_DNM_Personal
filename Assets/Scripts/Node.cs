using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class Node
{
    public Vector3 Position;
    public string NodeName { get; private set; }
    public int Floor { get; private set; }
    public NodeObject.NodeType Type { get; private set; }
    public bool AllowGeneratorSpawn { get; private set; }
    public List<Node> Neighbors { get; private set; }

    public Node(Vector3 position, string nodeName, int floor, NodeObject.NodeType type, bool allowGeneratorSpawn)
    {
        Position = position;
        NodeName = nodeName;
        Floor = floor;
        Type = type;
        AllowGeneratorSpawn = allowGeneratorSpawn;
        Neighbors = new List<Node>();
    }
}

