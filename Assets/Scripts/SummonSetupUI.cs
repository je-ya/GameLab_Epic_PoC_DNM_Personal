using UnityEngine;
using UnityEngine.UI; 
using TMPro; 

public class SummonSetupUI : MonoBehaviour
{
    [Header("Attacker UI Elements")]
    public TextMeshProUGUI attackerCountText; // TMP_Text 또는 TextMeshProUGUI로 변경
    public Button increaseAttackerButton;
    public Button decreaseAttackerButton;
    private int attackerCount = 0;

    [Header("Worker UI Elements")]
    public TextMeshProUGUI workerCountText; // TMP_Text 또는 TextMeshProUGUI로 변경
    public Button increaseWorkerButton;
    public Button decreaseWorkerButton;
    private int workerCount = 0;

    [Header("Outlooker UI Elements")]
    public TextMeshProUGUI outlookerCountText; // TMP_Text 또는 TextMeshProUGUI로 변경
    public Button increaseOutlookerButton;
    public Button decreaseOutlookerButton;
    private int outlookerCount = 0;

    [Header("Total & Start UI")]
    public TextMeshProUGUI totalCountText; // TMP_Text 또는 TextMeshProUGUI로 변경
    public Button startButton;

    private int maxTotalSummonsAllowed;
    private readonly int maxIndividualCount = 999;
    private readonly int minIndividualCount = 0;

    void Start()
    {
        if (GameManager.Instance != null)
        {
            maxTotalSummonsAllowed = GameManager.Instance.maxTotalSummons;
        }
        else
        {
            Debug.LogError("SummonSetupUI: GameManager instance not found! Max total summons will default to 20.");
            maxTotalSummonsAllowed = 20;
        }

        // 버튼 리스너 연결
        increaseAttackerButton.onClick.AddListener(() => AdjustCount(ref attackerCount, 1, attackerCountText));
        decreaseAttackerButton.onClick.AddListener(() => AdjustCount(ref attackerCount, -1, attackerCountText));

        increaseWorkerButton.onClick.AddListener(() => AdjustCount(ref workerCount, 1, workerCountText));
        decreaseWorkerButton.onClick.AddListener(() => AdjustCount(ref workerCount, -1, workerCountText));

        increaseOutlookerButton.onClick.AddListener(() => AdjustCount(ref outlookerCount, 1, outlookerCountText));
        decreaseOutlookerButton.onClick.AddListener(() => AdjustCount(ref outlookerCount, -1, outlookerCountText));

        startButton.onClick.AddListener(OnStartButtonPressed);

        attackerCount = 0;
        workerCount = 0;
        outlookerCount = 0;

        UpdateAllCountTexts();
        UpdateTotalAndStartButtonState();
    }

    void OnEnable()
    {
        // 필요하다면 활성화 시 UI 상태 리셋
        // ResetUIState(); // 주석 해제하여 사용 가능
    }

    void AdjustCount(ref int countVariable, int amount, TextMeshProUGUI countTextElement) // 파라미터 타입 변경
    {
        int currentTotal = attackerCount + workerCount + outlookerCount;

        // 현재 총합이 최대치를 넘었고, 더 늘리려고 할 때
        if (amount > 0 && currentTotal >= maxTotalSummonsAllowed && (currentTotal + amount > maxTotalSummonsAllowed))
        {
            // 이미 빨간색인 상태에서 더 늘리려는 것을 막으려면 (선택적)
            // if (currentTotal >= maxTotalSummonsAllowed) return;
        }




        int newCount = countVariable + amount;
        countVariable = Mathf.Clamp(newCount, minIndividualCount, maxIndividualCount);

        if (countTextElement != null)
        {
            countTextElement.text = countVariable.ToString();
        }

        UpdateTotalAndStartButtonState();
    }

    void UpdateAllCountTexts()
    {
        if (attackerCountText != null) attackerCountText.text = attackerCount.ToString();
        if (workerCountText != null) workerCountText.text = workerCount.ToString();
        if (outlookerCountText != null) outlookerCountText.text = outlookerCount.ToString();
    }

    void UpdateTotalAndStartButtonState()
    {
        int totalSummons = attackerCount + workerCount + outlookerCount;

        if (totalCountText != null)
        {
            totalCountText.text = $"Total : {totalSummons}";
        }

        if (startButton != null)
        {
            if (totalSummons > maxTotalSummonsAllowed)
            {
                if (totalCountText != null) totalCountText.color = Color.red;
                startButton.interactable = false;
            }
            else
            {
                if (totalCountText != null) totalCountText.color = Color.white; // 기본 색상으로 변경
                startButton.interactable = (totalSummons > 0); // 총합이 0이면 시작 버튼 비활성화 (선택 사항)
            }
        }
    }

    void OnStartButtonPressed()
    {
        if (GameManager.Instance != null)
        {
            int currentTotal = attackerCount + workerCount + outlookerCount;
            if (currentTotal <= maxTotalSummonsAllowed)
            {
                if (currentTotal == 0 && !startButton.interactable) // 이미 버튼이 비활성화된 상태 (0마리일 때)
                {
                    Debug.LogWarning("Attempting to start with 0 summons, but start button is disabled.");
                    return; // 시작 안 함
                }
                if (currentTotal == 0)
                {
                    Debug.LogWarning("Attempting to start with 0 summons. Make sure this is intended.");
                }

                GameManager.Instance.StartGameWithConfiguredSummons(attackerCount, workerCount, outlookerCount);
            }
            else
            {
                Debug.LogWarning("Cannot start game. Total summons exceed the maximum allowed. This should have been prevented by UI.");
            }
        }
        else
        {
            Debug.LogError("SummonSetupUI: GameManager instance not found! Cannot start game.");
        }
    }

    public void ResetUIState()
    {
        attackerCount = 0;
        workerCount = 0;
        outlookerCount = 0;
        UpdateAllCountTexts();
        UpdateTotalAndStartButtonState();
    }
}