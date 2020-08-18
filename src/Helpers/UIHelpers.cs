using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;
using System.Reflection;

namespace Explorer
{
    public class UIHelpers
    {
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
                    color = UIStyles.LightGreen;
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

            Il2CppSystem.Type ilType = null;
            if (value is Il2CppSystem.Object ilObject)
            {
                ilType = ilObject.GetIl2CppType();
            }

            if (valueType.IsPrimitive || value.GetType() == typeof(string))
            {
                DrawPrimitive(ref value, rect, setTarget, setAction);
            }
            else if (ilType != null && ilType == ReflectionHelpers.GameObjectType || ReflectionHelpers.TransformType.IsAssignableFrom(ilType))
            {
                GameObject go;
                var ilObj = value as Il2CppSystem.Object;
                if (ilType == ReflectionHelpers.GameObjectType)
                {
                    go = ilObj.TryCast<GameObject>();
                }
                else
                {
                    go = ilObj.TryCast<Transform>().gameObject;
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
            else if (value is System.Collections.IEnumerable || ReflectionHelpers.IsList(valueType))
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

                string typeLabel;
                if (ilType != null)
                {
                    typeLabel = ilType.FullName;
                }
                else
                {
                    typeLabel = value.GetType().FullName;
                }
                if (!label.Contains(typeLabel))
                {
                    label += $" ({typeLabel})";
                }

                GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                if (GUILayout.Button("<color=yellow>" + label + "</color>", new GUILayoutOption[] { GUILayout.MaxWidth(rect.width - 230) }))
                {
                    WindowManager.InspectObject(value, out bool _);
                }
                GUI.skin.button.alignment = TextAnchor.MiddleCenter;
            }
        }

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
