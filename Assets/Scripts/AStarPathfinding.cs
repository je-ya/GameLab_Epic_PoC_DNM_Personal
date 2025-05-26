using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class AStarPathfinding
{
    public static List<Node> FindPath(Node startNode, Node targetNode)
    {
        var openSet = new List<Node> { startNode };
        var closedSet = new HashSet<Node>();
        var cameFrom = new Dictionary<Node, Node>();
        var gScore = new Dictionary<Node, float> { [startNode] = 0 };
        var fScore = new Dictionary<Node, float> { [startNode] = Heuristic(startNode, targetNode) };

        while (openSet.Count > 0)
        {
            Node current = openSet.OrderBy(n => fScore.ContainsKey(n) ? fScore[n] : float.MaxValue).First();

            if (current == targetNode)
            {
                return ReconstructPath(cameFrom, current);
            }

            openSet.Remove(current);
            closedSet.Add(current);

            foreach (Node neighbor in current.Neighbors)
            {
                if (closedSet.Contains(neighbor)) continue;

                float tentativeGScore = gScore[current] + Vector3.Distance(current.Position, neighbor.Position);

                if (!openSet.Contains(neighbor))
                {
                    openSet.Add(neighbor);
                }
                else if (tentativeGScore >= (gScore.ContainsKey(neighbor) ? gScore[neighbor] : float.MaxValue))
                {
                    continue;
                }

                cameFrom[neighbor] = current;
                gScore[neighbor] = tentativeGScore;
                fScore[neighbor] = gScore[neighbor] + Heuristic(neighbor, targetNode);
            }
        }

        return null; // 경로를 찾지 못함
    }

    private static float Heuristic(Node a, Node b)
    {
        return Vector3.Distance(a.Position, b.Position);
    }

    private static List<Node> ReconstructPath(Dictionary<Node, Node> cameFrom, Node current)
    {
        List<Node> path = new List<Node> { current };
        while (cameFrom.ContainsKey(current))
        {
            current = cameFrom[current];
            path.Add(current);
        }
        path.Reverse();
        return path;
    }
}