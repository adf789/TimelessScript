
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

public class GroundReferenceAuthoring : MonoBehaviour
{
    [SerializeField] private GroundEntry[] _grounds;
    [SerializeField] private LadderEntry[] _ladders;

    private class Baker : Baker<GroundReferenceAuthoring>
    {
        public override void Bake(GroundReferenceAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            AddComponent(entity, new SetNameComponent() { Name = authoring.name });

            var referenceBuffer = AddBuffer<GroundReferenceBuffer>(entity);

            for (int i = 0; i < authoring._grounds.Length; i++)
            {
                referenceBuffer.Add(CreateReference(in authoring._grounds[i]));
            }
        }

        private GroundReferenceBuffer CreateReference(in GroundEntry entry)
        {
            return new GroundReferenceBuffer()
            {
                Ground = GetEntity(entry.Ground, TransformUsageFlags.None),
                Min = new int2(entry.Min.x, entry.Min.y),
                Max = new int2(entry.Max.x, entry.Max.y),
            };
        }
    }

#if UNITY_EDITOR
    /// <summary>
    /// 에디터용
    /// </summary>
    [SerializeField] private Transform _groundParent;
    /// <summary>
    /// 에디터용
    /// </summary>
    [SerializeField] private Transform _ladderParent;

    private void OnDrawGizmos()
    {
        // Draw grid
        Vector3 startPos = transform.position;

        startPos.x -= IntDefine.MAP_TOTAL_GRID_WIDTH * 0.5f;
        startPos.y -= IntDefine.MAP_TOTAL_GRID_HEIGHT * 0.5f;

        // Draw vertical lines
        for (int x = 0; x <= IntDefine.MAP_TOTAL_GRID_WIDTH; x++)
        {
            if (x == 0 || x == IntDefine.MAP_TOTAL_GRID_WIDTH)
                Gizmos.color = Color.red;
            else if (x % 5 == 0)
                Gizmos.color = Color.white;
            else
                Gizmos.color = Color.black;

            Vector3 start = startPos + new Vector3(x * IntDefine.MAP_GRID_SIZE, 0, 0);
            Vector3 end = start + new Vector3(0, IntDefine.MAP_TOTAL_GRID_HEIGHT * IntDefine.MAP_GRID_SIZE, 0);
            Gizmos.DrawLine(start, end);
        }

        // Draw horizontal lines
        for (int y = 0; y <= IntDefine.MAP_TOTAL_GRID_HEIGHT; y++)
        {
            if (y == 0 || y == IntDefine.MAP_TOTAL_GRID_HEIGHT)
                Gizmos.color = Color.red;
            else if (y % 5 == 0)
                Gizmos.color = Color.white;
            else
                Gizmos.color = Color.black;

            Vector3 start = startPos + new Vector3(0, y * IntDefine.MAP_GRID_SIZE, 0);
            Vector3 end = start + new Vector3(IntDefine.MAP_TOTAL_GRID_WIDTH * IntDefine.MAP_GRID_SIZE, 0, 0);
            Gizmos.DrawLine(start, end);
        }

        // Draw grid index labels (5x5 chunks)
        int devide = 5;
        GUIStyle style = new();
        style.normal.textColor = Color.yellow;
        style.fontSize = 10;
        style.fontStyle = FontStyle.Bold;
        style.alignment = TextAnchor.MiddleCenter;

        for (int chunkX = 0; chunkX < IntDefine.MAP_TOTAL_GRID_WIDTH; chunkX += devide)
        {
            for (int chunkY = 0; chunkY < IntDefine.MAP_TOTAL_GRID_HEIGHT; chunkY += devide)
            {
                // Calculate center position of 5x5 chunk
                float leftBottomX = startPos.x + (chunkX + 0.5f) * IntDefine.MAP_GRID_SIZE;
                float leftBottomY = startPos.y + (chunkY + 0.5f) * IntDefine.MAP_GRID_SIZE;
                Vector3 labelPos = new(leftBottomX, leftBottomY, 0);

                string label = $"{chunkX}:{chunkY}";
                UnityEditor.Handles.Label(labelPos, label, style);
            }
        }
    }
#endif
}
