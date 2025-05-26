using UnityEngine;

public class FogOfWar : MonoBehaviour
{
    public Material fogMaterial; // FogOfWarMat 머티리얼
    public Transform unit; // 아군 유닛
    public float sightRadius = 0.1f; // UV 공간 기준
    public float mapWidth = 100f; // 맵 실제 너비 (X축)
    public float mapHeight = 50f; // 맵 실제 높이 (Y축)
    public Vector2 mapOrigin = Vector2.zero; // 맵의 왼쪽 아래 원점 (X, Y)
    public float fogAlpha = 0.3f; // 안개 투명도

    void Update()
    {
        // 유닛의 월드 좌표를 맵의 UV 좌표로 변환 (X-Y 평면)
        Vector2 uvPos = new Vector2(
            (unit.position.x - mapOrigin.x) / mapWidth,
            (unit.position.y - mapOrigin.y) / mapHeight
        );
        fogMaterial.SetVector("_UnitPositions", new Vector4(uvPos.x, uvPos.y, 0, 0));
        fogMaterial.SetFloat("_SightRadius", sightRadius);
        fogMaterial.SetFloat("_FogAlpha", fogAlpha);

        // 디버깅: UV 좌표와 유닛 위치 로그
        //Debug.Log($"UV Position: {uvPos}, Unit Position: {unit.position}");
    }
}