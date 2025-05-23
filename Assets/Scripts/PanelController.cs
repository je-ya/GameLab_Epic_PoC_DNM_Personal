using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

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

    //void Start()
    //{
        
    //    dragSelection = gameObject.GetComponent<DragSelection>();

    //    // Button listeners
    //    moveButton.onClick.AddListener(OnMoveClicked);
    //    attackButton.onClick.AddListener(OnAttackClicked);
    //    workButton.onClick.AddListener(OnWorkClicked);
    //    stealthButton.onClick.AddListener(OnStealthClicked);
    //}

    //public void ShowPanel(List<GameObject> selectedUnits, Vector3 worldPosition)
    //{
    //    if (selectedUnits == null || selectedUnits.Count == 0)
    //    {
    //        Debug.LogWarning("ActionPanelController: ShowPanel called with no selected units.");
    //        return;
    //    }

    //    currentSelectedUnits = selectedUnits;
    //    clickWorldPosition = worldPosition;
    //    clickTargetNode = FindClosestNodeToPosition(clickWorldPosition);

    //    actionPanelObject.transform.position = Input.mousePosition;

    //    bool canStealth = selectedUnits.Any(u => u.GetComponent<EmployeeActions>()?.monData.monType == MonType.Outlook);
    //    attackButton.gameObject.SetActive(true);
    //    workButton.gameObject.SetActive(true);
    //    stealthButton.gameObject.SetActive(canStealth);

    //    actionPanelObject.SetActive(true);
    //}

    //public void HidePanel()
    //{
    //    actionPanelObject.SetActive(false);
    //    currentSelectedUnits = null;
    //    dragSelection.ClearObjList();
    //}

    //void OnMoveClicked()
    //{
    //    if (clickTargetNode == null)
    //    {
    //        Debug.LogWarning("No target node selected for movement.");
    //        HidePanel();
    //        return;
    //    }

    //    Debug.Log($"Action Panel: Issuing Move command to node {clickTargetNode.Name}");
    //    foreach (GameObject unit in currentSelectedUnits)
    //    {
    //        MonMovemont movement = unit.GetComponent<MonMovemont>();
    //        EmployeeActions actions = unit.GetComponent<EmployeeActions>();
    //        if (movement != null && actions != null)
    //        {
    //            actions.CancelAction(); // Clear any existing actions
    //            movement.MoveToNode(clickTargetNode.Name); // Move without setting action
    //        }
    //    }
    //    HidePanel();
    //}

    //void OnAttackClicked()
    //{
    //    if (clickTargetNode == null)
    //    {
    //        Debug.LogWarning("No target node selected for attack.");
    //        HidePanel();
    //        return;
    //    }

    //    Debug.Log("Action Panel: Issuing Attack command.");
    //    foreach (GameObject unit in currentSelectedUnits)
    //    {
    //        MonMovemont movement = unit.GetComponent<MonMovemont>();
    //        EmployeeActions actions = unit.GetComponent<EmployeeActions>();
    //        if (movement != null && actions != null)
    //        {
    //            actions.CancelAction();
    //            movement.MoveToNode(clickTargetNode.Name, () => actions.SetPendingAction(EmployeeActions.ActionState.Attacking));
    //        }
    //    }
    //    HidePanel();
    //}

    //void OnWorkClicked()
    //{
    //    if (clickTargetNode == null)
    //    {
    //        Debug.LogWarning("No target node selected for work.");
    //        HidePanel();
    //        return;
    //    }

    //    Generator targetGenerator = FindGeneratorAtNode(clickTargetNode);
    //    if (targetGenerator == null)
    //    {
    //        Debug.LogWarning("No generator found at target node for work.");
    //        HidePanel();
    //        return;
    //    }

    //    Debug.Log("Action Panel: Issuing Work command.");
    //    foreach (GameObject unit in currentSelectedUnits)
    //    {
    //        MonMovemont movement = unit.GetComponent<MonMovemont>();
    //        EmployeeActions actions = unit.GetComponent<EmployeeActions>();
    //        if (movement != null && actions != null)
    //        {
    //            actions.CancelAction();
    //            actions.SetTargetGenerator(targetGenerator);
    //            movement.MoveToNode(clickTargetNode.Name, () => actions.SetPendingAction(EmployeeActions.ActionState.Working));
    //        }
    //    }
    //    HidePanel();
    //}

    //void OnStealthClicked()
    //{
    //    Debug.Log("Action Panel: Issuing Stealth command.");
    //    foreach (GameObject unit in currentSelectedUnits)
    //    {
    //        EmployeeActions actions = unit.GetComponent<EmployeeActions>();
    //        if (actions != null && actions.monData.monType == MonType.Outlook)
    //        {
    //            actions.CancelAction();
    //            actions.SetPendingAction(EmployeeActions.ActionState.Stealth);
    //        }
    //    }
    //    HidePanel();
    //}




    private Generator FindGeneratorAtNode(Node node)
    {
        if (node == null) return null;

        Generator[] allGenerators = FindObjectsOfType<Generator>();
        foreach (Generator gen in allGenerators)
        {
            if (Vector3.Distance(gen.transform.position, node.Position) < 0.1f)
            {
                return gen;
            }
        }
        return null;
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