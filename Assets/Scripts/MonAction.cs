using UnityEngine;
using System.Collections.Generic;
using static UnityEngine.GraphicsBuffer;
using System.Collections;

public class MonAction : MonoBehaviour
{
    [SerializeField] private MonData _monData;
    public MonData monData => _monData;
    private float currentHp;
    public enum ActionState { None, Attacking, Working, Stealth }
    private ActionState currentAction = ActionState.None;
    private ActionState pendingAction = ActionState.None; // New: Tracks action to perform when idle
    private MonMovemont movement;
    [SerializeField] private Generator targetGenerator;
    private float workTimer = 0f;
    private GameObject attackTarget;
    private GeneratorSpawner generatorSpawner;
    private DragSelection dragSelection;

    Node Target;

    void Start()
    {
        currentHp = _monData.maxHp;
        movement = GetComponent<MonMovemont>();
        dragSelection = FindObjectOfType<DragSelection>();
        if (movement == null)
        {
            Debug.LogError("NodeBasedMovement component not found on " + gameObject.name);
        }

        generatorSpawner = FindFirstObjectByType<GeneratorSpawner>();
        if (generatorSpawner == null)
        {
            Debug.LogWarning("No GeneratorSpawner found in the scene.");
        }

    }


    void Update()
    {
        if (movement.GetState() == MonMovemont.MonMovemontState.Idle && pendingAction != ActionState.None)
        {
            switch (pendingAction)
            {
                case ActionState.Attacking:
                    StartAttack();
                    break;
                case ActionState.Working:
                    StartWork();
                    break;
                case ActionState.Stealth:
                    StartStealth();
                    break;
            }
            pendingAction = ActionState.None; // Clear pending action after execution
        }

        if (currentAction == ActionState.Working && targetGenerator != null)
        {
            workTimer += Time.deltaTime;
            if (workTimer >= _monData.workInterval)
            {
                float workAmount = _monData.workEfficiency;
                if (!string.IsNullOrEmpty(_monData.specialAbilityType) && _monData.specialAbilityType == "WorkSpeedBoost")
                {
                    workAmount *= _monData.abilityValue;
                }
                targetGenerator.AddProgress(workAmount);
                workTimer = 0f;
                Debug.Log($"{gameObject.name}이 {targetGenerator.name}에서 작업했습니다. 진행도: {targetGenerator.CurrentProgress}/{targetGenerator.MaxProgress}");

                if (targetGenerator.IsFullyRepaired())
                {
                    Debug.Log($"{gameObject.name}이 {targetGenerator.name}을 완전히 수리했습니다!");
                    CancelAction();
                    targetGenerator = null;
                }
            }
        }

        if(movement.GetState() != MonMovemont.MonMovemontState.Idle)
        {
            gameObject.tag = "PlayerMon";
        }

        if (currentAction == ActionState.Attacking && attackTarget == null)
        {
            attackTarget = FindClosestEnemy(10f);
        }
        if (currentAction == ActionState.Attacking && attackTarget != null)
        {
            // 공격 대상이 사거리 내에 있는지 확인
            float distanceToTarget = Vector3.Distance(transform.position, attackTarget.transform.position);
            if (distanceToTarget > _monData.attackRange || !attackTarget.activeInHierarchy)
            {
                attackTarget = null;
                currentAction = ActionState.None;
                Debug.Log($"{gameObject.name}: 대상이 사거리 밖이거나 비활성화되었습니다. 공격 중지.");
                return;
            }

            TryAttack();
        }  
    }

    float lastAttackTime;
    private void TryAttack()
    {
        if (Time.time < lastAttackTime + _monData.attackCooldown)
            return;

        Enemy enemy = attackTarget.GetComponent<Enemy>();
        if (enemy != null && _monData != null)
        {
            enemy.TakeDamage(_monData.attackPower);
            lastAttackTime = Time.time;
            Debug.Log($"{gameObject.name}이(가) {attackTarget.name}을(를) 공격하여 {_monData.attackPower} 데미지를 입혔습니다.");
        }
        else
        {
            Debug.LogWarning($"{gameObject.name}: Enemy 컴포넌트 또는 MonData가 없습니다. 공격 실패.");
            attackTarget = null;
            currentAction = ActionState.None;
        }
    }

    public void SetPendingAction(ActionState newAction)
    {
        pendingAction = newAction;
        Debug.Log($"{gameObject.name} set pending action to {newAction}.");
    }

    public void StartAttack()
    {
        CancelAction();
        currentAction = ActionState.Attacking;
        
        Debug.Log($"{gameObject.name} started Attack action targeting {attackTarget.name}.");
    }

    private GameObject FindClosestEnemy(float radius)
    {
        // Find all colliders within the specified radius
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, radius);
        GameObject closestEnemy = null;
        float closestDistance = Mathf.Infinity;

        foreach (Collider collider in hitColliders)
        {
            // Check if the collider belongs to an enemy (e.g., tagged as "Enemy")
            if (collider.CompareTag("Enemy"))
            {
                float distance = Vector3.Distance(transform.position, collider.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestEnemy = collider.gameObject;
                }
            }else { CancelAction(); }
        }

        return closestEnemy;
    }



    public void StartWork()
    {
        CancelAction();
        targetGenerator = MapManager.Instance.GetGeneratorAtNode(Target);
        currentAction = ActionState.Working;
        Debug.Log($"{gameObject.name} started Work action on Generator at {targetGenerator.gameObject.name}.");
    }

    
    public void GetReachNodeData(Node targetNode)
    {
        Target = targetNode;   
    }

    public void StartStealth()
    {
        CancelAction();
        currentAction = ActionState.Stealth;
        if (_monData.monType == MonType.Outlook)
        {
            StartCoroutine(ChangeTagAfterDelay());
        }
        Debug.Log($"{gameObject.name} started Stealth action.");
    }

    private IEnumerator ChangeTagAfterDelay()
    {
        yield return new WaitForSeconds(0.5f);
        gameObject.tag = "Stealth";
    }

    public void CancelAction()
    {
        if (currentAction != ActionState.None)
        {
            Debug.Log($"{gameObject.name} canceled {currentAction} action.");
            currentAction = ActionState.None;
            targetGenerator = null;
            workTimer = 0f;
            gameObject.tag = "PlayerMon";
        }
        pendingAction = ActionState.None; // Clear pending action
    }



    public void TakeDamage(float damage)
    {
        currentHp -= damage;
        Debug.Log($"{gameObject.name} took {damage} damage. Current HP: {currentHp}");
        if (currentHp <= 0)
        {
            Debug.Log($"{gameObject.name} is Dead!");
            if (dragSelection != null)
            {
                dragSelection.RemoveFromSelection(gameObject);
                
            }
            GameManager.Instance.ReportSummonDeath(gameObject);
            Destroy(gameObject);
        }
    }    

}