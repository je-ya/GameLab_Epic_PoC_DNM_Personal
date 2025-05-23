using UnityEngine;
using System.Collections.Generic;

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



    void Start()
    {
        currentHp = _monData.maxHp;
        movement = GetComponent<MonMovemont>();
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
            if (workTimer >= 1f) // 1 second per work tick
            {
                float workAmount = _monData.workEfficiency;
                if (!string.IsNullOrEmpty(_monData.specialAbilityType) && _monData.specialAbilityType == "WorkSpeedBoost")
                {
                    workAmount *= _monData.abilityValue;
                }
                targetGenerator.AddProgress(workAmount);
                workTimer = 0f;
                Debug.Log($"{gameObject.name} worked on {targetGenerator.name}. Progress: {targetGenerator.CurrentProgress}/{targetGenerator.MaxProgress}");

                if (targetGenerator.IsFullyRepaired())
                {
                    Debug.Log($"{targetGenerator.name} is fully repaired by {gameObject.name}!");
                    CancelAction();
                    targetGenerator = null;
                }
            }
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



    public void StartWork()
    {
        if (movement.GetState() != MonMovemont.MonMovemontState.Idle)
        {
            Debug.LogWarning($"{gameObject.name} cannot start Work: Not in Idle state.");
            return;
        }

        if (targetGenerator == null)
        {
            if (generatorSpawner != null)
            {
                var generators = generatorSpawner.GetSpawnedGenerators();
                targetGenerator = generators.Find(g => Vector3.Distance(g.transform.position, transform.position) < 1.0f &&
                                                     Vector3.Distance(g.transform.position, MapManager.Instance.GetCurrentNode().Position) < 0.1f);
            }
        }

        if (targetGenerator == null)
        {
            Debug.LogWarning($"{gameObject.name} cannot start Work: No Generator found at current node or specified.");
            return;
        }

        if (targetGenerator.IsFullyRepaired())
        {
            Debug.LogWarning($"{gameObject.name} cannot start Work: Generator is already fully repaired.");
            targetGenerator = null;
            return;
        }

        CancelAction();
        currentAction = ActionState.Working;
        Debug.Log($"{gameObject.name} started Work action on Generator at {targetGenerator.gameObject.name}.");
    }

    public void StartStealth()
    {
        CancelAction();
        currentAction = ActionState.Stealth;
        Debug.Log($"{gameObject.name} started Stealth action.");
    }

    public void CancelAction()
    {
        if (currentAction != ActionState.None)
        {
            Debug.Log($"{gameObject.name} canceled {currentAction} action.");
            currentAction = ActionState.None;
            workTimer = 0f;
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
            Destroy(gameObject);
        }
    }    


    public void SetTargetGenerator(Generator generator)
    {
        targetGenerator = generator;
        Debug.Log($"{gameObject.name} assigned to work on Generator: {generator?.gameObject.name}");
    }
}