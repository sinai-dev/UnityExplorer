using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MelonLoader;
using UnhollowerRuntimeLib;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Explorer
{
    public class GameObjectWindow : UIWindow
    {
        public override string Title => WindowManager.TabView
            ? $"<color=cyan>[G]</color> {m_object.name}"
            : $"GameObject Inspector ({m_object.name})";

        public GameObject m_object;

        // gui element holders
        private string m_name;
        private string m_scene;

        private Vector2 m_transformScroll = Vector2.zero;
        private Transform[] m_children;
        private Component[] m_components;

        private Vector2 m_compScroll = Vector2.zero;

        private float m_translateAmount = 0.3f;
        private float m_rotateAmount = 50f;
        private float m_scaleAmount = 0.1f;

        private readonly List<Component> m_cachedDestroyList = new List<Component>();
        //private string m_addComponentInput = "";

        private string m_setParentInput = "Enter a GameObject name or path";

        public bool GetObjectAsGameObject()
        {
            if (Target == null)
            {
                MelonLogger.Log("Target is null!");
                return false;
            }

            var targetType = Target.GetType();

            if (targetType == typeof(GameObject))
            {
                m_object = Target as GameObject;
                return true;
            }
            else if (targetType == typeof(Transform))
            {
                m_object = (Target as Transform).gameObject;
                return true;
            }

            MelonLogger.Log("Error: Target is null or not a GameObject/Transform!");
            DestroyWindow();
            return false;
        }

        public override void Init()
        {
            if (!GetObjectAsGameObject())
            {
                return;
            }

            m_name = m_object.name;
            m_scene = string.IsNullOrEmpty(m_object.scene.name) 
                ? "None" 
                : m_object.scene.name;

            var list = new List<Transform>();
            for (int i = 0; i < m_object.transform.childCount; i++)
            {
                list.Add(m_object.transform.GetChild(i));
            }
            m_children = list.ToArray();
        }

        public override void Update()
        {
            try
            {
                if (!m_object && !GetObjectAsGameObject())
                {
                    throw new Exception("Object is null!");
                }

                var list = new List<Component>();
                foreach (var comp in m_object.GetComponents(ReflectionHelpers.ComponentType))
                {
                    list.Add(comp);
                }
                m_components = list.ToArray();
            }
            catch (Exception e)
            {
                DestroyOnException(e);
            }
        }

        private void DestroyOnException(Exception e)
        {
            MelonLogger.Log($"{e.GetType()}, {e.Message}");
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

        private void ReflectObject(Il2CppSystem.Object obj)
        {
            var window = WindowManager.InspectObject(obj, out bool created);

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
                    GUILayout.BeginArea(new Rect(5, 25, rect.width - 10, rect.height - 35), GUI.skin.box);
                }

                scroll = GUILayout.BeginScrollView(scroll, GUI.skin.scrollView);

                GUILayout.BeginHorizontal(null);
                GUILayout.Label("Scene: <color=cyan>" + (m_scene == "" ? "n/a" : m_scene) + "</color>", null);
                if (m_scene == UnityHelpers.ActiveSceneName)
                {
                    if (GUILayout.Button("<color=#00FF00>< View in Scene Explorer</color>", new GUILayoutOption[] { GUILayout.Width(230) }))
                    {
                        ScenePage.Instance.SetTransformTarget(m_object.transform);
                        MainMenu.SetCurrentPage(0);
                    }
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal(null);
                GUILayout.Label("Path:", new GUILayoutOption[] { GUILayout.Width(50) });
                string pathlabel = m_object.transform.GetGameObjectPath();
                if (m_object.transform.parent != null)
                {
                    if (GUILayout.Button("<-", new GUILayoutOption[] { GUILayout.Width(35) }))
                    {
                        InspectGameObject(m_object.transform.parent);
                    }
                }
                GUILayout.TextArea(pathlabel, null);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal(null);
                GUILayout.Label("Name:", new GUILayoutOption[] { GUILayout.Width(50) });
                GUILayout.TextArea(m_name, null);
                GUILayout.EndHorizontal();

                // --- Horizontal Columns section ---
                GUILayout.BeginHorizontal(null);

                GUILayout.BeginVertical(new GUILayoutOption[] { GUILayout.Width(rect.width / 2 - 17) });
                TransformList(rect);
                GUILayout.EndVertical();

                GUILayout.BeginVertical(new GUILayoutOption[] { GUILayout.Width(rect.width / 2 - 17) });
                ComponentList(rect);
                GUILayout.EndVertical();

                GUILayout.EndHorizontal(); // end horiz columns

                GameObjectControls();

                GUILayout.EndScrollView();

                if (!WindowManager.TabView)
                {
                    m_rect = ResizeDrag.ResizeWindow(rect, windowID);

                    GUILayout.EndArea();
                }
            }
            catch (Exception e)
            {
                DestroyOnException(e);
            }
        }

        private void TransformList(Rect m_rect)
        {
            GUILayout.BeginVertical(GUI.skin.box, null); // new GUILayoutOption[] { GUILayout.Height(250) });
            m_transformScroll = GUILayout.BeginScrollView(m_transformScroll, GUI.skin.scrollView);

            GUILayout.Label("<b>Children:</b>", null);
            if (m_children != null && m_children.Length > 0)
            {
                foreach (var obj in m_children.Where(x => x.childCount > 0))
                {
                    if (!obj)
                    {
                        GUILayout.Label("null", null);
                        continue;
                    }
                    UIHelpers.GameobjButton(obj.gameObject, InspectGameObject, false, m_rect.width / 2 - 60);
                }
                foreach (var obj in m_children.Where(x => x.childCount == 0))
                {
                    if (!obj)
                    {
                        GUILayout.Label("null", null);
                        continue;
                    }
                    UIHelpers.GameobjButton(obj.gameObject, InspectGameObject, false, m_rect.width / 2 - 60);
                }
            }
            else
            {
                GUILayout.Label("<i>None</i>", null);
            }

            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }


        private void ComponentList(Rect m_rect)
        {
            GUILayout.BeginVertical(GUI.skin.box, null); // new GUILayoutOption[] { GUILayout.Height(250) });
            m_compScroll = GUILayout.BeginScrollView(m_compScroll, GUI.skin.scrollView);
            GUILayout.Label("<b><size=15>Components</size></b>", null);

            GUI.skin.button.alignment = TextAnchor.MiddleLeft;
            if (m_cachedDestroyList.Count > 0)
            {
                m_cachedDestroyList.Clear();
            }

            if (m_components != null)
            {
                foreach (var component in m_components)
                {
                    if (!component) continue;

                    var ilType = component.GetIl2CppType();
                    if (ilType == ReflectionHelpers.TransformType)
                    {
                        continue;
                    }

                    GUILayout.BeginHorizontal(null);
                    if (ReflectionHelpers.BehaviourType.IsAssignableFrom(ilType))
                    {
                        BehaviourEnabledBtn(component.TryCast<Behaviour>());
                    }
                    else
                    {
                        GUILayout.Space(26);
                    }
                    if (GUILayout.Button("<color=cyan>" + ilType.Name + "</color>", new GUILayoutOption[] { GUILayout.Width(m_rect.width / 2 - 90) }))
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

            GUILayout.EndScrollView();

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
            GUILayout.BeginVertical(GUI.skin.box, new GUILayoutOption[] { GUILayout.Width(520) });
            GUILayout.Label("<b><size=15>GameObject Controls</size></b>", null);

            GUILayout.BeginHorizontal(null);
            bool m_active = m_object.activeSelf;
            m_active = GUILayout.Toggle(m_active, (m_active ? "<color=lime>Enabled " : "<color=red>Disabled") + "</color>",
                new GUILayoutOption[] { GUILayout.Width(80) });
            if (m_object.activeSelf != m_active) { m_object.SetActive(m_active); }

            UIHelpers.InstantiateButton(m_object, 100);

            if (GUILayout.Button("Set DontDestroyOnLoad", new GUILayoutOption[] { GUILayout.Width(170) }))
            {
                GameObject.DontDestroyOnLoad(m_object);
                m_object.hideFlags |= HideFlags.DontUnloadUnusedAsset;
            }

            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal(null);

            m_setParentInput = GUILayout.TextField(m_setParentInput, null);
            if (GUILayout.Button("Set Parent", new GUILayoutOption[] { GUILayout.Width(80) }))
            {
                if (GameObject.Find(m_setParentInput) is GameObject newparent)
                {
                    m_object.transform.parent = newparent.transform;
                }
                else
                {
                    MelonLogger.LogWarning($"Could not find gameobject '{m_setParentInput}'");
                }
            }

            if (GUILayout.Button("Detach from parent", new GUILayoutOption[] { GUILayout.Width(160) }))
            {
                m_object.transform.parent = null;
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginVertical(GUI.skin.box, null);

            var t = m_object.transform;
            TranslateControl(t, TranslateType.Position, ref m_translateAmount,  false);
            TranslateControl(t, TranslateType.Rotation, ref m_rotateAmount,     true);
            TranslateControl(t, TranslateType.Scale,    ref m_scaleAmount,      false);

            GUILayout.EndVertical();

            if (GUILayout.Button("<color=red><b>Destroy</b></color>", new GUILayoutOption[] { GUILayout.Width(120) }))
            {
                GameObject.Destroy(m_object);
                DestroyWindow();
                return;
            }

            GUILayout.EndVertical();
        }

        public enum TranslateType
        {
            Position,
            Rotation,
            Scale
        }

        private void TranslateControl(Transform transform, TranslateType mode, ref float amount, bool multByTime)
        {
            GUILayout.BeginHorizontal(null);
            GUILayout.Label("<color=cyan><b>" + mode + "</b></color>:", new GUILayoutOption[] { GUILayout.Width(65) });

            Vector3 vector = Vector3.zero;
            switch (mode)
            {
                case TranslateType.Position: vector = transform.localPosition; break;
                case TranslateType.Rotation: vector = transform.localRotation.eulerAngles; break;
                case TranslateType.Scale:    vector = transform.localScale; break;
            }
            GUILayout.Label(vector.ToString(), new GUILayoutOption[] { GUILayout.Width(250)  });
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal(null);
            GUI.skin.label.alignment = TextAnchor.MiddleRight;

            GUILayout.Label("<color=cyan>X:</color>", new GUILayoutOption[] { GUILayout.Width(20) });
            PlusMinusFloat(ref vector.x, amount, multByTime);

            GUILayout.Label("<color=cyan>Y:</color>", new GUILayoutOption[] { GUILayout.Width(20) });
            PlusMinusFloat(ref vector.y, amount, multByTime);

            GUILayout.Label("<color=cyan>Z:</color>", new GUILayoutOption[] { GUILayout.Width(20) });
            PlusMinusFloat(ref vector.z, amount, multByTime);

            switch (mode)
            {
                case TranslateType.Position: transform.localPosition = vector; break;
                case TranslateType.Rotation: transform.localRotation = Quaternion.Euler(vector); break;
                case TranslateType.Scale:    transform.localScale = vector; break;
            }

            GUILayout.Label("+/-:", new GUILayoutOption[] { GUILayout.Width(30) });
            var input = amount.ToString("F3");
            input = GUILayout.TextField(input, new GUILayoutOption[] { GUILayout.Width(40) });
            if (float.TryParse(input, out float f))
            {
                amount = f;
            }

            GUI.skin.label.alignment = TextAnchor.UpperLeft;
            GUILayout.EndHorizontal();
        }

        private void PlusMinusFloat(ref float f, float amount, bool multByTime)
        {
            string s = f.ToString("F3");
            s = GUILayout.TextField(s, new GUILayoutOption[] { GUILayout.Width(60) });
            if (float.TryParse(s, out float f2))
            {
                f = f2;
            }
            if (GUILayout.RepeatButton("-", new GUILayoutOption[] { GUILayout.Width(20) }))
            {
                f -= multByTime ? amount * Time.deltaTime : amount;
            }
            if (GUILayout.RepeatButton("+", new GUILayoutOption[] { GUILayout.Width(20) }))
            {
                f += multByTime ? amount * Time.deltaTime : amount;
            }
        }

        
    }
}
