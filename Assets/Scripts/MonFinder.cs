using UnityEngine;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MonFinder : MonoBehaviour
{
    [SerializeField] private Button[] findButtons; // 여러 버튼 참조
    private List<GameObject> monObjects = new List<GameObject>(); // 결과 리스트

    void Start()
    {
        // 버튼이 null이 아니고 3개라고 가정하고 각 버튼에 타입 연결
        if (findButtons != null && findButtons.Length >= 3)
        {
            findButtons[0].onClick.AddListener(() => FindMonObjectsByType(MonType.Attack));
            findButtons[1].onClick.AddListener(() => FindMonObjectsByType(MonType.Work));
            findButtons[2].onClick.AddListener(() => FindMonObjectsByType(MonType.Outlook));
        }
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(1))
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            if (monObjects.Count == 0)
            {
                Debug.Log("No units selected to show action panel.");
                return;
            }

            Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, Camera.main.nearClipPlane));
            ActionPanelController.Instance.ShowPanel(monObjects, worldPos);
        }
    }

    void FindMonObjectsByType(MonType targetMonType)
    {
        monObjects.Clear();

        MonAction[] monComponents = FindObjectsOfType<MonAction>();

        foreach (MonAction monComp in monComponents)
        {
            if (monComp.monData != null && monComp.monData.monType == targetMonType)
            {
                monObjects.Add(monComp.gameObject);
            }
        }

        Debug.Log($"Found {monObjects.Count} objects with MonType: {targetMonType}");
        foreach (GameObject obj in monObjects)
        {
            Debug.Log($" - {obj.name}");
        }
    }
}
