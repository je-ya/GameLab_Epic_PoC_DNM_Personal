// EnemySpawner.cs
using UnityEngine;
using System.Collections.Generic;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private GameObject enemyPrefab; // 스폰할 적 프리팹
    [SerializeField] private List<NodeObject> spawnNodes; // 스폰 위치로 사용할 NodeObject 리스트
    [SerializeField] private int spawnCountPerNode = 1; // 각 노드에서 스폰할 적의 수
    [SerializeField] private Transform enemyParent; // 적이 자식으로 추가될 부모 Transform (Enemy 오브젝트)

    private void Start()
    {
        if (enemyPrefab == null)
        {
            Debug.LogError("EnemySpawner: Enemy 프리팹이 할당되지 않았습니다.");
            return;
        }
    }

    public void SpawnEnemies()
    {
        foreach (NodeObject nodeObj in spawnNodes)
        {
            if (nodeObj == null)
            {
                Debug.LogWarning("EnemySpawner: 스폰 노드가 null입니다.");
                continue;
            }

            for (int i = 0; i < spawnCountPerNode; i++)
            {
                // NodeObject의 transform.position에서 적 스폰
                Vector3 spawnPosition = nodeObj.transform.position;
                GameObject enemyObj = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity, enemyParent);
                Debug.Log($"EnemySpawner: '{nodeObj.NodeName}' 위치에 적 스폰됨.");
            }
        }

        Debug.Log($"EnemySpawner: 총 {spawnNodes.Count * spawnCountPerNode}개의 적을 스폰했습니다.");
    }
}
