// Assets/TS/Scripts/LowLevel/Utils/GeometryUtils.cs
using Unity.Mathematics;

namespace Utility
{
    public static class Geometry
    {
        /// <summary>
        /// 두 개의 AABB(축 정렬 경계 상자) 사각형이 겹치는지 확인합니다.
        /// 각 사각형은 min과 max 지점으로 정의됩니다.
        /// </summary>
        /// <param name="minA">사각형 A의 최소 지점 (좌측 하단)</param>
        /// <param name="maxA">사각형 A의 최대 지점 (우측 상단)</param>
        /// <param name="minB">사각형 B의 최소 지점 (좌측 하단)</param>
        /// <param name="maxB">사각형 B의 최대 지점 (우측 상단)</param>
        /// <returns>겹치면 true, 그렇지 않으면 false</returns>
        public static bool IsAABBOverlap(float2 minA, float2 maxA, float2 minB, float2 maxB)
        {
            // A의 오른쪽 끝이 B의 왼쪽 끝보다 왼쪽에 있거나,
            // A의 왼쪽 끝이 B의 오른쪽 끝보다 오른쪽에 있으면 겹치지 않음.
            if (maxA.x < minB.x || minA.x > maxB.x)
            {
                return false;
            }

            // A의 위쪽 끝이 B의 아래쪽 끝보다 아래에 있거나,
            // A의 아래쪽 끝이 B의 위쪽 끝보다 위에 있으면 겹치지 않음.
            if (maxA.y < minB.y || minA.y > maxB.y)
            {
                return false;
            }

            // 위의 모든 조건을 통과하면 겹치는 것임.
            return true;
        }

        /// <summary>
        /// 선분(두 점으로 정의)과 AABB 사각형(중심점과 크기로 정의)이 겹치는지 확인합니다.
        /// </summary>
        /// <param name="lineP1">선분의 시작점</param>
        /// <param name="lineP2">선분의 끝점</param>
        /// <param name="rectCenter">사각형의 중심점</param>
        /// <param name="rectSize">사각형의 전체 크기 (가로, 세로)</param>
        /// <returns>겹치면 true, 그렇지 않으면 false</returns>
        public static bool IntersectLineAABB(float2 lineP1, float2 lineP2, float2 rectCenter, float2 rectSize)
        {
            float2 rectMin = rectCenter - rectSize * 0.5f;
            float2 rectMax = rectCenter + rectSize * 0.5f;
            float2 lineDir = lineP2 - lineP1;

            // 선분이 아닌 점(point)인 경우, 점이 사각형 내에 있는지 확인
            if (math.all(lineDir == 0))
            {
                return lineP1.x >= rectMin.x && lineP1.x <= rectMax.x &&
                       lineP1.y >= rectMin.y && lineP1.y <= rectMax.y;
            }

            float2 invDir = 1.0f / lineDir;

            float2 t_near = (rectMin - lineP1) * invDir;
            float2 t_far = (rectMax - lineP1) * invDir;

            // 각 축에 대해 t_near가 t_far보다 작도록 정렬
            if (t_near.x > t_far.x) (t_near.x, t_far.x) = (t_far.x, t_near.x);
            if (t_near.y > t_far.y) (t_near.y, t_far.y) = (t_far.y, t_near.y);

            // X축과 Y축의 진입/진출 시간 중 가장 늦은 진입 시간과 가장 빠른 진출 시간을 찾음
            float tmin = math.max(t_near.x, t_near.y);
            float tmax = math.min(t_far.x, t_far.y);

            // 광선이 사각형을 비껴가거나, 선분 범위 밖에서 교차하는 경우
            // tmin > tmax: 광선이 사각형을 비껴감
            // tmax < 0: 사각형이 선분의 시작점보다 뒤에 있음
            // tmin > 1: 사각형이 선분의 끝점보다 앞에 있음
            if (tmin > tmax || tmax < 0 || tmin > 1)
            {
                return false;
            }

            // 위의 모든 조건을 통과하면 선분과 사각형은 겹침
            return true;
        }
    }
}
