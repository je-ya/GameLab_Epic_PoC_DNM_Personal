using UnityEngine;

[CreateAssetMenu(fileName = "NewEnemyData", menuName = "Enemy/EnemyStats")]
public class EnemyData : ScriptableObject
{
    public float MaxHP; //HP
    public float MoveSpeed; //이동 속도
    public float AttackRange;   //공격 범위
    public float AttackCooldown;    //공격 쿨타임
    public float AttackPower;   //데미지 
    public float FrontDetectionRange; // 앞 감지범위
    public float RearDetectionRange;  // 뒤 감지범위
}