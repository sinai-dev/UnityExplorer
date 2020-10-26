using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using ExplorerBeta.Input;
using ExplorerBeta.Helpers;
using System.IO;
using System.Linq;
#if CPP
using ExplorerBeta.Unstrip.ImageConversion;
#endif

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
                LoadCursorImage();
            }
            catch (Exception e)
            {
                ExplorerCore.Log("Exception loading resize cursor: " + e.ToString());
            }
        }

        public void Update()
        {
            var rawMousePos = InputManager.MousePosition;

            ResizeTypes type;
            var resizePos = Panel.InverseTransformPoint(rawMousePos);
            var dragPos = DragableArea.InverseTransformPoint(rawMousePos);

            if (WasHoveringResize)
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
                    type = GetResizeType(resizePos);
                    if (type != ResizeTypes.NONE)
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
                else if (MouseInResizeArea(resizePos) && (type = GetResizeType(resizePos)) != ResizeTypes.NONE)
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
        private Vector2 m_lastResizePos;

        private bool WasHoveringResize { get; set; }
        private ResizeTypes m_lastResizeHoverType;
        private GameObject m_resizeCursorImage;

        private Rect m_resizeRect;

        private readonly Dictionary<ResizeTypes, Rect> m_resizeMask = new Dictionary<ResizeTypes, Rect>
        {
            { ResizeTypes.Top,      Rect.zero },
            { ResizeTypes.Left,     Rect.zero },
            { ResizeTypes.Right,    Rect.zero },
            { ResizeTypes.Bottom,   Rect.zero },
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

        private void UpdateResizeCache()
        {
            int halfThick = RESIZE_THICKNESS / 2;
            int dblThick = RESIZE_THICKNESS * 2;

            // calculate main outer rect
            // the resize area is both outside and inside the panel,
            // to give a bit of buffer and make it easier to use.

            // outer rect is the outer-most bounds of our resize area
            var outer = new Rect(Panel.rect.x - halfThick, 
                Panel.rect.y - halfThick, 
                Panel.rect.width + dblThick, 
                Panel.rect.height + dblThick);
            m_resizeRect = outer;

            // calculate the four cross sections to use as flags

            m_resizeMask[ResizeTypes.Bottom] = new Rect(outer.x, outer.y, outer.width, RESIZE_THICKNESS);

            m_resizeMask[ResizeTypes.Left] = new Rect(outer.x, outer.y, RESIZE_THICKNESS, outer.height);

            m_resizeMask[ResizeTypes.Top] = new Rect(outer.x, outer.y + Panel.rect.height, outer.width, RESIZE_THICKNESS);

            m_resizeMask[ResizeTypes.Right] = new Rect(outer.x + Panel.rect.width, outer.y, RESIZE_THICKNESS, outer.height);
        }

        private bool MouseInResizeArea(Vector2 mousePos)
        {
            return m_resizeRect.Contains(mousePos);
        }

        private ResizeTypes GetResizeType(Vector2 mousePos)
        {
            // Calculate which part of the resize area we're in, if any.
            // We do this via a bitmask with the ResizeTypes enum.
            // We can return Top/Right/Bottom/Left, or a corner like TopLeft.

            int mask = 0;

            if (m_resizeMask[ResizeTypes.Top].Contains(mousePos))
                mask |= (int)ResizeTypes.Top;
            else if (m_resizeMask[ResizeTypes.Bottom].Contains(mousePos))
                mask |= (int)ResizeTypes.Bottom;

            if (m_resizeMask[ResizeTypes.Left].Contains(mousePos))
                mask |= (int)ResizeTypes.Left;
            else if (m_resizeMask[ResizeTypes.Right].Contains(mousePos))
                mask |= (int)ResizeTypes.Right;

            return (ResizeTypes)mask;
        }

        public void OnHoverResize(ResizeTypes resizeType)
        {
            if (WasHoveringResize && m_lastResizeHoverType == resizeType)
                return;

            // we are entering resize, or the resize type has changed.

            WasHoveringResize = true;
            m_lastResizeHoverType = resizeType;

            m_resizeCursorImage.SetActive(true);

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

            var rot = m_resizeCursorImage.transform.rotation;
            rot.eulerAngles = new Vector3(0, 0, iconRotation);
            m_resizeCursorImage.transform.rotation = rot;

            UpdateHoverImagePos();
        }

        // update the resize icon position to be above the mouse
        private void UpdateHoverImagePos()
        {
            if (!m_resizeCursorImage)
                return;

            var t = UIManager.CanvasRoot.GetComponent<RectTransform>();
            m_resizeCursorImage.transform.localPosition = t.InverseTransformPoint(InputManager.MousePosition);
        }

        public void OnHoverResizeEnd()
        {
            WasHoveringResize = false;
            m_resizeCursorImage.SetActive(false);
        }

        public void OnBeginResize(ResizeTypes resizeType)
        {
            m_currentResizeType = resizeType;
            m_lastResizePos = InputManager.MousePosition;
            WasResizing = true;
        }

        public void OnResize()
        {
            var mousePos = InputManager.MousePosition;
            var diff = m_lastResizePos - (Vector2)mousePos;
            m_lastResizePos = mousePos;

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

        private void LoadCursorImage()
        {
            var path = @"Mods\Explorer\cursor.png";
            var data = File.ReadAllBytes(path);

            var tex = new Texture2D(32, 32);
            tex.LoadImage(data, false);
            UnityEngine.Object.DontDestroyOnLoad(tex);

            var sprite = UIManager.CreateSprite(tex);
            UnityEngine.Object.DontDestroyOnLoad(sprite);

            m_resizeCursorImage = new GameObject("ResizeCursorImage");
            m_resizeCursorImage.transform.SetParent(UIManager.CanvasRoot.transform);

            var image = m_resizeCursorImage.AddComponent<Image>();
            image.sprite = sprite;
            var rect = image.transform.GetComponent<RectTransform>();
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 32);
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 32);

            m_resizeCursorImage.SetActive(false);
        }

#endregion
    }
}