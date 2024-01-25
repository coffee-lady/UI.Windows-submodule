using System;
using System.Collections.Generic;
using UnityEngine.UI.Windows.Components;
using UnityEngine.UI.Windows.Utilities;

namespace UnityEngine.UI.Windows
{
    public interface IEndlessElement
    {
        void GetHeight();
    }

    public interface IDataSource
    {
        float GetSize(int index);
    }

    [ComponentModuleDisplayName("Endless List")]
    public class ListEndlessComponentModule : ListComponentModule
    {
        public enum Direction
        {
            Vertical,
            Horizontal,
            VerticalUpside,
            HorizontalUpside
        }

        [Space(10f)] [RequiredReference] public ScrollRect scrollRect;

        public Direction direction;

        [Space(10f)] public HorizontalOrVerticalLayoutGroup layoutGroup;

        public List<RegistryBase> registries = new();
        public float createOffset = 50f;

        private int allCount;
        private int currentVisibleCount;
        private int requiredVisibleCount;
        private int fromIndex;
        private int toIndex;
        private float contentSize;
        private IDataSource dataSource;
        private Item[] items;
        private bool forceRebuild;
        private float contentRectExtend;

        private Vector2 listSize;
        private int prevCount;
        private float invOffset;
        private float offset;
        private float visibleContentHeight;

        public override void ValidateEditor()
        {
            base.ValidateEditor();

            if (scrollRect == null)
            {
                scrollRect = windowComponent.GetComponentInChildren<ScrollRect>(true);
            }

            if (scrollRect != null)
            {
                scrollRect.vertical = direction == Direction.Vertical || direction == Direction.VerticalUpside;
                scrollRect.horizontal = direction == Direction.Horizontal || direction == Direction.HorizontalUpside;
            }

            if (scrollRect != null && scrollRect.content != null && layoutGroup == null)
            {
                layoutGroup = scrollRect.content.GetComponent<HorizontalOrVerticalLayoutGroup>();
            }
        }

        public override bool HasCustomAdd()
        {
            return true;
        }

        public override void OnInit()
        {
            base.OnInit();

            if (scrollRect != null)
            {
                scrollRect.onValueChanged.AddListener(OnScrollValueChanged);
                OnScrollValueChanged(scrollRect.normalizedPosition);

                HorizontalOrVerticalLayoutGroup contentLayoutGroup = layoutGroup;
                if (contentLayoutGroup != null)
                {
                    if (direction == Direction.Horizontal || direction == Direction.HorizontalUpside)
                    {
                        contentRectExtend = contentLayoutGroup.padding.horizontal;
                    }
                    else
                    {
                        contentRectExtend = contentLayoutGroup.padding.vertical;
                    }
                }
            }
        }

        public override void OnDeInit()
        {
            if (scrollRect != null)
            {
                scrollRect.onValueChanged.RemoveListener(OnScrollValueChanged);
            }

            base.OnDeInit();
        }

        public override void OnHideBegin()
        {
            base.OnHideBegin();

            if (scrollRect != null)
            {
                OnScrollValueChanged(scrollRect.normalizedPosition);
            }
        }

        public override void OnShowBegin()
        {
            base.OnShowBegin();

            if (scrollRect != null)
            {
                OnScrollValueChanged(scrollRect.normalizedPosition);
            }

            UpdateContentItems(true);
        }

        public override void OnShowEnd()
        {
            base.OnShowEnd();

            if (scrollRect != null)
            {
                OnScrollValueChanged(scrollRect.normalizedPosition);
            }

            UpdateContentItems(true);
        }

        public override void OnLayoutChanged()
        {
            base.OnLayoutChanged();

            if (scrollRect != null)
            {
                OnScrollValueChanged(scrollRect.normalizedPosition);
            }

            UpdateContentItems(true);
        }

        public override void OnComponentsChanged()
        {
            base.OnComponentsChanged();

            if (scrollRect != null)
            {
                OnScrollValueChanged(scrollRect.normalizedPosition);
            }
        }

        public override void AddItem<T, TClosure>(Resource source, TClosure closure, Action<T, TClosure> onComplete)
        {
        }

        private Registry<T, TClosure> GetRegistry<T, TClosure>() where T : WindowComponent
            where TClosure : IListClosureParameters
        {
            foreach (RegistryBase regBase in registries)
            {
                if (regBase is Registry<T, TClosure> reg)
                {
                    return reg;
                }
            }

            var registry = PoolClassCustom<RegistryBase>.Spawn<Registry<T, TClosure>>();
            return registry;
        }

        public override void SetDataSource(IDataSource dataSource)
        {
            this.dataSource = dataSource;
        }

        public override void SetItems<T, TClosure>(int count, Resource source, Action<T, TClosure> onItem,
            TClosure closure, Action<TClosure> onComplete)
        {
            foreach (RegistryBase reg in registries)
            {
                PoolClassCustom<RegistryBase>.Recycle(reg);
                reg.Clear();
            }

            registries.Clear();

            forceRebuild = allCount != count;
            allCount = count;
            Array.Resize(ref items, count);
            Registry<T, TClosure> registry = GetRegistry<T, TClosure>();
            registry.module = this;
            for (var i = 0; i < count; ++i)
            {
                var item = new Item<T, TClosure>
                {
                    source = source,
                    onItem = onItem,
                    closure = closure,
                    initialized = false
                };

                registry.Add(item);
            }

            registries.Add(registry);
            //this.forceRebuild = true;

            if (onComplete != null)
            {
                onComplete.Invoke(closure);
            }
        }

        public void UpdateContentItems(bool forceRebuild)
        {
            foreach (RegistryBase reg in registries)
            {
                reg.UpdateContent(forceRebuild);
            }
        }

        public int GetIndexByOffset(float pos)
        {
            for (var i = 0; i < allCount; ++i)
            {
                ref Item item = ref items[i];
                if (item.accumulatedSize >= pos)
                {
                    return i;
                }
            }

            return -1;
        }

        public int GetCount()
        {
            return items.Length;
        }

        public Item GetItemByIndex(int index)
        {
            return items[index];
        }

        public void CalculateBounds()
        {
            var forceRebuild = false;
            Vector2 size = listComponent.rectTransform.rect.size;
            if (listSize != size)
            {
                listSize = size;
                prevCount = 0;
                forceRebuild = true;
            }

            if (prevCount != allCount)
            {
                forceRebuild = true;
            }

            RectOffset padding = layoutGroup.padding;

            var accumulatedSize = 0f;
            if (forceRebuild)
            {
                switch (direction)
                {
                    case Direction.Horizontal:
                        accumulatedSize = padding.left;
                        break;
                    case Direction.Vertical:
                        accumulatedSize = padding.top;
                        break;
                    case Direction.HorizontalUpside:
                        accumulatedSize = padding.right;
                        break;
                    case Direction.VerticalUpside:
                        accumulatedSize = padding.bottom;
                        break;
                }

                if (prevCount > 0 && prevCount - 1 < items.Length)
                {
                    accumulatedSize = items[prevCount - 1].accumulatedSize + items[prevCount - 1].size;
                }

                for (int i = prevCount; i < allCount; ++i)
                {
                    ref Item item = ref items[i];
                    item.size = dataSource.GetSize(i);
                    accumulatedSize += i == 0 ? 0 : layoutGroup.spacing;
                    item.accumulatedSize = accumulatedSize;
                    accumulatedSize += item.size;
                }

                prevCount = allCount;
            }
            else
            {
                if (allCount > 0)
                {
                    accumulatedSize = items[allCount - 1].accumulatedSize + items[allCount - 1].size;
                }
            }

            var scrollRect = (RectTransform) this.scrollRect.transform;
            RectTransform contentRect = this.scrollRect.content;
            var viewSize = 0f;
            float contentSize = accumulatedSize;
            var axis = new Vector2(0f, 0f);
            var posOffset = 0f;
            switch (direction)
            {
                case Direction.Horizontal:
                    contentSize += padding.right;
                    posOffset = 1f - this.scrollRect.normalizedPosition.x;
                    axis.x = 1f;
                    viewSize = scrollRect.rect.width;
                    break;
                case Direction.Vertical:
                    contentSize += padding.bottom;
                    posOffset = this.scrollRect.normalizedPosition.y;
                    axis.y = 1f;
                    viewSize = scrollRect.rect.height;
                    break;
                case Direction.HorizontalUpside:
                    contentSize += padding.left;
                    posOffset = 1f - this.scrollRect.normalizedPosition.x;
                    axis.x = 1f;
                    viewSize = scrollRect.rect.width;
                    break;
                case Direction.VerticalUpside:
                    contentSize += padding.top;
                    posOffset = this.scrollRect.normalizedPosition.y;
                    axis.y = 1f;
                    viewSize = scrollRect.rect.height;
                    break;
            }

            this.contentSize = contentSize;
            contentRect.sizeDelta = new Vector2((accumulatedSize + contentRectExtend) * axis.x,
                (accumulatedSize + contentRectExtend) * axis.y);

            float offInv = 1f - posOffset;
            invOffset = offInv;
            float offset = offInv * (contentSize - viewSize);
            if (contentSize <= viewSize)
            {
                offset = 0f;
            }

            float visibleContentHeight = Mathf.Min(viewSize, contentSize);
            this.offset = offset;
            this.visibleContentHeight = visibleContentHeight;
            int fromIndex = GetIndexByOffset(offset - createOffset);
            if (fromIndex == -1)
            {
                fromIndex = 0;
            }
            else
            {
                --fromIndex;
            }

            int toIndex = GetIndexByOffset(offset + visibleContentHeight + createOffset);
            if (toIndex == -1)
            {
                toIndex = allCount;
            }
            else
            {
                ++toIndex;
            }

            fromIndex = Mathf.Clamp(fromIndex, 0, allCount);
            toIndex = Mathf.Clamp(toIndex, 0, allCount);

            requiredVisibleCount = toIndex - fromIndex;
            this.fromIndex = fromIndex;
            this.toIndex = toIndex - 1;
        }

        private void OnScrollValueChanged(Vector2 position)
        {
#if UNITY_EDITOR
            if (Application.isPlaying == false)
            {
                return;
            }
#endif

            if (scrollRect == null || scrollRect.content == null)
            {
                return;
            }

            {
                CalculateBounds();
                UpdateContentItems(forceRebuild);
                forceRebuild = false;
            }
        }

        public abstract class RegistryBase
        {
            public ListEndlessComponentModule module;

            public abstract void Clear();

            public abstract void UpdateContent(bool forceRebuild = false);
        }

        public class Registry<T, TClosure> : RegistryBase
            where T : WindowComponent where TClosure : IListClosureParameters
        {
            private readonly List<Item<T, TClosure>> items = new();
            private int loadingCount;
            private bool isDirty;
            private bool forceRebuild;
            private int fromIndex = -1;
            private int toIndex = -1;
            private int prevFromIndex;
            private int prevToIndex;

            public override void Clear()
            {
                items.Clear();
            }

            public void Add(Item<T, TClosure> data)
            {
                items.Add(data);
            }

            public override void UpdateContent(bool forceRebuild = false)
            {
                ref int currentVisibleCount = ref module.currentVisibleCount;
                ref int requiredVisibleCount = ref module.requiredVisibleCount;
                int fromIndex = module.fromIndex;
                int toIndex = module.toIndex;
                float contentSize = module.contentSize;
                if (this.fromIndex != fromIndex ||
                    this.toIndex != toIndex)
                {
                    prevFromIndex = this.fromIndex;
                    prevToIndex = this.toIndex;
                    this.fromIndex = fromIndex;
                    this.toIndex = toIndex;
                    isDirty = true;
                }

                int delta = requiredVisibleCount - currentVisibleCount;
                if (delta > 0)
                {
                    if (loadingCount == 0)
                    {
                        for (var i = 0; i < delta; ++i)
                        {
                            int k = i + fromIndex;
                            Item<T, TClosure> item = items[k];
                            //item.closure.index = k;
                            ++currentVisibleCount;
                            ++loadingCount;
                            module.listComponent.AddItemInternal<T, InnerClosure>(item.source,
                                new InnerClosure {closure = item.closure, registry = this, item = item},
                                (obj, closure) =>
                                {
                                    --closure.registry.loadingCount;
                                    //closure.item.onItem.Invoke(obj, closure.closure);
                                    closure.registry.isDirty = true;
                                    closure.registry.forceRebuild = true;
                                });
                            isDirty = true;
                        }
                    }
                }
                else if (delta < 0)
                {
                    if (loadingCount == 0)
                    {
                        //Debug.Log("REMOVE ITEMS: " + delta);
                        currentVisibleCount += delta;
                        module.listComponent.RemoveRange(module.listComponent.items.Count + delta,
                            module.listComponent.items.Count);
                        isDirty = true;
                    }
                }

                if (isDirty || forceRebuild)
                {
                    // Update
                    var k = 0;
                    for (int i = fromIndex; i <= toIndex; ++i)
                    {
                        if (k >= module.listComponent.items.Count)
                        {
                            break;
                        }

                        WindowComponent instance = module.listComponent.items[k];
                        Item<T, TClosure> item = items[i];
                        ref Item data = ref module.items[i];
                        if (forceRebuild)
                        {
                            data.size = module.dataSource.GetSize(i);
                        }

                        var isLocalDirty = false;
                        Vector2 pos = instance.rectTransform.anchoredPosition;

                        var axis = new Vector2(0f, 0f);
                        switch (module.direction)
                        {
                            case Direction.Horizontal:
                            {
                                axis = new Vector2(1f, 0f);
                                float posOffset = data.accumulatedSize;
                                if (Mathf.Abs(pos.x - posOffset) >= Mathf.Epsilon)
                                {
                                    pos.x = posOffset;
                                    pos.y = 0f;
                                    instance.rectTransform.anchoredPosition = pos;
                                    isLocalDirty = true;
                                }
                            }
                                break;

                            case Direction.HorizontalUpside:
                            {
                                axis = new Vector2(1f, 0f);
                                float posOffset = contentSize - data.accumulatedSize - data.size;
                                if (Mathf.Abs(pos.x - posOffset) >= Mathf.Epsilon)
                                {
                                    pos.x = -posOffset;
                                    pos.y = 0f;
                                    instance.rectTransform.anchoredPosition = pos;
                                    isLocalDirty = true;
                                }
                            }
                                break;

                            case Direction.Vertical:
                            {
                                axis = new Vector2(0f, 1f);
                                float posOffset = data.accumulatedSize;
                                if (Mathf.Abs(pos.y - posOffset) >= Mathf.Epsilon)
                                {
                                    pos.y = -posOffset;
                                    pos.x = 0f;
                                    instance.rectTransform.anchoredPosition = pos;
                                    isLocalDirty = true;
                                }
                            }
                                break;

                            case Direction.VerticalUpside:
                            {
                                axis = new Vector2(0f, 1f);
                                float posOffset = contentSize - data.accumulatedSize - data.size;
                                if (Mathf.Abs(pos.y - posOffset) >= Mathf.Epsilon)
                                {
                                    pos.y = posOffset;
                                    pos.x = 0f;
                                    instance.rectTransform.anchoredPosition = pos;
                                    isLocalDirty = true;
                                }
                            }
                                break;
                        }

                        var newSize = new Vector2(data.size * axis.x, data.size * axis.y);
                        if (instance.rectTransform.sizeDelta != newSize)
                        {
                            instance.rectTransform.sizeDelta = newSize;
                            isLocalDirty = true;
                        }

                        if (isLocalDirty || this.forceRebuild || forceRebuild)
                        {
                            LayoutRebuilder.ForceRebuildLayoutImmediate(instance.rectTransform);
                        }

                        {
                            //if (item.closure.index != i || item.initialized == false)
                            {
                                item.closure.index = i;
                                item.onItem.Invoke((T) instance, item.closure);
                            }
                        }

                        ++k;
                    }

                    isDirty = false;
                }
            }

            private struct InnerClosure : IListClosureParameters
            {
                public int index { get; set; }

                public TClosure closure;
                public Registry<T, TClosure> registry;
                public Item<T, TClosure> item;
            }
        }

        public struct Item<T, TClosure> where T : WindowComponent where TClosure : IListClosureParameters
        {
            public Resource source;
            public Action<T, TClosure> onItem;
            public TClosure closure;
            public bool initialized;
        }

        [Serializable]
        public struct Item
        {
            public float size;
            public float accumulatedSize;
        }
    }
}