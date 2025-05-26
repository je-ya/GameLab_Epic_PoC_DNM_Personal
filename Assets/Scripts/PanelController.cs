using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using static MonAction;

public class ActionPanelController : MonoBehaviour
{
    public static ActionPanelController Instance { get; private set; }

    [Header("UI Panel")]
    public GameObject actionPanelObject;

    [Header("Action Buttons")]
    public Button moveButton;
    public Button attackButton;
    public Button workButton;
    public Button stealthButton;

    private List<GameObject> currentSelectedUnits;
    private Vector3 clickWorldPosition;

    private DragSelection dragSelection;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        actionPanelObject.SetActive(false);
    }

    void Start()
    {
        dragSelection = gameObject.GetComponent<DragSelection>();

        // Button listeners
        moveButton.onClick.AddListener(OnMoveClicked);
        attackButton.onClick.AddListener(OnAttackClicked);
        workButton.onClick.AddListener(OnWorkClicked);
        stealthButton.onClick.AddListener(OnStealthClicked);
    }

    public void ShowPanel(List<GameObject> selectedUnits, Vector3 worldPosition)
    {
        if (selectedUnits == null || selectedUnits.Count == 0)
        {
            Debug.LogWarning("ActionPanelController: ShowPanel called with no selected units.");
            return;
        }

        currentSelectedUnits = selectedUnits;
        clickWorldPosition = worldPosition;

        actionPanelObject.transform.position = Input.mousePosition;

        bool canStealth = selectedUnits.Any(u => u.GetComponent<MonAction>()?.monData.monType == MonType.Outlook);
        attackButton.gameObject.SetActive(true);
        workButton.gameObject.SetActive(true);
        stealthButton.gameObject.SetActive(canStealth);

        actionPanelObject.SetActive(true);
    }

    public void HidePanel()
    {
        actionPanelObject.SetActive(false);
        currentSelectedUnits = null;
        dragSelection.ClearObjList();
    }



    private void MoveToNodeAndAct(string actionName)
    {
        if (currentSelectedUnits == null || currentSelectedUnits.Count == 0)
        {
            Debug.LogWarning($"No units selected for {actionName} action.");
            HidePanel();
            return;
        }

        // 트리거 콜라이더도 감지하도록 설정
        Physics.queriesHitTriggers = true;

        // "Node" 레이어만 타겟팅
        LayerMask nodeLayer = LayerMask.GetMask("Node");
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit[] hits = Physics.RaycastAll(ray, Mathf.Infinity, nodeLayer);
        foreach (RaycastHit hit in hits)
        {
            NodeObject nodeObject = hit.collider.GetComponent<NodeObject>();
            if (nodeObject != null)
            {
                string targetNodeName = nodeObject.NodeName;
                Debug.Log($"{actionName} command issued to Node: {targetNodeName}");

                Node targetNode = MapManager.Instance.GetNodeByName(targetNodeName);

                // 선택된 모든 오브젝트에 대해 MoveToNode 호출
                foreach (GameObject obj in currentSelectedUnits)
                {
                    MonMovemont movement = obj.GetComponent<MonMovemont>();
                    MonAction action = obj.GetComponent<MonAction>();

                    MonAction.ActionState actionState;
                    switch (actionName)
                    {
                        case "Move":
                            actionState = MonAction.ActionState.None; 
                            break;
                        case "Attack":
                            actionState = MonAction.ActionState.Attacking;
                            break;
                        case "Working":
                            actionState = MonAction.ActionState.Working;
                            break;
                        case "Stealth":
                            actionState = MonAction.ActionState.Stealth;
                            break;
                        default:
                            Debug.LogWarning($"알 수 없는 액션: {actionName}");
                            HidePanel();
                            return;
                    }

                    
                    if (movement != null)
                    {
                        movement.MoveToNode(targetNodeName, () =>
                        {
                            action.SetPendingAction(actionState);
                            action.GetReachNodeData(targetNode);
                        });
                    }
                }
                break; // 첫 번째 NodeObject를 찾으면 종료
            }
        }

        if (hits.Length == 0)
        {
            Debug.Log($"No collider hit in 'Node' layer for {actionName} command.");
        }
        else if (hits.All(hit => hit.collider.GetComponent<NodeObject>() == null))
        {
            Debug.Log($"No NodeObject found in 'Node' layer for {actionName} command.");
        }

        // 트리거 감지 설정 원복
        Physics.queriesHitTriggers = false;

        HidePanel();
    }

    void OnMoveClicked()
    {
        MoveToNodeAndAct("Move");
    }

    void OnAttackClicked()
    {
        MoveToNodeAndAct("Attack");
    }

    void OnWorkClicked()
    {
        MoveToNodeAndAct("Working");
    }

    void OnStealthClicked()
    {
        MoveToNodeAndAct("Stealth");
    }


    void Update()
    {
        if (actionPanelObject.activeSelf && (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1)))
        {
            if (UnityEngine.EventSystems.EventSystem.current != null &&
                !UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            {
                //HidePanel(); // Optional: Uncomment to close panel on non-UI click
            }
        }
    }
}