using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace KHI.Utility.Inspector
{
    public class BehaviourTrigger : MonoBehaviour
    {
        [Serializable]
        public class BehaviourEvent : UnityEvent<BehaviourTrigger>
        {}
        
        [Serializable]
        public class Entry
        {
            /// <summary>
            /// The MonoBehaviour action that prompted this event.
            /// </summary>
            public BehaviourTriggerType eventID = BehaviourTriggerType.Awake;
    
            /// <summary>
            /// The desired callback to be Invoked.
            /// </summary>
            public BehaviourEvent callback = new BehaviourEvent();
        }
    
        [SerializeField]
        private List<Entry> _delegates;
        
        public List<Entry> Delegates
        {
            get
            {
                if (_delegates == null)
                    _delegates = new List<Entry>();
                // Null _executeEveryFrame cached value if accessed publicly; someone might be adding/removing an entry
                _executesEveryFrame = null;
                return _delegates;
            }
            set
            {
                _delegates = value;
                _executesEveryFrame = null;
            }
        }
    
        private bool? _executesEveryFrame;
    
        public bool IsExecutingEveryFrame
        {
            get
            {
                if (!_executesEveryFrame.HasValue)
                {
                    _executesEveryFrame = Delegates.Any(e => e.eventID >= BehaviourTriggerType.Update);
                }

                return _executesEveryFrame.Value;
            }
        }
        
        private void Execute(BehaviourTriggerType id)
        {
            for (int i = 0, imax = _delegates.Count; i < imax; ++i)
            {
                var ent = _delegates[i];
                if (ent.eventID == id && ent.callback != null)
                    ent.callback.Invoke(this);
            }
        }
    
        private void Awake()
        {
            Execute(BehaviourTriggerType.Awake);
        }
    
        private void Start()
        {
            Execute(BehaviourTriggerType.Start);
        }
    
        private void OnEnable()
        {
            Execute(BehaviourTriggerType.OnEnable);
        }
    
        private void OnDisable()
        {
            Execute(BehaviourTriggerType.OnDisable);
        }
    
        private void Update()
        {
            if (!IsExecutingEveryFrame)
                return;
            
            Execute(BehaviourTriggerType.Update);
        }
    
        private void LateUpdate()
        {
            if (!IsExecutingEveryFrame)
                return;
            
            Execute(BehaviourTriggerType.LateUpdate);
        }

        private void OnDestroy()
        {
            Execute(BehaviourTriggerType.OnDestroy);
        }
    }

}
