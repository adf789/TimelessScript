using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(Camera))]
public class PickingAddon : MonoBehaviour
{

    #region Coding rule : Property

    public bool PickingLock => pickingLock;
    #endregion Coding rule : Property

    #region Coding rule : Value

    [SerializeField, Header("터치 딜레이")] private float touchDelay = 0.1f;
    [SerializeField] private Camera pickingCamera;
    [SerializeField] private PixelPerfectCamera pixelPerfectCamera;
    [SerializeField] private PickedAddon dragRange;

    [Header("Non-Pixel zoom")]
    [SerializeField] private float zoomSpeed = 10.0f;
    [SerializeField] private float minSize = 10.0f;
    [SerializeField] private float maxSize = 25.0f;

    [Header("Pixel zoom")]
    [SerializeField] private float pixelZoomSpeed = 10000.0f;
    [SerializeField] private float pixelMinSize = 10.0f;
    [SerializeField] private float pixelMaxSize = 17.0f;

    private MouseInputSystem mouseInputSystem;
    private float prevPinchDistance;
    private float scrollY;
    private float touchTime;

    private readonly LinkedList<PickedAddon> pickObjects = new();
    private List<PickedAddon> prevOverlapObjects = new();
    private Vector2 prevPosition;

    private bool isPickStart;
    private bool isDragStart;
    private const float DragThreshold = 5f;

    private bool pickingLock = false;
    private bool cameraZoomLock = false;
    #endregion Coding rule : Value

    #region Coding rule : Function

    private void Awake()
    {
        if (pickingCamera == null)
            pickingCamera = GetComponent<Camera>();

        if (pixelPerfectCamera == null)
            pixelPerfectCamera = pickingCamera.GetComponent<PixelPerfectCamera>();
    }

    private void Start()
    {
        InputSystemInit();

        if (!pickingCamera.orthographic)
            enabled = false;
    }

    private void OnDestroy()
    {
        mouseInputSystem?.Dispose();
    }

    private void Update()
    {
        if (!pickingLock)
            CheckPicking();

        if (!pickingLock && !cameraZoomLock && TouchSubManager.Instance.CheckTouchWithinScreen())
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
        if (!TouchSubManager.Instance.IsMobile)
        {
            mouseInputSystem = new MouseInputSystem();
            mouseInputSystem.Enable();

            scrollY = 0;
            ZoomPC();
            mouseInputSystem.Mouse.MouseScrollY.performed += x =>
            {
                if (pickingLock || cameraZoomLock)
                    return;

                if (!TouchSubManager.Instance.CheckTouchWithinScreen())
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
        if (TouchSubManager.Instance.CheckTouchDown()
        && TouchSubManager.Instance.CheckTouchWithinScreen())
        {
            if (touchTime + touchDelay > Time.realtimeSinceStartup)
                return;

            touchTime = Time.realtimeSinceStartup;
            OnTouchStart();
            return;
        }

        if (!isPickStart) return;

        if (TouchSubManager.Instance.CheckTouchUp())
            OnTouchStop();
        else if (prevPosition != TouchSubManager.Instance.GetTouchPositionVector())
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
        prevPosition = TouchSubManager.Instance.GetTouchPosition();

        foreach (var pickObject in pickObjects)
            pickObject.OnEventPointDown(pickingData);

        isPickStart = true;
    }

    private void OnDrag()
    {
        Vector2 screenTouchPos = GetScreenTouchPosition();
        Vector2 currentPos = TouchSubManager.Instance.GetTouchPosition();
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
        Vector2 screenTouchPos = GetScreenTouchPosition();
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

    private List<PickedAddon> GetPickTargets(Vector2 position)
    {
        var targets = new List<PickedAddon>();
        if (EventSystem.current.IsPointerOverGameObject()) return targets;

        foreach (var collider in Physics2D.OverlapPointAll(position))
        {
            if (collider != null && collider.TryGetComponent(out PickedAddon target))
                targets.Add(target);
        }

        return targets;
    }

    private Vector2 GetScreenTouchPosition() => TouchSubManager.Instance.GetScreenTouchPosition(pickingCamera);

    private void ResetPickValues()
    {
        isPickStart = false;
        isDragStart = false;
        prevPosition = Vector3.zero;
        pickObjects.Clear();
        prevOverlapObjects.Clear();
    }

    private void ZoomPC()
    {
        pickObjects.AddLast(dragRange);

        // PixelPerfectCamera 임시 비활성화 (부드러운 줌을 위해)
        bool wasPixelPerfectEnabled = false;
        if (pixelPerfectCamera != null && pixelPerfectCamera.enabled)
        {
            wasPixelPerfectEnabled = true;
            pixelPerfectCamera.enabled = false;
        }

        Vector3 mouseWorldBeforeZoom = TouchSubManager.Instance.GetScreenTouchPosition(pickingCamera);

        if (pixelPerfectCamera == null)
        {
            pickingCamera.orthographicSize = Mathf.Clamp(
                pickingCamera.orthographicSize + scrollY * GetZoomSpeed() * Time.deltaTime,
                GetMinZoom(), GetMaxZoom());
        }
        else
        {
            pixelPerfectCamera.assetsPPU = (int) Mathf.Clamp(
                pixelPerfectCamera.assetsPPU + scrollY * GetZoomSpeed() * Time.deltaTime,
                GetMinZoom(), GetMaxZoom());
        }

        Vector3 mouseWorldAfterZoom = TouchSubManager.Instance.GetScreenTouchPosition(pickingCamera);
        Vector3 camOffset = mouseWorldBeforeZoom - mouseWorldAfterZoom;
        pickingCamera.transform.position += camOffset;

        // PixelPerfectCamera 재활성화
        if (wasPixelPerfectEnabled && pixelPerfectCamera != null)
            pixelPerfectCamera.enabled = true;

        pickObjects.Clear();
    }

    private void ZoomMobile()
    {
        if (!TouchSubManager.Instance.IsMobile)
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

        // PixelPerfectCamera 임시 비활성화 (부드러운 줌을 위해)
        bool wasPixelPerfectEnabled = false;
        if (pixelPerfectCamera != null && pixelPerfectCamera.enabled)
        {
            wasPixelPerfectEnabled = true;
            pixelPerfectCamera.enabled = false;
        }

        Vector3 worldBeforeZoom = pickingCamera.ScreenToWorldPoint(pinchCenter);

        if (pixelPerfectCamera == null)
        {
            pickingCamera.orthographicSize = Mathf.Clamp(
                pickingCamera.orthographicSize + delta * GetZoomSpeed() * Time.deltaTime,
                GetMinZoom(), GetMaxZoom());
        }
        else
        {
            pixelPerfectCamera.assetsPPU = (int) Mathf.Clamp(
                pixelPerfectCamera.assetsPPU + delta * GetZoomSpeed() * Time.deltaTime,
                GetMinZoom(), GetMaxZoom());
        }

        Vector3 worldAfterZoom = pickingCamera.ScreenToWorldPoint(pinchCenter);
        Vector3 camOffset = worldBeforeZoom - worldAfterZoom;
        pickingCamera.transform.position += camOffset;

        // PixelPerfectCamera 재활성화
        if (wasPixelPerfectEnabled && pixelPerfectCamera != null)
            pixelPerfectCamera.enabled = true;
    }

    private float GetZoomSpeed()
    {
        return pixelPerfectCamera == null ? zoomSpeed : pixelZoomSpeed;
    }

    private float GetMinZoom()
    {
        return pixelPerfectCamera == null ? minSize : pixelMinSize;
    }

    private float GetMaxZoom()
    {
        return pixelPerfectCamera == null ? maxSize : pixelMaxSize;
    }
    #endregion Coding rule : Function
}
