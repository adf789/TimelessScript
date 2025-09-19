using Unity.Entities;
using UnityEngine;

public class NavigationAuthoring : MonoBehaviour
{
    [Header("Navigation Settings")]
    public bool EnableNavigation = true;

    public class NavigationBaker : Baker<NavigationAuthoring>
    {
        public override void Bake(NavigationAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            if (authoring.EnableNavigation)
            {
                AddComponent(entity, new NavigationComponent
                {
                    IsActive = false,
                    FinalTargetPosition = default,
                    FinalTargetGround = Entity.Null,
                    CurrentWaypointIndex = 0,
                    State = NavigationState.None
                });

                AddBuffer<NavigationWaypoint>(entity);
            }
        }
    }
}