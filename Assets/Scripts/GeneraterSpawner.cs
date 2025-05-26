using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class GeneratorSpawner : MonoBehaviour
{
    [SerializeField] private int numberOfGenerators = 3;
    [SerializeField] private GameObject generatorPrefab;
    private List<Generator> spawnedGenerators = new List<Generator>();

    void Start()
    {
        if (generatorPrefab == null)
        {
            Debug.LogError("Generator Prefab is not assigned in the Inspector!");
            return;
        }

        SpawnGenerators();
    }

    private void SpawnGenerators()
    {
        NodeObject[] allNodes = FindObjectsByType<NodeObject>(FindObjectsSortMode.None);
        List<NodeObject> roomNodes = allNodes
            .Where(node => node.Type == NodeObject.NodeType.Room && node.AllowGeneratorSpawn)
            .ToList();

        if (roomNodes.Count == 0)
        {
            Debug.LogError($"No eligible room nodes found to spawn generators. Check AllowGeneratorSpawn settings on NodeObjects.");
            return;
        }

        if (roomNodes.Count < numberOfGenerators)
        {
            Debug.LogWarning($"Not enough eligible room nodes ({roomNodes.Count}) to spawn {numberOfGenerators} generators. Spawning {roomNodes.Count} instead.");
            numberOfGenerators = roomNodes.Count;
        }

        List<NodeObject> selectedNodes = roomNodes.OrderBy(x => Random.value).Take(numberOfGenerators).ToList();

        foreach (NodeObject nodeObj in selectedNodes)
        {
            GameObject generatorObj = Instantiate(generatorPrefab, nodeObj.transform.position, Quaternion.identity);
            generatorObj.name = $"Generator_{nodeObj.NodeName}";
            Generator generator = generatorObj.GetComponent<Generator>();

            if (generator == null)
            {
                Debug.LogError($"Generator component not found on instantiated prefab at {nodeObj.NodeName}");
                Destroy(generatorObj);
                continue;
            }

            spawnedGenerators.Add(generator);

            // --- 생성된 Generator를 MapManager에 등록 ---
            if (MapManager.Instance != null)
            {
                Node node = MapManager.Instance.GetNodeByName(nodeObj.NodeName);
                if (node != null)
                {
                    MapManager.Instance.RegisterGenerator(generator, node);
                }
                else
                {
                    Debug.LogError($"GeneratorSpawner: Node '{nodeObj.NodeName}' not found in MapManager for generator '{generator.name}'.");
                }
            }
            else
            {
                Debug.LogError($"GeneratorSpawner: MapManager.Instance is null. Cannot register generator '{generator.name}'.");
            }
            // ---------------------------------------------

            Debug.Log($"Spawned generator at {nodeObj.NodeName} (Position: {nodeObj.transform.position})");
        }
    }

    public List<Generator> GetSpawnedGenerators()
    {
        return spawnedGenerators;
    }
}