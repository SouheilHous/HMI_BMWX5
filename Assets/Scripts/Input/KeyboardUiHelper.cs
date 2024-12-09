using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace KHI.Input
{
    public class KeyboardUiHelper : MonoBehaviour
    {
        // implementation to use tab for selecting next ui control

        void Update()
        {
            if (Keyboard.current.tabKey.wasPressedThisFrame)
            {
                GameObject c = EventSystem.current.currentSelectedGameObject;
                if (c == null) return;

                Selectable s = c.GetComponent<Selectable>();
                if (s == null) return;

                Selectable jump = Keyboard.current.shiftKey.isPressed ? s.FindSelectableOnUp() : s.FindSelectableOnDown();

                // try similar direction
                if (!jump)
                {
                    jump = Keyboard.current.shiftKey.isPressed ? s.FindSelectableOnLeft() : s.FindSelectableOnRight();
                    if (!jump) return;
                }

                jump.Select();
            }
        }
    }
}


