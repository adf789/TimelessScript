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
            return bounds1.min.x < bounds2.max.x && bounds1.max.x > bounds2.min.x &&
                   bounds1.min.y < bounds2.max.y && bounds1.max.y > bounds2.min.y;
        }

        /// <summary>
        /// Collision
        /// </summary>
        public static float2 GetSeparationVector(in ColliderBoundsComponent bounds1, in ColliderBoundsComponent bounds2)
        {
            float2 separation = float2.zero;

            float overlapX = math.min(bounds1.max.x, bounds2.max.x) -
                            math.max(bounds1.min.x, bounds2.min.x);
            float overlapY = math.min(bounds1.max.y, bounds2.max.y) -
                            math.max(bounds1.min.y, bounds2.min.y);

            if (overlapX < overlapY)
            {
                // X축으로 분리
                separation.x = bounds1.center.x < bounds2.center.x ? -overlapX : overlapX;
            }
            else
            {
                // Y축으로 분리
                separation.y = bounds1.center.y < bounds2.center.y ? -overlapY : overlapY;
            }

            return separation;
        }

        /// <summary>
        /// Physics
        /// </summary>
        public static void AddForce(ref LightweightPhysicsComponent physics, float2 force)
        {
            physics.velocity += force / physics.mass;
        }

        /// <summary>
        /// Physics
        /// </summary>
        public static void SetVelocity(ref LightweightPhysicsComponent physics, float2 velocity)
        {
            physics.velocity = velocity;
        }
    }
}