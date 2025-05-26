using UnityEngine;

public class EDamager : MonoBehaviour
{
    private float damage = 2f;
    private float lifetime = 0.5f;

    void OnEnable()
    {
        // 오브젝트가 활성화될 때마다 0.5초 후 비활성화
        Invoke(nameof(Deactivate), lifetime);
    }

    void OnTriggerEnter(Collider other)
    {
        // "PlayerMon" 태그를 가진 오브젝트 확인
        if (other.CompareTag("PlayerMon")||other.CompareTag("Stealth"))
        {
            Debug.Log($"Triggered with: {other.gameObject.name}, Tag: {other.gameObject.tag}, Position: {other.transform.position}");
            // MonAction 컴포넌트 가져오기
            MonAction mon = other.GetComponent<MonAction>();
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