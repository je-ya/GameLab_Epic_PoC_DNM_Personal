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
    private List<Generator> mapGenerators = new List<Generator>();
    public List<Generator> GetGenerators()
    {
        return mapGenerators;
    }

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

        // mapGenerators 리스트 초기화 (중복 방지 및 명확한 시작)
        mapGenerators = new List<Generator>();

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

        // 씬에 미리 배치된 Generator들을 먼저 찾아서 등록 (선택 사항)
        // 만약 모든 Generator가 GeneratorSpawner를 통해서만 생성된다면 이 부분은 생략 가능
        Generator[] preExistingGenerators = FindObjectsByType<Generator>(FindObjectsSortMode.None);
        foreach (Generator gen in preExistingGenerators)
        {
            if (!mapGenerators.Contains(gen)) // 중복 등록 방지
            {
                mapGenerators.Add(gen);
                Debug.Log($"Pre-existing generator '{gen.name}' registered.");
            }
        }

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
                // Time.timeScale = 0f; // 설정 중에는 게임을 멈출 수 있음 (선택)
                // UI에서 소환수 개수 입력을 받는 로직 활성화
                // 이 예제에서는 StartGameWithConfiguredSummons()를 외부(예: UI 버튼)에서 호출한다고 가정
                break;
            case GameState.Playing:
                // Time.timeScale = 1f; // 게임 시간 정상화
                // 설정된 소환수 스폰 로직은 StartGameWithConfiguredSummons에서 처리
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

            // TODO: 소환 위치를 좀 더 정교하게 설정 (예: 플레이어 주변, 특정 스폰 포인트들)
            // 현재는 Mon 오브젝트 위치 근처에 랜덤하게 스폰
            Vector3 spawnPos = monParentTransform.position + Random.insideUnitSphere * 2f; // 예시 위치
            spawnPos.y = monParentTransform.position.y; // Y축은 부모와 같게 (지형 고려 필요 시 수정)

            GameObject newSummon = Instantiate(prefab, spawnPos, Quaternion.identity, monParentTransform);
            // newSummon.tag = "PlayerMon"; // 프리팹에 이미 태그가 설정되어 있다고 가정했으므로 주석 처리.
            // 만약 프리팹에 없다면 여기서 설정.
            activeSummons.Add(newSummon);

            // 각 소환수 스크립트에서 죽음 이벤트를 GameManager에 알려야 합니다.
            // 예시: newSummon.GetComponent<SummonHealth>().OnDeath += HandleSummonDeath;
            // 또는 소환수 스크립트의 Die() 메서드에서 GameManager.Instance.ReportSummonDeath(this.gameObject); 호출
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

    public void RegisterGenerator(Generator newGenerator)
    {
        if (newGenerator == null)
        {
            Debug.LogWarning("GameManager: Attempted to register a null Generator.");
            return;
        }

        if (!mapGenerators.Contains(newGenerator))
        {
            mapGenerators.Add(newGenerator);
            Debug.Log($"GameManager: Generator '{newGenerator.name}' registered successfully. Total generators: {mapGenerators.Count}");
            UpdateTotalEnergy(); // 새 제너레이터가 등록되었으니 에너지 총합 업데이트
        }
        else
        {
            Debug.LogWarning($"GameManager: Generator '{newGenerator.name}' is already registered.");
        }
    }

    private void HandleGeneratorProgressUpdate(Generator updatedGenerator)
    {
        // 어떤 Generator가 업데이트되었는지 인자로 받지만, 현재는 전체를 다시 계산
        // 만약 최적화가 필요하다면 updatedGenerator 정보 활용 가능
        UpdateTotalEnergy();
    }

    void UpdateTotalEnergy()
    {
        currentTotalEnergy = 0f;
        // mapGenerators에 null이 포함될 수 있으므로 (예: Generator가 파괴되었지만 Unregister 안 된 경우) null 체크
        foreach (Generator gen in mapGenerators.Where(g => g != null))
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