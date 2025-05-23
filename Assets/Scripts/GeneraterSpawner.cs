// GeneratorSpawner.cs (일부 수정)
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

        // GameManager가 먼저 초기화될 수 있도록 약간의 지연을 주거나,
        // 스크립트 실행 순서를 GameManager가 더 먼저 실행되도록 설정하는 것이 좋습니다.
        // 여기서는 Start에서 바로 호출합니다.
        SpawnGenerators();
    }

    private void SpawnGenerators()
    {
        NodeObject[] allNodes = FindObjectsOfType<NodeObject>();
        List<NodeObject> roomNodes = allNodes
            .Where(node => node.Type == NodeObject.NodeType.Room && node.AllowGeneratorSpawn)
            .ToList();

        if (roomNodes.Count == 0) // 수정: 0개일 때도 오류를 명확히
        {
            Debug.LogError($"No eligible room nodes found to spawn generators. Check AllowGeneratorSpawn settings on NodeObjects.");
            return;
        }

        if (roomNodes.Count < numberOfGenerators)
        {
            Debug.LogWarning($"Not enough eligible room nodes ({roomNodes.Count}) to spawn {numberOfGenerators} generators. Spawning {roomNodes.Count} instead.");
            numberOfGenerators = roomNodes.Count; // 가능한 만큼만 스폰하도록 수정
        }

        List<NodeObject> selectedNodes = roomNodes.OrderBy(x => Random.value).Take(numberOfGenerators).ToList();

        foreach (NodeObject node in selectedNodes)
        {
            GameObject generatorObj = Instantiate(generatorPrefab, node.transform.position, Quaternion.identity);
            generatorObj.name = $"Generator_{node.NodeName}"; // 이름 생성 규칙 유지
            Generator generator = generatorObj.GetComponent<Generator>();

            if (generator == null)
            {
                Debug.LogError($"Generator component not found on instantiated prefab at {node.NodeName}");
                Destroy(generatorObj);
                continue;
            }

            spawnedGenerators.Add(generator); // GeneratorSpawner 내부 목록에도 추가

            // --- 중요: 생성된 Generator를 GameManager에 등록 ---
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