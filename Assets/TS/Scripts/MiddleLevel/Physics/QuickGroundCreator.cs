using UnityEditor;
using UnityEngine;

public class QuickGroundCreator : MonoBehaviour
{
    [MenuItem("GameObject/2D Object/Lightweight Ground", false, 10)]
    static void CreateGround()
    {
        GameObject ground = new GameObject("Ground");
        ground.transform.position = Vector3.zero;
        
        // 컴포넌트 추가
        var physics = ground.AddComponent<LightweightPhysics2D>();
        var groundComponent = ground.AddComponent<GroundObject>();
        var setup = ground.AddComponent<GroundSetup>();
        
        // 기본 설정
        physics.colliderSize = new Vector2(10, 1);
        physics.isTrigger = false;
        physics.useGravity = false;
        physics.mass = float.MaxValue;
        
        ground.tag = "Ground";
        
        // 선택된 상태로 만들기
        Selection.activeGameObject = ground;
        
        Debug.Log("Ground 객체가 생성되었습니다!");
    }
}