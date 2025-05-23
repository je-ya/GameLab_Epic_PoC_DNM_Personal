using UnityEngine;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using System.Linq;

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
                    Debug.Log($"Right-clicked on Node: {targetNodeName}");

                    // 선택된 모든 오브젝트에 대해 MoveToNode 호출
                    foreach (GameObject obj in selectedObjects)
                    {
                        MonMovemont movement = obj.GetComponent<MonMovemont>();
                        if (movement != null)
                        {
                            movement.MoveToNode(targetNodeName, () =>
                            {
                                Debug.Log($"{obj.name} arrived at {targetNodeName}");
                            });
                        }
                    }
                    break; // 첫 번째 NodeObject를 찾으면 종료
                }
            }

            if (hits.Length == 0)
            {
                Debug.Log("No collider hit in 'Node' layer by right-click.");
            }
            else if (hits.All(hit => hit.collider.GetComponent<NodeObject>() == null))
            {
                Debug.Log("No NodeObject found in 'Node' layer at clicked position.");
            }

            // 트리거 감지 설정 원복 (선택 사항)
            Physics.queriesHitTriggers = false;
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


        GameObject[] allSelectables = GameObject.FindGameObjectsWithTag("PlayerMon");
        if (allSelectables.Length == 0)
        {
            Debug.LogWarning("No objects with tag 'PlayerMon' found to select.");
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
}