// Generator.cs
using UnityEngine;
using System; // 이벤트 사용을 위해 추가

public class Generator : MonoBehaviour
{
    // static 이벤트를 사용하여 어떤 Generator 인스턴스에서든 변경이 발생하면 GameManager가 알 수 있도록 함
    public static event Action<Generator> OnProgressChanged;

    private float currentProgress = 0f;
    private const float maxProgress = 100f; // 각 Generator의 최대치는 100으로 가정

    public float CurrentProgress
    {
        get { return currentProgress; }
    }

    public float MaxProgress // 외부에서 최대 진행도를 알 수 있도록 프로퍼티 추가
    {
        get { return maxProgress; }
    }

    public void AddProgress(float amount)
    {
        if (IsFullyRepaired() || amount <= 0) return; // 이미 다 찼거나, 증가량이 없으면 반환

        float oldProgress = currentProgress;
        currentProgress = Mathf.Clamp(currentProgress + amount, 0f, maxProgress);

        if (currentProgress != oldProgress) // 실제로 진행도가 변경되었을 때만 이벤트 발생
        {
            OnProgressChanged?.Invoke(this); // 변경된 Generator 인스턴스를 전달
        }

        if (IsFullyRepaired())
        {
            Debug.Log($"{gameObject.name} is fully repaired!");
        }
    }

    public bool IsFullyRepaired()
    {
        return currentProgress >= maxProgress;
    }

    public void ResetProgress()
    {
        float oldProgress = currentProgress;
        currentProgress = 0f;

        if (currentProgress != oldProgress)
        {
            OnProgressChanged?.Invoke(this);
        }
        Debug.Log($"{gameObject.name} progress reset to 0.");
    }
}