using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Utility
{
    public static class Entities
    {
        public static Entity FindComponentInParents<T>(Entity entity, EntityManager manager, int maxDepth = 10) where T : unmanaged, IComponentData
        {
            var currentEntity = entity;
            int depth = 0;

            while (currentEntity != Entity.Null && depth < maxDepth)
            {
                if (manager.HasComponent<T>(currentEntity))
                {
                    return currentEntity;
                }

                if (manager.HasComponent<Parent>(currentEntity))
                {
                    var parent = manager.GetComponentData<Parent>(currentEntity);
                    currentEntity = parent.Value;
                }
                else
                {
                    break;
                }

                depth++;
            }

            return Entity.Null;
        }
    }
}