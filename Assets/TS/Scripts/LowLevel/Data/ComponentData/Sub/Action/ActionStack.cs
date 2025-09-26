using Unity.Collections;

public struct ActionStack
{
    public int InteractCount => Interacts.Count;
    public NativeQueue<InteractAction> Interacts;

    public void AddInteract(uint dataId, TableDataType dataType)
    {
        Interacts.Enqueue(new InteractAction()
        {
            DataID = dataId,
            DataType = dataType
        });
    }

    public InteractAction RemoveInteract()
    {
        if (InteractCount == 0)
            return default;

        return Interacts.Dequeue();
    }
}
