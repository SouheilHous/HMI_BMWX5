using System;
using System.ComponentModel.Design.Serialization;
using System.Linq;
using KHI.Camera;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using KHI.Utility;
using Unity.VisualScripting;
using UnityEditor;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

namespace KHI.Input
{ 
    [RequireComponent(typeof(UnityEngine.Camera))]
    [RequireComponent(typeof(BlendedMatrixCamera))]
    public class OrbitalCameraController : MonoBehaviour
    {
        [System.Serializable]
        public enum Movements
        {
            None = 0,
            PanSpherical = 1,
            PanOrthogonal = 2,
            LookAround = 4,
            Zoom = 8
        }
        
        [SerializeField] float zoomDistLevelMin = 0.25f;
        [SerializeField] float zoomDistLevelMax = 12f;
        [SerializeField] [Range(-1f, 1f)]
        float xOrthoScalar = -0.01f;
        [SerializeField] [Range(-1f, 1f)]
        float yOrthoScalar = -0.0075f;
        
        [SerializeField] [Range(-1f, 1f)]
        float xAngleScalar = 0.6f;
        [SerializeField] [Range(-1f, 1f)]
        float yAngleScalar = -0.4f;
        // Confusingly, in the euler angles, "x" is vertical rotation.
        
        [SerializeField] [Range(50f, 90f)]
        float yAngularMax = 80f;
        [SerializeField] [Range(-25f, 25f)]
        float yAngularMin = -25f;

        [SerializeField] [Range(0, 1)] 
        float pinchDetectionThreshold = 0.9f;
        
        [SerializeField] AnimationCurve dragResistanceCurve;
        [SerializeField] GameObject reticlePrefab;

        [Header("Simple Binds")]
        [SerializeField] Movements[] mouseActions = new Movements[3];

        [SerializeField] Movements[] touchActions = new Movements[3];
        

        BlendedMatrixCamera blendCamera;
        Transform reticle;
        Interpolator pivotInterpolator;
        Interpolator cameraInterpolator;

        float orthographicSizeToCameraDistanceRatio;

        string inputDebug = "None";
        float dotDebug = 0;
        float maxDeltaMagnitude = 0;

        
        
        public Transform Pivot
        {
            get => blendCamera.pivot;
            set
            {
                transform.parent = value;
                reticle.parent = value;
                reticle.localPosition = Vector3.zero;
                blendCamera.pivot = value;
            }
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            if (mouseActions == null || mouseActions.Length != 3)
                mouseActions = new Movements[3];
            if (touchActions == null || touchActions.Length != 3)
                touchActions = new Movements[3];
            
            if (dragResistanceCurve != null && dragResistanceCurve.keys.Length >= 7)
            {
                dragResistanceCurve.keys[0].value = 0;
                dragResistanceCurve.keys[4].value = 0;
            }
            else
            {
                dragResistanceCurve = AnimationCurve.EaseInOut(0, 0, 1, 0);
                dragResistanceCurve.AddKey(0.02f, 0.5f);
                dragResistanceCurve.AddKey(0.1f, 1f);
                dragResistanceCurve.AddKey(0.5f, 1f);
                dragResistanceCurve.AddKey(0.9f, 1f);
                dragResistanceCurve.AddKey(0.98f, 0.5f);

                AnimationUtility.SetKeyRightTangentMode(dragResistanceCurve, 1, AnimationUtility.TangentMode.ClampedAuto);
                AnimationUtility.SetKeyRightTangentMode(dragResistanceCurve, 2, AnimationUtility.TangentMode.Constant);
                AnimationUtility.SetKeyLeftTangentMode(dragResistanceCurve, 5, AnimationUtility.TangentMode.ClampedAuto);
                AnimationUtility.SetKeyLeftTangentMode(dragResistanceCurve, 4, AnimationUtility.TangentMode.Constant);
            }
        }
#endif

        void Start()
        {
            EnhancedTouchSupport.Enable();
            blendCamera = GetComponent<BlendedMatrixCamera>();
            pivotInterpolator = blendCamera.pivot.GetOrAddComponent<Interpolator>();
            cameraInterpolator = blendCamera.GetOrAddComponent<Interpolator>();
            
            transform.localEulerAngles = Vector3.zero;
            orthographicSizeToCameraDistanceRatio = blendCamera.transform.localPosition.z / blendCamera.ZoomDistanceLevel;
            
            
            
            if (reticle == null)
            {
                if (reticlePrefab == null)
                {
                    var standin = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    standin.transform.localScale = Vector3.one * 0.15f;
                    reticle = standin.transform;
                }
                else
                {
                    var prefab = GameObject.Instantiate(reticlePrefab);
                    reticle = prefab.transform;
                }
                reticle.name = "[Camera Reticle]";
            }
            
            if (transform.parent is null)
            {
                var pivotGameObject = new GameObject("[Camera Pivot]");
                Pivot = pivotGameObject.transform;
            }
            else
            {
                Pivot = transform.parent;
            }
            
            Pivot.position = Vector3.up;
            Debug.Assert(Pivot != this.transform);
        }
        
        void OnGUI()
        {
            //GUI.Label(new Rect(10, 10, 300, 20), $"Event = {eventDebug}");
            GUI.Label(new Rect(10, 30, 300, 20), $"Input = {inputDebug}");
            //GUI.Label(new Rect(10, 50, 300, 20), $"Touch = {currentTouchCount}");
            GUI.Label(new Rect(10, 70, 300, 20), $"Dot   = {Screen.width} {Screen.height}");
            GUI.Label(new Rect(10, 90, 300, 20), $"DeltaM= {Pointer.current.position.ReadValue()}");
            //GUI.Label(new Rect(10, 110, 400, 60), $"Pos   = {currentTouchLocations[0]}\n        {currentTouchLocations[1]}\n        {currentTouchLocations[2]}");
        }

        void OnEnable()
        {
            //inputMaps.Enable();
        }

        void OnDisable()
        {
            //inputMaps.Disable();
        }

        void Update()
        {
            var delta = Vector2.zero;
            var moveAction = Movements.None;

            // Ensure pointer is within the screen
            var pointerPos = Pointer.current.position.ReadValue();
            if (pointerPos.x < 0 || pointerPos.x > Screen.width ||
                pointerPos.y < 0 || pointerPos.y > Screen.height)
                return;

            // Set delta based on touch input
            if (TryGetMovementsByTouch(out moveAction))
            {
                if (moveAction == Movements.Zoom)
                {
                    // Pinch distance calculation for zoom
                    var (tA, tB) = (Touch.activeTouches[0], Touch.activeTouches[1]);

                    var startDistance = Vector2.Distance(tA.startScreenPosition, tB.startScreenPosition);
                    var currentDistance = Vector2.Distance(
                        tA.startScreenPosition + tA.delta,
                        tB.startScreenPosition + tB.delta);

                    delta = new Vector2(0, currentDistance - startDistance);
                }
                else if (moveAction == Movements.PanOrthogonal)
                {
                    delta = (Touch.activeTouches[0].delta + Touch.activeTouches[1].delta) * 0.5f; // Average of both touch deltas
                }
                else
                {
                    delta = Touch.activeTouches[0].delta;
                }
            }

            else if (TryGetMovementsByMouse(out moveAction))
            {
                if (moveAction == Movements.Zoom)
                {
                    delta = new Vector2(0, Mouse.current.scroll.y.ReadValue());
                }
                else
                {
                    delta = Mouse.current.delta.ReadValue();
                }
            }

            inputDebug = moveAction.ToString();
            PerformMovement(moveAction, delta * (Time.deltaTime * 100));
        }

        bool TryGetMovementsByTouch(out Movements movements)
        {
            movements = Movements.None;
            var fingers = Mathf.Min(Touch.activeFingers.Count, 2); // Limit to two fingers

            if (fingers <= 0)
                return false;

            // One-finger drag for spherical rotation
            if (fingers == 1 && Touch.activeFingers[0].currentTouch.phase == TouchPhase.Moved)
            {
                movements = Movements.PanSpherical;
                return true;
            }

            if (fingers == 2)
            {
                var tA = Touch.activeTouches[0];
                var tB = Touch.activeTouches[1];

                var primaryDirection = tA.delta.normalized;
                var secondaryDirection = tB.delta.normalized;

                var dot = Vector2.Dot(primaryDirection, secondaryDirection);
                dotDebug = dot;

                if (dot > pinchDetectionThreshold) // Two fingers moving in the same direction for panning
                {
                    movements = Movements.PanOrthogonal;
                }
                else if (dot < -pinchDetectionThreshold) // Two fingers moving apart or together for zooming
                {
                    movements = Movements.Zoom;
                }

                return true;
            }

            return false;
        }


        bool TryGetMovementsByMouse(out Movements movements)
        {
            movements = Movements.None;

            if (Mouse.current.middleButton.isPressed)
            {
                movements = mouseActions[2];
            }
            else if (Mouse.current.rightButton.isPressed)
            {
                movements = mouseActions[1];
            }
            else if (Mouse.current.leftButton.isPressed)
            {
                movements = mouseActions[0];
            }
            else if (Mouse.current.scroll.y.IsActuated())
            {
                movements = Movements.Zoom;
            }

            return movements != Movements.None;
        }

        void PerformMovement(Movements move, Vector2 delta)
        {
            switch (move)
            {
                case Movements.None:
                    EnableReticle(false);
                    break;
                case Movements.PanSpherical:
                    PanCameraSpherical(delta);
                    EnableReticle(true);

                    break;
                case Movements.PanOrthogonal:
                    PanCameraOrthogonal(delta);
                    EnableReticle(true);
                    break;
                case Movements.LookAround:
                    TiltCamera(delta);
                    EnableReticle(true);
                    break;
                case Movements.Zoom:
                    ZoomCamera(delta.y);
                    EnableReticle(true);

                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(move), move, null);
            }
        }

        public void TiltCamera(Vector2 dragDelta)
        {
            var scaledDelta = dragDelta;// * blendCamera.OrthographicSize * 0.1f;
            var cameraPosition = transform.position;
            
            // Separate tilt directions for conveniences' sake
            if (Mathf.Abs(scaledDelta.y) > Mathf.Abs(scaledDelta.x))
            {
                var prevState = new TransformState(Pivot);
                Pivot.RotateAround(cameraPosition, Pivot.right, scaledDelta.y * yAngleScalar);
                // If this goes over the angle limit, apply resistance
                var resultAngle = Pivot.localEulerAngles.x;
                var resistanceScalar = GetVerticalAngleResistanceScalar(resultAngle, prevState.Rotation.eulerAngles.x);
                if (resistanceScalar < 1f)
                {
                    prevState.ApplyTo(Pivot); // Undo change
                    Pivot.RotateAround(cameraPosition, Pivot.right, scaledDelta.y * yAngleScalar * resistanceScalar);
                }
            }
            else
            {
                Pivot.RotateAround(cameraPosition, Vector3.up, scaledDelta.x * xAngleScalar);
            }
            
            transform.LookAt(Pivot, Vector3.up);
        }

        public void PanCameraOrthogonal(Vector2 dragDelta)
        {
            var scaledDelta = dragDelta * blendCamera.ZoomDistanceLevel;
            var upTranslation = (transform.up * scaledDelta.y * yOrthoScalar)/ blendCamera.ZoomDistanceLevel;
            var rightTranslation = (transform.right * scaledDelta.x * xOrthoScalar) / blendCamera.ZoomDistanceLevel;
            var nextPivotLocation = Pivot.position + upTranslation + rightTranslation;
            Pivot.position = nextPivotLocation;
        }

        /// <summary>
        /// Convert drag delta into rotation around the pivot
        /// </summary>
        public void PanCameraSpherical(Vector2 dragDelta)
        {
            var pivotAngles = Pivot.localEulerAngles;

            var xNext = pivotAngles.x + dragDelta.y * yAngleScalar;
            var resistanceScalar = GetVerticalAngleResistanceScalar(xNext, pivotAngles.x);
            if (resistanceScalar < 1f)
            {
                // Recalculate, applying resistance to delta.y
                xNext = pivotAngles.x + dragDelta.y * yAngleScalar * resistanceScalar;
            }
            
            var yNext = pivotAngles.y + dragDelta.x * xAngleScalar;

            Pivot.localEulerAngles = new Vector3(xNext, yNext, 0f);
        }

        public void ZoomCamera(float delta)
        {
            // Zooming requires special resistance handling as scroll wheels can give a wide variety of values.
            var scaledDelta = delta * xOrthoScalar;
            var nextZoom = blendCamera.ZoomDistanceLevel + scaledDelta;
            var t = Mathf.InverseLerp(zoomDistLevelMin, zoomDistLevelMax, nextZoom);
            var resistanceScalar = dragResistanceCurve.Evaluate(t);
           
            // Determine if initial resistance should be applied
            if (resistanceScalar < 1 &&
                ((t < 0.5 && nextZoom < blendCamera.ZoomDistanceLevel)
                 || (t > 0.5 && nextZoom > blendCamera.ZoomDistanceLevel)))
            {
                // Try again with half delta
                scaledDelta = delta * xOrthoScalar * 0.5f;
                nextZoom = blendCamera.ZoomDistanceLevel + scaledDelta;
                t = Mathf.InverseLerp(zoomDistLevelMin, zoomDistLevelMax, nextZoom);
                resistanceScalar = dragResistanceCurve.Evaluate(t);
            }

            nextZoom = blendCamera.ZoomDistanceLevel + scaledDelta * resistanceScalar;
           
            
            if (nextZoom < zoomDistLevelMin)
            {
                blendCamera.ZoomDistanceLevel = zoomDistLevelMin;
            }
            else if (nextZoom > zoomDistLevelMax)
            {
                blendCamera.ZoomDistanceLevel = zoomDistLevelMax;
            }
            else
            {
                blendCamera.ZoomDistanceLevel = nextZoom;
            }

            blendCamera.ZoomDistanceLevel = nextZoom;
        }

        float GetVerticalAngleResistanceScalar(float yExpected, float yPrevious)
        {
            if (yExpected > 180)
            {
                // Negative angles are generally not supported
                yExpected -= 360;
                yPrevious -= 360;
            }

            // Evaluate animation curve to get resistance level
            var t = Mathf.InverseLerp(yAngularMin, yAngularMax, yExpected);
            var scalar = dragResistanceCurve.Evaluate(t);
            
            // Determine whether resistance should be applied
            if (scalar < 1)
            {
                if (t < 0.5f && yExpected < yPrevious)
                    return scalar;
                if (t > 0.5f && yExpected > yPrevious)
                    return scalar;
            }

            // Return no resistance
            return 1;
        }

        void EnableReticle(bool ActiveState)
        {

            Vector2 inputPosition;
            bool isActive = false;
            reticle.gameObject.SetActive(ActiveState);
            if (ActiveState == false)
                return;
                // Check for touch input
                if (Touch.activeTouches.Count > 0)
            {
                inputPosition = Touch.activeTouches[0].screenPosition; // First touch position
                isActive = true;
            }
            // Check for mouse input
            else if (Mouse.current.leftButton.isPressed || Mouse.current.rightButton.isPressed || Mouse.current.middleButton.isPressed)
            {
                inputPosition = Mouse.current.position.ReadValue(); // Mouse position
                isActive = true;
            }
            else
            {
                // Default input position when no input is detected
                inputPosition = Pointer.current.position.ReadValue();
                isActive = false;
            }

            // Set the reticle's active state
            reticle.gameObject.SetActive(isActive);

            // If active, update its position
            if (isActive)
            {
                Ray ray = blendCamera.GetComponent<UnityEngine.Camera>().ScreenPointToRay(inputPosition);

                // Update the reticle's position
                if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity))
                {
                    reticle.position = hit.point; // Place at the hit point on a collider
                }
                else
                {
                    reticle.position = ray.GetPoint(5.0f); // Place 5 units in front of the camera if no collider is hit
                }
            }
        }

        #region Interface Implementations for Callback Assignment
        /*void UpdateTouchCount(int touchEventValue, InputAction.CallbackContext context)
        {
            if (context.started && !isInputBlockedByUgui)
            {
                eventDebug++;
                // Prevent Windows from sending Touchscreen events as Mouse clicks
                if (!Touch.activeTouches.Any() || context.control.device is Touchscreen)
                {
                    currentTouchCount = Mathf.Max(currentTouchCount, touchEventValue);    
                }
            }
            else if (context.performed)
            {
                currentTouchLocations[touchEventValue-1] = context.ReadValue<Vector2>();
            }
            else if (context.canceled)
            {
                if (context.control.device is Touchscreen)
                    currentTouchCount--;
                else
                    currentTouchCount = 0;
            }
        }
        
        void CameraInputs.ITouchAndMouseActions.OnTouchOne(InputAction.CallbackContext context)
        {
            UpdateTouchCount(1, context);
            EnableReticle(!context.canceled);
        }

        void CameraInputs.ITouchAndMouseActions.OnTouchTwo(InputAction.CallbackContext context)
        {
            UpdateTouchCount(2, context);
        }

        void CameraInputs.ITouchAndMouseActions.OnTouchThree(InputAction.CallbackContext context)
        {
            UpdateTouchCount(3, context);
            EnableReticle(!context.canceled);
        }

        void CameraInputs.ITouchAndMouseActions.OnPrimaryDrag(InputAction.CallbackContext context)
        {
            if (!context.performed || currentTouchCount == 0)
                return;

            var primaryDelta = context.ReadValue<Vector2>();
            currentPrimaryDelta = primaryDelta;
            maxDeltaMagnitude = Mathf.Max(maxDeltaMagnitude, currentPrimaryDelta.magnitude);
            
            switch (currentTouchCount)
            {
                case 1:
                    TiltCamera(primaryDelta);
                    inputDebug = "Tilt";
                    break;
                case 2:
                    PanCameraSpherical(primaryDelta);
                    inputDebug = "PanSpherical";
                    break;
                case 3:
                    PanCameraOrthogonal(primaryDelta);
                    inputDebug = "PanOrthogonal";
                    break;
            }
        }

        void CameraInputs.ITouchAndMouseActions.OnSecondaryDrag(InputAction.CallbackContext context)
        {
            if (!context.performed || currentTouchCount != 2)
                return;

            var secondaryDelta = context.ReadValue<Vector2>();

            // Filter out insignificant movement
            if (currentPrimaryDelta.sqrMagnitude < 1 || secondaryDelta.sqrMagnitude < 1)
                return;
            
            var sync = Vector2.Dot(currentPrimaryDelta.normalized, secondaryDelta.normalized);
            dotDebug = sync;
        }

        void CameraInputs.ITouchAndMouseActions.OnZoom(InputAction.CallbackContext context)
        {
            if (!context.performed)
                return;
            
            ZoomCamera(context.ReadValue<float>());
        }*/

        #endregion
    }
}
