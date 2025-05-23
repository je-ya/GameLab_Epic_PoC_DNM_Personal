using UnityEngine;
using System.Collections.Generic;

public class EmployeeActions : MonoBehaviour
{
    [SerializeField] private MonData _monData;
    public MonData monData => _monData;
    private float currentHp;
    public enum ActionState { None, Attacking, Working, Stealth }
    private ActionState currentAction = ActionState.None;
    private ActionState pendingAction = ActionState.None; // New: Tracks action to perform when idle
    private NodeBasedMovement movement;
    [SerializeField] private Generator targetGenerator;
    private float workTimer = 0f;
    private float lastAttackTime = 0f;
    private GameObject attackTarget;
    private GeneratorSpawner generatorSpawner;
    private Dictionary<string, System.Action> specialAbilities;

    void Awake()
    {
        specialAbilities = new Dictionary<string, System.Action>
        {
            { "None", () => Debug.Log($"{gameObject.name} has no special ability.") },
            { "DamageBoost", () =>
                {
                    Debug.Log($"{gameObject.name} used Damage Boost! Attack power increased by {_monData.abilityValue}x.");
                }
            },
            { "WorkSpeedBoost", () =>
                {
                    Debug.Log($"{gameObject.name} used Work Speed Boost! Work efficiency increased by {_monData.abilityValue}x.");
                }
            },
            { "Heal", () =>
                {
                    currentHp = Mathf.Min(currentHp + _monData.abilityValue, _monData.maxHp);
                    Debug.Log($"{gameObject.name} healed for {_monData.abilityValue} HP. Current HP: {currentHp}");
                }
            }
        };
    }

    void Start()
    {
        currentHp = _monData.maxHp;
        movement = GetComponent<NodeBasedMovement>();
        if (movement == null)
        {
            Debug.LogError("NodeBasedMovement component not found on " + gameObject.name);
        }

        generatorSpawner = FindObjectOfType<GeneratorSpawner>();
        if (generatorSpawner == null)
        {
            Debug.LogWarning("No GeneratorSpawner found in the scene.");
        }

        AdjustStatsByType();
    }


    void Update()
    {
        // Handle pending actions when idle
        if (movement.GetState() == NodeBasedMovement.EmployeeState.Idle && pendingAction != ActionState.None)
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

        // Handle ongoing work action
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

        if (currentAction == ActionState.Attacking)
        {
            HandleAttackState();
        }
    }



private void AdjustStatsByType()
    {
        switch (_monData.monType)
        {
            case MonType.Attack:
                break;
            case MonType.Work:
                break;
            case MonType.Outlook:
                break;
        }
        currentHp = _monData.maxHp;
        Debug.Log($"{gameObject.name} is a {_monData.monType} type with HP: {_monData.maxHp}, Attack: {_monData.attackPower}, Work: {_monData.workEfficiency}, Special: {(string.IsNullOrEmpty(_monData.specialAbilityType) || _monData.specialAbilityType == "None" ? "None" : _monData.specialAbilityType)}");
    }

    public void SetPendingAction(ActionState newAction)
    {
        pendingAction = newAction;
        Debug.Log($"{gameObject.name} set pending action to {newAction}.");
    }

    public void StartAttack()
    {
        if (movement.GetState() != NodeBasedMovement.EmployeeState.Idle)
        {
            Debug.LogWarning($"{gameObject.name} cannot start Attack: Not in Idle state.");
            return;
        }

        CancelAction();
        currentAction = ActionState.Attacking;
        attackTarget = DetectEnemy(); // 근처 Enemy 감지
        if (attackTarget == null)
        {
            Debug.LogWarning($"{gameObject.name} cannot start Attack: No Enemy found within range.");
            CancelAction();
            return;
        }

        Debug.Log($"{gameObject.name} started Attack action targeting {attackTarget.name}.");
    }

    private GameObject DetectEnemy()
    {
        // First, check enemies on the same node
        NodeBasedMovement nodeMovement = GetComponent<NodeBasedMovement>();
        NodeBasedMovement.Node currentNode = nodeMovement.GetCurrentNode();

        // Find all enemies in the scene
        Enemy[] allEnemies = FindObjectsOfType<Enemy>();
        foreach (Enemy enemy in allEnemies)
        {
            NodeBasedMovement enemyMovement = enemy.GetComponent<NodeBasedMovement>();
            if (enemyMovement != null)
            {
                NodeBasedMovement.Node enemyNode = enemyMovement.GetCurrentNode();
                if (enemyNode != null && enemyNode == currentNode && enemy.health > 0)
                {
                    return enemy.gameObject;
                }
            }
            else
            {
                // Fallback to proximity check if enemy doesn't have NodeBasedMovement
                float distance = Vector3.Distance(transform.position, enemy.transform.position);
                if (distance <= _monData.attackRange && enemy.health > 0)
                {
                    return enemy.gameObject;
                }
            }
        }

        // Fallback to original sphere overlap if no enemies on the same node
        Collider[] colliders = Physics.OverlapSphere(transform.position, _monData.attackRange);
        foreach (Collider col in colliders)
        {
            if (col.CompareTag("Enemy") && col.GetComponent<Enemy>().health > 0)
            {
                return col.gameObject;
            }
        }
        return null;
    }

    private void HandleAttackState()
    {
        if (attackTarget == null || attackTarget.GetComponent<Enemy>().health <= 0)
        {
            Debug.Log($"{gameObject.name} stopped attacking: Target is null or defeated.");
            CancelAction();
            attackTarget = null;
            // Optionally resume movement if there was a previous target
            if (movement.GetCurrentNode() != null && movement.GetState() == NodeBasedMovement.EmployeeState.Idle)
            {
                Debug.Log($"{gameObject.name} resuming movement after combat.");
                // You may need a method to resume the previous movement
            }
            return;
        }

        float distanceToTarget = Vector3.Distance(transform.position, attackTarget.transform.position);
        if (distanceToTarget <= _monData.attackRange && Time.time >= lastAttackTime + _monData.attackCooldown)
        {
            float attackPower = _monData.attackPower;
            if (!string.IsNullOrEmpty(_monData.specialAbilityType) && _monData.specialAbilityType == "DamageBoost")
            {
                attackPower *= _monData.abilityValue;
            }
            Attack(attackTarget);
            lastAttackTime = Time.time;
        }
        else if (distanceToTarget > _monData.attackRange)
        {
            Debug.Log($"{gameObject.name} moving closer to attack target.");
            // Move toward the enemy's node
            NodeObject enemyNode = FindClosestNodeTo(attackTarget.transform.position);
            if (enemyNode != null)
            {
                string targetNodeName = enemyNode.name; // Assuming NodeObject has a name property
                movement.MoveToNode(targetNodeName, () => Debug.Log($"{gameObject.name} reached enemy node."));
            }
        }
    }

    private NodeObject FindClosestNodeTo(Vector3 position)
    {
        NodeObject[] allNodes = FindObjectsOfType<NodeObject>();
        NodeObject closest = null;
        float minDistance = Mathf.Infinity;

        foreach (NodeObject node in allNodes)
        {
            float distance = Vector3.Distance(position, node.transform.position);
            if (distance < minDistance)
            {
                minDistance = distance;
                closest = node;
            }
        }
        return closest;
    }

    private void Attack(GameObject target)
    {
        Enemy enemy = target.GetComponent<Enemy>();
        if (enemy != null)
        {
            enemy.TakeDamage(_monData.attackPower);
            Debug.Log($"{gameObject.name} attacked {target.name}, dealt {_monData.attackPower} damage");
        }
    }



    public void StartWork()
    {
        if (movement.GetState() != NodeBasedMovement.EmployeeState.Idle)
        {
            Debug.LogWarning($"{gameObject.name} cannot start Work: Not in Idle state.");
            return;
        }

        // Check if target generator is set and valid
        if (targetGenerator == null)
        {
            if (generatorSpawner != null)
            {
                var generators = generatorSpawner.GetSpawnedGenerators();
                targetGenerator = generators.Find(g => Vector3.Distance(g.transform.position, transform.position) < 1.0f &&
                                                     Vector3.Distance(g.transform.position, movement.GetCurrentNode().Position) < 0.1f);
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
        if (movement.GetState() != NodeBasedMovement.EmployeeState.Idle)
        {
            Debug.LogWarning($"{gameObject.name} cannot start Stealth: Not in Idle state.");
            return;
        }

        if (_monData.monType != MonType.Outlook)
        {
            Debug.LogWarning($"{gameObject.name} cannot start Stealth: Not an Outlook type.");
            return;
        }

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

    public void ActivateSpecialAbility()
    {
        string abilityType = string.IsNullOrEmpty(_monData.specialAbilityType) ? "None" : _monData.specialAbilityType;
        specialAbilities[abilityType]?.Invoke();
    }

    public void TakeDamage(float damage)
    {
        currentHp -= damage;
        Debug.Log($"{gameObject.name} took {damage} damage. Current HP: {currentHp}");
        if (currentHp <= 0)
        {
            Debug.Log($"{gameObject.name} is defeated!");
            Destroy(gameObject);
        }
    }    


    public void SetTargetGenerator(Generator generator)
    {
        targetGenerator = generator;
        Debug.Log($"{gameObject.name} assigned to work on Generator: {generator?.gameObject.name}");
    }
}