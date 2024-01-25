using System;
using UnityEngine.EventSystems;

namespace UnityEngine.UI.Windows.Components.DragAndDropModules
{
    public class DragComponentModule : WindowComponentModule, IBeginDragHandler, IEndDragHandler, IDragHandler
    {
        private Action<PointerEventData> onDragCallback;
        private Action<PointerEventData> onBeginDragCallback;
        private Action<PointerEventData> onEndDragCallback;

        public virtual void OnBeginDrag(PointerEventData eventData)
        {
            onBeginDragCallback?.Invoke(eventData);
        }

        public virtual void OnDrag(PointerEventData eventData)
        {
            onDragCallback?.Invoke(eventData);
        }

        public virtual void OnEndDrag(PointerEventData eventData)
        {
            onEndDragCallback?.Invoke(eventData);
        }

        public void SetDragCallback(Action<PointerEventData> callback)
        {
            onDragCallback = callback;
        }

        public void SetBeginDragCallback(Action<PointerEventData> callback)
        {
            onBeginDragCallback = callback;
        }

        public void SetEndDragCallback(Action<PointerEventData> callback)
        {
            onEndDragCallback = callback;
        }

        public void AddDragCallback(Action<PointerEventData> callback)
        {
            onDragCallback += callback;
        }

        public void AddBeginDragCallback(Action<PointerEventData> callback)
        {
            onBeginDragCallback += callback;
        }

        public void AddEndDragCallback(Action<PointerEventData> callback)
        {
            onEndDragCallback += callback;
        }

        public void RemoveDragCallback(Action<PointerEventData> callback)
        {
            onDragCallback -= callback;
        }

        public void RemoveBeginDragCallback(Action<PointerEventData> callback)
        {
            onBeginDragCallback -= callback;
        }

        public void RemoveEndDragCallback(Action<PointerEventData> callback)
        {
            onEndDragCallback -= callback;
        }

        public void RemoveAllDragCallbacks()
        {
            onDragCallback = null;
        }

        public void RemoveAllBeginDragCallbacks()
        {
            onBeginDragCallback = null;
        }

        public void RemoveAllEndDragCallbacks()
        {
            onEndDragCallback = null;
        }

        private Transform GetParentCanvasTransform()
        {
            GameObject currentGameObject = windowComponent.gameObject;

            while (currentGameObject.GetComponent<Canvas>() == null)
            {
                currentGameObject = currentGameObject.transform.parent.gameObject;
            }

            return currentGameObject.transform;
        }
    }
}