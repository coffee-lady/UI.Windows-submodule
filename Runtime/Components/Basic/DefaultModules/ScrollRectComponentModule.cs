using UnityEngine.EventSystems;

namespace UnityEngine.UI.Windows
{
    public class ScrollRectComponentModule : ButtonComponentModule, IInitializePotentialDragHandler, IBeginDragHandler,
        IDragHandler, IEndDragHandler
    {
        public ScrollRect scrollRect;

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (scrollRect != null)
            {
                scrollRect.OnBeginDrag(eventData);
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (scrollRect != null)
            {
                scrollRect.OnDrag(eventData);
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (scrollRect != null)
            {
                scrollRect.OnEndDrag(eventData);
            }
        }

        public void OnInitializePotentialDrag(PointerEventData eventData)
        {
            if (scrollRect != null)
            {
                scrollRect.OnInitializePotentialDrag(eventData);
            }
        }

        public override void ValidateEditor()
        {
            base.ValidateEditor();

            scrollRect = windowComponent.GetComponentInParent<ScrollRect>();
        }
    }
}