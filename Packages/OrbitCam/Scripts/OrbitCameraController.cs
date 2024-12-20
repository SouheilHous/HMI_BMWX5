﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

public class OrbitCameraController : MonoBehaviour
{
    private Camera cameraComponent;

    public RotationSettings rotationSettings;
    public ZoomSettings zoomSettings;
    public MovementSettings movementSettings;

    private Vector3 CurrentRotation { get { return transform.eulerAngles; } }
    private Vector3 keyboardInput { get { return new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical")); } }
    private bool IsMouseDown { get { return Input.GetMouseButton((int)rotationSettings.rotationButton); } }
    private Vector2 rotationSpeed;
    private Vector2 oldMousePosition;

    private float normalizedTargetZoom = .5f;

    private float AbsoluteTargetZoom { get { return Mathf.Lerp(zoomSettings.zoomRange.x, zoomSettings.zoomRange.y, normalizedTargetZoom); } set { normalizedTargetZoom = Mathf.InverseLerp(zoomSettings.zoomRange.x, zoomSettings.zoomRange.y, value); } }

    public static bool IsInteractingWithPicker = false; // Static flag to track interaction



    private enum CameraControllerState
    {
        Free,
        MovRot,
        Pan,
        Zoom,
        TouchRotate,
        TouchZoom,
        TouchPan
    }

    private CameraControllerState state;


    public void OnEnable()
    {
        EnhancedTouchSupport.Enable();
        cameraComponent = GetComponentInChildren<Camera>();
        if (!cameraComponent)
        {
            enabled = false;
            Debug.LogError("No camera found!");
        }
        if (cameraComponent.transform == transform)
        {
            enabled = false;
            Debug.LogError("Camera component needs to be on a child GameObject");
        }
    }

    public void OnDisable()
    {
        EnhancedTouchSupport.Disable();
    }

    private bool TryGetTouchInput(out CameraControllerState touchState)
    {
        touchState = CameraControllerState.Free;

        var fingers = Mathf.Min(Touch.activeFingers.Count, 2);

        if (fingers == 1)
        {
            if (Touch.activeTouches[0].phase == UnityEngine.InputSystem.TouchPhase.Moved)
            {
                touchState = CameraControllerState.TouchRotate;
                return true;
            }
        }
        else if (fingers == 2)
        {
            var tA = Touch.activeTouches[0];
            var tB = Touch.activeTouches[1];

            var primaryDirection = tA.delta.normalized;
            var secondaryDirection = tB.delta.normalized;

            var dot = Vector2.Dot(primaryDirection, secondaryDirection);

            if (dot > 0.9f) // Two fingers moving in the same direction for panning
            {
                touchState = CameraControllerState.TouchPan;
            }
            else if (dot < -0.9f) // Two fingers moving apart or together for zooming
            {
                touchState = CameraControllerState.TouchZoom;
            }
            return true;
        }

        return false;
    }


    public void Update()
    {

        if (IsInteractingWithPicker)
            return;

        UpdateState();

        switch (state)
        {
            case CameraControllerState.Free:
                DoMouseWheelZoom();
                DoKeyboardMovement();
                break;
            case CameraControllerState.MovRot:
                DoKeyboardMovement();
                DoMouseWheelZoom();
                DoMouseRotation();
                break;
            case CameraControllerState.Pan:
                if (IsMouseDown)
                    DoMousePan();
                break;
            case CameraControllerState.Zoom:
                if (IsMouseDown)
                    DoMouseZoom();
                break;
            case CameraControllerState.TouchRotate:
                DoTouchRotate();
                break;
            case CameraControllerState.TouchZoom:
                DoTouchZoom();
                break;
            case CameraControllerState.TouchPan:
               
                    DoTouchPan();

                break;
        }

        UpdateRotation();
        UpdateControllerHeight();
        UpdateZoom();
        DecreaseRotationSpeed();
        oldMousePosition = Input.mousePosition;
    }
    private void UpdateState()
    {
        if (TryGetTouchInput(out CameraControllerState touchState))
        {
            state = touchState;
            return;
        }

        bool panControlKey = Input.GetKey(KeyCode.LeftShift);
        bool zoomControlKey = Input.GetKey(KeyCode.LeftControl);
        bool movementInput = keyboardInput.sqrMagnitude > .01f;
        bool anyControlKeyInput = panControlKey || zoomControlKey;
        bool onlyMouseInput = IsMouseDown && !anyControlKeyInput;
        bool anyInput = IsMouseDown || anyControlKeyInput;

        switch (state)
        {
            case CameraControllerState.MovRot:
                if (movementInput || panControlKey || IsMouseDown)
                    return;
                break;
            case CameraControllerState.Free:
                if (movementInput || onlyMouseInput)
                {
                    state = CameraControllerState.MovRot;
                    return;
                }
                if (panControlKey && IsMouseDown)
                {
                    state = CameraControllerState.Pan;
                    return;
                }
                if (zoomControlKey && IsMouseDown)
                {
                    state = CameraControllerState.Zoom;
                    return;
                }
                break;
            default:
                if (anyInput)
                    return;
                break;
        }
        state = CameraControllerState.Free;
    }


    private void UpdateControllerHeight()
    {
        Vector3 desiredPosition = transform.position;
        Vector3 finalPosition = desiredPosition;

        //match surface height
        if (movementSettings.surfaceFollowType != MovementSettings.SurfaceFollowType.None)
        {
            if (Physics.Raycast(desiredPosition + Vector3.up * movementSettings.surfaceCheckRange, Vector3.down, out RaycastHit hit, movementSettings.surfaceCheckRange * 2f, movementSettings.groundMask))
            {
                switch (movementSettings.collisionDetection)
                {
                    case MovementSettings.CollisionDetectionMethod.None:
                        finalPosition = hit.point;
                        break;
                    case MovementSettings.CollisionDetectionMethod.SweepTest:
                        bool hitBackfaces = Physics.queriesHitBackfaces;
                        Physics.queriesHitBackfaces = true;
                        desiredPosition += Vector3.up * 0.0005f;
                        if (Physics.RaycastAll(desiredPosition, Vector3.up, movementSettings.surfaceCheckRange, movementSettings.groundMask).Length % 2 == 1)
                        {
                            Physics.queriesHitBackfaces = false;
                            float upperHitDistance = movementSettings.surfaceCheckRange;
                            if (Physics.Raycast(desiredPosition, Vector3.down, out hit, movementSettings.surfaceCheckRange, movementSettings.groundMask))
                            {
                                finalPosition = hit.point;
                                upperHitDistance = hit.distance;
                            }
                            Physics.queriesHitBackfaces = true;
                            if (Physics.Raycast(desiredPosition, Vector3.up, out hit, movementSettings.surfaceCheckRange, movementSettings.groundMask))
                                if (hit.distance < upperHitDistance)
                                    finalPosition = hit.point;
                        }
                        else if (Physics.Raycast(desiredPosition, Vector3.down, out hit, movementSettings.surfaceCheckRange, movementSettings.groundMask))
                            finalPosition = hit.point;
                        Physics.queriesHitBackfaces = hitBackfaces;
                        break;
                }
            }
            switch (movementSettings.surfaceFollowType)
            {
                case MovementSettings.SurfaceFollowType.MatchSurfaceInstant:
                    desiredPosition = finalPosition;
                    break;
                case MovementSettings.SurfaceFollowType.MatchSurfaceSmooth:
                    desiredPosition = Vector3.Lerp(desiredPosition, finalPosition, 1 - Mathf.Pow(movementSettings.smoothness * movementSettings.smoothness * .02f, Time.deltaTime));
                    break;
            }
        }
        transform.position = desiredPosition;
    }

    private void DoTouchRotate()
    {
        if (Touch.activeTouches.Count == 1)
        {
            Vector2 touchDelta = Touch.activeTouches[0].delta;
            rotationSpeed = new Vector2(-touchDelta.y, touchDelta.x) * rotationSettings.rotationSensitivity * 0.01f;
        }
    }
    private void DoTouchZoom()
    {
        if (Touch.activeTouches.Count == 2)
        {
            var tA = Touch.activeTouches[0];
            var tB = Touch.activeTouches[1];

            float currentDistance = Vector2.Distance(tA.screenPosition, tB.screenPosition);
            float previousDistance = Vector2.Distance(tA.startScreenPosition, tB.startScreenPosition);

            float zoomInput = (currentDistance - previousDistance) * zoomSettings.zoomSensitivity * 0.001f;
            AbsoluteTargetZoom = Mathf.Clamp(AbsoluteTargetZoom - zoomInput, zoomSettings.zoomRange.x, zoomSettings.zoomRange.y);
        }
    }
    private void DoTouchPan()
    {
        if (Touch.activeTouches.Count == 2)
        {
            // Calculate the average movement delta between two fingers
            Vector2 averageDelta = (Touch.activeTouches[0].delta + Touch.activeTouches[1].delta) * 0.5f;

            // Convert the average delta to world space movement
            Vector3 rightMovement = transform.right * averageDelta.x * movementSettings.movementSpeed * Time.deltaTime;
            Vector3 upMovement = transform.up * averageDelta.y * movementSettings.movementSpeed * Time.deltaTime;

            // Apply the movement to the camera's position
            transform.position -= rightMovement + upMovement;
        }
    }



    private void DoKeyboardMovement()
    {
        float zoomBasedSpeed = Mathf.Tan(cameraComponent.fieldOfView / 360f * 3.141f) * AbsoluteTargetZoom;
        zoomBasedSpeed = Mathf.Pow(zoomBasedSpeed, .75f);
        float speed = movementSettings.movementSpeed * (Input.GetKey(KeyCode.LeftShift) ? movementSettings.sprintSpeedMultiplier : 1) * Time.deltaTime * zoomBasedSpeed;
        Vector3 movementInput = Quaternion.Euler(0, CurrentRotation.y, 0) * new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical"));
        transform.position += movementInput * speed;
    }

    private void DoMousePan()
    {
        Vector2 startMousePosition = oldMousePosition;
        Vector2 endMousePosition = Input.mousePosition;
        Vector3 deltaPosition = ScreenPointToWorldXZPlane(transform.position.y, endMousePosition) - ScreenPointToWorldXZPlane(transform.position.y, startMousePosition);
        deltaPosition = Vector3.ClampMagnitude(deltaPosition, zoomSettings.zoomRange.y * zoomSettings.zoomRange.y * Time.deltaTime);
        transform.position -= deltaPosition;
    }

    private Vector3 ScreenPointToWorldXZPlane(float worldHeight, Vector3 screenPoint)
    {
        Ray ray = this.cameraComponent.ScreenPointToRay(screenPoint);
        float t = (ray.origin.y - worldHeight) / -ray.direction.y;
        return ray.origin + t * ray.direction;
    }

    private void DoMouseZoom()
    {
        float zoomInput = Input.GetAxis("Mouse Y") * zoomSettings.zoomSensitivity / Screen.height * 18f;
        AbsoluteTargetZoom = Mathf.Lerp(AbsoluteTargetZoom, AbsoluteTargetZoom - zoomInput * (zoomSettings.zoomRange.y - zoomSettings.zoomRange.x), .3f);
    }

    private void DoMouseWheelZoom()
    {
        float zoomInput = Input.GetAxis("Mouse ScrollWheel") * zoomSettings.zoomSensitivity;
        AbsoluteTargetZoom = Mathf.Lerp(AbsoluteTargetZoom, AbsoluteTargetZoom - zoomInput * (zoomSettings.zoomRange.y - zoomSettings.zoomRange.x), .3f);
    }

    private void DoMouseRotation()
    {
        if (!IsMouseDown)
            return;
        Vector2 rotationInput = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")) * rotationSettings.rotationSensitivity;
        Vector2 desiredRotationSpeed;
        desiredRotationSpeed.x = -rotationInput.y;
        desiredRotationSpeed.y = rotationInput.x;
        desiredRotationSpeed /= 10f;
        if (rotationSettings.easingBehaviour == RotationSettings.RotationEasing.Always)
            rotationSpeed = Vector2.Lerp(rotationSpeed, desiredRotationSpeed, 1 - Mathf.Pow(rotationSettings.smoothness * rotationSettings.smoothness * .2f / 100f, Time.deltaTime));
        else
            rotationSpeed = desiredRotationSpeed;
    }

    private void UpdateRotation()
    {
        float rX = CurrentRotation.x > 180 ? CurrentRotation.x - 360 : CurrentRotation.x;
        rX += rotationSpeed.x;
        if (rotationSettings.constrainX)
            rX = Mathf.Clamp(rX, rotationSettings.rotationConstraintsX.x, rotationSettings.rotationConstraintsX.y);

        float rY = CurrentRotation.y + rotationSpeed.y;
        if (rotationSettings.constrainY)
            rY = Mathf.Clamp(rY, rotationSettings.rotationConstraintsY.x, rotationSettings.rotationConstraintsY.y);
        transform.rotation = Quaternion.Euler(rX, rY, CurrentRotation.z);
    }

    private void DecreaseRotationSpeed()
    {
        if (rotationSettings.easingBehaviour != RotationSettings.RotationEasing.None)
        {
            rotationSpeed = Vector2.Lerp(rotationSpeed, Vector2.zero, 1 - Mathf.Pow(rotationSettings.smoothness * rotationSettings.smoothness * .1f / 100f, Time.deltaTime));
        }
        else rotationSpeed = Vector2.zero;
    }

    private void UpdateZoom()
    {
        AbsoluteTargetZoom = Mathf.Lerp(zoomSettings.zoomRange.x, zoomSettings.zoomRange.y, normalizedTargetZoom);
        switch (zoomSettings.collisionDetection)
        {
            case ZoomSettings.CollisionDetectionMethod.None:
                break;

            case ZoomSettings.CollisionDetectionMethod.SweepTest:
                //sweep test, if number of intersections is odd, camera is inside mesh
                bool hitBackfaces = Physics.queriesHitBackfaces;
                Physics.queriesHitBackfaces = true;
                if (Physics.RaycastAll(transform.position - AbsoluteTargetZoom * transform.forward, Vector3.up, 1000f, zoomSettings.collisionLayerMask).Length % 2 == 1)
                {
                    if (Physics.Raycast(transform.position - AbsoluteTargetZoom * transform.forward, transform.forward, out RaycastHit backfaceHit, AbsoluteTargetZoom + 0.05f, zoomSettings.collisionLayerMask))
                        AbsoluteTargetZoom = Vector3.Distance(backfaceHit.point, transform.position) - .05f;
                }
                Physics.queriesHitBackfaces = hitBackfaces;
                break;

            case ZoomSettings.CollisionDetectionMethod.RaycastFromCenter:
                if (Physics.Raycast(transform.position, -transform.forward, out RaycastHit hit, AbsoluteTargetZoom, zoomSettings.collisionLayerMask))
                {
                    AbsoluteTargetZoom = Vector3.Distance(hit.point, transform.position) - .05f;
                }
                break;
        }
        cameraComponent.transform.localPosition = Vector3.Lerp(cameraComponent.transform.localPosition, Vector3.back * AbsoluteTargetZoom, .3f);
        //normalizedTargetZoom = Mathf.InverseLerp(zoomSettings.zoomRange.x, zoomSettings.zoomRange.y, absoluteTargetZoom);
    }

}

[System.Serializable]
public class MovementSettings
{
    [Tooltip("Base speed of the controller. Set to '0' to disable movement")]
    public float movementSpeed = 3f;

    [Tooltip("Modifies speed when holding 'left-shift'")]
    public float sprintSpeedMultiplier = 3f;

    [Tooltip("When disabled, constraints the controller to move only on collider surfaces")]
    public bool allowFlight = false;

    public enum SurfaceFollowType { None, MatchSurfaceInstant, MatchSurfaceSmooth }
    [Tooltip("Controls how the controller follows surface heights")]
    public SurfaceFollowType surfaceFollowType = SurfaceFollowType.MatchSurfaceSmooth;

    public enum CollisionDetectionMethod { None, SweepTest }
    [Tooltip("When should the controller update the current target height?")]
    public CollisionDetectionMethod collisionDetection = CollisionDetectionMethod.SweepTest;

    [Tooltip("Maximum height difference the controller checks for new surface collisions at")]
    public float surfaceCheckRange = 50f;

    [Tooltip("Layer mask containing only the ground layer(s)")]
    public LayerMask groundMask = 1;

    [Tooltip("Delay with which the controller follows the surface if MatchSurfaceSmooth is active")]
    public float smoothness = 1f;
}

[System.Serializable]
public class RotationSettings
{
    public enum RotationEasing { None, Always, Subtle }
    [Tooltip("Controls, how the rotation input is smoothed")]
    public RotationEasing easingBehaviour = RotationEasing.Subtle;
    public enum MouseButton { Left = 0, Right = 1, Middle = 2 }
    [Tooltip("Determines, what mouse button is responsible for rotating this camera")]
    public MouseButton rotationButton = MouseButton.Middle;
    [Tooltip("Speeds up the rotation")]
    public float rotationSensitivity = 24f;
    [Tooltip("The amount of smoothing applied to the rotation input")]
    public float smoothness = 1f;
    [Tooltip("When enabled, constraints the rotation on the X axis (vertical rotation)")]
    public bool constrainX;
    [Tooltip("When enabled, constraints the rotation on the Y axis (horizontal rotation)")]
    public bool constrainY;
    [Tooltip("Lower and upper rotation angle limit")]
    public Vector2 rotationConstraintsX, rotationConstraintsY;
}



[System.Serializable]
public class ZoomSettings
{
    [Tooltip("Minimum and maximum distance of the camera to its orbit center")]
    public Vector2 zoomRange = new Vector2(1f, 15f);
    [Tooltip("Dynamicaly zooms in, to provide the camera from clipping inside of geometry")]
    public bool autoZoomIn = true;
    public enum CollisionDetectionMethod { None, RaycastFromCenter, SweepTest }
    [Tooltip("How should the controller determine, whether the camera is inside geometry or not?")]
    public CollisionDetectionMethod collisionDetection = CollisionDetectionMethod.SweepTest;
    [Tooltip("Speeds up zooming in and out")]
    public float zoomSensitivity = 4f;
    [Tooltip("Layermask used to determine, whether the camera is inside geometry or not")]
    public LayerMask collisionLayerMask = 1;

}