using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace KHI.Utility.Inspector
{
    //TODO: ...Figure out why I numbered it like this.
    public enum BehaviourTriggerType
    {
        Awake = 0,
        OnEnable = 1,
        Start = 10,
        OnDisable = 20,
        Update = 30,
        LateUpdate = 40,
        OnDestroy = 99
    }
}
