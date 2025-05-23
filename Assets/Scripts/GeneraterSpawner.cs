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

        foreach (NodeObject node in selectedNodes)
        {
            GameObject generatorObj = Instantiate(generatorPrefab, node.transform.position, Quaternion.identity);
            generatorObj.name = $"Generator_{node.NodeName}";
            Generator generator = generatorObj.GetComponent<Generator>();

            if (generator == null)
            {
                Debug.LogError($"Generator component not found on instantiated prefab at {node.NodeName}");
                Destroy(generatorObj);
                continue;
            }

            spawnedGenerators.Add(generator); 

            // --- 생성된 Generator를 GameManager에 등록 ---
            if (GameManager.Instance != null)
            {
                GameManager.Instance.RegisterGenerator(generator);
            }
            else
            {
                Debug.LogError($"GeneratorSpawner: GameManager.Instance is null. Cannot register generator '{generator.name}'. Make sure GameManager exists and is initialized.");
            }
            // ---------------------------------------------

            Debug.Log($"Spawned generator at {node.NodeName} (Position: {node.transform.position})");
        }
    }

    public List<Generator> GetSpawnedGenerators()
    {
        return spawnedGenerators;
    }
}