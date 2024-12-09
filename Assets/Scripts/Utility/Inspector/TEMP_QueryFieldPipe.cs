using System.Collections;
using System.Collections.Generic;
using KHI.Utility.Inspector;
using TMPro;
using UnityEngine;

namespace KHI.Utility.Inspector
{
    public class TEMP_QueryFieldPipe : MonoBehaviour
    {
        public TMP_InputField target;
        public QueryFieldPipe.PipeToStringEvent currentCharsPipe;
        public QueryFieldPipe.PipeToStringEvent maximumCharsPipe;

        public void Emit()
        {
            currentCharsPipe?.Invoke(target.text.Length.ToString());
            maximumCharsPipe?.Invoke(target.characterLimit.ToString());
        }

        void Start()
        {
            Emit();
        }
    }
}