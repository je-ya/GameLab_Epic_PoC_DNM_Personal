using UnityEngine;
using System.Collections.Generic;
using UnityEngine.EventSystems;


public class DragSelection : MonoBehaviour
{
    private Vector2 startPos;
    private Vector2 currentPos;
    private bool isDragging;
    private Rect selectionRect;
    private List<GameObject> selectedObjects = new List<GameObject>();


    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {

            if (ActionPanelController.Instance != null && ActionPanelController.Instance.actionPanelObject.activeSelf &&
                UnityEngine.EventSystems.EventSystem.current != null &&
                UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }


            isDragging = true;
            startPos = Input.mousePosition;

            foreach (GameObject obj in selectedObjects)
            {
                MonMovemont movement = obj.GetComponent<MonMovemont>();
                if (movement != null)
                {
                    movement.SetSelected(false);
                }
            }
            selectedObjects.Clear();
        }

        if (Input.GetMouseButtonUp(0))
        {
            if (isDragging)
            {
                isDragging = false;
                SelectObjectsInRect();
            }
        }

        if (isDragging)
        {
            currentPos = Input.mousePosition;
            selectionRect = GetScreenRect(startPos, currentPos);
        }

        if (Input.GetMouseButtonDown(1))
        {
            // UI 위에 마우스가 있을 때는 무시
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            // 선택된 유닛이 없으면 패널을 띄우지 않음
            if (selectedObjects.Count == 0)
            {
                Debug.Log("No units selected to show action panel.");
                return;
            }

            Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, Camera.main.nearClipPlane));
            ActionPanelController.Instance.ShowPanel(selectedObjects, worldPos);
        }
    }

    void OnGUI()
    {
        if (isDragging)
        {
            GUI.color = new Color(0.5f, 0.5f, 1f, 0.3f);
            GUI.DrawTexture(selectionRect, Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Box(selectionRect, "");
        }
    }

    private Rect GetScreenRect(Vector2 start, Vector2 end)
    {
        start.y = Screen.height - start.y;
        end.y = Screen.height - end.y;

        var topLeft = Vector2.Min(start, end);
        var bottomRight = Vector2.Max(start, end);

        return new Rect(topLeft.x, topLeft.y, bottomRight.x - topLeft.x, bottomRight.y - topLeft.y);
    }

    private void SelectObjectsInRect()
    {
        // 검색할 태그 배열
        string[] targetTags = { "PlayerMon", "Stealth" };
        List<GameObject> allSelectables = new List<GameObject>();

        // 각 태그에 대해 오브젝트 수집
        foreach (string tag in targetTags)
        {
            GameObject[] objectsWithTag = GameObject.FindGameObjectsWithTag(tag);
            if (objectsWithTag.Length == 0)
            {
                Debug.LogWarning($"No objects with tag '{tag}' found to select.");
            }
            allSelectables.AddRange(objectsWithTag);
        }

        // 선택된 오브젝트가 없으면 종료
        if (allSelectables.Count == 0)
        {
            Debug.LogWarning("No objects with target tags found to select.");
            return;
        }

        foreach (GameObject obj in allSelectables)
        {
            Vector3 screenPos = Camera.main.WorldToScreenPoint(obj.transform.position);

            if (screenPos.z > 0 && selectionRect.Contains(new Vector2(screenPos.x, Screen.height - screenPos.y), true))
            {
                selectedObjects.Add(obj);
                MonMovemont movement = obj.GetComponent<MonMovemont>();
                if (movement != null)
                {
                    movement.SetSelected(true);
                }
                Debug.Log(obj.name + " selected");
            }
        }

        if (selectedObjects.Count > 0)
        {
            Debug.Log("Total selected objects: " + selectedObjects.Count);
        }
    }

    public void ClearObjList()
    {
        selectedObjects.Clear();
    }

    public void RemoveFromSelection(GameObject obj)
    {
        if (selectedObjects.Contains(obj))
        {
            selectedObjects.Remove(obj);
            Debug.Log($"{obj.name} removed from selectedObjects. Current count: {selectedObjects.Count}");
        }
    }
}