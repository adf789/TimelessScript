using Unity.Mathematics;

namespace Utility
{
    public static class Physics
    {
        /// <summary>
        /// Collision
        /// </summary>
        public static bool BoundsIntersect(in ColliderBoundsComponent bounds1, in ColliderBoundsComponent bounds2)
        {
            return bounds1.Min.x < bounds2.Max.x && bounds1.Max.x > bounds2.Min.x &&
                   bounds1.Min.y < bounds2.Max.y && bounds1.Max.y > bounds2.Min.y;
        }

        /// <summary>
        /// Collision
        /// </summary>
        public static float2 GetSeparationVector(in ColliderBoundsComponent bounds1, in ColliderBoundsComponent bounds2)
        {
            float2 separation = float2.zero;

            // 겹침 크기 계산 (교집합 영역의 크기)
            float overlapX = math.min(bounds1.Max.x, bounds2.Max.x) -
                            math.max(bounds1.Min.x, bounds2.Min.x);
            float overlapY = math.min(bounds1.Max.y, bounds2.Max.y) -
                            math.max(bounds1.Min.y, bounds2.Min.y);

            // 최소 이동 거리로 분리 (MTV - Minimum Translation Vector)
            if (overlapX < overlapY)
            {
                // X축으로 분리 (겹침이 적은 축 선택)
                separation.x = bounds1.Center.x < bounds2.Center.x ? -overlapX : overlapX;
            }
            else
            {
                // Y축으로 분리
                separation.y = bounds1.Center.y < bounds2.Center.y ? -overlapY : overlapY;
            }

            return separation;
        }
    }
}