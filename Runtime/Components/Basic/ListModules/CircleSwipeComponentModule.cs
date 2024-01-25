using System;
using System.Collections;
using UnityEngine.EventSystems;

namespace UnityEngine.UI.Windows
{
    [ComponentModuleDisplayName("Circle swipe")]
    public class CircleSwipeComponentModule : ListComponentDraggableModule
    {
        public RectTransform container;

        public float snapDuration = 0.2f;
        public float rotationAngle = 45f;

        private Coroutine smoothLerpCoroutine;
        private int currentItemIndex;

        public Action<int> onSelectedPageChanged;

        public override void OnInit()
        {
            base.OnInit();

            UpdatePages();
        }

        public override void OnShowEnd()
        {
            base.OnShowEnd();

            var sectorCount = 0;

            for (var i = 0; i < container.childCount; ++i)
            {
                if (container.GetChild(i).gameObject.activeSelf)
                {
                    ++sectorCount;
                }
            }

            if (sectorCount == 0)
            {
                return;
            }

            rotationAngle = 360f / sectorCount;
        }

        private void UpdatePages()
        {
            currentItemIndex = 0;
            onSelectedPageChanged?.Invoke(currentItemIndex);
        }

        private IEnumerator SmoothLerp()
        {
            var timer = 0f;
            Quaternion originRotation = container.localRotation;

            Quaternion targetRotation = Quaternion.Euler(originRotation.eulerAngles.x, originRotation.eulerAngles.y,
                currentItemIndex * rotationAngle);

            while (timer < 1f)
            {
                timer += Time.deltaTime / snapDuration;
                container.localRotation = Quaternion.Lerp(originRotation, targetRotation, timer);
                yield return null;
            }

            container.localRotation = targetRotation;
            onSelectedPageChanged?.Invoke(currentItemIndex);
        }

        public override void OnBeginDrag(PointerEventData data)
        {
            if (windowComponent.isActiveAndEnabled == false)
            {
                return;
            }

            if (smoothLerpCoroutine != null)
            {
                windowComponent.StopCoroutine(smoothLerpCoroutine);
            }
        }

        public override void OnEndDrag(PointerEventData data)
        {
            if (windowComponent.isActiveAndEnabled == false)
            {
                return;
            }

            float sector = Mathf.Round(container.localRotation.eulerAngles.z / rotationAngle);
            if (sector >= 360f / rotationAngle)
            {
                sector = 0f;
            }

            currentItemIndex = (int) sector;
            smoothLerpCoroutine = windowComponent.StartCoroutine(SmoothLerp());
        }

        public override void OnDrag(PointerEventData eventData)
        {
            container.Rotate(0f, 0f, -eventData.delta.x / 10f, Space.Self);
        }
    }
}