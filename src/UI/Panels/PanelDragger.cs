using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorer.UI.Widgets.AutoComplete;
using UniverseLib.Input;
using UniverseLib.UI;
using UniverseLib.Utility;

namespace UnityExplorer.UI.Panels
{
    public class PanelDragger
    {
        private enum MouseState
        {
            Down,
            Held,
            NotPressed
        }

        #region Static

        public static bool Resizing { get; private set; }
        public static bool ResizePrompting => resizeCursorObj && resizeCursorObj.activeSelf;

        public static GameObject resizeCursorObj;
        internal static bool wasAnyDragging;

        internal static List<PanelDragger> Instances = new();

        private static bool handledInstanceThisFrame;

        static PanelDragger()
        {
            UIPanel.OnPanelsReordered += OnPanelsReordered;
        }

        internal static void ForceEnd()
        {
            resizeCursorObj.SetActive(false);
            wasAnyDragging = false;
            Resizing = false;

            foreach (PanelDragger instance in Instances)
            {
                instance.WasDragging = false;
                instance.WasResizing = false;
            }
        }

        public static void OnPanelsReordered()
        {
            Instances.Sort((a, b) => b.Panel.GetSiblingIndex().CompareTo(a.Panel.GetSiblingIndex()));

            // move AutoCompleter to bottom
            if (AutoCompleteModal.Instance != null)
            {
                int idx = Instances.IndexOf(AutoCompleteModal.Instance.Dragger);
                Instances.RemoveAt(idx);
                Instances.Insert(0, AutoCompleteModal.Instance.Dragger);
            }
        }

        public static void UpdateInstances()
        {
            if (!DisplayManager.MouseInTargetDisplay)
                return;

            if (!resizeCursorObj)
                CreateCursorUI();

            MouseState state;
            if (InputManager.GetMouseButtonDown(0))
                state = MouseState.Down;
            else if (InputManager.GetMouseButton(0))
                state = MouseState.Held;
            else
                state = MouseState.NotPressed;

            Vector3 mousePos = DisplayManager.MousePosition;

            handledInstanceThisFrame = false;
            foreach (PanelDragger instance in Instances)
            {
                if (!instance.Panel.gameObject.activeSelf)
                    continue;

                instance.Update(state, mousePos);
                if (handledInstanceThisFrame)
                    break;
            }

            if (wasAnyDragging && state == MouseState.NotPressed)
            {
                foreach (PanelDragger instance in Instances)
                    instance.WasDragging = false;
                wasAnyDragging = false;
            }
        }

        #endregion

        // Instance

        public UIPanel UIPanel { get; private set; }
        public bool AllowDragAndResize => UIPanel.CanDragAndResize;

        public RectTransform Panel { get; set; }
        public event Action<RectTransform> OnFinishResize;
        public event Action<RectTransform> OnFinishDrag;

        // Dragging
        public RectTransform DragableArea { get; set; }
        public bool WasDragging { get; set; }
        private Vector2 lastDragPosition;

        // Resizing
        private const int RESIZE_THICKNESS = 10;

        private bool WasResizing { get; set; }
        private ResizeTypes currentResizeType = ResizeTypes.NONE;
        private Vector2 lastResizePos;

        private bool WasHoveringResize => resizeCursorObj.activeInHierarchy;

        private ResizeTypes lastResizeHoverType;

        private Rect totalResizeRect;

        public PanelDragger(RectTransform dragArea, RectTransform panelToDrag, UIPanel panel)
        {
            this.UIPanel = panel;
            Instances.Add(this);
            DragableArea = dragArea;
            Panel = panelToDrag;

            UpdateResizeCache();
        }

        public void Destroy()
        {
            if (resizeCursorObj)
                GameObject.Destroy(resizeCursorObj);

            if (Instances.Contains(this))
                Instances.Remove(this);
        }

        private void Update(MouseState state, Vector3 rawMousePos)
        {
            ResizeTypes type;
            Vector3 resizePos = Panel.InverseTransformPoint(rawMousePos);
            bool inResizePos = !UIManager.NavBarRect.rect.Contains(UIManager.NavBarRect.InverseTransformPoint(rawMousePos))
                && MouseInResizeArea(resizePos);

            Vector3 dragPos = DragableArea.InverseTransformPoint(rawMousePos);
            bool inDragPos = DragableArea.rect.Contains(dragPos);

            if (WasHoveringResize && resizeCursorObj)
                UpdateHoverImagePos();

            switch (state)
            {
                case MouseState.Down:
                    if (inDragPos || inResizePos)
                        UIManager.SetPanelActive(Panel, true);

                    if (inDragPos)
                    {
                        if (AllowDragAndResize)
                            OnBeginDrag();
                        handledInstanceThisFrame = true;
                        return;
                    }
                    else if (inResizePos)
                    {
                        type = GetResizeType(resizePos);
                        if (type != ResizeTypes.NONE)
                            OnBeginResize(type);

                        handledInstanceThisFrame = true;
                    }
                    break;

                case MouseState.Held:
                    if (WasDragging)
                    {
                        OnDrag();
                        handledInstanceThisFrame = true;
                    }
                    else if (WasResizing)
                    {
                        OnResize();
                        handledInstanceThisFrame = true;
                    }
                    break;

                case MouseState.NotPressed:
                    if (AllowDragAndResize && inDragPos)
                    {
                        if (WasDragging)
                            OnEndDrag();

                        if (WasHoveringResize)
                            OnHoverResizeEnd();

                        handledInstanceThisFrame = true;
                    }
                    else if (inResizePos || WasResizing)
                    {
                        if (WasResizing)
                            OnEndResize();

                        type = GetResizeType(resizePos);
                        if (type != ResizeTypes.NONE)
                            OnHoverResize(type);
                        else if (WasHoveringResize)
                            OnHoverResizeEnd();

                        handledInstanceThisFrame = true;
                    }
                    else if (WasHoveringResize)
                        OnHoverResizeEnd();
                    break;
            }

            return;
        }

        #region DRAGGING

        public void OnBeginDrag()
        {
            wasAnyDragging = true;
            WasDragging = true;
            lastDragPosition = DisplayManager.MousePosition;
        }

        public void OnDrag()
        {
            Vector3 mousePos = DisplayManager.MousePosition;

            Vector2 diff = (Vector2)mousePos - lastDragPosition;
            lastDragPosition = mousePos;

            Panel.localPosition = Panel.localPosition + (Vector3)diff;

            UIPanel.EnsureValidPosition(Panel);
        }

        public void OnEndDrag()
        {
            WasDragging = false;

            OnFinishDrag?.Invoke(Panel);
        }

        #endregion

        #region RESIZE

        private readonly Dictionary<ResizeTypes, Rect> m_resizeMask = new()
        {
            { ResizeTypes.Top, default },
            { ResizeTypes.Left, default },
            { ResizeTypes.Right, default },
            { ResizeTypes.Bottom, default },
        };

        [Flags]
        public enum ResizeTypes : ulong
        {
            NONE = 0,
            Top = 1,
            Left = 2,
            Right = 4,
            Bottom = 8,
            TopLeft = Top | Left,
            TopRight = Top | Right,
            BottomLeft = Bottom | Left,
            BottomRight = Bottom | Right,
        }

        // private const int HALF_THICKESS = RESIZE_THICKNESS / 2;
        private const int DBL_THICKESS = RESIZE_THICKNESS * 2;

        private void UpdateResizeCache()
        {
            totalResizeRect = new Rect(Panel.rect.x - RESIZE_THICKNESS + 1,
                Panel.rect.y - RESIZE_THICKNESS + 1,
                Panel.rect.width + DBL_THICKESS - 2,
                Panel.rect.height + DBL_THICKESS - 2);

            // calculate the four cross sections to use as flags
            if (AllowDragAndResize)
            {
                m_resizeMask[ResizeTypes.Bottom] = new Rect(
                    totalResizeRect.x,
                    totalResizeRect.y,
                    totalResizeRect.width,
                    RESIZE_THICKNESS);

                m_resizeMask[ResizeTypes.Left] = new Rect(
                    totalResizeRect.x,
                    totalResizeRect.y,
                    RESIZE_THICKNESS,
                    totalResizeRect.height);

                m_resizeMask[ResizeTypes.Top] = new Rect(
                    totalResizeRect.x,
                    Panel.rect.y + Panel.rect.height - 2,
                    totalResizeRect.width,
                    RESIZE_THICKNESS);

                m_resizeMask[ResizeTypes.Right] = new Rect(
                    totalResizeRect.x + Panel.rect.width + RESIZE_THICKNESS - 2,
                    totalResizeRect.y,
                    RESIZE_THICKNESS,
                    totalResizeRect.height);
            }
        }

        private bool MouseInResizeArea(Vector2 mousePos)
        {
            return totalResizeRect.Contains(mousePos);
        }

        private ResizeTypes GetResizeType(Vector2 mousePos)
        {
            // Calculate which part of the resize area we're in, if any.
            // More readable method commented out below.

            int mask = 0;
            mask |= (int)ResizeTypes.Top * (m_resizeMask[ResizeTypes.Top].Contains(mousePos) ? 1 : 0);
            mask |= (int)ResizeTypes.Bottom * (m_resizeMask[ResizeTypes.Bottom].Contains(mousePos) ? 1 : 0);
            mask |= (int)ResizeTypes.Left * (m_resizeMask[ResizeTypes.Left].Contains(mousePos) ? 1 : 0);
            mask |= (int)ResizeTypes.Right * (m_resizeMask[ResizeTypes.Right].Contains(mousePos) ? 1 : 0);

            //if (m_resizeMask[ResizeTypes.Top].Contains(mousePos))
            //    mask |= ResizeTypes.Top;
            //else if (m_resizeMask[ResizeTypes.Bottom].Contains(mousePos))
            //    mask |= ResizeTypes.Bottom;

            //if (m_resizeMask[ResizeTypes.Left].Contains(mousePos))
            //    mask |= ResizeTypes.Left;
            //else if (m_resizeMask[ResizeTypes.Right].Contains(mousePos))
            //    mask |= ResizeTypes.Right;

            return (ResizeTypes)mask;
        }

        public void OnHoverResize(ResizeTypes resizeType)
        {
            if (WasHoveringResize && lastResizeHoverType == resizeType)
                return;

            // we are entering resize, or the resize type has changed.

            //WasHoveringResize = true;
            lastResizeHoverType = resizeType;

            resizeCursorObj.SetActive(true);
            resizeCursorObj.transform.SetAsLastSibling();

            // set the rotation for the resize icon
            float iconRotation = 0f;
            switch (resizeType)
            {
                case ResizeTypes.TopRight:
                case ResizeTypes.BottomLeft:
                    iconRotation = 45f; break;
                case ResizeTypes.Top:
                case ResizeTypes.Bottom:
                    iconRotation = 90f; break;
                case ResizeTypes.TopLeft:
                case ResizeTypes.BottomRight:
                    iconRotation = 135f; break;
            }

            Quaternion rot = resizeCursorObj.transform.rotation;
            rot.eulerAngles = new Vector3(0, 0, iconRotation);
            resizeCursorObj.transform.rotation = rot;

            UpdateHoverImagePos();
        }

        // update the resize icon position to be above the mouse
        private void UpdateHoverImagePos()
        {
            resizeCursorObj.transform.localPosition = UIManager.UIRootRect.InverseTransformPoint(DisplayManager.MousePosition);
        }

        public void OnHoverResizeEnd()
        {
            //WasHoveringResize = false;
            resizeCursorObj.SetActive(false);
        }

        public void OnBeginResize(ResizeTypes resizeType)
        {
            currentResizeType = resizeType;
            lastResizePos = DisplayManager.MousePosition;
            WasResizing = true;
            Resizing = true;
        }

        public void OnResize()
        {
            Vector3 mousePos = DisplayManager.MousePosition;
            Vector2 diff = lastResizePos - (Vector2)mousePos;

            if ((Vector2)mousePos == lastResizePos)
                return;

            if (mousePos.x < 0 || mousePos.y < 0 || mousePos.x > DisplayManager.Width || mousePos.y > DisplayManager.Height)
                return;

            lastResizePos = mousePos;

            float diffX = (float)((decimal)diff.x / DisplayManager.Width);
            float diffY = (float)((decimal)diff.y / DisplayManager.Height);

            Vector2 anchorMin = Panel.anchorMin;
            Vector2 anchorMax = Panel.anchorMax;

            if (currentResizeType.HasFlag(ResizeTypes.Left))
                anchorMin.x -= diffX;
            else if (currentResizeType.HasFlag(ResizeTypes.Right))
                anchorMax.x -= diffX;

            if (currentResizeType.HasFlag(ResizeTypes.Top))
                anchorMax.y -= diffY;
            else if (currentResizeType.HasFlag(ResizeTypes.Bottom))
                anchorMin.y -= diffY;

            Vector2 prevMin = Panel.anchorMin;
            Vector2 prevMax = Panel.anchorMax;

            Panel.anchorMin = new Vector2(anchorMin.x, anchorMin.y);
            Panel.anchorMax = new Vector2(anchorMax.x, anchorMax.y);

            if (Panel.rect.width < UIPanel.MinWidth)
            {
                Panel.anchorMin = new Vector2(prevMin.x, Panel.anchorMin.y);
                Panel.anchorMax = new Vector2(prevMax.x, Panel.anchorMax.y);
            }
            if (Panel.rect.height < UIPanel.MinHeight)
            {
                Panel.anchorMin = new Vector2(Panel.anchorMin.x, prevMin.y);
                Panel.anchorMax = new Vector2(Panel.anchorMax.x, prevMax.y);
            }
        }

        public void OnEndResize()
        {
            WasResizing = false;
            Resizing = false;
            try { OnHoverResizeEnd(); } catch { }
            UpdateResizeCache();
            OnFinishResize?.Invoke(Panel);
        }

        internal static void CreateCursorUI()
        {
            try
            {
                Text text = UIFactory.CreateLabel(UIManager.UIRoot, "ResizeCursor", "↔", TextAnchor.MiddleCenter, Color.white, true, 35);
                resizeCursorObj = text.gameObject;
                Outline outline = text.gameObject.AddComponent<Outline>();
                outline.effectColor = Color.black;
                outline.effectDistance = new(1, 1);

                RectTransform rect = resizeCursorObj.GetComponent<RectTransform>();
                rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 64);
                rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 64);

                resizeCursorObj.SetActive(false);
            }
            catch (Exception e)
            {
                ExplorerCore.LogWarning("Exception creating Resize Cursor UI!\r\n" + e.ToString());
            }
        }

        #endregion
    }
}