// Node.cs
using System.Collections.Generic;

// This class represents the logical data of a node, independent of Unity's MonoBehaviour.
public class Node
{
    public string Name { get; private set; }
    public int Floor { get; private set; }
    public NodeObject.NodeType Type { get; private set; }
    public bool AllowGeneratorSpawn { get; private set; }
    public List<Node> Neighbors { get; set; } // We'll populate this in MapDataManager

    // Reference to the original NodeObject, might be useful for some operations
    public NodeObject OriginalNodeObject { get; private set; }

    public Node(string name, int floor, NodeObject.NodeType type, bool allowGeneratorSpawn, NodeObject originalNodeObject)
    {
        Name = name;
        Floor = floor;
        Type = type;
        AllowGeneratorSpawn = allowGeneratorSpawn;
        OriginalNodeObject = originalNodeObject;
        Neighbors = new List<Node>();
    }

    // Optional: Override ToString for easier debugging
    public override string ToString()
    {
        return $"Node: {Name} (Floor: {Floor}, Type: {Type}, Neighbors: {Neighbors.Count})";
    }
}
