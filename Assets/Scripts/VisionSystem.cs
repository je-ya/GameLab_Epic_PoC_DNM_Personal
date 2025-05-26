using UnityEngine;

public class VisionSystem : MonoBehaviour
{
    public float sightRadius = 5f; // 월드 공간 기준 시야 반경
    public LayerMask visionLayer; // Vision 레이어

    void Update()
    {
        // 모든 Vision 레이어 오브젝트의 렌더러 비활성화
        SpriteRenderer[] renderers = FindObjectsOfType<SpriteRenderer>();
        foreach (var renderer in renderers)
        {
            if (visionLayer == (visionLayer | (1 << renderer.gameObject.layer)))
            {
                renderer.enabled = false;
            }
        }

        // 시야 내 Vision 레이어 오브젝트만 활성화 (3D Collider 사용)
        Collider[] enemies = Physics.OverlapSphere(transform.position, sightRadius, visionLayer);
        foreach (var enemy in enemies)
        {
            SpriteRenderer renderer = enemy.GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                renderer.enabled = true;
            }
        }

        // 디버깅: 감지된 오브젝트 정보 출력
        Debug.Log($"Enemies in sight: {enemies.Length}");
        foreach (var enemy in enemies)
        {
            Debug.Log($"Detected: {enemy.gameObject.name}, Position: {enemy.transform.position}");
        }

        // 디버깅: 시야 범위 시각화 (씬 뷰에서 원형 범위 확인)
        Debug.DrawRay(transform.position, Vector2.right * sightRadius, Color.green, 0.1f);
        Debug.DrawRay(transform.position, Vector2.left * sightRadius, Color.green, 0.1f);
        Debug.DrawRay(transform.position, Vector2.up * sightRadius, Color.green, 0.1f);
        Debug.DrawRay(transform.position, Vector2.down * sightRadius, Color.green, 0.1f);
    }
}