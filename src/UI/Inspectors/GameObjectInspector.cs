using System;
using System.Collections.Generic;
using UnityEngine;
using Explorer.UI.Shared;
using Explorer.UI.Main;
#if CPP
using UnhollowerRuntimeLib;
#endif

namespace Explorer.UI.Inspectors
{
    public class GameObjectInspector : WindowBase
    {
        public override string Title => WindowManager.TabView
           ? $"<color=cyan>[G]</color> {TargetGO.name}"
           : $"GameObject Inspector ({TargetGO.name})";

        public GameObject TargetGO;

        private static bool m_hideControls;

        // gui element holders
        private string m_name;
        private string m_scene;

        private Transform[] m_children;
        private Vector2 m_transformScroll = Vector2.zero;
        private readonly PageHelper ChildPages = new PageHelper();

        private Component[] m_components;
        private Vector2 m_compScroll = Vector2.zero;
        private readonly PageHelper CompPages = new PageHelper();

        private readonly Vector3[] m_cachedInput = new Vector3[3];
        private float m_translateAmount = 0.3f;
        private float m_rotateAmount = 50f;
        private float m_scaleAmount = 0.1f;
        private bool m_freeze;
        private Vector3 m_frozenPosition;
        private Quaternion m_frozenRotation;
        private Vector3 m_frozenScale;
        private bool m_autoApplyTransform;
        private bool m_autoUpdateTransform;
        private bool m_localContext;

        private readonly List<Component> m_cachedDestroyList = new List<Component>();
        private string m_addComponentInput = "";

        private string m_setParentInput = "Enter a GameObject name or path";

        public bool GetObjectAsGameObject()
        {
            var targetType = Target?.GetType();

            if (targetType == typeof(GameObject))
            {
                TargetGO = Target as GameObject;
                return true;
            }
            else if (targetType == typeof(Transform))
            {
                TargetGO = (Target as Transform).gameObject;
                return true;
            }

            ExplorerCore.Log("Error: Target is null or not a GameObject/Transform!");
            DestroyWindow();
            return false;
        }

        public override void Init()
        {
            if (!GetObjectAsGameObject())
            {
                return;
            }

            m_name = TargetGO.name;
            m_scene = string.IsNullOrEmpty(TargetGO.scene.name)
                        ? "None (Asset/Resource)"
                        : TargetGO.scene.name;

            CacheTransformValues();

            Update();
        }

        private void CacheTransformValues()
        {
            if (m_localContext)
            {
                m_cachedInput[0] = TargetGO.transform.localPosition;
                m_cachedInput[1] = TargetGO.transform.localEulerAngles;
            }
            else
            {
                m_cachedInput[0] = TargetGO.transform.position;
                m_cachedInput[1] = TargetGO.transform.eulerAngles;
            }
            m_cachedInput[2] = TargetGO.transform.localScale;
        }

        public override void Update()
        {
            try
            {
                if (Target == null)
                {
                    ExplorerCore.Log("Target is null!");
                    DestroyWindow();
                    return;
                }
                if (!TargetGO && !GetObjectAsGameObject())
                {
                    ExplorerCore.Log("Target was destroyed!");
                    DestroyWindow();
                    return;
                }

                if (m_freeze)
                {
                    if (m_localContext)
                    {
                        TargetGO.transform.localPosition = m_frozenPosition;
                        TargetGO.transform.localRotation = m_frozenRotation;
                    }
                    else
                    {
                        TargetGO.transform.position = m_frozenPosition;
                        TargetGO.transform.rotation = m_frozenRotation;
                    }
                    TargetGO.transform.localScale = m_frozenScale;
                }

                // update child objects
                var childList = new List<Transform>();
                for (int i = 0; i < TargetGO.transform.childCount; i++)
                {
                    childList.Add(TargetGO.transform.GetChild(i));
                }
                childList.Sort((a, b) => b.childCount.CompareTo(a.childCount));
                m_children = childList.ToArray();

                ChildPages.ItemCount = m_children.Length;

                // update components
#if CPP
                var compList = new Il2CppSystem.Collections.Generic.List<Component>();
                TargetGO.GetComponentsInternal(ReflectionHelpers.ComponentType, true, false, true, false, compList);
                m_components = compList.ToArray();
#else
                m_components = TargetGO.GetComponents<Component>();
#endif

                CompPages.ItemCount = m_components.Length;
            }
            catch (Exception e)
            {
                DestroyOnException(e);
            }
        }

        private void DestroyOnException(Exception e)
        {
            ExplorerCore.Log($"Exception drawing GameObject Window: {e.GetType()}, {e.Message}");
            DestroyWindow();
        }

        private void InspectGameObject(Transform obj)
        {
            var window = WindowManager.InspectObject(obj, out bool created);

            if (created)
            {
                window.m_rect = new Rect(this.m_rect.x, this.m_rect.y, this.m_rect.width, this.m_rect.height);
                DestroyWindow();
            }
        }

#if CPP
        private void ReflectObject(Il2CppSystem.Object obj)
#else
        private void ReflectObject(object obj)
#endif
        {
            var window = WindowManager.InspectObject(obj, out bool created, true);

            if (created)
            {
                if (this.m_rect.x <= (Screen.width - this.m_rect.width - 100))
                {
                    window.m_rect = new Rect(
                        this.m_rect.x + this.m_rect.width + 20,
                        this.m_rect.y,
                        550,
                        700);
                }
                else
                {
                    window.m_rect = new Rect(this.m_rect.x + 50, this.m_rect.y + 50, 550, 700);
                }
            }
        }

        public override void WindowFunction(int windowID)
        {
            try
            {
                var rect = WindowManager.TabView ? TabViewWindow.Instance.m_rect : this.m_rect;

                if (!WindowManager.TabView)
                {
                    Header();
                    GUIUnstrip.BeginArea(new Rect(5, 25, rect.width - 10, rect.height - 35), GUI.skin.box);
                }

                scroll = GUIUnstrip.BeginScrollView(scroll);

                GUIUnstrip.BeginHorizontal(new GUILayoutOption[0]);
                GUILayout.Label("Scene: <color=cyan>" + (m_scene == "" ? "n/a" : m_scene) + "</color>", new GUILayoutOption[0]);
                if (m_scene == UnityHelpers.ActiveSceneName)
                {
                    if (GUILayout.Button("<color=#00FF00>Send to Scene View</color>", new GUILayoutOption[] { GUILayout.Width(150) }))
                    {
                        ScenePage.Instance.SetTransformTarget(TargetGO.transform);
                        MainMenu.SetCurrentPage(0);
                    }
                }
                if (GUILayout.Button("Reflection Inspect", new GUILayoutOption[] { GUILayout.Width(150) }))
                {
                    WindowManager.InspectObject(Target, out _, true);
                }
                GUILayout.EndHorizontal();

                GUIUnstrip.BeginHorizontal(new GUILayoutOption[0]);
                GUILayout.Label("Path:", new GUILayoutOption[] { GUILayout.Width(50) });
                string pathlabel = TargetGO.transform.GetGameObjectPath();
                if (TargetGO.transform.parent != null)
                {
                    if (GUILayout.Button("<-", new GUILayoutOption[] { GUILayout.Width(35) }))
                    {
                        InspectGameObject(TargetGO.transform.parent);
                    }
                }
                GUIUnstrip.TextArea(pathlabel, new GUILayoutOption[0]);
                GUILayout.EndHorizontal();

                GUIUnstrip.BeginHorizontal(new GUILayoutOption[0]);
                GUILayout.Label("Name:", new GUILayoutOption[] { GUILayout.Width(50) });
                GUIUnstrip.TextArea(m_name, new GUILayoutOption[0]);
                GUILayout.EndHorizontal();

                // --- Horizontal Columns section ---
                GUIUnstrip.BeginHorizontal(new GUILayoutOption[0]);

                GUIUnstrip.BeginVertical(new GUILayoutOption[] { GUILayout.Width(rect.width / 2 - 17) });
                TransformList(rect);
                GUILayout.EndVertical();

                GUIUnstrip.BeginVertical(new GUILayoutOption[] { GUILayout.Width(rect.width / 2 - 17) });
                ComponentList(rect);
                GUILayout.EndVertical();

                GUILayout.EndHorizontal(); // end horiz columns

                GameObjectControls();

                GUIUnstrip.EndScrollView();

                if (!WindowManager.TabView)
                {
                    m_rect = ResizeDrag.ResizeWindow(rect, windowID);

                    GUIUnstrip.EndArea();
                }
            }
            catch (Exception e)
            {
                DestroyOnException(e);
            }
        }

        private void TransformList(Rect m_rect)
        {
            GUIUnstrip.BeginVertical(GUIContent.none, GUI.skin.box, null);
            m_transformScroll = GUIUnstrip.BeginScrollView(m_transformScroll);

            GUILayout.Label("<b><size=15>Children</size></b>", new GUILayoutOption[0]);

            GUIUnstrip.BeginHorizontal(new GUILayoutOption[0]);
            ChildPages.DrawLimitInputArea();

            if (ChildPages.ItemCount > ChildPages.ItemsPerPage)
            {
                ChildPages.CurrentPageLabel();

                GUILayout.EndHorizontal();
                GUIUnstrip.BeginHorizontal(new GUILayoutOption[0]);

                if (GUILayout.Button("< Prev", new GUILayoutOption[] { GUILayout.Width(80) }))
                {
                    ChildPages.TurnPage(Turn.Left, ref this.m_transformScroll);
                }
                if (GUILayout.Button("Next >", new GUILayoutOption[] { GUILayout.Width(80) }))
                {
                    ChildPages.TurnPage(Turn.Right, ref this.m_transformScroll);
                }
            }
            GUILayout.EndHorizontal();

            if (m_children != null && m_children.Length > 0)
            {
                int start = ChildPages.CalculateOffsetIndex();

                for (int j = start; (j < start + ChildPages.ItemsPerPage && j < ChildPages.ItemCount); j++)
                {
                    var obj = m_children[j];

                    if (!obj)
                    {
                        GUILayout.Label("null", new GUILayoutOption[0]);
                        continue;
                    }

                    Buttons.GameObjectButton(obj.gameObject, InspectGameObject, false, m_rect.width / 2 - 80);
                }
            }
            else
            {
                GUILayout.Label("<i>None</i>", new GUILayoutOption[0]);
            }

            GUIUnstrip.EndScrollView();
            GUILayout.EndVertical();
        }

        private void ComponentList(Rect m_rect)
        {
            GUIUnstrip.BeginVertical(GUIContent.none, GUI.skin.box, null);
            m_compScroll = GUIUnstrip.BeginScrollView(m_compScroll);
            GUILayout.Label("<b><size=15>Components</size></b>", new GUILayoutOption[0]);

            GUIUnstrip.BeginHorizontal(new GUILayoutOption[0]);
            CompPages.DrawLimitInputArea();

            if (CompPages.ItemCount > CompPages.ItemsPerPage)
            {
                CompPages.CurrentPageLabel();

                GUILayout.EndHorizontal();
                GUIUnstrip.BeginHorizontal(new GUILayoutOption[0]);

                if (GUILayout.Button("< Prev", new GUILayoutOption[] { GUILayout.Width(80) }))
                {
                    CompPages.TurnPage(Turn.Left, ref this.m_compScroll);
                }
                if (GUILayout.Button("Next >", new GUILayoutOption[] { GUILayout.Width(80) }))
                {
                    CompPages.TurnPage(Turn.Right, ref this.m_compScroll);
                }
            }
            GUILayout.EndHorizontal();

            GUIUnstrip.BeginHorizontal(new GUILayoutOption[0]);
            var width = m_rect.width / 2 - 135f;
            m_addComponentInput = GUIUnstrip.TextField(m_addComponentInput, new GUILayoutOption[] { GUILayout.Width(width) });
            if (GUILayout.Button("Add Comp", new GUILayoutOption[0]))
            {
                if (ReflectionHelpers.GetTypeByName(m_addComponentInput) is Type compType)
                {
                    if (typeof(Component).IsAssignableFrom(compType))
                    {
#if CPP
                        TargetGO.AddComponent(Il2CppType.From(compType));
#else
                        TargetGO.AddComponent(compType);
#endif
                    }
                    else
                    {
                        ExplorerCore.LogWarning($"Type '{compType.Name}' is not assignable from Component!");
                    }
                }
                else
                {
                    ExplorerCore.LogWarning($"Could not find a type by the name of '{m_addComponentInput}'!");
                }
            }
            GUILayout.EndHorizontal();

            GUI.skin.button.alignment = TextAnchor.MiddleLeft;
            if (m_cachedDestroyList.Count > 0)
            {
                m_cachedDestroyList.Clear();
            }

            if (m_components != null)
            {
                int start = CompPages.CalculateOffsetIndex();

                for (int j = start; (j < start + CompPages.ItemsPerPage && j < CompPages.ItemCount); j++)
                {
                    var component = m_components[j];

                    if (!component) continue;

                    var type =
#if CPP
                        component.GetIl2CppType();
#else
                        component.GetType();
#endif
                    GUIUnstrip.BeginHorizontal(new GUILayoutOption[0]);
                    if (ReflectionHelpers.BehaviourType.IsAssignableFrom(type))
                    {
#if CPP
                        BehaviourEnabledBtn(component.TryCast<Behaviour>());
#else
                        BehaviourEnabledBtn(component as Behaviour);
#endif
                    }
                    else
                    {
                        GUIUnstrip.Space(26);
                    }
                    if (GUILayout.Button("<color=cyan>" + type.Name + "</color>", new GUILayoutOption[] { GUILayout.Width(m_rect.width / 2 - 100) }))
                    {
                        ReflectObject(component);
                    }
                    if (GUILayout.Button("<color=red>-</color>", new GUILayoutOption[] { GUILayout.Width(20) }))
                    {
                        m_cachedDestroyList.Add(component);
                    }
                    GUILayout.EndHorizontal();
                }
            }

            GUI.skin.button.alignment = TextAnchor.MiddleCenter;
            if (m_cachedDestroyList.Count > 0)
            {
                for (int i = m_cachedDestroyList.Count - 1; i >= 0; i--)
                {
                    var comp = m_cachedDestroyList[i];
                    GameObject.Destroy(comp);
                }
            }

            GUIUnstrip.EndScrollView();

            GUILayout.EndVertical();
        }

        private void BehaviourEnabledBtn(Behaviour obj)
        {
            var _col = GUI.color;
            bool _enabled = obj.enabled;
            if (_enabled)
            {
                GUI.color = Color.green;
            }
            else
            {
                GUI.color = Color.red;
            }

            // ------ toggle active button ------

            _enabled = GUILayout.Toggle(_enabled, "", new GUILayoutOption[] { GUILayout.Width(18) });
            if (obj.enabled != _enabled)
            {
                obj.enabled = _enabled;
            }
            GUI.color = _col;
        }

        private void GameObjectControls()
        {
            if (m_hideControls)
            {
                GUIUnstrip.BeginHorizontal(new GUILayoutOption[0]);
                GUILayout.Label("<b><size=15>GameObject Controls</size></b>", new GUILayoutOption[] { GUILayout.Width(200) });
                if (GUILayout.Button("^ Show ^", new GUILayoutOption[] { GUILayout.Width(75) }))
                {
                    m_hideControls = false;
                }
                GUILayout.EndHorizontal();

                return;
            }

            GUIUnstrip.BeginVertical(GUIContent.none, GUI.skin.box, new GUILayoutOption[] { GUILayout.Width(520) });

            GUIUnstrip.BeginHorizontal(new GUILayoutOption[0]);
            GUILayout.Label("<b><size=15>GameObject Controls</size></b>", new GUILayoutOption[] { GUILayout.Width(200) });
            if (GUILayout.Button("v Hide v", new GUILayoutOption[] { GUILayout.Width(75) }))
            {
                m_hideControls = true;
            }
            GUILayout.EndHorizontal();

            GUIUnstrip.BeginHorizontal(new GUILayoutOption[0]);
            bool m_active = TargetGO.activeSelf;
            m_active = GUILayout.Toggle(m_active, (m_active ? "<color=lime>Enabled " : "<color=red>Disabled") + "</color>",
                new GUILayoutOption[] { GUILayout.Width(80) });
            if (TargetGO.activeSelf != m_active) { TargetGO.SetActive(m_active); }

            Buttons.InstantiateButton(TargetGO, 100);

            if (GUILayout.Button("Set DontDestroyOnLoad", new GUILayoutOption[] { GUILayout.Width(170) }))
            {
                GameObject.DontDestroyOnLoad(TargetGO);
                TargetGO.hideFlags |= HideFlags.DontUnloadUnusedAsset;
            }

            var lbl = m_freeze ? "<color=lime>Unfreeze</color>" : "<color=orange>Freeze Pos/Rot</color>";
            if (GUILayout.Button(lbl, new GUILayoutOption[] { GUILayout.Width(110) }))
            {
                m_freeze = !m_freeze;
                if (m_freeze)
                {
                    UpdateFreeze();
                }
            }

            GUILayout.EndHorizontal();
            GUIUnstrip.BeginHorizontal(new GUILayoutOption[0]);

            m_setParentInput = GUIUnstrip.TextField(m_setParentInput, new GUILayoutOption[0]);
            if (GUILayout.Button("Set Parent", new GUILayoutOption[] { GUILayout.Width(80) }))
            {
                if (GameObject.Find(m_setParentInput) is GameObject newparent)
                {
                    TargetGO.transform.parent = newparent.transform;
                }
                else
                {
                    ExplorerCore.LogWarning($"Could not find gameobject '{m_setParentInput}'");
                }
            }

            if (GUILayout.Button("Detach from parent", new GUILayoutOption[] { GUILayout.Width(160) }))
            {
                TargetGO.transform.parent = null;
            }
            GUILayout.EndHorizontal();

            GUIUnstrip.BeginVertical(GUIContent.none, GUI.skin.box, null);

            m_cachedInput[0] = TranslateControl(TranslateType.Position, ref m_translateAmount, false);
            m_cachedInput[1] = TranslateControl(TranslateType.Rotation, ref m_rotateAmount, true);
            m_cachedInput[2] = TranslateControl(TranslateType.Scale, ref m_scaleAmount, false);

            GUIUnstrip.BeginHorizontal(new GUILayoutOption[0]);
            if (GUILayout.Button("<color=lime>Apply to Transform</color>", new GUILayoutOption[0]) || m_autoApplyTransform)
            {
                if (m_localContext)
                {
                    TargetGO.transform.localPosition = m_cachedInput[0];
                    TargetGO.transform.localEulerAngles = m_cachedInput[1];
                }
                else
                {
                    TargetGO.transform.position = m_cachedInput[0];
                    TargetGO.transform.eulerAngles = m_cachedInput[1];
                }
                TargetGO.transform.localScale = m_cachedInput[2];

                if (m_freeze)
                {
                    UpdateFreeze();
                }
            }
            if (GUILayout.Button("<color=lime>Update from Transform</color>", new GUILayoutOption[0]) || m_autoUpdateTransform)
            {
                CacheTransformValues();
            }
            GUILayout.EndHorizontal();

            GUIUnstrip.BeginHorizontal(new GUILayoutOption[0]);
            BoolToggle(ref m_autoApplyTransform, "Auto-apply to Transform?");
            BoolToggle(ref m_autoUpdateTransform, "Auto-update from transform?");
            GUILayout.EndHorizontal();

            bool b = m_localContext;
            b = GUILayout.Toggle(b, "<color=" + (b ? "lime" : "orange") + ">Use local transform values?</color>", new GUILayoutOption[0]);
            if (b != m_localContext)
            {
                m_localContext = b;
                CacheTransformValues();
                if (m_freeze)
                {
                    UpdateFreeze();
                }
            }

            GUILayout.EndVertical();

            if (GUILayout.Button("<color=red><b>Destroy</b></color>", new GUILayoutOption[] { GUILayout.Width(120) }))
            {
                GameObject.Destroy(TargetGO);
                DestroyWindow();
                return;
            }

            GUILayout.EndVertical();
        }

        private void UpdateFreeze()
        {
            if (m_localContext)
            {
                m_frozenPosition = TargetGO.transform.localPosition;
                m_frozenRotation = TargetGO.transform.localRotation;
            }
            else
            {
                m_frozenPosition = TargetGO.transform.position;
                m_frozenRotation = TargetGO.transform.rotation;
            }
            m_frozenScale = TargetGO.transform.localScale;
        }

        private void BoolToggle(ref bool value, string message)
        {
            string lbl = "<color=";
            lbl += value ? "lime" : "orange";
            lbl += $">{message}</color>";

            value = GUILayout.Toggle(value, lbl, new GUILayoutOption[0]);
        }

        public enum TranslateType
        {
            Position,
            Rotation,
            Scale
        }

        private Vector3 TranslateControl(TranslateType mode, ref float amount, bool multByTime)
        {
            GUIUnstrip.BeginHorizontal(new GUILayoutOption[0]);
            GUILayout.Label($"<color=cyan><b>{(m_localContext ? "Local " : "")}{mode}</b></color>:",
                new GUILayoutOption[] { GUILayout.Width(m_localContext ? 110 : 65) });

            var transform = TargetGO.transform;
            switch (mode)
            {
                case TranslateType.Position:
                    var pos = m_localContext ? transform.localPosition : transform.position;
                    GUILayout.Label(pos.ToString(), new GUILayoutOption[] { GUILayout.Width(250) });
                    break;
                case TranslateType.Rotation:
                    var rot = m_localContext ? transform.localEulerAngles : transform.eulerAngles;
                    GUILayout.Label(rot.ToString(), new GUILayoutOption[] { GUILayout.Width(250) });
                    break;
                case TranslateType.Scale:
                    GUILayout.Label(transform.localScale.ToString(), new GUILayoutOption[] { GUILayout.Width(250) });
                    break;
            }
            GUILayout.EndHorizontal();

            Vector3 input = m_cachedInput[(int)mode];

            GUIUnstrip.BeginHorizontal(new GUILayoutOption[0]);
            GUI.skin.label.alignment = TextAnchor.MiddleRight;

            GUILayout.Label("<color=cyan>X:</color>", new GUILayoutOption[] { GUILayout.Width(20) });
            PlusMinusFloat(ref input.x, amount, multByTime);

            GUILayout.Label("<color=cyan>Y:</color>", new GUILayoutOption[] { GUILayout.Width(20) });
            PlusMinusFloat(ref input.y, amount, multByTime);

            GUILayout.Label("<color=cyan>Z:</color>", new GUILayoutOption[] { GUILayout.Width(20) });
            PlusMinusFloat(ref input.z, amount, multByTime);

            GUILayout.Label("+/-:", new GUILayoutOption[] { GUILayout.Width(30) });
            var amountInput = amount.ToString("F3");
            amountInput = GUIUnstrip.TextField(amountInput, new GUILayoutOption[] { GUILayout.Width(60) });
            if (float.TryParse(amountInput, out float f))
            {
                amount = f;
            }

            GUI.skin.label.alignment = TextAnchor.UpperLeft;
            GUILayout.EndHorizontal();

            return input;
        }

        private void PlusMinusFloat(ref float f, float amount, bool multByTime)
        {
            string s = f.ToString("F3");
            s = GUIUnstrip.TextField(s, new GUILayoutOption[] { GUILayout.Width(60) });
            if (float.TryParse(s, out float f2))
            {
                f = f2;
            }
            if (GUIUnstrip.RepeatButton("-", new GUILayoutOption[] { GUILayout.Width(20) }))
            {
                f -= multByTime ? amount * Time.deltaTime : amount;
            }
            if (GUIUnstrip.RepeatButton("+", new GUILayoutOption[] { GUILayout.Width(20) }))
            {
                f += multByTime ? amount * Time.deltaTime : amount;
            }
        }
    }
}
