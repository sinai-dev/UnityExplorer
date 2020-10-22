//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Reflection;
//using UnityEngine;
//using Explorer.CacheObject;
//using Explorer.Helpers;
//using Explorer.UI.Shared;

//namespace Explorer.UI
//{
//    public class InteractiveValue
//    {
//        //public const float MAX_LABEL_WIDTH = 400f;
//        //public const string EVALUATE_LABEL = "<color=lime>Evaluate</color>";

//        public CacheObjectBase OwnerCacheObject;

//        public object Value { get; set; }
//        public Type ValueType;

//        public string ButtonLabel => m_btnLabel ?? GetButtonLabel();
//        private string m_btnLabel;

//        public MethodInfo ToStringMethod => m_toStringMethod ?? GetToStringMethod();
//        private MethodInfo m_toStringMethod;

//        public virtual void Init()
//        {
//            UpdateValue();
//        }

//        public virtual void UpdateValue()
//        {
//            GetButtonLabel();
//        }

        

//        private MethodInfo GetToStringMethod()
//        {
//            try
//            {
//                m_toStringMethod = ReflectionHelpers.GetActualType(Value).GetMethod("ToString", new Type[0])
//                                   ?? typeof(object).GetMethod("ToString", new Type[0]);

//                // test invoke
//                m_toStringMethod.Invoke(Value, null);
//            }
//            catch
//            {
//                m_toStringMethod = typeof(object).GetMethod("ToString", new Type[0]);
//            }
//            return m_toStringMethod;
//        }

//        public string GetButtonLabel()
//        {
//            if (Value == null) return null;

//            var valueType = ReflectionHelpers.GetActualType(Value);

//            string label;

//            if (valueType == typeof(TextAsset))
//            {
//                var textAsset = Value as TextAsset;

//                label = textAsset.text;

//                if (label.Length > 10)
//                {
//                    label = $"{label.Substring(0, 10)}...";
//                }

//                label = $"\"{label}\" {textAsset.name} (<color={Syntax.Class_Instance}>UnityEngine.TextAsset</color>)";
//            }
//            else
//            {
//                label = (string)ToStringMethod?.Invoke(Value, null) ?? Value.ToString();

//                var classColor = valueType.IsAbstract && valueType.IsSealed
//                    ? Syntax.Class_Static
//                    : Syntax.Class_Instance;

//                string typeLabel = $"<color={classColor}>{valueType.FullName}</color>";

//                if (Value is UnityEngine.Object)
//                {
//                    label = label.Replace($"({valueType.FullName})", $"({typeLabel})");
//                }
//                else
//                {
//                    if (!label.Contains(valueType.FullName))
//                    {
//                        label += $" ({typeLabel})";
//                    }
//                    else
//                    {
//                        label = label.Replace(valueType.FullName, typeLabel);
//                    }
//                }
//            }

//            return m_btnLabel = label;
//        }
//    }
//}
