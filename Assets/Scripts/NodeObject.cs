// NodeObject.cs
using UnityEngine;
using System.Collections.Generic;

public class NodeObject : MonoBehaviour
{
    public enum NodeType { Room, Hallway, Elevator }
    public string NodeName; 
    public int Floor;
    public NodeType Type;
    public bool AllowGeneratorSpawn = true;
    public List<NodeObject> Neighbors = new List<NodeObject>(); 

    void Awake() 
    {
        if (Type != NodeType.Room)
        {
            AllowGeneratorSpawn = false;
        }
    }

    public Node GetNodeData()
    {
        return new Node(transform.position, NodeName, Floor, Type, AllowGeneratorSpawn);
    }

}