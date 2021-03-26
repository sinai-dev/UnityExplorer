using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorer.Core.Input;
using System.IO;
using UnityExplorer.Core.Inspectors;
using System.Diagnostics;

namespace UnityExplorer.UI
{
    // Handles dragging and resizing for the main explorer window.

    public class PanelDragger
    {
        public static PanelDragger Instance { get; private set; }

        public RectTransform Panel { get; set; }

        public static event Action OnFinishResize;

        public PanelDragger(RectTransform dragArea, RectTransform panelToDrag)
        {
            Instance = this;
            DragableArea = dragArea;
            Panel = panelToDrag;

            UpdateResizeCache();

            SceneExplorer.OnToggleShow += OnEndResize;
        }

        public void Update()
        {
            Vector3 rawMousePos = InputManager.MousePosition;

            ResizeTypes type;
            Vector3 resizePos = Panel.InverseTransformPoint(rawMousePos);

            Vector3 dragPos = DragableArea.InverseTransformPoint(rawMousePos);
            bool inDragPos = DragableArea.rect.Contains(dragPos);

            if (WasHoveringResize && s_resizeCursorObj)
            {
                UpdateHoverImagePos();
            }

            // If Mouse pressed this frame
            if (InputManager.GetMouseButtonDown(0))
            {
                if (inDragPos)
                {
                    OnBeginDrag();
                    return;
                }
                else if (MouseInResizeArea(resizePos))
                {
                    type = GetResizeType(resizePos);
                    if (type != ResizeTypes.NONE)
                    {
                        OnBeginResize(type);
                    }
                }
            }
            // If mouse still pressed from last frame
            else if (InputManager.GetMouseButton(0))
            {
                if (WasDragging)
                {
                    OnDrag();
                }
                else if (WasResizing)
                {
                    OnResize();
                }
            }
            // If mouse not pressed
            else
            {
                if (WasDragging)
                {
                    OnEndDrag();
                }
                else if (WasResizing)
                {
                    OnEndResize();
                }
                else if (!inDragPos && MouseInResizeArea(resizePos) && (type = GetResizeType(resizePos)) != ResizeTypes.NONE)
                {
                    OnHoverResize(type);
                }
                else if (WasHoveringResize)
                {
                    OnHoverResizeEnd();
                }
            }

            return;
        }

        #region DRAGGING

        public RectTransform DragableArea { get; set; }
        public bool WasDragging { get; set; }
        private Vector3 m_lastDragPosition;

        public void OnBeginDrag()
        {
            WasDragging = true;
            m_lastDragPosition = InputManager.MousePosition;
        }

        public void OnDrag()
        {
            Vector3 diff = InputManager.MousePosition - m_lastDragPosition;
            m_lastDragPosition = InputManager.MousePosition;

            Vector3 pos = Panel.localPosition;
            float z = pos.z;
            pos += diff;
            pos.z = z;
            Panel.localPosition = pos;
        }

        public void OnEndDrag()
        {
            WasDragging = false;
            //UpdateResizeCache();
        }

        #endregion

        #region RESIZE

        private const int RESIZE_THICKNESS = 15;

        internal readonly Vector2 minResize = new Vector2(400, 400);

        private bool WasResizing { get; set; }
        private ResizeTypes m_currentResizeType = ResizeTypes.NONE;
        private Vector2 m_lastResizePos;

        private bool WasHoveringResize { get; set; }
        private ResizeTypes m_lastResizeHoverType;
        public static GameObject s_resizeCursorObj;

        private Rect m_resizeRect;

        private readonly Dictionary<ResizeTypes, Rect> m_resizeMask = new Dictionary<ResizeTypes, Rect>
        {
            { ResizeTypes.Top,      default },
            { ResizeTypes.Left,     default },
            { ResizeTypes.Right,    default },
            { ResizeTypes.Bottom,   default },
        };

        [Flags]
        public enum ResizeTypes
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

        private const int HALF_THICKESS = RESIZE_THICKNESS / 2;
        private const int DBL_THICKNESS = RESIZE_THICKNESS* 2;

        private void UpdateResizeCache()
        {
            m_resizeRect = new Rect(Panel.rect.x - HALF_THICKESS,
                Panel.rect.y - HALF_THICKESS,
                Panel.rect.width + DBL_THICKNESS,
                Panel.rect.height + DBL_THICKNESS);

            // calculate the four cross sections to use as flags

            m_resizeMask[ResizeTypes.Bottom] = new Rect(m_resizeRect.x, m_resizeRect.y, m_resizeRect.width, RESIZE_THICKNESS);

            m_resizeMask[ResizeTypes.Left] = new Rect(m_resizeRect.x, m_resizeRect.y, RESIZE_THICKNESS, m_resizeRect.height);

            m_resizeMask[ResizeTypes.Top] = new Rect(m_resizeRect.x, m_resizeRect.y + Panel.rect.height, m_resizeRect.width, RESIZE_THICKNESS);

            m_resizeMask[ResizeTypes.Right] = new Rect(m_resizeRect.x + Panel.rect.width, m_resizeRect.y, RESIZE_THICKNESS, m_resizeRect.height);
        }

        private bool MouseInResizeArea(Vector2 mousePos)
        {
            return m_resizeRect.Contains(mousePos);
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
            if (WasHoveringResize && m_lastResizeHoverType == resizeType)
                return;

            // we are entering resize, or the resize type has changed.

            WasHoveringResize = true;
            m_lastResizeHoverType = resizeType;

            s_resizeCursorObj.SetActive(true);

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

            Quaternion rot = s_resizeCursorObj.transform.rotation;
            rot.eulerAngles = new Vector3(0, 0, iconRotation);
            s_resizeCursorObj.transform.rotation = rot;

            UpdateHoverImagePos();
        }

        // update the resize icon position to be above the mouse
        private void UpdateHoverImagePos()
        {
            RectTransform t = UIManager.CanvasRoot.GetComponent<RectTransform>();
            s_resizeCursorObj.transform.localPosition = t.InverseTransformPoint(InputManager.MousePosition);
        }

        public void OnHoverResizeEnd()
        {
            WasHoveringResize = false;
            s_resizeCursorObj.SetActive(false);
        }

        public void OnBeginResize(ResizeTypes resizeType)
        {
            m_currentResizeType = resizeType;
            m_lastResizePos = InputManager.MousePosition;
            WasResizing = true;
        }

        public void OnResize()
        {
            Vector3 mousePos = InputManager.MousePosition;
            Vector2 diff = m_lastResizePos - (Vector2)mousePos;

            if ((Vector2)mousePos == m_lastResizePos)
                return;

            m_lastResizePos = mousePos;

            float diffX = (float)((decimal)diff.x / Screen.width);
            float diffY = (float)((decimal)diff.y / Screen.height);

            Vector2 anchorMin = Panel.anchorMin;
            Vector2 anchorMax = Panel.anchorMax;

            if (m_currentResizeType.HasFlag(ResizeTypes.Left))
                anchorMin.x -= diffX;
            else if (m_currentResizeType.HasFlag(ResizeTypes.Right))
                anchorMax.x -= diffX;

            if (m_currentResizeType.HasFlag(ResizeTypes.Top))
                anchorMax.y -= diffY;
            else if (m_currentResizeType.HasFlag(ResizeTypes.Bottom))
                anchorMin.y -= diffY;

            Panel.anchorMin = new Vector2(anchorMin.x, anchorMin.y);
            Panel.anchorMax = new Vector2(anchorMax.x, anchorMax.y);

            var newWidth = (anchorMax.x - anchorMin.x) * Screen.width;
            var newHeight = (anchorMax.y - anchorMin.y) * Screen.height;

            if (newWidth >= minResize.x)
            {
                Panel.anchorMin = new Vector2(anchorMin.x, Panel.anchorMin.y);
                Panel.anchorMax = new Vector2(anchorMax.x, Panel.anchorMax.y);
            }
            if (newHeight >= minResize.y)
            {
                Panel.anchorMin = new Vector2(Panel.anchorMin.x, anchorMin.y);
                Panel.anchorMax = new Vector2(Panel.anchorMax.x, anchorMax.y);
            }
        }

        public void OnEndResize()
        {
            WasResizing = false;
            UpdateResizeCache();
            OnFinishResize?.Invoke();
        }

        internal static void LoadCursorImage()
        {
            try
            {
                s_resizeCursorObj = UIFactory.CreateLabel(UIManager.CanvasRoot.gameObject, TextAnchor.MiddleCenter);

                var text = s_resizeCursorObj.GetComponent<Text>();
                text.text = "↔";
                text.fontSize = 35;
                text.color = Color.white;

                RectTransform rect = s_resizeCursorObj.transform.GetComponent<RectTransform>();
                rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 64);
                rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 64);

                s_resizeCursorObj.SetActive(false);
            }
            catch (Exception e)
            {
                ExplorerCore.LogWarning("Exception loading cursor image!\r\n" + e.ToString());
            }
        }

        #endregion
    }

    // Just to allow Enum to do .HasFlag() in NET 3.5
    public static class Net35FlagsEx
    {
        public static bool HasFlag(this Enum flags, Enum value)
        {
            ulong num = Convert.ToUInt64(value);
            return (Convert.ToUInt64(flags) & num) == num;
        }
    }
}