// DragSelection.cs (DragSelectionWithObjectDetection에서 이름 변경 또는 해당 파일 수정)
using UnityEngine;
using System.Collections.Generic;

public class DragSelection : MonoBehaviour // 클래스 이름은 파일명과 일치해야 함
{
    private Vector2 startPos;
    private Vector2 currentPos;
    private bool isDragging;
    private Rect selectionRect;
    private List<GameObject> selectedObjects = new List<GameObject>();
    // private NodeBasedMovement nodeSystem; // ActionPanelController에서 사용하므로 여기선 직접 필요 없을 수 있음

    void Start()
    {
        // nodeSystem = FindObjectOfType<NodeBasedMovement>(); // 필요하다면 유지
        // if (nodeSystem == null)
        // {
        //     Debug.LogError("NodeBasedMovement not found in scene!");
        // }
    }

    void Update()
    {
        // 좌클릭 드래그 선택 로직 (기존과 동일)
        if (Input.GetMouseButtonDown(0))
        {
            
            // UI 위에서 클릭했는지 확인 (Action Panel이 떠 있을 때 드래그 시작 방지)
            if (ActionPanelController.Instance != null && ActionPanelController.Instance.actionPanelObject.activeSelf &&
                UnityEngine.EventSystems.EventSystem.current != null &&
                UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            {
                return; // UI 위에서는 드래그 시작 안 함
            }


            isDragging = true;
            startPos = Input.mousePosition;

            foreach (GameObject obj in selectedObjects)
            {
                NodeBasedMovement movement = obj.GetComponent<NodeBasedMovement>();
                if (movement != null)
                {
                    movement.SetSelected(false); // 기존 선택된 객체 선택 해제
                }
            }
            selectedObjects.Clear(); // 리스트 초기화
        }

        if (Input.GetMouseButtonUp(0))
        {
            if (isDragging) // 드래그 중이었을 때만 선택 로직 실행
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

        // 우클릭 로직 변경
        if (Input.GetMouseButtonDown(1))
        {
            // UI 위에서 우클릭했는지 확인 (Action Panel 자체를 우클릭하는 것 방지)
            if (UnityEngine.EventSystems.EventSystem.current != null &&
                UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            {
                // 만약 UI 위에서 우클릭 시 패널을 닫고 싶다면 ActionPanelController.Instance.HidePanel();
                return;
            }

            if (selectedObjects.Count > 0)
            {
                // 카메라로부터 레이를 쏴서 월드 좌표 얻기 (3D 환경 가정)
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;
                Vector3 worldClickPosition;

                // 바닥이나 특정 레이어에만 반응하도록 LayerMask 사용 권장
                if (Physics.Raycast(ray, out hit, Mathf.Infinity /*, groundLayerMask*/))
                {
                    worldClickPosition = hit.point;
                }
                else
                {
                    // 레이가 아무것도 맞추지 못한 경우, 카메라에서 적당한 거리에 있는 평면상의 점을 사용
                    Plane groundPlane = new Plane(Vector3.up, Vector3.zero); // Y=0 평면
                    float rayDistance;
                    if (groundPlane.Raycast(ray, out rayDistance))
                    {
                        worldClickPosition = ray.GetPoint(rayDistance);
                    }
                    else
                    {
                        Debug.LogWarning("DragSelection: Right-click raycast failed to hit anything and ground plane.");
                        // 기본값 또는 오류 처리
                        return;
                    }
                }

                if (ActionPanelController.Instance != null)
                {
                    ActionPanelController.Instance.ShowPanel(selectedObjects, worldClickPosition);
                }
                else
                {
                    Debug.LogError("DragSelection: ActionPanelController.Instance is null!");
                }
            }
            else // 선택된 유닛이 없을 때 우클릭
            {
                if (ActionPanelController.Instance != null && ActionPanelController.Instance.actionPanelObject.activeSelf)
                {
                    ActionPanelController.Instance.HidePanel(); // 패널 닫기
                }
            }
        }
    }

    // OnGUI, GetScreenRect, SelectObjectsInRect (기존과 거의 동일, SetSelected 호출 확인)
    void OnGUI()
    {
        if (isDragging)
        {
            GUI.color = new Color(0.5f, 0.5f, 1f, 0.3f); // 색상 및 투명도 조절
            GUI.DrawTexture(selectionRect, Texture2D.whiteTexture);
            GUI.color = Color.white; // 원래 색상으로 복원
            GUI.Box(selectionRect, ""); // 테두리 (선택 사항)
        }
    }

    private Rect GetScreenRect(Vector2 start, Vector2 end)
    {
        // y 좌표를 화면 하단 기준으로 변환
        start.y = Screen.height - start.y;
        end.y = Screen.height - end.y;

        var topLeft = Vector2.Min(start, end);
        var bottomRight = Vector2.Max(start, end);

        return new Rect(topLeft.x, topLeft.y, bottomRight.x - topLeft.x, bottomRight.y - topLeft.y);
    }

    private void SelectObjectsInRect()
    {
        // 이전에 선택된 객체는 이미 해제되었으므로, 새로 선택되는 객체만 처리
        // selectedObjects.Clear(); // Update의 GetMouseButtonDown(0)에서 이미 처리됨

        GameObject[] allSelectables = GameObject.FindGameObjectsWithTag("PlayerMon"); // "Selectable" 대신 "PlayerMon" 태그 사용
        if (allSelectables.Length == 0)
        {
            Debug.LogWarning("No objects with tag 'PlayerMon' found to select.");
        }


        foreach (GameObject obj in allSelectables)
        {
            Vector3 screenPos = Camera.main.WorldToScreenPoint(obj.transform.position);
            // screenPos.y는 이미 화면 하단 기준이므로 변환 불필요 (WorldToScreenPoint 결과)
            // 단, selectionRect는 y가 상단 기준이므로, screenPos.y를 뒤집어 비교하거나,
            // selectionRect.Contains의 y 비교를 반대로 해야 함.
            // 여기서는 selectionRect가 이미 화면 하단 기준 y를 사용하도록 GetScreenRect에서 처리했으므로 직접 비교 가능

            // 오브젝트가 화면에 보이는지, 그리고 사각형 내에 있는지 확인
            if (screenPos.z > 0 && selectionRect.Contains(new Vector2(screenPos.x, Screen.height - screenPos.y), true))
            {
                selectedObjects.Add(obj);
                NodeBasedMovement movement = obj.GetComponent<NodeBasedMovement>();
                if (movement != null)
                {
                    movement.SetSelected(true); // NodeBasedMovement에 이 함수가 있어야 함
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

    // MoveSelectedObjectsToNode 와 FindClosestNode 는 ActionPanelController로 이동 또는 거기서 재구현
}