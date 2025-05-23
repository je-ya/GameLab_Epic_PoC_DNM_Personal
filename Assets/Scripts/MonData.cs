using UnityEngine;

[CreateAssetMenu(fileName = "NewMonData", menuName = "Mon/MonData")]
public class MonData : ScriptableObject
{
    public string monName; // 소환수 이름
    public MonType monType; // 소환수 타입
    public float maxHp; // 최대 체력
    public float attackPower; // 공격력
    public float attackRange;
    public float attackCooldown;
    public float workEfficiency; // 작업 효율
    public string specialAbilityType; // 특수 능력 타입 (비어 있거나 "None"이면 특수 능력 없음)
    public float abilityValue; // 특수 능력 값 (특수 능력 없을 때는 무시)
}

public enum MonType
{
    None,
    Attack,    // 높은 체력, 낮은 공격력
    Work,     // 높은 공격력, 중간 체력
    Outlook  // 높은 작업 효율, 특수 능력 중심
}