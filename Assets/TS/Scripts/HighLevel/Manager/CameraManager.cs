using UnityEngine;

public class CameraManager : BaseManager<CameraManager>
{
    [Header("드래그 속력")]
    [SerializeField]
    private float _dragSpeed = 0.03f;
    [Header("드래그 타겟")]
    [SerializeField]
    private BoxCollider2D _dragRange = null;
    [Header("드래그 패딩(상,하,좌,우)")]
    [SerializeField]
    private Vector4 _dragPadding = Vector4.zero;
    [Header("드래그 범위 제한 유무")]
    [SerializeField]
    private bool _isLimitDragX = true;
    [SerializeField]
    private bool _isLimitDragY = true;

    private Bounds _dragBounds;

    private void Start()
    {
        SetDragRange();
    }

    public void OnDrag(PickingData data)
    {
        OnEventDrag(data.DeltaPosition * _dragSpeed);
    }

    private void OnEventDrag(Vector3 deltaPosition)
    {
        Vector3 position = Camera.main.transform.localPosition;

        position.x -= deltaPosition.x;
        position.y -= deltaPosition.y;

        SetFocusPosition(position);
    }

    public void SetDragRange()
    {
        _dragBounds = _dragRange.bounds;
    }

    public void SetFocusPosition(Vector3 position)
    {
        // 카메라 및 BoxCollider2D 참조
        var camera = Camera.main;
        var camTransform = camera.transform;
        var camOrthographicSize = camera.orthographicSize;
        float camAspect = camera.aspect;

        // 카메라 뷰 크기 계산
        float camHeight = camOrthographicSize * 2f;
        float camWidth = camHeight * camAspect;

        // 카메라 뷰 중심이 이동 가능한 최소/최대 위치 계산
        float minX = _dragBounds.min.x;
        float maxX = _dragBounds.max.x;
        float minY = _dragBounds.min.y;
        float maxY = _dragBounds.max.y;

        if (_dragBounds.min.x + camWidth * 0.5f < _dragBounds.max.x - camWidth * 0.5f)
        {
            minX = _dragBounds.min.x + camWidth * 0.5f;
            maxX = _dragBounds.max.x - camWidth * 0.5f;
        }
        else
        {
            minX = _dragBounds.center.x;
            maxX = _dragBounds.center.x;
        }

        if (_dragBounds.min.y + camHeight * 0.5f < _dragBounds.max.y - camHeight * 0.5f)
        {
            minY = _dragBounds.min.y + camHeight * 0.5f;
            maxY = _dragBounds.max.y - camHeight * 0.5f;
        }
        else
        {
            minY = _dragBounds.center.y;
            maxY = _dragBounds.center.y;
        }

        // 원하는 위치를 제한된 영역 안으로 클램프
        float min = Mathf.Min(minX, maxX);
        float max = Mathf.Max(minX, maxX);

        if (_isLimitDragX)
            position.x = Mathf.Clamp(position.x, min, max);

        min = Mathf.Min(minY, maxY);
        max = Mathf.Max(minY, maxY);

        if (_isLimitDragY)
            position.y = Mathf.Clamp(position.y, min, max);

        position.z = -10;

        // 카메라 위치 적용
        camTransform.localPosition = position;
    }
}
