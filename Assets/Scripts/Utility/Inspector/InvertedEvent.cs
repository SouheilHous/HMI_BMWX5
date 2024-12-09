using UnityEngine;
using UnityEngine.Events;

namespace KHI.Utility.Inspector
{
    public class InvertedEvent : MonoBehaviour
    {
        public UnityEvent<bool> onReceiveValue;

        public void Passthrough(bool value)
        {
            onReceiveValue?.Invoke(!value);
        }
    }
}

