using Cysharp.Threading.Tasks;
using System;
using UnityEngine;
using UnityEngine.Events;

public class PickedAddon : MonoBehaviour, IComparable<PickedAddon>
{
    public int PickOrder { get => pickOrder; }
    public bool IsIgnoreLowOrder { get => isIgnoreLowOrder; }
    public bool IsCallHoldEvent { get; private set; }

    [Header("터치 우선 순위(높을 수록 우선)")]
    [SerializeField]
    private int pickOrder;

    [Header("우선 순위 아래는 무시")]
    [SerializeField]
    private bool isIgnoreLowOrder;

    [Header("버튼 다운")]
    [SerializeField]
    private UnityEvent<PickingData> onEventPointDown = null;

    [Header("버튼 업")]
    [SerializeField]
    private UnityEvent<PickingData> onEventPointUp = null;

    [Header("클릭 간 딜레이")]
    [SerializeField]
    private int clickDelayMilliSeconds = 1000;
    [Header("클릭")]
    [SerializeField]
    private UnityEvent<PickingData> onEventClick = null;

    [Header("홀드 유지 시간")]
    [SerializeField]
    private int holdDelayMilliSeconds = 500;
    [Header("클릭")]
    [SerializeField]
    private UnityEvent<PickingData> onEventHold = null;

    [Header("드래그 중 들어올 때")]
    [SerializeField]
    private UnityEvent<PickingData> onEventDragEnter = null;

    [Header("드래그할 때")]
    [SerializeField]
    private UnityEvent<PickingData> onEventDrag = null;

    [Header("드래그 중 나갈 때")]
    [SerializeField]
    private UnityEvent<PickingData> onEventDragExit = null;

    private bool isClickDelay;
    private bool isPointDown;

    public bool OnEventPointDown(PickingData data)
    {
        isPointDown = true;
        IsCallHoldEvent = false;

        if (HasHoldEvent())
            CheckHold(data).Forget();

        if (onEventPointDown == null)
            return false;

        if (onEventPointDown.GetPersistentEventCount() == 0)
            return false;

        onEventPointDown.Invoke(data);
        return true;
    }

    public bool OnEventPointUp(PickingData data)
    {
        isPointDown = false;

        StopHold();

        if (onEventPointUp == null)
            return false;

        if (onEventPointUp.GetPersistentEventCount() == 0)
            return false;

        onEventPointUp.Invoke(data);
        return true;
    }

    public bool OnEventClick(PickingData data)
    {
        if (isClickDelay || IsCallHoldEvent)
            return false;

        if (onEventClick == null)
            return false;

        if (onEventClick.GetPersistentEventCount() == 0)
            return false;

        onEventClick.Invoke(data);

        SetDisableDelay().Forget();

        return true;
    }

    public bool OnEventHold(PickingData data)
    {
        if (onEventHold == null)
            return false;

        if (onEventHold.GetPersistentEventCount() == 0)
            return false;

        onEventHold.Invoke(data);

        return true;
    }

    public bool OnEventDragEnter(PickingData data)
    {
        if (onEventDragEnter == null)
            return false;

        if (onEventDragEnter.GetPersistentEventCount() == 0)
            return false;

        onEventDragEnter.Invoke(data);
        return true;
    }

    public bool OnEventDrag(PickingData data)
    {
        if (onEventDrag == null)
            return false;

        if (onEventDrag.GetPersistentEventCount() == 0)
            return false;

        onEventDrag.Invoke(data);
        return true;
    }

    public bool OnEventDragExit(PickingData data)
    {
        if (onEventDragExit == null)
            return false;

        if (onEventDragExit.GetPersistentEventCount() == 0)
            return false;

        onEventDragExit.Invoke(data);
        return true;
    }

    public int CompareTo(PickedAddon other)
    {
        int value = other.pickOrder.CompareTo(pickOrder);

        if (value != 0)
            return value;

        return isIgnoreLowOrder.CompareTo(other.isIgnoreLowOrder);
    }

    public void ResetClickDelay()
    {
        TokenPool.Cancel(GetHashCode());

        isClickDelay = false;
    }

    private async UniTask SetDisableDelay()
    {
        if (clickDelayMilliSeconds <= 0)
            return;

        isClickDelay = true;

        if (await UniTask.Delay(clickDelayMilliSeconds, cancellationToken: TokenPool.Get(GetHashCode())).SuppressCancellationThrow())
            return;

        if (this)
            isClickDelay = false;
    }

    private bool HasHoldEvent()
    {
        if (onEventHold == null)
            return false;

        if (onEventHold.GetPersistentEventCount() == 0)
            return false;

        return true;
    }

    private async UniTask CheckHold(PickingData data)
    {
        float holdDelay = 0;

        while (isPointDown)
        {
            if (await UniTask.NextFrame(cancellationToken: TokenPool.Get(GetHashCode())).SuppressCancellationThrow())
                return;

            if (!this)
                return;

            holdDelay += Time.deltaTime * IntDefine.TIME_MILLISECONDS_ONE;

            if (holdDelay >= holdDelayMilliSeconds)
            {
                IsCallHoldEvent = OnEventHold(data);
                return;
            }
        }
    }

    private void StopHold()
    {
        TokenPool.Cancel(GetHashCode());
    }
}
