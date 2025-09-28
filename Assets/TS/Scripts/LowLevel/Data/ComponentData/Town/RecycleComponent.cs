
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

public struct RecycleComponent : IComponentData
{
    public NativeQueue<TSActorComponent> RemoveActors;

    /// <summary>
    /// 큐에 추가합니다.
    /// </summary>
    /// <param name="component"></param>
    public void AddActor(TSActorComponent component)
    {
        RemoveActors.Enqueue(component);
    }

    /// <summary>
    /// 가져온 후 큐에서 제거됩니다.
    /// </summary>
    /// <returns></returns>
    public TSActorComponent GetActor()
    {
        return RemoveActors.Dequeue();
    }

    public int GetActorCount()
    {
        return RemoveActors.Count;
    }
}
