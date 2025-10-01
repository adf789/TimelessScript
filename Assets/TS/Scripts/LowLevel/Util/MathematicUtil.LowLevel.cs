// Assets/TS/Scripts/LowLevel/Utils/GeometryUtils.cs
using System;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Utility
{
    public static class Mathematic
    {
        /// <summary>
        /// 원과 사각형(지형) 사이의 접촉점을 계산하는 메서드
        /// 원이 지형 상단면과 접촉하는 실제 지점들을 모두 찾아서 가장 적절한 점을 반환
        /// </summary>
        public static float2 CalculateCircleRectangleContact(float2 basePosition, float2 circleCenter, float circleRadius, float2 rectMin, float2 rectMax)
        {
            float groundTopY = rectMax.y;

            // 원과 지형 상단면(수평선)의 교점들을 찾기
            var intersections = FindCircleLineIntersections(circleCenter, circleRadius, groundTopY, rectMin.x, rectMax.x);

            if (intersections.Length > 0)
            {
                // 교점이 있으면 원의 중심에서 가장 가까운 교점 반환
                float2 bestPoint = intersections[0];
                float shortestDist = math.distance(basePosition, bestPoint);

                for (int i = 1; i < intersections.Length; i++)
                {
                    float dist = math.distance(basePosition, intersections[i]);
                    if (dist < shortestDist)
                    {
                        shortestDist = dist;
                        bestPoint = intersections[i];
                    }
                }

                return bestPoint;
            }

            // 접촉하지 않는 경우
            return new float2(float.NaN, float.NaN);
        }

        /// <summary>
        /// 원과 수평선의 교점들을 찾는 메서드
        /// </summary>
        public static NativeList<float2> FindCircleLineIntersections(float2 circleCenter, float circleRadius, float lineY, float lineMinX, float lineMaxX)
        {
            var intersections = new NativeList<float2>(2, Unity.Collections.Allocator.Temp);

            // 원의 방정식: (x - cx)² + (y - cy)² = r²
            // 수평선: y = lineY
            // 교점을 구하기 위해 y = lineY를 원의 방정식에 대입

            float dy = lineY - circleCenter.y;
            float discriminant = circleRadius * circleRadius - dy * dy;

            // 판별식이 음수면 교점 없음
            if (discriminant < 0)
            {
                return intersections;
            }

            // 교점의 X 좌표들 계산
            float sqrtDiscriminant = math.sqrt(discriminant);
            float x1 = circleCenter.x - sqrtDiscriminant;
            float x2 = circleCenter.x + sqrtDiscriminant;

            // 교점이 지형의 X 범위 내에 있는지 확인
            if (x1 >= lineMinX && x1 <= lineMaxX)
            {
                intersections.Add(new float2(x1, lineY));
            }

            if (x2 >= lineMinX && x2 <= lineMaxX && math.abs(x2 - x1) > 0.001f)
            {
                intersections.Add(new float2(x2, lineY));
            }

            return intersections;
        }
    }
}
