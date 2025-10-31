// Assets/TS/Scripts/HighLevel/System/Common/SelectionVisualSystem.cs
using Unity.Entities;

[UpdateInGroup(typeof(PresentationSystemGroup))]
public partial class SelectionVisualSystem : SystemBase
{
    protected override void OnUpdate()
    {
        // Managed Component는 SystemAPI.ManagedAPI를 통해 접근
        foreach (var (selection, entity) in SystemAPI.Query<RefRO<SelectionComponent>>().WithEntityAccess())
        {
            if (SystemAPI.ManagedAPI.HasComponent<SelectVisualComponent>(entity))
            {
                var visual = SystemAPI.ManagedAPI.GetComponent<SelectVisualComponent>(entity);

                visual.SelectVisual.SetActive(selection.ValueRO.IsSelected);
            }
        }
    }
}
