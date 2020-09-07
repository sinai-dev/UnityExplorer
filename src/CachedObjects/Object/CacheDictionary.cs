using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MelonLoader;
using UnityEngine;
using System.Reflection;
using UnhollowerBaseLib;

namespace Explorer
{
    public class CacheDictionary : CacheObjectBase, IExpandHeight
    {
        public bool IsExpanded { get; set; }
        public float WhiteSpace { get; set; } = 215f;
        public float ButtonWidthOffset { get; set; } = 290f;

        public PageHelper Pages = new PageHelper();

        private CacheObjectBase[] m_cachedKeys;
        private CacheObjectBase[] m_cachedValues;

        public Type TypeOfKeys
        {
            get
            {
                if (m_keysType == null) GetGenericArguments();
                return m_keysType;
            }
        }
        private Type m_keysType;

        public Type TypeOfValues
        {
            get
            {
                if (m_valuesType == null) GetGenericArguments();
                return m_valuesType;
            }
        }
        private Type m_valuesType;

        public IDictionary IDict
        {
            get => m_iDictionary ?? (m_iDictionary = Value as IDictionary) ?? Il2CppDictionaryToMono();
            set => m_iDictionary = value;
        }
        private IDictionary m_iDictionary;

        // ========== Methods ==========

        // This is a bit janky due to Il2Cpp Dictionary not implementing IDictionary.
        private IDictionary Il2CppDictionaryToMono()
        {
            // note: "ValueType" is the Dictionary itself, TypeOfValues is the 'Dictionary.Values' type.

            // make generic dictionary from key and value type
            var dict = (IDictionary)Activator.CreateInstance(typeof(Dictionary<,>)
                                             .MakeGenericType(TypeOfKeys, TypeOfValues));

            // get keys and values
            var keys   = ValueType.GetProperty("Keys")  .GetValue(Value);
            var values = ValueType.GetProperty("Values").GetValue(Value);

            // create lists to hold them
            var keyList   = new List<object>();
            var valueList = new List<object>();

            // get keys enumerator and store keys
            var keyEnumerator = keys.GetType().GetMethod("GetEnumerator").Invoke(keys, null);
            var keyCollectionType = keyEnumerator.GetType();
            var keyMoveNext = keyCollectionType.GetMethod("MoveNext");
            var keyCurrent = keyCollectionType.GetProperty("Current");
            while ((bool)keyMoveNext.Invoke(keyEnumerator, null))
            {
                keyList.Add(keyCurrent.GetValue(keyEnumerator));
            }

            // get values enumerator and store values
            var valueEnumerator = values.GetType().GetMethod("GetEnumerator").Invoke(values, null);
            var valueCollectionType = valueEnumerator.GetType();
            var valueMoveNext = valueCollectionType.GetMethod("MoveNext");
            var valueCurrent = valueCollectionType.GetProperty("Current");
            while ((bool)valueMoveNext.Invoke(valueEnumerator, null))
            {
                valueList.Add(valueCurrent.GetValue(valueEnumerator));
            }

            // finally iterate into actual dictionary
            for (int i = 0; i < keyList.Count; i++)
            {
                dict.Add(keyList[i], valueList[i]);
            }

            return dict;
        }

        private void GetGenericArguments()
        {
            if (this.MemInfo != null)
            {
                Type memberType = null;
                switch (this.MemInfo.MemberType)
                {
                    case MemberTypes.Field:
                        memberType = (MemInfo as FieldInfo).FieldType;
                        break;
                    case MemberTypes.Property:
                        memberType = (MemInfo as PropertyInfo).PropertyType;
                        break;
                }

                if (memberType != null && memberType.IsGenericType)
                {
                    m_keysType = memberType.GetGenericArguments()[0];
                    m_valuesType = memberType.GetGenericArguments()[1];
                }
            }
            else if (Value != null)
            {
                var type = Value.GetType();
                if (type.IsGenericType)
                {
                    m_keysType = type.GetGenericArguments()[0];
                    m_valuesType = type.GetGenericArguments()[1];
                }
            }
        }

        public override void UpdateValue()
        {
            // first make sure we won't run into a TypeInitializationException.
            if (!EnsureDictionaryIsSupported())
            {
                ReflectionException = "Dictionary Type not supported with Reflection!";
                return;
            }

            base.UpdateValue();

            // reset
            IDict = null;

            if (Value == null || IDict == null)
            {
                return;
            }

            var keys = new List<CacheObjectBase>();
            foreach (var key in IDict.Keys)
            {
                var cache = GetCacheObject(key, TypeOfKeys);
                cache.UpdateValue();
                keys.Add(cache);
            }

            var values = new List<CacheObjectBase>();
            foreach (var val in IDict.Values)
            {
                var cache = GetCacheObject(val, TypeOfValues);
                cache.UpdateValue();
                values.Add(cache);
            }

            m_cachedKeys = keys.ToArray();
            m_cachedValues = values.ToArray();
        }

        private bool EnsureDictionaryIsSupported()
        {
            try
            {
                //var ilTypes = new List<Il2CppSystem.Type>();
                var monoTypes = new Type[] { TypeOfKeys, TypeOfValues };

                foreach (var type in monoTypes)
                {
                    var generic = typeof(Il2CppClassPointerStore<>).MakeGenericType(type);
                    if (generic == null) return false;

                    var genericPtr = (IntPtr)generic.GetField("NativeClassPtr").GetValue(null);
                    if (genericPtr == null) return false;

                    var classPtr = IL2CPP.il2cpp_class_get_type(genericPtr);
                    if (classPtr == null) return false;

                    var internalType = Il2CppSystem.Type.internal_from_handle(classPtr);
                    if (internalType == null) return false;

                    //ilTypes.Add(internalType);
                }
            }
            catch 
            { 
                return false; 
            }

            // Should be fine if we got this far, but I'll leave the rest below commented out just in case.
            return true;

            //MelonLogger.Log("Got both generic types, continuing...");

            //var dictIlClass = IL2CPP.GetIl2CppClass("mscorlib.dll", "System.Collections.Generic", "Dictionary`2");
            //if (dictIlClass == null) return;

            //MelonLogger.Log("Got base dictionary Il2Cpp type");

            //var ilClassFromType = IL2CPP.il2cpp_class_get_type(dictIlClass);
            //if (ilClassFromType == null) return;

            //MelonLogger.Log("got IntPtr from base dictionary type");

            //var internalHandle = Il2CppSystem.Type.internal_from_handle(ilClassFromType);
            //if (internalHandle == null) return;

            //var generic = internalHandle.MakeGenericType(new Il2CppReferenceArray<Il2CppSystem.Type>(new Il2CppSystem.Type[]
            //{
            //        ilTypes[0], ilTypes[1]
            //}));
            //if (generic == null) return;

            //MelonLogger.Log("Made generic handle for our entry types");

            //var nativeClassPtr = generic.TypeHandle.value;
            //if (nativeClassPtr == null) return;

            //MelonLogger.Log("Got the actual nativeClassPtr for the handle");

            //var dictType = typeof(Il2CppSystem.Collections.Generic.Dictionary<,>).MakeGenericType(TypeOfKeys, TypeOfValues);
            //if (dictType == null) return;

            //MelonLogger.Log("Made the generic type for the dictionary");

            //var pointerStoreType = typeof(Il2CppClassPointerStore<>).MakeGenericType(dictType);
            //if (pointerStoreType == null) return;

            //MelonLogger.Log("Made the generic PointerStoreType for our dict");

            //var ptrToSet = IL2CPP.il2cpp_class_from_type(nativeClassPtr);
            //if (ptrToSet == null) return;

            //MelonLogger.Log("Got class from nativeClassPtr, setting value...");

            //pointerStoreType.GetField("NativeClassPtr").SetValue(null, ptrToSet);

            //MelonLogger.Log("Ok");
        }

        // ============= GUI Draw =============

        public override void DrawValue(Rect window, float width)
        {
            if (m_cachedKeys == null || m_cachedValues == null)
            {
                GUILayout.Label("Cached keys or values is null!", null);
                return;
            }

            int count = m_cachedKeys.Length;

            if (!IsExpanded)
            {
                if (GUILayout.Button("v", new GUILayoutOption[] { GUILayout.Width(25) }))
                {
                    IsExpanded = true;
                }
            }
            else
            {
                if (GUILayout.Button("^", new GUILayoutOption[] { GUILayout.Width(25) }))
                {
                    IsExpanded = false;
                }
            }

            GUI.skin.button.alignment = TextAnchor.MiddleLeft;
            string btnLabel = $"<color=yellow>[{count}] Dictionary<{TypeOfKeys.FullName}, {TypeOfValues.FullName}></color>";
            if (GUILayout.Button(btnLabel, new GUILayoutOption[] { GUILayout.MaxWidth(window.width - ButtonWidthOffset) }))
            {
                WindowManager.InspectObject(Value, out bool _);
            }
            GUI.skin.button.alignment = TextAnchor.MiddleCenter;

            GUILayout.Space(5);

            if (IsExpanded)
            {
                float whitespace = WhiteSpace;
                if (whitespace > 0)
                {
                    ClampLabelWidth(window, ref whitespace);
                }

                Pages.ItemCount = count;

                if (count > Pages.ItemsPerPage)
                {
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal(null);

                    GUILayout.Space(whitespace);

                    Pages.CurrentPageLabel();

                    // prev/next page buttons
                    if (GUILayout.Button("< Prev", new GUILayoutOption[] { GUILayout.Width(60) }))
                    {
                        Pages.TurnPage(Turn.Left);
                    }
                    if (GUILayout.Button("Next >", new GUILayoutOption[] { GUILayout.Width(60) }))
                    {
                        Pages.TurnPage(Turn.Right);
                    }

                    Pages.DrawLimitInputArea();

                    GUILayout.Space(5);
                }

                int offset = Pages.CalculateOffsetIndex();

                for (int i = offset; i < offset + Pages.ItemsPerPage && i < count; i++)
                {
                    var key = m_cachedKeys[i];
                    var val = m_cachedValues[i];

                    //collapsing the BeginHorizontal called from ReflectionWindow.WindowFunction or previous array entry
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal(null);

                    //GUILayout.Space(whitespace);

                    if (key == null || val == null)
                    {
                        GUILayout.Label($"[{i}] <i><color=grey>(null)</color></i>", null);
                    }
                    else
                    {
                        GUI.skin.label.alignment = TextAnchor.MiddleCenter;
                        GUILayout.Label($"[{i}]", new GUILayoutOption[] { GUILayout.Width(30) });

                        GUILayout.Label("Key:", new GUILayoutOption[] { GUILayout.Width(40) });
                        key.DrawValue(window, (window.width / 2) - 30f);

                        GUILayout.Label("Value:", new GUILayoutOption[] { GUILayout.Width(40) });
                        val.DrawValue(window, (window.width / 2) - 30f);
                    }

                }

                GUI.skin.label.alignment = TextAnchor.UpperLeft;
            }
        }
    }
}
