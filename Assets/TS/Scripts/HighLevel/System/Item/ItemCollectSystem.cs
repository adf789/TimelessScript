
using Unity.Burst;
using Unity.Entities;
using UnityEngine;

public partial class ItemCollectSystem : SystemBase
{
    protected override void OnCreate()
    {
        
    }

    protected override void OnUpdate()
    {
        if(SystemAPI.TryGetSingleton(out CollectorComponent collector))
        {
            for(int i = 0; i < collector.InteractCollector.Length; i++)
            {
                Debug.Log($"Get Interaction: ({collector.InteractCollector[i].DataID},{collector.InteractCollector[i].DataType.ToString()})");
            }
        }
    }
}
