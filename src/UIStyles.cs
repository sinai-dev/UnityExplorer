using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Il2CppSystem.Collections;
//using Il2CppSystem.Reflection;
using MelonLoader;
using UnhollowerBaseLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Explorer
{
    public class UIStyles
    {
        public static Color LightGreen = new Color(Color.green.r - 0.3f, Color.green.g - 0.3f, Color.green.b - 0.3f);

        public static GUISkin WindowSkin
        {
            get
            {
                if (_customSkin == null)
                {
                    try
                    {
                        _customSkin = CreateWindowSkin();
                    }
                    catch
                    {
                        _customSkin = GUI.skin;
                    }
                }

                return _customSkin;
            }
        }

        public static void HorizontalLine(Color color)
        {
            var c = GUI.color;
            GUI.color = color;
            GUILayout.Box(GUIContent.none, HorizontalBar, null);
            GUI.color = c;
        }

        private static GUISkin _customSkin;

        public static Texture2D m_nofocusTex;
        public static Texture2D m_focusTex;

        private static GUIStyle _horizBarStyle;

        private static GUIStyle HorizontalBar
        {
            get
            {
                if (_horizBarStyle == null)
                {
                    _horizBarStyle = new GUIStyle();
                    _horizBarStyle.normal.background = Texture2D.whiteTexture;
                    _horizBarStyle.margin = new RectOffset(0, 0, 4, 4);
                    _horizBarStyle.fixedHeight = 2;
                }

                return _horizBarStyle;
            }
        }

        private static GUISkin CreateWindowSkin()
        {
            var newSkin = Object.Instantiate(GUI.skin);
            Object.DontDestroyOnLoad(newSkin);

            m_nofocusTex = MakeTex(550, 700, new Color(0.1f, 0.1f, 0.1f, 0.7f));
            m_focusTex = MakeTex(550, 700, new Color(0.3f, 0.3f, 0.3f, 1f));

            newSkin.window.normal.background = m_nofocusTex;
            newSkin.window.onNormal.background = m_focusTex;

            newSkin.box.normal.textColor = Color.white;
            newSkin.window.normal.textColor = Color.white;
            newSkin.button.normal.textColor = Color.white;
            newSkin.textField.normal.textColor = Color.white;
            newSkin.label.normal.textColor = Color.white;

            return newSkin;
        }

        public static Texture2D MakeTex(int width, int height, Color col)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; ++i)
            {
                pix[i] = col;
            }
            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }

        // *********************************** METHODS FOR DRAWING VALUES IN GUI ************************************

        // helper for "Instantiate" button on UnityEngine.Objects
        public static void InstantiateButton(Object obj, float width = 100)
        {
            if (GUILayout.Button("Instantiate", new GUILayoutOption[] { GUILayout.Width(width) }))
            {
                var newobj = Object.Instantiate(obj);

                WindowManager.InspectObject(newobj, out _);
            }
        }

        // helper for drawing a styled button for a GameObject or Transform
        public static void GameobjButton(GameObject obj, Action<GameObject> specialInspectMethod = null, bool showSmallInspectBtn = true, float width = 380)
        {
            bool children = obj.transform.childCount > 0;

            string label = children ? "[" + obj.transform.childCount + " children] " : "";
            label += obj.name;

            bool enabled = obj.activeSelf;
            int childCount = obj.transform.childCount;
            Color color;

            if (enabled)
            {
                if (childCount > 0)
                {
                    color = Color.green;
                }
                else
                {
                    color = LightGreen;
                }
            }
            else
            {
                color = Color.red;
            }

            FastGameobjButton(obj, color, label, obj.activeSelf, specialInspectMethod, showSmallInspectBtn, width);
        }
        
        public static void FastGameobjButton(GameObject obj, Color activeColor, string label, bool enabled, Action<GameObject> specialInspectMethod = null, bool showSmallInspectBtn = true, float width = 380)
        {
            if (!obj)
            {
                GUILayout.Label("<i><color=red>null</color></i>", null);
                return;
            }

            // ------ toggle active button ------

            GUILayout.BeginHorizontal(null);
            GUI.skin.button.alignment = TextAnchor.UpperLeft;

            GUI.color = activeColor;

            enabled = GUILayout.Toggle(enabled, "", new GUILayoutOption[] { GUILayout.Width(18) });
            if (obj.activeSelf != enabled)
            {
                obj.SetActive(enabled);
            }

            // ------- actual button ---------

            if (GUILayout.Button(label, new GUILayoutOption[] { GUILayout.Height(22), GUILayout.Width(width) }))
            {
                if (specialInspectMethod != null)
                {
                    specialInspectMethod(obj);
                }
                else
                {
                    WindowManager.InspectObject(obj, out bool _);
                }
            }

            // ------ small "Inspect" button on the right ------

            GUI.skin.button.alignment = TextAnchor.MiddleCenter;
            GUI.color = Color.white;

            if (showSmallInspectBtn)
            {
                if (GUILayout.Button("Inspect", null))
                {
                    WindowManager.InspectObject(obj, out bool _);
                }
            }

            GUILayout.EndHorizontal();
        }

        public static void DrawMember(ref object value, ref bool isExpanded, ref int arrayOffset, MemberInfo memberInfo, Rect rect, object setTarget = null, Action<object> setAction = null, float labelWidth = 180, bool autoSet = false)
        {
            GUILayout.Label("<color=cyan>" + memberInfo.Name + ":</color>", new GUILayoutOption[] { GUILayout.Width(labelWidth) });

            string valueType = "";
            bool canWrite = true;
            if (memberInfo is FieldInfo fi)
            {
                valueType = fi.FieldType.Name;
                canWrite = !(fi.IsLiteral && !fi.IsInitOnly);
            }
            else if (memberInfo is PropertyInfo pi)
            {
                valueType = pi.PropertyType.Name;
                canWrite = pi.CanWrite;
            }

            DrawValue(ref value, ref isExpanded, ref arrayOffset, rect, valueType, (canWrite ? setTarget : null), (canWrite ? setAction : null), autoSet);
        }

        public static void DrawValue(ref object value, ref bool isExpanded, ref int arrayOffset, Rect rect, string nullValueType = null, object setTarget = null, Action<object> setAction = null, bool autoSet = false)
        {
            if (value == null)
            {
                GUILayout.Label("<i>null (" + nullValueType + ")</i>", null);
                return;
            }

            var valueType = value.GetType();
            if (valueType.IsPrimitive || value.GetType() == typeof(string))
            {
                DrawPrimitive(ref value, rect, setTarget, setAction);
            }
            else if (valueType == typeof(GameObject) || valueType == typeof(Transform))
            {
                GameObject go;
                if (value.GetType() == typeof(Transform))
                {
                    go = (value as Transform).gameObject;
                }
                else
                {
                    go = (value as GameObject);
                }

                GameobjButton(go, null, false, rect.width - 250);
            }
            else if (valueType.IsEnum)
            {
                if (setAction != null)
                {
                    if (GUILayout.Button("<", new GUILayoutOption[] { GUILayout.Width(25) }))
                    {
                        SetEnum(ref value, -1);
                        setAction.Invoke(setTarget);
                    }
                    if (GUILayout.Button(">", new GUILayoutOption[] { GUILayout.Width(25) }))
                    {
                        SetEnum(ref value, 1);
                        setAction.Invoke(setTarget);
                    }
                }

                GUILayout.Label(value.ToString(), null);
            }
            else if (value is System.Collections.IEnumerable || ReflectionWindow.IsList(valueType))
            {
                System.Collections.IEnumerable enumerable;

                if (value is System.Collections.IEnumerable isEnumerable)
                {
                    enumerable = isEnumerable;
                }
                else
                {
                    var listValueType = value.GetType().GetGenericArguments()[0];
                    var listType = typeof(Il2CppSystem.Collections.Generic.List<>).MakeGenericType(new Type[] { listValueType });
                    var method = listType.GetMethod("ToArray");
                    enumerable = (System.Collections.IEnumerable)method.Invoke(value, new object[0]);
                }

                int count = enumerable.Cast<object>().Count();

                if (!isExpanded)
                {
                    if (GUILayout.Button("v", new GUILayoutOption[] { GUILayout.Width(25) }))
                    {
                        isExpanded = true;
                    }
                }
                else
                {
                    if (GUILayout.Button("^", new GUILayoutOption[] { GUILayout.Width(25) }))
                    {
                        isExpanded = false;
                    }
                }

                GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                string btnLabel = "<color=yellow>[" + count + "] " + valueType + "</color>";
                if (GUILayout.Button(btnLabel, new GUILayoutOption[] { GUILayout.MaxWidth(rect.width - 260) }))
                {
                    WindowManager.InspectObject(value, out bool _);
                }
                GUI.skin.button.alignment = TextAnchor.MiddleCenter;

                if (isExpanded)
                {
                    if (count > CppExplorer.ArrayLimit)
                    {
                        GUILayout.EndHorizontal();
                        GUILayout.BeginHorizontal(null);
                        GUILayout.Space(190);
                        int maxOffset = (int)Mathf.Ceil(count / CppExplorer.ArrayLimit);
                        GUILayout.Label($"Page {arrayOffset + 1}/{maxOffset + 1}", new GUILayoutOption[] { GUILayout.Width(80) });
                        // prev/next page buttons
                        if (GUILayout.Button("< Prev", null))
                        {
                            if (arrayOffset > 0) arrayOffset--;
                        }
                        if (GUILayout.Button("Next >", null))
                        {
                            if (arrayOffset < maxOffset) arrayOffset++;
                        }
                    }

                    int offset = arrayOffset * CppExplorer.ArrayLimit;

                    if (offset >= count) offset = 0;

                    var enumerator = enumerable.GetEnumerator();
                    if (enumerator != null)
                    {
                        int i = 0;
                        int preiterated = 0;
                        while (enumerator.MoveNext())
                        {
                            if (offset > 0 && preiterated < offset)
                            {
                                preiterated++;
                                continue;
                            }

                            var obj = enumerator.Current;

                            //collapsing the BeginHorizontal called from ReflectionWindow.WindowFunction or previous array entry
                            GUILayout.EndHorizontal();
                            GUILayout.BeginHorizontal(null);
                            GUILayout.Space(190);

                            if (i > CppExplorer.ArrayLimit - 1)
                            {
                                //GUILayout.Label($"<i><color=red>{count - CppExplorer.ArrayLimit} results omitted, array is too long!</color></i>", null);
                                break;
                            }

                            if (obj == null)
                            {
                                GUILayout.Label("<i><color=grey>null</color></i>", null);
                            }
                            else
                            {
                                var type = obj.GetType();

                                var lbl = (i + offset) + ": <color=cyan>" + obj.ToString() + "</color>";

                                if (type.IsPrimitive || typeof(string).IsAssignableFrom(type))
                                {
                                    GUILayout.Label(lbl, null);
                                }
                                else
                                {
                                    GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                                    if (GUILayout.Button(lbl, null))
                                    {
                                        WindowManager.InspectObject(obj, out _);
                                    }
                                    GUI.skin.button.alignment = TextAnchor.MiddleCenter;
                                }
                                //var type = obj.GetType();
                                //DrawMember(ref obj, type.ToString(), i.ToString(), rect, setTarget, setAction, 25, true);
                            }

                            i++;
                        }
                    }
                }
            }
            else
            {
                var label = value.ToString();

                if (valueType == typeof(Object))
                {
                    label = (value as Object).name;
                }
                else if (value is Vector4 vec4)
                {
                    label = vec4.ToString();
                }
                else if (value is Vector3 vec3)
                {
                    label = vec3.ToString();
                }
                else if (value is Vector2 vec2)
                {
                    label = vec2.ToString();
                }
                else if (value is Rect rec)
                {
                    label = rec.ToString();
                }
                else if (value is Matrix4x4 matrix)
                {
                    label = matrix.ToString();
                }
                else if (value is Color col)
                {
                    label = col.ToString();
                }

                GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                if (GUILayout.Button("<color=yellow>" + label + "</color>", new GUILayoutOption[] { GUILayout.MaxWidth(rect.width - 230) }))
                {
                    WindowManager.InspectObject(value, out bool _);
                }
                GUI.skin.button.alignment = TextAnchor.MiddleCenter;
            }
        }

        //public static void DrawMember(ref object value, string valueType, string memberName, Rect rect, object setTarget = null, Action<object> setAction = null, float labelWidth = 180, bool autoSet = false)
        //{
        //    GUILayout.Label("<color=cyan>" + memberName + ":</color>", new GUILayoutOption[] { GUILayout.Width(labelWidth) });

        //    DrawValue(ref value, rect, valueType, memberName, setTarget, setAction, autoSet);
        //}

        //public static void DrawValue(ref object value, Rect rect, string nullValueType = null, string memberName = null, object setTarget = null, Action<object> setAction = null, bool autoSet = false)
        //{
        //    if (value == null)
        //    {
        //        GUILayout.Label("<i>null (" + nullValueType + ")</i>", null);
        //    }
        //    else
        //    {

        //    }
        //}

        // Helper for drawing primitive values (with Apply button)

        public static void DrawPrimitive(ref object value, Rect m_rect, object setTarget = null, Action<object> setAction = null, bool autoSet = false)
        {
            bool allowSet = setAction != null;

            if (value.GetType() == typeof(bool))
            {
                bool b = (bool)value;
                var color = "<color=" + (b ? "lime>" : "red>");

                if (allowSet)
                {
                    value = GUILayout.Toggle((bool)value, color + value.ToString() + "</color>", null);

                    if (b != (bool)value)
                    {
                        setAction.Invoke(setTarget);
                    }
                }
            }
            else
            {
                if (value.ToString().Length > 37)
                {
                    value = GUILayout.TextArea(value.ToString(), new GUILayoutOption[] { GUILayout.MaxWidth(m_rect.width - 260) });
                }
                else
                {
                    value = GUILayout.TextField(value.ToString(), new GUILayoutOption[] { GUILayout.MaxWidth(m_rect.width - 260) });
                }

                if (autoSet || (allowSet && GUILayout.Button("<color=#00FF00>Apply</color>", new GUILayoutOption[] { GUILayout.Width(60) })))
                {
                    setAction.Invoke(setTarget);
                }
            }
        }

        // Helper for setting an enum

        public static void SetEnum(ref object value, int change)
        {
            var type = value.GetType();
            var names = Enum.GetNames(type).ToList();

            int newindex = names.IndexOf(value.ToString()) + change;

            if ((change < 0 && newindex >= 0) || (change > 0 && newindex < names.Count))
            {
                value = Enum.Parse(type, names[newindex]);
            }
        }
    }
}
