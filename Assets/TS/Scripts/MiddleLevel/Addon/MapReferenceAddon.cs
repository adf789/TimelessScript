
using UnityEngine;

public class MapReferenceAddon : MonoBehaviour
{
    //============================================================
    //=========    Coding rule에 맞춰서 작업 바랍니다.   =========
    //========= Coding rule region은 절대 지우지 마세요. =========
    //=========    문제 시 '김철옥'에게 문의 바랍니다.   =========
    //============================================================

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.white;
        Vector3 startPos = transform.position;

        startPos.x -= IntDefine.MAP_TOTAL_GRID_WIDTH * 0.5f;
        startPos.y -= IntDefine.MAP_TOTAL_GRID_HEIGHT * 0.5f;

        // Draw vertical lines
        for (int x = 0; x <= IntDefine.MAP_TOTAL_GRID_WIDTH; x++)
        {
            Vector3 start = startPos + new Vector3(x * IntDefine.MAP_GRID_SIZE, 0, 0);
            Vector3 end = start + new Vector3(0, IntDefine.MAP_TOTAL_GRID_HEIGHT * IntDefine.MAP_GRID_SIZE, 0);
            Gizmos.DrawLine(start, end);
        }

        // Draw horizontal lines
        for (int y = 0; y <= IntDefine.MAP_TOTAL_GRID_HEIGHT; y++)
        {
            Vector3 start = startPos + new Vector3(0, y * IntDefine.MAP_GRID_SIZE, 0);
            Vector3 end = start + new Vector3(IntDefine.MAP_TOTAL_GRID_WIDTH * IntDefine.MAP_GRID_SIZE, 0, 0);
            Gizmos.DrawLine(start, end);
        }
    }
#endif
}
