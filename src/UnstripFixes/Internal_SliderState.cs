using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Explorer
{
    public class Internal_SliderState
    {
        public float dragStartPos;
        public float dragStartValue;
        public bool isDragging;

        public static Dictionary<IntPtr, Internal_SliderState> Dict = new Dictionary<IntPtr, Internal_SliderState>();

        public static Internal_SliderState FromPointer(IntPtr ptr)
        {
            if (!Dict.ContainsKey(ptr))
            {
                Dict.Add(ptr, new Internal_SliderState());
            }

            return Dict[ptr];
        }
    }
}
