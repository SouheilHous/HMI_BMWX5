
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

namespace KHI.Input
{
    [DefaultExecutionOrder(-1000)] // we want to get Touch inputs before other update scripts run
    public class EnhancedTouchInput : MonoBehaviour
    {
        public enum TouchMode { None, OneFingerDrag, TwoFingerPinch, TwoFingerDrag, ThreeFingerDrag }
        public enum MouseMode { None, LeftButtonDrag, ScrollWheel, MiddleButtonDrag, RightButtonDrag }

        [field: SerializeField]
        public TouchMode TouchInputMode { get; private set; } = TouchMode.None;

        [field: SerializeField]
        public Vector2 TouchCurrentValue { get; private set; } = Vector2.zero;

        [field: SerializeField]
        public MouseMode MouseInputMode { get; private set; } = MouseMode.None;

        [field: SerializeField]
        public Vector2 MouseCurrentValue { get; private set; } = Vector2.zero;

        private float lastTwoFingerPinchDistance = 0;

        [SerializeField] bool debug = false;

        public static EnhancedTouchInput Singleton;

        Vector2 screenResolution;

        private void Awake()
        {
            if (Singleton != null)
            {
                Object.Destroy(this);
                return;
            }
            else Singleton = this;
            EnhancedTouchSupport.Enable();

            screenResolution = new Vector2(Screen.width, Screen.height);
        }

        public int GetLastRepeatTouchTapCount()
        {
            if (Touch.activeFingers.Count == 0)
                return 0;

            var lastTouch = Touch.activeFingers[0].lastTouch;

            if (lastTouch.delta.sqrMagnitude > 1 || lastTouch.tapCount <= 1)
            {
                return 1;
            }
            
            var touchHistory = Touch.activeFingers[0].touchHistory;
            var totalDelta = lastTouch.delta;
            
            for (int i = 1; i < touchHistory.Count; i++)
            {
                totalDelta += touchHistory[i].delta;
                var sequenceTime = lastTouch.time - touchHistory[i].time;
                Debug.Log(sequenceTime);
                if (totalDelta.sqrMagnitude > 1 || touchHistory[i].tapCount <= 1 || sequenceTime > 0.3f)
                    return i;
            }

            Debug.LogWarning("Failed to ascertain last repeat touch tap count");
            return 0;
        }

        void GetTouchInputMode()
        {
            // reset values
            TouchCurrentValue = Vector2.zero;

            // get touch mode
            if (Touch.activeFingers.Count == 1)
            {
                TouchInputMode = TouchMode.OneFingerDrag;
                if (Touch.activeTouches[0].phase != TouchPhase.Moved)
                {
                    return;
                }
                TouchCurrentValue = new Vector2(Touch.activeTouches[0].delta.x,
                  Touch.activeTouches[0].delta.y);
                TouchCurrentValue = (TouchCurrentValue /= screenResolution) * Screen.dpi;
            }
            else if (Touch.activeFingers.Count == 2)
            {
                if (Touch.activeTouches[0].phase == TouchPhase.Began ||
                  Touch.activeTouches[1].phase == TouchPhase.Began)
                {
                    // init distance if just started 2 finger touch phase
                    lastTwoFingerPinchDistance = Vector2.Distance(Touch.activeTouches[0].screenPosition / screenResolution,
                      Touch.activeTouches[1].screenPosition / screenResolution);
                }
                if (Touch.activeTouches[0].phase != TouchPhase.Moved ||
                  Touch.activeTouches[1].phase != TouchPhase.Moved)
                {
                    return;
                }
                float twoFingerPinchDistance = Vector2.Distance(Touch.activeTouches[0].screenPosition / screenResolution, Touch.activeTouches[1].screenPosition / screenResolution);
                float pinchDelta = twoFingerPinchDistance - lastTwoFingerPinchDistance;
                lastTwoFingerPinchDistance = twoFingerPinchDistance;

                var dragDelta = new Vector2((Touch.activeTouches[0].delta.x + Touch.activeTouches[1].delta.x) / 2,
                        (Touch.activeTouches[0].delta.y + Touch.activeTouches[1].delta.y) / 2);
                dragDelta /= screenResolution;

                ///Debug.Log($"pinch {Mathf.Abs(pinchDelta / Time.unscaledDeltaTime)}");
                //Debug.Log($"drag {Mathf.Abs(dragDelta.magnitude / Time.unscaledDeltaTime)}");

                if (Mathf.Abs(pinchDelta) > dragDelta.magnitude)
                {
                    // pinching
                    TouchInputMode = TouchMode.TwoFingerPinch;
                    TouchCurrentValue = new Vector2(pinchDelta, 0);
                    TouchCurrentValue *= Screen.dpi;
                }
                else
                {
                    // dragging
                    TouchInputMode = TouchMode.TwoFingerDrag;
                    TouchCurrentValue = dragDelta;
                    TouchCurrentValue *= Screen.dpi;
                }
            }
            else if (Touch.activeFingers.Count == 3)
            {
                TouchInputMode = TouchMode.ThreeFingerDrag;
                if (Touch.activeTouches[0].phase != TouchPhase.Moved && Touch.activeTouches[1].phase != TouchPhase.Moved && Touch.activeTouches[2].phase != TouchPhase.Moved)
                {
                    return;
                }
                TouchCurrentValue = new Vector2((Touch.activeTouches[0].delta.x + Touch.activeTouches[1].delta.x + Touch.activeTouches[2].delta.x) * 0.333f,
                        (Touch.activeTouches[0].delta.y + Touch.activeTouches[1].delta.y + Touch.activeTouches[2].delta.y) * 0.333f);
                TouchCurrentValue = (TouchCurrentValue /= screenResolution) * Screen.dpi;
            }
            else if (Touch.activeFingers.Count == 5)
            {
                // enable and disable debug mode with 4 fingers
                if (Touch.activeTouches[4].phase == TouchPhase.Began) debug = !debug;
                TouchInputMode = TouchMode.None;
            }
            else TouchInputMode = TouchMode.None;
        }

        void GetMouseInputMode()
        {

            // reset values
            MouseCurrentValue = Vector2.zero;

            // get mouse mode
            if (TouchInputMode != TouchMode.None || Mouse.current == null)
            {
                // we want to disable mouse movement if no mouse connect, or touch input in progress
                MouseCurrentValue = Vector2.zero;
                MouseInputMode = MouseMode.None;
                return;
            }

            if (Mouse.current.leftButton.isPressed)
            {
                MouseCurrentValue = (Mouse.current.delta.ReadValue() / screenResolution) * Screen.dpi;
                MouseInputMode = MouseMode.LeftButtonDrag;
            }
            else if (Mouse.current.middleButton.isPressed)
            {
                MouseCurrentValue = (Mouse.current.delta.ReadValue() / screenResolution) * Screen.dpi;
                MouseInputMode = MouseMode.MiddleButtonDrag;
            }
            else if (Mouse.current.rightButton.isPressed)
            {
                MouseCurrentValue = (Mouse.current.delta.ReadValue() / screenResolution) * Screen.dpi;
                MouseInputMode = MouseMode.RightButtonDrag;
            }
            else if (Mathf.Abs(Mouse.current.scroll.ReadValue().y) > 0)
            {
                MouseCurrentValue = new Vector2(Mouse.current.scroll.ReadValue().y / 120, 0);
                MouseInputMode = MouseMode.ScrollWheel;
            }
            else MouseInputMode = MouseMode.None;

#if !PLATFORM_STANDALONE_WIN
                // Windows seems to scale this value down, so on other platforms we need to adjust
                MouseCurrentValue *= 10;
#endif
        }

        void Update()
        {
#if UNITY_EDITOR
            // allows us to switch screen sizes in the editor and the code will react
            screenResolution = new Vector2(Screen.width, Screen.height);
#endif
            GetTouchInputMode();
            GetMouseInputMode();
        }

#region Debug Info
        Vector2 oneFingerDragDebug;
        Vector2 twoFingerDragDebug;
        Vector2 threeFingerDragDebug;
        Vector2 twoFingerPinchDebug;

        void OnGUI()
        {
            if (!debug) return;

            GUI.contentColor = new Color(1,0,0,0.5f);
            GUI.skin.label.fontSize = 40;
            GUI.skin.textField.fontSize = 40;

            GUILayout.Label($"Touch Mode : {TouchInputMode}\n{TouchCurrentValue / Time.unscaledDeltaTime}");
            GUILayout.Label($"Mouse Mode : {MouseInputMode}\n{MouseCurrentValue / Time.unscaledDeltaTime}");
            switch (TouchInputMode)
            {
                case TouchMode.None:
                    break;
                case TouchMode.OneFingerDrag:
                    oneFingerDragDebug += TouchCurrentValue;
                    break;
                case TouchMode.TwoFingerPinch:
                    twoFingerPinchDebug += TouchCurrentValue;
                    break;
                case TouchMode.TwoFingerDrag:
                    twoFingerDragDebug += TouchCurrentValue;
                    break;
                case TouchMode.ThreeFingerDrag:
                    threeFingerDragDebug += TouchCurrentValue;
                    break;
                default:
                    break;
            }

            GUILayout.Label($"oneFingerDragDebug : {oneFingerDragDebug}");
            GUILayout.Label($"twoFingerPinchDebug : {twoFingerPinchDebug}");
            GUILayout.Label($"twoFingerDragDebug : {twoFingerDragDebug}");
            GUILayout.Label($"threeFingerDragDebug : {threeFingerDragDebug}");
            GUILayout.Label($"Press 5 fingers to hide this");
        }

#endregion
    }

}

