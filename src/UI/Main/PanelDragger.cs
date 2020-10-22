using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using ExplorerBeta.Input;
using ExplorerBeta.Helpers;
using ExplorerBeta.Unstrip.ImageConversion;
using System.IO;
using System.Linq;

namespace ExplorerBeta.UI
{
    // Handles dragging and resizing for the main explorer window.

    public class PanelDragger
    {
        public static PanelDragger Instance { get; private set; }

        public RectTransform Panel { get; set; }

        public PanelDragger(RectTransform dragArea, RectTransform panelToDrag)
        {
            Instance = this;
            DragableArea = dragArea;
            Panel = panelToDrag;

            UpdateResizeCache();

            try
            {
                var path = @"Mods\Explorer\cursor.png";
                var data = File.ReadAllBytes(path);

                var tex = new Texture2D(32, 32);
                tex.LoadImage(data, false);
                UnityEngine.Object.DontDestroyOnLoad(tex);

                var size = new Rect();
                size.width = 32;
                size.height = 32;
                var sprite = UIManager.CreateSprite(tex, size);
                UnityEngine.Object.DontDestroyOnLoad(sprite);

                m_resizeCursorImage = new GameObject("ResizeCursorImage");
                m_resizeCursorImage.transform.SetParent(UIManager.CanvasRoot.transform);

                var image = m_resizeCursorImage.AddComponent<Image>();
                image.sprite = sprite;
                var rect = image.transform.TryCast<RectTransform>();
                rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 32);
                rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 32);

                m_resizeCursorImage.SetActive(false);
            }
            catch (Exception e)
            {
                ExplorerCore.Log("Exception loading resize cursor: " + e.ToString());
            }
        }

        public void Update()
        {
            var rawMousePos = InputManager.MousePosition;

            var resizePos = Panel.InverseTransformPoint(rawMousePos);
            var dragPos = DragableArea.InverseTransformPoint(rawMousePos);

            if (m_wasHoveringResize)
            {
                UpdateHoverImagePos();
            }

            // If Mouse pressed this frame
            if (InputManager.GetMouseButtonDown(0))
            {
                if (DragableArea.rect.Contains(dragPos))
                {
                    OnBeginDrag();
                    return;
                }
                else if (MouseInResizeArea(resizePos))
                {
                    var type = GetResizeType(resizePos);
                    OnBeginResize(type);
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
                else if (MouseInResizeArea(resizePos))
                {
                    var type = GetResizeType(resizePos);
                    OnHoverResize(type);
                }
                else if (m_wasHoveringResize)
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
            var diff = InputManager.MousePosition - m_lastDragPosition;
            m_lastDragPosition = InputManager.MousePosition;

            var pos = Panel.localPosition;
            var z = pos.z;
            pos += diff;
            pos.z = z;
            Panel.localPosition = pos;
        }

        public void OnEndDrag()
        {
            WasDragging = false;
            UpdateResizeCache();
        }

        #endregion

        #region RESIZE

        private const int RESIZE_THICKNESS = 10;
        private bool WasResizing { get; set; }
        private ResizeTypes m_currentResizeType = ResizeTypes.NONE;
        private Vector2 m_lastMousePos;

        private bool m_wasHoveringResize;
        private ResizeTypes m_lastResizeHoverType;
        private GameObject m_resizeCursorImage;

        private Rect m_outerResize;
        private Rect m_innerResize;
        private readonly Dictionary<ResizeTypes, Rect> m_cachedResizeAreas = new Dictionary<ResizeTypes, Rect>
        {
            { ResizeTypes.Top,          Rect.zero },
            { ResizeTypes.Left,         Rect.zero },
            { ResizeTypes.Right,        Rect.zero },
            { ResizeTypes.Bottom,       Rect.zero },
            { ResizeTypes.TopLeft,      Rect.zero },
            { ResizeTypes.TopRight,     Rect.zero },
            { ResizeTypes.BottomLeft,   Rect.zero },
            { ResizeTypes.BottomRight,  Rect.zero },
        };

        [Flags]
        public enum ResizeTypes
        {
            NONE        = 0,
            Top         = 1,
            Left        = 2,
            Right       = 4,
            Bottom      = 8,
            TopLeft     = Top | Left,
            TopRight    = Top | Right,
            BottomLeft  = Bottom | Left,
            BottomRight = Bottom | Right,
        }

        private void UpdateHoverImagePos()
        {
            if (!m_resizeCursorImage)
                return;

            m_resizeCursorImage.transform.localPosition = UIManager.CanvasRoot.transform.TryCast<RectTransform>()
                                                            .InverseTransformPoint(InputManager.MousePosition);
        }

        private void UpdateResizeCache()
        {
            int halfThick = RESIZE_THICKNESS / 2;

            // calculate main two rects

            this.m_outerResize = new Rect();
            m_outerResize.x = Panel.rect.x - halfThick;
            m_outerResize.y = Panel.rect.y - halfThick;
            m_outerResize.width = Panel.rect.width + (RESIZE_THICKNESS * 2);
            m_outerResize.height = Panel.rect.height + (RESIZE_THICKNESS * 2);

            this.m_innerResize = new Rect();
            m_innerResize.x = m_outerResize.x + RESIZE_THICKNESS;
            m_innerResize.y = m_outerResize.y + RESIZE_THICKNESS;
            m_innerResize.width = Panel.rect.width - RESIZE_THICKNESS;
            m_innerResize.height = Panel.rect.height - RESIZE_THICKNESS;

            // calculate resize areas

            var left = m_cachedResizeAreas[ResizeTypes.Left];
            left.x = m_outerResize.x;
            left.y = m_outerResize.y + RESIZE_THICKNESS;
            left.width = RESIZE_THICKNESS;
            left.height = m_innerResize.height - RESIZE_THICKNESS;
            m_cachedResizeAreas[ResizeTypes.Left] = left;

            var topLeft = m_cachedResizeAreas[ResizeTypes.TopLeft];
            topLeft.x = m_outerResize.x;
            topLeft.y = m_innerResize.y + m_innerResize.height;
            topLeft.width = RESIZE_THICKNESS;
            topLeft.height = RESIZE_THICKNESS;
            m_cachedResizeAreas[ResizeTypes.TopLeft] = topLeft;

            var top = m_cachedResizeAreas[ResizeTypes.Top];
            top.x = m_innerResize.x;
            top.y = m_innerResize.y + m_innerResize.height;
            top.width = m_innerResize.width;
            top.height = RESIZE_THICKNESS;
            m_cachedResizeAreas[ResizeTypes.Top] = top;

            var topRight = m_cachedResizeAreas[ResizeTypes.TopRight];
            topRight.x = m_innerResize.x + m_innerResize.width;
            topRight.y = m_innerResize.y + m_innerResize.height;
            topRight.width = RESIZE_THICKNESS;
            topRight.height = RESIZE_THICKNESS;
            m_cachedResizeAreas[ResizeTypes.TopRight] = topRight;

            var right = m_cachedResizeAreas[ResizeTypes.Right];
            right.x = m_innerResize.x + m_innerResize.width;
            right.y = m_innerResize.y + halfThick;
            right.width = RESIZE_THICKNESS;
            right.height = m_innerResize.height - RESIZE_THICKNESS;
            m_cachedResizeAreas[ResizeTypes.Right] = right;

            var bottomRight = m_cachedResizeAreas[ResizeTypes.BottomRight];
            bottomRight.x = m_innerResize.x + m_innerResize.width;
            bottomRight.y = m_outerResize.y;
            bottomRight.width = RESIZE_THICKNESS;
            bottomRight.height = RESIZE_THICKNESS;
            m_cachedResizeAreas[ResizeTypes.BottomRight] = bottomRight;

            var bottom = m_cachedResizeAreas[ResizeTypes.Bottom];
            bottom.x = m_innerResize.x;
            bottom.y = m_outerResize.y;
            bottom.width = m_innerResize.width;
            bottom.height = RESIZE_THICKNESS;
            m_cachedResizeAreas[ResizeTypes.Bottom] = bottom;

            var bottomLeft = m_cachedResizeAreas[ResizeTypes.BottomLeft];
            bottomLeft.x = m_outerResize.x;
            bottomLeft.y = m_outerResize.y;
            bottomLeft.width = RESIZE_THICKNESS;
            bottomLeft.height = RESIZE_THICKNESS;
            m_cachedResizeAreas[ResizeTypes.BottomLeft] = bottomLeft;
        }

        private bool MouseInResizeArea(Vector2 mousePos)
        {           
            return m_outerResize.Contains(mousePos) && !m_innerResize.Contains(mousePos);
        }

        private ResizeTypes GetResizeType(Vector2 mousePos)
        {
            foreach (var entry in m_cachedResizeAreas)
            {
                if (entry.Value.Contains(mousePos))
                {
                    return entry.Key;
                }
            }
            return ResizeTypes.NONE;
        }

        public void OnHoverResize(ResizeTypes resizeType)
        {
            if (m_wasHoveringResize && m_lastResizeHoverType == resizeType)
                return;

            // we are entering resize, or the resize type has changed.

            m_wasHoveringResize = true;
            m_lastResizeHoverType = resizeType;

            m_resizeCursorImage.SetActive(true);

            float rotation = 0;
            switch (resizeType)
            {
                case ResizeTypes.TopRight:
                case ResizeTypes.BottomLeft:
                    rotation = 45f; break;
                case ResizeTypes.Top:
                case ResizeTypes.Bottom:
                    rotation = 90f; break;
                case ResizeTypes.TopLeft:
                case ResizeTypes.BottomRight:
                    rotation = 135f; break;
            }

            var rot = m_resizeCursorImage.transform.rotation;
            rot.eulerAngles = new Vector3(0, 0, rotation);
            m_resizeCursorImage.transform.rotation = rot;

            UpdateHoverImagePos();
        }

        public void OnHoverResizeEnd()
        {
            m_wasHoveringResize = false;
            m_resizeCursorImage.SetActive(false);
        }

        public void OnBeginResize(ResizeTypes resizeType)
        {
            m_currentResizeType = resizeType;
            m_lastMousePos = InputManager.MousePosition;
            WasResizing = true;
        }

        public void OnResize()
        {
            var mousePos = InputManager.MousePosition;
            var diff = m_lastMousePos - (Vector2)mousePos;
            m_lastMousePos = mousePos;

            var diffX = (float)((decimal)diff.x / Screen.width);
            var diffY = (float)((decimal)diff.y / Screen.height);

            if (m_currentResizeType.HasFlag(ResizeTypes.Left))
            {
                var anch = Panel.anchorMin;
                anch.x -= diffX;
                Panel.anchorMin = anch;
            }
            else if (m_currentResizeType.HasFlag(ResizeTypes.Right))
            {
                var anch = Panel.anchorMax;
                anch.x -= diffX;
                Panel.anchorMax = anch;
            }

            if (m_currentResizeType.HasFlag(ResizeTypes.Top))
            {
                var anch = Panel.anchorMax;
                anch.y -= diffY;
                Panel.anchorMax = anch;
            }
            else if (m_currentResizeType.HasFlag(ResizeTypes.Bottom))
            {
                var anch = Panel.anchorMin;
                anch.y -= diffY;
                Panel.anchorMin = anch;
            }
        }

        public void OnEndResize()
        {
            WasResizing = false;
            UpdateResizeCache();
        }

        #endregion
    }
}