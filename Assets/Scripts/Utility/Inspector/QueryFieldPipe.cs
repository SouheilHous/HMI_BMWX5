using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace KHI.Utility.Inspector
{
    public class QueryFieldPipe : MonoBehaviour
    {
        [Serializable]
        public class PipeToStringEvent : UnityEvent<string>
        {
        }

        [Serializable]
        public struct TargetField
        {
            public Component Comp;
            public string FieldName;
        }

        public bool queryOnStart;
        public PipeToStringEvent onReceiveValue;

        private void Start()
        {
            if (queryOnStart)
            {
                // Do something
            }
        }
    }

}