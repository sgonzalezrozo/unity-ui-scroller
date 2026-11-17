using UnityEngine;
using UnityEngine.EventSystems;

namespace UIScroller.Tests.PlayMode
{
    /// <summary>A parent that handles drags and wheels, and counts what reached it.</summary>
    public class SpyDragParent : MonoBehaviour, IInitializePotentialDragHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IScrollHandler
    {
        public int PotentialDrags;
        public int BeginDrags;
        public int Drags;
        public int EndDrags;
        public int Scrolls;

        public void OnInitializePotentialDrag(PointerEventData eventData) => PotentialDrags++;
        public void OnBeginDrag(PointerEventData eventData) => BeginDrags++;
        public void OnDrag(PointerEventData eventData) => Drags++;
        public void OnEndDrag(PointerEventData eventData) => EndDrags++;
        public void OnScroll(PointerEventData eventData) => Scrolls++;
    }
}
