using UnityEngine;

[CreateAssetMenu(fileName = "NewMonData", menuName = "Mon/MonData")]
public class MonData : ScriptableObject
{
    public string monName; // 소환수 이름
    public MonType monType; // 소환수 타입
    public float maxHp; // 최대 체력
    public float attackPower; // 공격력
    public float attackRange; // 공격 범위
    public float attackCooldown; // 공격 반복 주기
    public float workEfficiency; // 작업 효율
    public float workInterval; // 작업 반복 주기
    public string specialAbilityType; // 특수 능력 타입 (비어 있거나 "None"이면 특수 능력 없음)
    public float abilityValue; // 특수 능력 값 (특수 능력 없을 때는 무시)
}

public enum MonType
{
    None,
    Attack,    // 공격 특화
    Work,     // 작업 특화
    Outlook  // 정찰
}