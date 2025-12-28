using System.Collections.Generic;
using UnityEngine;

public class LadderResizingAddon : MonoBehaviour
{
    [SerializeField][Range(2, 10)] private int _height;
    [SerializeField] private Transform _ladderParent;
    [SerializeField] private GameObject _top;
    [SerializeField] private GameObject _middle;
    [SerializeField] private GameObject _bottom;

    public void SetHeight(int height)
    {
        _height = height;
    }

    public Transform GetParent()
    {
        return _ladderParent ? _ladderParent : transform;
    }

    [ContextMenu("Initialize")]
    public void Initialize()
    {
        SetReadyInitialze(out var middleObjects);

        ActiveLadderObjects(middleObjects);
    }

    private void SetReadyInitialze(out Queue<GameObject> middleObjects)
    {
        middleObjects = new Queue<GameObject>();

        var parent = GetParent();

        for (int i = 0; i < parent.childCount; i++)
        {
            var obj = parent.GetChild(i).gameObject;
            obj.SetActive(false);

            if (_top != null && obj == _top)
                continue;
            else if (_bottom != null && obj == _bottom)
                continue;

            middleObjects.Enqueue(obj);
        }
    }

    private void ActiveLadderObjects(Queue<GameObject> middleObjects)
    {
        if (_top == null || _middle == null || _bottom == null)
        {
            this.DebugLogError("Initialize Failed. Target is null.");
            return;
        }

        int value = _height / 2;
        int startIndex = -value;
        int endIndex = value;
        bool isEven = _height % 2 == 0;

        if (isEven) endIndex--;

        for (int index = startIndex; index <= endIndex; index++)
        {
            Vector2 pos = new Vector2(0, index);

            if (isEven)
                pos.y += 0.5f;

            if (index == startIndex)
            {
                _bottom.transform.localPosition = pos;
                _bottom.SetActive(true);
            }
            else if (index == endIndex)
            {
                _top.transform.localPosition = pos;
                _top.SetActive(true);
            }
            else
            {
                GameObject middleObject = null;

                if (middleObjects != null && middleObjects.Count > 0)
                    middleObject = middleObjects.Dequeue();
                else
                    middleObject = Instantiate(_middle, GetParent());

                middleObject.transform.localPosition = pos;
                middleObject.SetActive(true);
            }
        }

        _top.transform.SetSiblingIndex(0);
        _bottom.transform.SetSiblingIndex(_height - 1);
    }
}
