
using UnityEngine;

public class MapReferenceAuthoring : MonoBehaviour
{
#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.gray;
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
