using Cysharp.Threading.Tasks;
using UnityEngine;

public class GameManager : BaseManager<GameManager>
{
    [Header("드래그 속력 Test")]
    [SerializeField]
    private float dragSpeed = 0.03f;
    [Header("드래그 타겟")]
    [SerializeField]
    private BoxCollider2D dragRange = null;
    [Header("드래그 패딩(상,하,좌,우)")]
    [SerializeField]
    private Vector4 dragPadding = Vector4.zero;
    [Header("드래그 범위 제한 유무")]
    [SerializeField]
    private bool isLimitDragX = true;
    [SerializeField]
    private bool isLimitDragY = true;

    [Header("Analysis")]
    public AnalysisData AnalysisData;

    private Bounds towerBounds;

    private void Awake()
    {
        DontDestroyOnLoad(Instance);
    }

    private void Start()
    {
        SetDragRange();

        Application.runInBackground = true;

        FlowManager.Instance.ChangeFlow(GameState.Intro).Forget();
    }

    [ContextMenu("Test")]
    public void TestCode()
    {
        FlowManager.Instance.ChangeFlow(GameState.Loading).Forget();
    }

    public void OnDrag(PickingData data)
    {
        OnEventDrag(data.DeltaPosition * dragSpeed);
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
        towerBounds = dragRange.bounds;
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
        float minX = towerBounds.min.x;
        float maxX = towerBounds.max.x;
        float minY = towerBounds.min.y;
        float maxY = towerBounds.max.y;

        if (towerBounds.min.x + camWidth * 0.5f < towerBounds.max.x - camWidth * 0.5f)
        {
            minX = towerBounds.min.x + camWidth * 0.5f;
            maxX = towerBounds.max.x - camWidth * 0.5f;
        }
        else
        {
            minX = towerBounds.center.x;
            maxX = towerBounds.center.x;
        }

        if (towerBounds.min.y + camHeight * 0.5f < towerBounds.max.y - camHeight * 0.5f)
        {
            minY = towerBounds.min.y + camHeight * 0.5f;
            maxY = towerBounds.max.y - camHeight * 0.5f;
        }
        else
        {
            minY = towerBounds.center.y;
            maxY = towerBounds.center.y;
        }

        // 원하는 위치를 제한된 영역 안으로 클램프
        float min = Mathf.Min(minX, maxX);
        float max = Mathf.Max(minX, maxX);

        if (isLimitDragX)
            position.x = Mathf.Clamp(position.x, min, max);

        min = Mathf.Min(minY, maxY);
        max = Mathf.Max(minY, maxY);

        if (isLimitDragY)
            position.y = Mathf.Clamp(position.y, min, max);

        position.z = -10;

        // 카메라 위치 적용
        camTransform.localPosition = position;
    }
}
