using System;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.UI.Windows.Utilities;

namespace UnityEngine.UI.Windows
{
    [ComponentModuleDisplayName("Arrangement")]
    public class ListArrangementComponentModule : ListComponentDraggableModule
    {
        public enum Type
        {
            Circle,
            Carousel
        }

        public ScrollRect scrollRect;

        [RequiredReference] public RectTransform view;

        [RequiredReference] public RectTransform root;

        public Type type;
        public float moveToTargetSpeed = 10f;
        public float movementFactor = 40f;
        public int targetIndex;

        private Vector2 targetPosition;
        private bool isDragging;

        public CircleParameters circleParameters = new()
        {
            startAngle = 0f,
            maxAngle = 360f,
            maxAnglePerElement = 360f,
            xRadiusOffset = 0f,
            yRadiusOffset = 0f,
            iterationXSizeFactor = 0f,
            iterationYSizeFactor = 0f,
            alignRotation = false
        };

        public CarouselParameters carouselParameters = new()
        {
            directionAngle = 0f
        };

        protected DrivenRectTransformTracker tracker;

        private int lastIndex;

        public override void ValidateEditor()
        {
            base.ValidateEditor();

            if (scrollRect == null)
            {
                scrollRect = windowComponent.GetComponent<ScrollRect>();
            }

            if (view == null && scrollRect != null)
            {
                view = scrollRect.transform as RectTransform;
            }

            if (root == null && scrollRect != null)
            {
                root = scrollRect.content;
            }

            tracker.Clear();
            CollectChildren();
            Arrange();
        }

        public override void OnBeginDrag(PointerEventData data)
        {
            isDragging = true;
        }

        public override void OnDrag(PointerEventData data)
        {
        }

        public override void OnEndDrag(PointerEventData data)
        {
            isDragging = false;
        }

        public override void OnInit()
        {
            base.OnInit();

            if (listComponent.customRoot == null)
            {
                listComponent.customRoot = root;
            }

            if (scrollRect != null)
            {
                scrollRect.onValueChanged.AddListener(OnScrollValueChanged);
            }
        }

        public override void OnDeInit()
        {
            base.OnDeInit();

            if (scrollRect != null)
            {
                scrollRect.onValueChanged.RemoveListener(OnScrollValueChanged);
            }
        }

        public override void OnShowBegin()
        {
            base.OnShowBegin();

            Arrange();
        }

        public override void OnShowEnd()
        {
            base.OnShowEnd();

            targetIndex = 0;
        }

        private void OnScrollValueChanged(Vector2 position)
        {
            if (isDragging == false)
            {
                return;
            }

            Vector2 velocity = scrollRect.velocity.normalized;

            Vector2 size = root.rect.size;
            Vector2 viewSize = view.rect.size;
            var pos2d = new Vector2(position.x * size.x - size.x * 0.5f, position.y * size.y - size.y * 0.5f);
            Vector2 offset = new Vector2(-viewSize.x * 0.5f, -viewSize.y * 0.5f) + velocity * movementFactor;

            int idx = -1;
            RectTransform nearest = null;
            var d = float.MaxValue;
            var currentDistance = 0f;
            for (var i = 0; i < listComponent.items.Count; ++i)
            {
                float dist = (listComponent.items[i].rectTransform.anchoredPosition + offset - pos2d).sqrMagnitude;
                if (i == targetIndex)
                {
                    currentDistance = dist;
                }

                if (dist < d)
                {
                    d = dist;
                    nearest = listComponent.items[i].rectTransform;
                    idx = i;
                }
            }

            if (idx >= 0)
            {
                if (currentDistance > d)
                {
                    Vector2 p = nearest.anchoredPosition;
                    targetPosition = new Vector2(-p.x, -p.y);
                    targetIndex = idx;
                }
            }
        }

        private void CollectChildren()
        {
        }

        public override void OnComponentsChanged()
        {
            base.OnComponentsChanged();

            CollectChildren();
            Arrange();
        }

        public void Arrange()
        {
            //var rect = this.root.rect;
            List<WindowComponent> childs = listComponent.items;

            Vector2 center = Vector2.zero;
            var minMaxRect = new Rect();
            int count = childs.Count;
            for (var c = 0; c < 2; ++c)
            {
                var k = 0;
                lastIndex = 0;

                for (var i = 0; i < count; ++i)
                {
                    WindowComponent tr = childs[i];
                    Vector2 p = GetPosition(childs, k++, count, view.rect.size, out Quaternion rotation,
                        out bool rotationChanged);

                    if (c == 1)
                    {
                        Vector2 middle = count > 1 ? center / count : Vector2.zero;
                        tr.rectTransform.anchoredPosition = new Vector2(p.x - middle.x, p.y - middle.y);
                        tracker.Add(windowComponent, tr.rectTransform, DrivenTransformProperties.AnchoredPosition);

                        if (rotationChanged)
                        {
                            tr.rectTransform.localRotation = rotation;
                            tracker.Add(windowComponent, tr.rectTransform, DrivenTransformProperties.Rotation);
                        }
                    }
                    else if (c == 0)
                    {
                        Rect itemRect = tr.rectTransform.rect;

                        center += new Vector2(p.x, p.y);

                        minMaxRect.xMin = Mathf.Min(minMaxRect.xMin, p.x - itemRect.width * 0.5f);
                        minMaxRect.xMax = Mathf.Max(minMaxRect.xMax, p.x + itemRect.width * 0.5f);
                        minMaxRect.yMin = Mathf.Min(minMaxRect.yMin, p.y - itemRect.height * 0.5f);
                        minMaxRect.yMax = Mathf.Max(minMaxRect.yMax, p.y + itemRect.height * 0.5f);
                    }
                }
            }

            tracker.Add(windowComponent, root, DrivenTransformProperties.SizeDelta);

            Rect rectView = view.rect;
            root.sizeDelta = new Vector2(minMaxRect.width + rectView.width, minMaxRect.height + rectView.height);
        }

        private void ClampToTarget()
        {
            targetIndex = Mathf.Clamp(targetIndex, 0, listComponent.items.Count);
            {
                float dt = Time.deltaTime;
                root.anchoredPosition = Vector2.Lerp(root.anchoredPosition, targetPosition, dt * moveToTargetSpeed);
            }
        }

        public void LateUpdate()
        {
            if (isDragging == false)
            {
                ClampToTarget();
                //this.UpdateSelected();
            }
        }

        public Vector2 GetPosition(List<WindowComponent> childs, int index, int count, Vector2 size,
            out Quaternion rotation, out bool rotationChanged)
        {
            Vector2 pos = Vector2.zero;
            rotation = Quaternion.identity;
            rotationChanged = false;
            switch (type)
            {
                case Type.Circle:
                {
                    var radius = new Vector2(size.x * 0.5f, size.y * 0.5f);
                    bool reverse = circleParameters.direction == CircleParameters.ArrangementDirection.CounterClockwise;
                    pos = GetPositionByCircle(
                        reverse ? count - index : index,
                        count,
                        radius.x + circleParameters.xRadiusOffset,
                        radius.y + circleParameters.yRadiusOffset,
                        circleParameters.side,
                        circleParameters.startAngle,
                        circleParameters.maxAnglePerElement,
                        circleParameters.maxAngle);

                    if (circleParameters.alignRotation)
                    {
                        float angle = Mathf.Atan2(pos.y, pos.x) * Mathf.Rad2Deg;
                        rotation = Quaternion.Euler(0f, 0f, angle - 90f + circleParameters.rotationAngle);
                        rotationChanged = true;
                    }
                }
                    break;

                case Type.Carousel:
                {
                    if (childs.Count > 0)
                    {
                        Vector2 firstSize = childs[0].rectTransform.rect.size;

                        var w = 0f;
                        var h = 0f;
                        for (var i = 0; i < childs.Count; ++i)
                        {
                            if (i >= index)
                            {
                                break;
                            }

                            Vector2 s = childs[i].rectTransform.rect.size;
                            w += s.x + carouselParameters.spacing.x;
                            h += s.y + carouselParameters.spacing.y;
                        }

                        w -= firstSize.x * 0.5f;
                        w += childs[index].rectTransform.rect.size.x * 0.5f;

                        h -= firstSize.y * 0.5f;
                        h += childs[index].rectTransform.rect.size.y * 0.5f;

                        float x = w;
                        float y = h;
                        pos = new Vector2(x, y);

                        float radiusX = Mathf.Abs(pos.x);
                        float radiusY = Mathf.Abs(pos.y);
                        float angle = carouselParameters.directionAngle * Mathf.Deg2Rad;
                        Vector2 position = Vector2.zero;
                        position.x = Mathf.Sin(angle) * radiusX * Mathf.Sign(pos.x);
                        position.y = Mathf.Cos(angle) * radiusY * Mathf.Sign(pos.y);
                        pos = position;
                    }
                }
                    break;
            }

            return pos;
        }

        private Vector2 GetPositionByCircle(int index, int count, float radiusX, float radiusY,
            CircleParameters.ArrangementSide side, float startAngle, float maxAnglePerElement, float maxAngle)
        {
            if (count <= 0)
            {
                return Vector2.zero;
            }

            float elementAngle = maxAngle / count;
            var offset = false;

            bool non360 = maxAngle < 360f;

            offset = elementAngle >= maxAnglePerElement;

            if (side == CircleParameters.ArrangementSide.BothSided ||
                side == CircleParameters.ArrangementSide.BothSidedSorted)
            {
                if (side == CircleParameters.ArrangementSide.BothSidedSorted)
                {
                    float elementAngleWithOffset = offset ? maxAnglePerElement : elementAngle;
                    if (count % 2 != 0)
                    {
                        startAngle -= elementAngleWithOffset * 0.5f;
                    }

                    startAngle -= elementAngleWithOffset * count * 0.5f - elementAngleWithOffset * 0.5f;
                }
                else
                {
                    if (index % 2 == 0)
                    {
                        index = lastIndex - index;
                    }
                    else
                    {
                        index = lastIndex + index;
                    }

                    float elementAngleWithOffset = offset ? maxAnglePerElement : elementAngle;
                    if (count % 2 == 0)
                    {
                        startAngle -= elementAngleWithOffset * 0.5f;
                    }
                }
            }
            else
            {
                if (non360)
                {
                    //--index;
                    --count;
                    //count -= 1 * Mathf.FloorToInt(maxAngle / 180f);
                }
                //--count;
            }

            if (offset)
            {
                maxAngle *= maxAnglePerElement / elementAngle;
            }

            if (count == 0)
            {
                count = 1;
            }

            maxAngle = maxAngle * Mathf.Deg2Rad;
            startAngle = startAngle * Mathf.Deg2Rad;
            Vector2 position = Vector2.zero;
            position.x = Mathf.Sin(maxAngle / count * index + startAngle) *
                         (radiusX + circleParameters.iterationXSizeFactor * 360f * index * (maxAngle / 360f));
            position.y = Mathf.Cos(maxAngle / count * index + startAngle) *
                         (radiusY + circleParameters.iterationYSizeFactor * 360f * index * (maxAngle / 360f));

            lastIndex = index;

            return new Vector2(position.x, position.y);
        }

        // Circle
        [Serializable]
        public struct CircleParameters
        {
            public enum ArrangementDirection
            {
                Clockwise,
                CounterClockwise
            }

            public enum ArrangementSide
            {
                OneSide,
                BothSided,
                BothSidedSorted
            }

            public ArrangementDirection direction;
            public ArrangementSide side;
            public float startAngle;
            public float maxAngle;
            public float maxAnglePerElement;
            public float xRadiusOffset;
            public float yRadiusOffset;
            public float iterationXSizeFactor;
            public float iterationYSizeFactor;
            public bool alignRotation;
            public float rotationAngle;
        }

        // Carousel
        [Serializable]
        public struct CarouselParameters
        {
            public float directionAngle;
            public Vector2 spacing;
        }
    }
}