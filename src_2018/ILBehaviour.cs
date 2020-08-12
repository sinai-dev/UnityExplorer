using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using MelonLoader;
using UnhollowerRuntimeLib;

namespace Explorer
{
    //public class ILBehaviour : MonoBehaviour
    //{
    //    public ILBehaviour(IntPtr intPtr) : base(intPtr) { }

    //    public static T AddToGameObject<T>(GameObject _go) where T : ILBehaviour
    //    {
    //        Il2CppSystem.Type ilType = UnhollowerRuntimeLib.Il2CppType.Of<T>();

    //        if (ilType == null)
    //        {
    //            MelonLogger.Log("Error - could not get MB as ilType");
    //            return null;
    //        }

    //        var obj = typeof(T)
    //            .GetConstructor(new Type[] { typeof(IntPtr) })
    //            .Invoke(new object[] { _go.AddComponent(UnhollowerRuntimeLib.Il2CppType.Of<T>()).Pointer });

    //        return (T)obj;
    //    }
    //}
}
