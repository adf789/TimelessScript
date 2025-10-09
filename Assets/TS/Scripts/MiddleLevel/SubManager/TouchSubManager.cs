
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

public class TouchSubManager : SubBaseManager<TouchSubManager>
{
    public bool IsMobile => isMobile;

#if UNITY_EDITOR
    private bool isMobile => CheckDeviceSimulator();

    private bool CheckDeviceSimulator()
    {
        // Device Simulator가 활성화되면 Touchscreen이 활성화됨
        return Touchscreen.current != null;
    }
#elif UNITY_ANDROID || UNITY_IOS
    private bool isMobile = true;
#else
    private bool isMobile = false;
#endif

    public bool CheckTouchDown() => !isMobile
            ? Mouse.current != null &&
              (Mouse.current.leftButton.wasPressedThisFrame || Mouse.current.rightButton.wasPressedThisFrame)
            : Touchscreen.current?.touches.Count > 0 && Touchscreen.current.touches[0].press.wasPressedThisFrame;

    public bool CheckTouchUp() => !isMobile
        ? Mouse.current != null && (Mouse.current.leftButton.wasReleasedThisFrame ||
                                    Mouse.current.rightButton.wasReleasedThisFrame)
        : Touchscreen.current?.touches.Count > 0 && Touchscreen.current.touches[0].press.wasReleasedThisFrame;

    public float2 GetTouchPosition() => !isMobile
        ? Mouse.current?.position.ReadValue() ?? float2.zero
        : Touchscreen.current?.touches.Count > 0
            ? Touchscreen.current.touches[0].position.ReadValue()
            : float2.zero;

    public Vector2 GetTouchPositionVector()
    {
        var position = GetTouchPosition();

        return new Vector2(position.x, position.y);
    }

    public Vector3 GetScreenTouchPosition(Camera camera)
    => camera ? camera.ScreenToWorldPoint(GetTouchPositionVector()) : Vector3.zero;

    public bool CheckTouchWithinScreen()
    {
        float2 pos = GetTouchPosition();

        return pos.x >= 0 && pos.x <= Screen.width && pos.y >= 0 && pos.y <= Screen.height;
    }
}
