//using UnityEngine;
//using System.Collections;
//using UnityEngine.UI;
//using System;
//using UnityEngine.EventSystems;


/////////////// kinda works, not really


//public class ScrollRectEx : ScrollRect, IEventSystemHandler
//{
//    internal SliderScrollbar sliderScrollbar;

//    private bool ShouldRouteToParent(PointerEventData data)
//        => !sliderScrollbar.IsActive
//            || sliderScrollbar.m_slider.value < 0.001f && data.delta.y > 0
//            || sliderScrollbar.m_slider.value == 1f && data.delta.y < 0;

//    private void DoForParents<T>(Action<T> action) where T : IEventSystemHandler
//    {
//        Transform parent = transform.parent;
//        while (parent != null)
//        {
//            foreach (var component in parent.GetComponents<Component>())
//            {
//                if (component is T)
//                    action((T)(IEventSystemHandler)component);
//            }
//            parent = parent.parent;
//        }
//    }

//    public override void OnScroll(PointerEventData data)
//    {
//        if (ShouldRouteToParent(data))
//            DoForParents<IScrollHandler>((parent) => { parent.OnScroll(data); });
//        else
//            base.OnScroll(data);
//    }

//    public override void OnInitializePotentialDrag(PointerEventData eventData)
//    {
//        DoForParents<IInitializePotentialDragHandler>((parent) => { parent.OnInitializePotentialDrag(eventData); });
//        base.OnInitializePotentialDrag(eventData);
//    }

//    public override void OnDrag(PointerEventData data)
//    {
//        if (ShouldRouteToParent(data))
//            DoForParents<IDragHandler>((parent) => { parent.OnDrag(data); });
//        else
//            base.OnDrag(data);
//    }

//    public override void OnBeginDrag(UnityEngine.EventSystems.PointerEventData data)
//    {
//        if (ShouldRouteToParent(data))
//            DoForParents<IBeginDragHandler>((parent) => { parent.OnBeginDrag(data); });
//        else
//            base.OnBeginDrag(data);
//    }

//    public override void OnEndDrag(UnityEngine.EventSystems.PointerEventData data)
//    {
//        if (ShouldRouteToParent(data))
//            DoForParents<IEndDragHandler>((parent) => { parent.OnEndDrag(data); });
//        else
//            base.OnEndDrag(data);
//    }
//}