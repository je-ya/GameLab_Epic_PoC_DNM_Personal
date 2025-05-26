using UnityEngine;

public class PDamager : MonoBehaviour
{
    private float damage = 2f;
    private float lifetime = 0.5f;

    void OnEnable()
    {
        Invoke(nameof(Deactivate), lifetime);
    }

    void OnTriggerEnter(Collider other)
    {
        // "PlayerMon" 태그를 가진 오브젝트 확인
        if (other.CompareTag("Enemy"))
        {
            Debug.Log($"Triggered with: {other.gameObject.name}, Tag: {other.gameObject.tag}, Position: {other.transform.position}");
            // MonAction 컴포넌트 가져오기
            Enemy mon = other.GetComponent<Enemy>();
            if (mon != null)
            {
                mon.TakeDamage(damage);
                Debug.Log($"{gameObject.name} dealt {damage} damage to {other.gameObject.name}");
            }
        }
    }

    private void Deactivate()
    {
        // 오브젝트 풀링을 위해 비활성화
        gameObject.SetActive(false);
    }

    void OnDisable()
    {
        // 비활성화 시 Invoke 취소 (안전 장치)
        CancelInvoke();
    }
}