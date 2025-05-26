using UnityEngine;
using UnityEngine.UI; // Unity UI 요소를 사용하기 위함 (예: Text)
using System.Collections.Generic;
using System.Linq;
using TMPro; // Sum()과 같은 Linq 확장 메서드 사용

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // --- 게임 상태 ---
    public enum GameState
    {
        Setup,      // 소환수 개수 지정 상태
        Playing,    // 게임 진행 중
        Victory,    // 승리
        Defeat      // 패배
    }
    public GameState CurrentGameState { get; private set; } = GameState.Setup;

    // --- 소환수 설정 ---
    [Header("Summon Prefabs")]
    public GameObject attackerPrefab;
    public GameObject workerPrefab;
    public GameObject outlookPrefab;

    [Header("Summon Settings")]
    public Transform monParentTransform; // "Mon" GameObject의 Transform
    public int maxTotalSummons = 20;
    // UI 또는 다른 시스템에서 이 값들을 설정하게 됩니다. 여기서는 Inspector에서 초기값 설정 가능.
    public int numAttackerToSpawn = 5;
    public int numWorkerToSpawn = 10;
    public int numOutlookToSpawn = 5;

    private List<GameObject> activeSummons = new List<GameObject>();
    private DragSelection DragScrpit;
    private ActionPanelController actionPanelScript;

    // --- 에너지 및 승리 조건 ---
    [Header("Energy & Victory")]
    public float currentTotalEnergy = 0f;
    public float victoryEnergyGoal = 200f;


    // --- UI 참조 ---
    [Header("UI Elements")]
    public TextMeshProUGUI energyTextUI; // 에너지를 표시할 UI Text
    public GameObject setupPanelUI; // 소환수 설정 단계 UI
    public GameObject victoryPanelUI;
    public GameObject defeatPanelUI;

    

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }



        if (monParentTransform == null)
        {
            GameObject monObj = GameObject.Find("Mon");
            if (monObj != null)
            {
                monParentTransform = monObj.transform;
            }
            else
            {
                Debug.LogError("GameManager: 'Mon' parent object not found and not assigned in Inspector.");
            }
        }
    }

    void Start()
    {
        InitializeGame();
    }

    void OnEnable()
    {
        Generator.OnProgressChanged += HandleGeneratorProgressUpdate;
    }

    void OnDisable()
    {
        Generator.OnProgressChanged -= HandleGeneratorProgressUpdate;
    }

    void InitializeGame()
    {
        ChangeGameState(GameState.Setup);

        DragScrpit = gameObject.GetComponent<DragSelection>();
        actionPanelScript = gameObject.GetComponent<ActionPanelController>();

        UpdateTotalEnergy(); // 초기 에너지 계산 및 UI 업데이트
    }

    public void ChangeGameState(GameState newState)
    {
        CurrentGameState = newState;
        Debug.Log($"Game state changed to: {newState}");

        // UI 패널 상태 업데이트
        if (setupPanelUI) setupPanelUI.SetActive(newState == GameState.Setup);
        if (victoryPanelUI) victoryPanelUI.SetActive(newState == GameState.Victory);
        if (defeatPanelUI) defeatPanelUI.SetActive(newState == GameState.Defeat);

        switch (newState)
        {
            case GameState.Setup:
                break;
            case GameState.Playing:
                break;
            case GameState.Victory:
                Time.timeScale = 0f; // 게임 멈춤
                Debug.Log("VICTORY! Energy goal reached.");
                // 추가적인 승리 처리 (예: 다음 레벨 버튼 표시, 점수판 등)
                break;
            case GameState.Defeat:
                Time.timeScale = 0f; // 게임 멈춤
                Debug.Log("DEFEAT! All summons are lost.");
                // 추가적인 패배 처리 (예: 재시작 버튼 표시, 로비로 돌아가기 등)
                break;
        }
    }

    // UI에서 소환수 개수 설정을 마친 후 "게임 시작" 버튼 등에 연결될 메서드
    public void StartGameWithConfiguredSummons(int attackers, int workers, int outlooks)
    {
        if (CurrentGameState != GameState.Setup)
        {
            Debug.LogWarning("Cannot start game, not in Setup state.");
            return;
        }

        if (attackers + workers + outlooks > maxTotalSummons)
        {
            Debug.LogError($"Total summons ({attackers + workers + outlooks}) exceed maximum ({maxTotalSummons}).");
            // 여기에 사용자에게 알리는 UI 로직 추가 가능
            return;
        }
        if (attackers + workers + outlooks == 0)
        {
            Debug.LogWarning("Starting game with 0 summons. This might lead to immediate defeat if not intended.");
        }


        numAttackerToSpawn = attackers;
        numWorkerToSpawn = workers;
        numOutlookToSpawn = outlooks;


        ClearExistingSummons(); // 이전 게임의 소환수가 남아있을 경우 정리
        SpawnAllSummons();

        DragScrpit.enabled = true;
        actionPanelScript.enabled = true;
        
        EnemySpawner enemySpawner;
        enemySpawner = GetComponent<EnemySpawner>();
        enemySpawner.SpawnEnemies();

        ChangeGameState(GameState.Playing);
    }

    // Inspector 값 또는 기본값으로 게임 시작 (테스트용)
    [ContextMenu("Start Game with Default Inspector Counts")]
    public void StartGameWithDefaultCounts()
    {
        StartGameWithConfiguredSummons(numAttackerToSpawn, numWorkerToSpawn, numOutlookToSpawn);
    }


    void SpawnAllSummons()
    {
        if (monParentTransform == null)
        {
            Debug.LogError("Mon Parent Transform is not set. Cannot spawn summons.");
            return;
        }

        SpawnSummonsOfType(attackerPrefab, numAttackerToSpawn, "Attacker");
        SpawnSummonsOfType(workerPrefab, numWorkerToSpawn, "Worker");
        SpawnSummonsOfType(outlookPrefab, numOutlookToSpawn, "Outlook");

        Debug.Log($"Total {activeSummons.Count} summons spawned.");
    }

    void SpawnSummonsOfType(GameObject prefab, int count, string typeName)
    {
        if (prefab == null)
        {
            Debug.LogWarning($"Prefab for {typeName} is not assigned. Skipping spawn.");
            return;
        }

        for (int i = 0; i < count; i++)
        {
            if (activeSummons.Count >= maxTotalSummons)
            {
                Debug.LogWarning($"Reached max total summons ({maxTotalSummons}). Cannot spawn more {typeName}.");
                break;
            }

            GameObject newSummon = Instantiate(prefab, Vector3.zero, Quaternion.identity, monParentTransform);
            activeSummons.Add(newSummon);

        }
    }

    void ClearExistingSummons()
    {
        foreach (GameObject summon in activeSummons)
        {
            if (summon != null) Destroy(summon);
        }
        activeSummons.Clear();
    }

    // 소환수가 죽었을 때 해당 소환수 스크립트에서 호출해야 하는 메서드
    public void ReportSummonDeath(GameObject deadSummon)
    {
        if (activeSummons.Contains(deadSummon))
        {
            activeSummons.Remove(deadSummon);
            Debug.Log($"A summon has died. Remaining summons: {activeSummons.Count}");

            if (activeSummons.Count == 0 && CurrentGameState == GameState.Playing)
            {
                ChangeGameState(GameState.Defeat);
            }
        }
    }

    private void HandleGeneratorProgressUpdate(Generator updatedGenerator)
    {
        UpdateTotalEnergy();
    }

    void UpdateTotalEnergy()
    {
        currentTotalEnergy = 0f;
        foreach (Generator gen in MapManager.Instance.MapGenerators.Where(g => g != null))
        {
            currentTotalEnergy += gen.CurrentProgress;
        }

        if (energyTextUI != null)
        {
            energyTextUI.text = $"Energy: {Mathf.FloorToInt(currentTotalEnergy)} / {victoryEnergyGoal}";
        }

        CheckVictoryCondition();
    }

    void CheckVictoryCondition()
    {
        if (currentTotalEnergy >= victoryEnergyGoal && CurrentGameState == GameState.Playing)
        {
            ChangeGameState(GameState.Victory);
        }
    }


}