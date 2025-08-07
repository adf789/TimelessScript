using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;

[RequireComponent(typeof(Camera))]
public class PickingSupport : MonoBehaviour
{

    #region Coding rule : Property

    public bool PickingLock => pickingLock;
    #endregion Coding rule : Property

    #region Coding rule : Value

    [SerializeField, Header("터치 딜레이")] private float touchDelay = 0.1f;
    [SerializeField] private Camera pickingCamera;
    [SerializeField] private PickedSupport dragRange;
    [SerializeField] private float zoomSpeed = 1.0f;
    [SerializeField] private float minSize = 65.0f;
    [SerializeField] private float maxSize = 130.0f;

    private MouseInputSystem mouseInputSystem;
    private float prevPinchDistance;
    private float scrollY;
    private float touchTime;

    private readonly LinkedList<PickedSupport> pickObjects = new();
    private SortedSet<PickedSupport> prevOverlapObjects = new();
    private Vector3 prevPosition;

    private bool isPickStart;
    private bool isDragStart;
    private bool isMobile;
    private const float DragThreshold = 5f;

    private bool pickingLock = false;
    private bool cameraZoomLock = false;
    #endregion Coding rule : Value

    #region Coding rule : Function

    private void Awake()
    {
       
    }

    private void Start()
    {
        InputSystemInit();
        
        if (!pickingCamera.orthographic)
            enabled = false;
    }

    private void OnEnable()
    {
#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_STANDALONE_WIN
        isMobile = false;
#else
        isMobile = true;
#endif
    }

    private void OnDestroy()
    {
        mouseInputSystem?.Dispose();
    }

    private void Update()
    {
        if(!pickingLock)
            CheckPicking();

        if (!pickingLock && !cameraZoomLock && CheckTouchWithinScreen())
            ZoomMobile();
    }

    public void SetPickingLock(bool isBool)
    {
        pickingLock = isBool;
    }
    
    public void SetCameraZoomLock(bool isBool)
    {
        cameraZoomLock = isBool;
    }
    
    private void InputSystemInit()
    {
        if (!isMobile)
        { 
            mouseInputSystem = new MouseInputSystem();
            mouseInputSystem.Enable();

            scrollY = 0;
            ZoomPC();
            mouseInputSystem.Mouse.MouseScrollY.performed += x =>
            {
                if (pickingLock || cameraZoomLock)
                    return;

                if (!CheckTouchWithinScreen())
                    return;
                
                scrollY = x.ReadValue<float>() * 0.02f;
                ZoomPC();
            };
        }
        else
        {
            if (!EnhancedTouchSupport.enabled)
                EnhancedTouchSupport.Enable();
        }
    }

    private void CheckPicking()
    {
        if (CheckTouchDown() && CheckTouchWithinScreen())
        {
            if (touchTime + touchDelay > Time.realtimeSinceStartup)
                return;

            touchTime = Time.realtimeSinceStartup;
            OnTouchStart();
            return;
        }

        if (!isPickStart) return;

        if (CheckTouchUp())
            OnTouchStop();
        else if (prevPosition != GetTouchPosition())
            OnDrag();
    }

    private void OnTouchStart()
    {
        Vector3 screenTouchPos = GetScreenTouchPosition();
        var pickTargets = GetPickTargets(screenTouchPos);

        foreach (var pickTarget in pickTargets)
        {
            pickObjects.AddLast(pickTarget);
            if (pickTarget.IsIgnoreLowOrder) break;
        }

        var pickingData = new PickingData(screenTouchPos, Vector3.zero);
        prevPosition = GetTouchPosition();

        foreach (var pickObject in pickObjects)
            pickObject.OnEventPointDown(pickingData);

        isPickStart = true;
    }

    private void OnDrag()
    {
        Vector3 screenTouchPos = GetScreenTouchPosition();
        Vector3 currentPos = GetTouchPosition();
        var pickingData = new PickingData(screenTouchPos, currentPos - prevPosition);

        foreach (var pickObject in pickObjects)
            pickObject.OnEventDrag(pickingData);

        if (pickObjects.Count > 0)
            HandleDragEnterExit(screenTouchPos, pickingData);

        if (!isDragStart && Vector3.Distance(currentPos, prevPosition) >= DragThreshold)
            isDragStart = true;

        prevPosition = currentPos;
    }

    private void HandleDragEnterExit(Vector3 position, PickingData pickingData)
    {
        var newTargets = GetPickTargets(position);

        foreach (var target in newTargets)
        {
            if (!prevOverlapObjects.Remove(target))
                target.OnEventDragEnter(pickingData);
        }

        foreach (var exited in prevOverlapObjects)
            exited.OnEventDragExit(pickingData);

        prevOverlapObjects = newTargets;
    }

    private void OnTouchStop()
    {
        Vector3 screenTouchPos = GetScreenTouchPosition();
        var upTargets = GetPickTargets(screenTouchPos);
        var pickingData = new PickingData(screenTouchPos, screenTouchPos - prevPosition);
        bool isCallClickEvent = false;

        foreach (var pickObject in pickObjects)
        {
            pickObject.OnEventPointUp(pickingData);

            if (!isDragStart && upTargets.Contains(pickObject))
                isCallClickEvent |= pickObject.OnEventClick(pickingData);
        }

        ResetPickValues();
    }

    private SortedSet<PickedSupport> GetPickTargets(Vector3 position)
    {
        var targets = new SortedSet<PickedSupport>();
        if (EventSystem.current.IsPointerOverGameObject()) return targets;

        foreach (var collider in Physics2D.OverlapPointAll(position))
        {
            if (collider != null && collider.TryGetComponent(out PickedSupport target))
                targets.Add(target);
        }

        return targets;
    }

    private Vector3 GetScreenTouchPosition() => pickingCamera.ScreenToWorldPoint(GetTouchPosition());

    private void ResetPickValues()
    {
        isPickStart = false;
        isDragStart = false;
        prevPosition = Vector3.zero;
        pickObjects.Clear();
        prevOverlapObjects.Clear();
    }

    private bool CheckTouchDown() => !isMobile
        ? Mouse.current != null &&
          (Mouse.current.leftButton.wasPressedThisFrame || Mouse.current.rightButton.wasPressedThisFrame)
        : Touchscreen.current?.touches.Count > 0 && Touchscreen.current.touches[0].press.wasPressedThisFrame;

    private bool CheckTouchUp() => !isMobile
        ? Mouse.current != null && (Mouse.current.leftButton.wasReleasedThisFrame ||
                                    Mouse.current.rightButton.wasReleasedThisFrame)
        : Touchscreen.current?.touches.Count > 0 && Touchscreen.current.touches[0].press.wasReleasedThisFrame;

    private Vector3 GetTouchPosition() => !isMobile
        ? Mouse.current?.position.ReadValue() ?? Vector2.zero
        : Touchscreen.current?.touches.Count > 0
            ? Touchscreen.current.touches[0].position.ReadValue()
            : Vector2.zero;

    private bool CheckTouchWithinScreen()
    {
        Vector3 pos = GetTouchPosition();

        return pos.x >= 0 && pos.x <= Screen.width && pos.y >= 0 && pos.y <= Screen.height;
    }
    
    private void ZoomPC()
    {
        pickObjects.AddLast(dragRange);
      
        Vector3 mouseWorldBeforeZoom = pickingCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue());

        pickingCamera.orthographicSize = Mathf.Clamp(
            pickingCamera.orthographicSize - scrollY * zoomSpeed,
            minSize, maxSize);

        Vector3 mouseWorldAfterZoom = pickingCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        Vector3 camOffset = mouseWorldBeforeZoom - mouseWorldAfterZoom;
        pickingCamera.transform.position += camOffset;
        
        OnDrag();
        pickObjects.Clear();
    }

    private void ZoomMobile()
    {
        if (!isMobile)
            return;

// 현재 활성화된 터치만 필터링
        var activeTouches = UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches;
        if (activeTouches.Count < 2)
            return;

        var t1 = activeTouches[0];
        var t2 = activeTouches[1];

        Vector2 p1 = t1.screenPosition;
        Vector2 p2 = t2.screenPosition;
        Vector2 pinchCenter = (p1 + p2) / 2f;

        float currentDist = Vector2.Distance(p1, p2);

// 터치 시작 시 초기 거리 기록
        if (t1.phase == UnityEngine.InputSystem.TouchPhase.Began ||
            t2.phase == UnityEngine.InputSystem.TouchPhase.Began)
        {
            prevPinchDistance = currentDist;
            return;
        }

        float delta = currentDist - prevPinchDistance;
        prevPinchDistance = currentDist;

        Vector3 worldBeforeZoom = pickingCamera.ScreenToWorldPoint(pinchCenter);

        pickingCamera.orthographicSize = Mathf.Clamp(
            pickingCamera.orthographicSize - delta * zoomSpeed * Time.deltaTime,
            minSize, maxSize);

        Vector3 worldAfterZoom = pickingCamera.ScreenToWorldPoint(pinchCenter);
        Vector3 camOffset = worldBeforeZoom - worldAfterZoom;
        pickingCamera.transform.position += camOffset;
    }
#endregion Coding rule : Function
}
