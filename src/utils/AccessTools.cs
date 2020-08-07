using System;
using System.Reflection;

namespace Explorer
{
    /// <summary>
    /// AccessTools 
    /// Some helpers for Reflection (GetValue, SetValue, Call, InheritBaseValues)
    /// </summary>
    public static class At
    {
        public static Il2CppSystem.Reflection.BindingFlags ilFlags = Il2CppSystem.Reflection.BindingFlags.Public
            | Il2CppSystem.Reflection.BindingFlags.NonPublic
            | Il2CppSystem.Reflection.BindingFlags.Instance
            | Il2CppSystem.Reflection.BindingFlags.Static;

        public static BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static;

        //reflection call
        public static object Call(object obj, string method, params object[] args)
        {
            var methodInfo = obj.GetType().GetMethod(method, flags);
            if (methodInfo != null)
            {
                return methodInfo.Invoke(obj, args);
            }
            return null;
        }

        // set value
        public static void SetValue<T>(T value, Type type, object obj, string field)
        {
            FieldInfo fieldInfo = type.GetField(field, flags);
            if (fieldInfo != null)
            {
                fieldInfo.SetValue(obj, value);
            }
        }

        // get value
        public static object GetValue(Type type, object obj, string value)
        {
            FieldInfo fieldInfo = type.GetField(value, flags);
            if (fieldInfo != null)
            {
                return fieldInfo.GetValue(obj);
            }
            else
            {
                return null;
            }
        }

        // inherit base values
        public static void InheritBaseValues(object _derived, object _base)
        {
            foreach (FieldInfo fi in _base.GetType().GetFields(flags))
            {
                try { _derived.GetType().GetField(fi.Name).SetValue(_derived, fi.GetValue(_base)); } catch { }
            }

            return;
        }
    }
}
