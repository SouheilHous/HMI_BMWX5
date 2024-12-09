using System;
using UnityEngine;

namespace KHI.Utility
{
    public class FaceCamera : MonoBehaviour
    {
        [SerializeField] private Vector3 offset = Vector3.zero;
        [SerializeField] private bool useLookAt;
        
        private void Update()
        {
            if (!Application.isPlaying)
                return;
            if (useLookAt)
            {
                transform.LookAt(UnityEngine.Camera.main.transform);
                return;
            }
            transform.rotation = UnityEngine.Camera.main.transform.rotation;
            transform.localPosition = offset;
        }
    }
}
