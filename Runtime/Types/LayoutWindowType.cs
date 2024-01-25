using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine.UI.Windows.Modules;
using UnityEngine.UI.Windows.Utilities;

namespace UnityEngine.UI.Windows.WindowTypes
{
    [Serializable]
    public class LayoutItem
    {
        [Tooltip("Current target filter for this layout. If no layout filtered - will be used layout with 0 index.")]
        public WindowSystemTargets targets;

        [ShowIf("IsShownWindowLayout")] public WindowLayout windowLayout;

        [SearchAssetsByTypePopup(typeof(WindowLayoutPreferences), menuName: "Layout Preferences")]
        public WindowLayoutPreferences layoutPreferences;

        public LayoutComponentItem[] components;

        [HideInInspector] public int localTag;

        internal WindowLayout windowLayoutInstance;

        private int loadingCount;

        private bool IsShownWindowLayout()
        {
            return windowLayout == null;
        }

        public void Unload()
        {
            windowLayoutInstance = null;
            for (var i = 0; i < components.Length; ++i)
            {
                components[i].componentInstance = null;
            }
        }

        public T FindComponent<T>(Func<T, bool> filter = null) where T : WindowComponent
        {
            return windowLayoutInstance.FindComponent(filter);
        }

        public int GetCanvasOrder()
        {
            if (windowLayoutInstance == null)
            {
                return 0;
            }

            return windowLayoutInstance.GetCanvasOrder();
        }

        public Canvas GetCanvas()
        {
#if UNITY_EDITOR
            if (Application.isPlaying == false)
            {
                return windowLayout.GetCanvas();
            }
#endif

            if (windowLayoutInstance == null)
            {
                return null;
            }

            return windowLayoutInstance.GetCanvas();
        }

        public void Validate(DirtyHelper helper)
        {
            helper.Set(ref localTag, 0);
        }

        public bool HasLocalTagId(int localTagId)
        {
            for (var i = 0; i < components.Length; ++i)
            {
                if (components[i].localTag == localTagId)
                {
                    return true;
                }
            }

            return false;
        }

        public WindowLayoutElement GetLayoutElement(int localTagId)
        {
            int globalTag = -1;
            for (var i = 0; i < components.Length; ++i)
            {
                LayoutComponentItem comp = components[i];
                if (comp.localTag == localTagId)
                {
                    globalTag = comp.tag;
                    break;
                }
            }

            for (var i = 0; i < windowLayoutInstance.layoutElements.Length; ++i)
            {
                if (windowLayoutInstance.layoutElements[i].tagId == globalTag)
                {
                    return windowLayoutInstance.layoutElements[i];
                }
            }

            return null;
        }

        public bool GetLayoutComponent<T>(out T component, int localTagId) where T : WindowComponent
        {
            for (var i = 0; i < components.Length; ++i)
            {
                LayoutComponentItem comp = components[i];
                if (comp.localTag == localTagId)
                {
                    component = comp.componentInstance as T;
                    return true;
                }
            }

            component = default;
            return false;
        }

        public bool GetLayoutComponent<T>(out T component, ref int lastIndex, Algorithm algorithm)
            where T : WindowComponent
        {
            if (algorithm == Algorithm.GetFirstTypeAny || algorithm == Algorithm.GetNextTypeAny)
            {
                for (var i = 0; i < components.Length; ++i)
                {
                    WindowComponent comp = components[i].componentInstance;
                    if (comp != null && comp is T c && lastIndex < i)
                    {
                        lastIndex = i;
                        component = c;
                        return true;
                    }
                }
            }
            else if (algorithm == Algorithm.GetFirstTypeStrong || algorithm == Algorithm.GetNextTypeStrong)
            {
                Type typeOf = typeof(T);
                for (var i = 0; i < components.Length; ++i)
                {
                    WindowComponent comp = components[i].componentInstance;
                    if (comp != null && comp.GetType() == typeOf && lastIndex < i)
                    {
                        lastIndex = i;
                        component = (T) comp;
                        return true;
                    }
                }
            }

            component = default;
            return false;
        }

        public void Unload(LayoutWindowType windowInstance)
        {
            WindowSystemResources resources = WindowSystem.GetResources();
            resources.DeleteAll(windowInstance);
        }

        public void LoadAsync(InitialParameters initialParameters, LayoutWindowType windowInstance, Action onComplete)
        {
            windowInstance.Setup(windowInstance);

            var used = new HashSet<WindowLayout>();
            LayoutItem layoutItem = this;
            Coroutines.Run(layoutItem.InitLayoutInstance(initialParameters, windowInstance, windowInstance,
                layoutItem.windowLayout, used, onComplete));
        }

        public void ApplyLayoutPreferences(WindowLayoutPreferences layoutPreferences)
        {
            if (layoutPreferences != null)
            {
                layoutPreferences.Apply(windowLayoutInstance.canvasScaler);
            }
        }

        private IEnumerator InitLayoutInstance(InitialParameters initialParameters, LayoutWindowType windowInstance,
            WindowObject root, WindowLayout windowLayout, HashSet<WindowLayout> used, Action onComplete,
            bool isInner = false)
        {
            if (((ILayoutInstance) root).windowLayoutInstance != null || windowLayout == null)
            {
                if (onComplete != null)
                {
                    onComplete.Invoke();
                }

                yield break;
            }

            if (windowLayout.createPool)
            {
                WindowSystem.GetPools().CreatePool(windowLayout);
            }

            WindowLayout windowLayoutInstance = WindowSystem.GetPools().Spawn(windowLayout, root.transform);
            windowLayoutInstance.isRootLayout = isInner == false;

            if (isInner)
            {
                windowLayoutInstance.canvasScaler.enabled = false;
            }

            windowLayoutInstance.Setup(windowInstance);
            windowLayoutInstance.SetCanvasOrder(windowInstance.GetCanvasDepth());
            root.RegisterSubObject(windowLayoutInstance);
            ((ILayoutInstance) root).windowLayoutInstance = windowLayoutInstance;
            ApplyLayoutPreferences(layoutPreferences);

            windowLayoutInstance.SetTransformFullRect();

            used.Add(this.windowLayout);

            loadingCount = 0;
            LayoutComponentItem[] arr = components;
            for (var i = 0; i < arr.Length; ++i)
            {
                LayoutComponentItem layoutComponent = arr[i];
                if (layoutComponent.windowLayout != windowLayout)
                {
                    continue;
                }

                WindowLayoutElement layoutElement = windowLayoutInstance.GetLayoutElementByTagId(layoutComponent.tag);
                layoutComponent.componentInstance = windowLayoutInstance.GetLoadedComponent(layoutComponent.tag);
                layoutElement.Setup(windowInstance);
                arr[i] = layoutComponent;

                if (layoutComponent.componentInstance == null)
                {
                    if (layoutComponent.component.IsEmpty() == false)
                    {
                        WindowSystemResources resources = WindowSystem.GetResources();
                        var data = new LoadingClosure
                        {
                            index = i,
                            element = layoutElement,
                            windowLayoutInstance = windowLayoutInstance,
                            layoutComponentItems = arr,
                            instance = this,
                            initialParameters = initialParameters
                        };
                        ++loadingCount;
                        Coroutines.Run(resources.LoadAsync<WindowComponent, LoadingClosure>(
                            new WindowSystemResources.LoadParameters {async = !initialParameters.showSync},
                            layoutElement, data, layoutComponent.component, (asset, closure) =>
                            {
                                if (asset == null)
                                {
                                    Debug.LogWarning(
                                        "Component is null while component resource is not empty. Skipped.");
                                    --closure.instance.loadingCount;
                                    return;
                                }

                                ref LayoutComponentItem item = ref closure.layoutComponentItems[closure.index];

                                WindowComponent instance = closure.element.Load(asset);
                                instance.SetInvisible();
                                closure.windowLayoutInstance.SetLoadedComponent(item.tag, instance);
                                item.componentInstance = instance;

                                instance.DoLoadScreenAsync(closure.initialParameters,
                                    () => { --closure.instance.loadingCount; });
                            }));
                    }
                }
            }

            while (loadingCount > 0)
            {
                yield return null;
            }

            for (var i = 0; i < arr.Length; ++i)
            {
                LayoutComponentItem layoutComponent = arr[i];
                if (layoutComponent.windowLayout != windowLayout)
                {
                    continue;
                }

                WindowLayoutElement layoutElement = windowLayoutInstance.GetLayoutElementByTagId(layoutComponent.tag);
                if (layoutElement.innerLayout != null)
                {
                    if (used.Contains(layoutElement.innerLayout) == false)
                    {
                        yield return InitLayoutInstance(initialParameters, windowInstance, layoutElement,
                            layoutElement.innerLayout, used, null, true);
                    }
                    else
                    {
                        Debug.LogWarning("Ignoring inner layout because of a cycle");
                    }
                }
            }

            if (onComplete != null)
            {
                onComplete.Invoke();
            }
        }

        public void Add(DirtyHelper helper, int tag, WindowLayout windowLayout)
        {
            for (var i = 0; i < components.Length; ++i)
            {
                if (components[i].tag == tag && components[i].windowLayout == windowLayout)
                {
                    return;
                }
            }

            List<LayoutComponentItem> list = components.ToList();
            list.Add(new LayoutComponentItem
            {
                component = new Resource(),
                componentInstance = null,
                tag = tag,
                localTag = ++localTag,
                windowLayout = windowLayout
            });
            helper.Set(ref components, list.ToArray());
        }

        public void Remove(DirtyHelper helper, int tag, WindowLayout windowLayout)
        {
            for (var i = 0; i < components.Length; ++i)
            {
                if (components[i].tag == tag && components[i].windowLayout == windowLayout)
                {
                    List<LayoutComponentItem> list = components.ToList();
                    list.RemoveAt(i);
                    helper.Set(ref components, list.ToArray());
                }
            }
        }

        public bool GetLayoutComponentItemByTagId(int tagId, WindowLayout windowLayout,
            out LayoutComponentItem layoutComponentItem)
        {
            for (var i = 0; i < components.Length; ++i)
            {
                if (components[i].tag == tagId && components[i].windowLayout == windowLayout)
                {
                    layoutComponentItem = components[i];
                    return true;
                }
            }

            layoutComponentItem = default;
            return false;
        }

        [Serializable]
        public struct LayoutComponentItem : IEquatable<LayoutComponentItem>
        {
            [HideInInspector] public WindowLayout windowLayout;
            [ReadOnly] public int tag;
            [HideInInspector] public int localTag;

            [ResourceType(typeof(WindowComponent))]
            public Resource component;

            [NonSerialized] internal WindowComponent componentInstance;

            public bool Equals(LayoutComponentItem other)
            {
                return Equals(windowLayout, other.windowLayout) && tag == other.tag && localTag == other.localTag &&
                       component.Equals(other.component) && Equals(componentInstance, other.componentInstance);
            }

            public override bool Equals(object obj)
            {
                return obj is LayoutComponentItem other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int hashCode = windowLayout != null ? windowLayout.GetHashCode() : 0;
                    hashCode = (hashCode * 397) ^ tag;
                    hashCode = (hashCode * 397) ^ localTag;
                    hashCode = (hashCode * 397) ^ component.GetHashCode();
                    hashCode = (hashCode * 397) ^ (componentInstance != null ? componentInstance.GetHashCode() : 0);
                    return hashCode;
                }
            }
        }

        private struct LoadingClosure
        {
            public int index;
            public WindowLayoutElement element;
            public WindowLayout windowLayoutInstance;
            public LayoutComponentItem[] layoutComponentItems;
            public LayoutItem instance;
            public InitialParameters initialParameters;
        }
    }

    [Serializable]
    public struct Layouts
    {
        public List<LayoutItem> items;

        private int activeIndex;
        private bool forcedIndex;

        public void Unload()
        {
            for (int i = items.Count - 1; i >= 0; --i)
            {
                items[i].Unload();
            }
        }

        public void SetActive(int index)
        {
            activeIndex = index;
            forcedIndex = true;
        }

        public void SetActive()
        {
            if (forcedIndex)
            {
                return;
            }

            TargetData targetData = WindowSystem.GetTargetData();
            for (int i = items.Count - 1; i >= 0; --i)
            {
                if (items[i].targets.IsValid(targetData))
                {
                    activeIndex = i;
                    return;
                }
            }

            activeIndex = 0;
        }

        public LayoutItem GetActive()
        {
            return items[activeIndex];
        }
    }

    public enum Algorithm
    {
        /// <summary>
        ///     Returns next component derived from type T or of type T
        /// </summary>
        GetNextTypeAny,

        /// <summary>
        ///     Returns next component strongly of type T
        /// </summary>
        GetNextTypeStrong,

        /// <summary>
        ///     Returns first component derived from type T or of type T
        /// </summary>
        GetFirstTypeAny,

        /// <summary>
        ///     Returns first component strongly of type T
        /// </summary>
        GetFirstTypeStrong
    }

    public abstract class LayoutWindowType : WindowBase, ILayoutInstance
    {
        [TabGroup("Layouts")] public Layouts layouts = new()
        {
            items = new List<LayoutItem>()
        };

        private readonly Dictionary<int, int> requestedIndexes = new();

        WindowLayout ILayoutInstance.windowLayoutInstance
        {
            get => layouts.GetActive().windowLayoutInstance;
            set => layouts.GetActive().windowLayoutInstance = value;
        }

        internal override void OnDeInitInternal()
        {
            LayoutItem currentItem = layouts.GetActive();
            currentItem.Unload(this);

            layouts.Unload();
            requestedIndexes.Clear();

            base.OnDeInitInternal();
        }

        public override int GetCanvasOrder()
        {
            return layouts.GetActive().GetCanvasOrder();
        }

        public override WindowLayoutPreferences GetCurrentLayoutPreferences()
        {
            return layouts.GetActive().layoutPreferences;
        }

        public override Canvas GetCanvas()
        {
            return layouts.GetActive().GetCanvas();
        }

        public bool GetLayoutComponent<T>(out T component, int localTagId) where T : WindowComponent
        {
            LayoutItem currentItem = layouts.GetActive();
            return currentItem.GetLayoutComponent(out component, localTagId);
        }

        public WindowLayoutElement GetLayoutElement(int localTagId)
        {
            LayoutItem currentItem = layouts.GetActive();
            return currentItem.GetLayoutElement(localTagId);
        }

        /// <summary>
        ///     Returns component instance of type T
        /// </summary>
        /// <param name="component">Component instance</param>
        /// <param name="algorithm">Algorithm which will be used to return component</param>
        /// <typeparam name="T"></typeparam>
        /// <returns>Return true if component found, otherwise false</returns>
        public bool GetLayoutComponent<T>(out T component, Algorithm algorithm = Algorithm.GetNextTypeAny)
            where T : WindowComponent
        {
            component = default;
            var result = false;
            switch (algorithm)
            {
                case Algorithm.GetNextTypeAny:
                case Algorithm.GetNextTypeStrong:
                {
                    int key = typeof(T).GetHashCode();
                    var addNew = false;
                    LayoutItem currentItem = layouts.GetActive();
                    if (requestedIndexes.TryGetValue(key, out int lastIndex) == false)
                    {
                        addNew = true;
                        lastIndex = -1;
                    }

                    result = currentItem.GetLayoutComponent(out component, ref lastIndex, algorithm);

                    if (addNew)
                    {
                        requestedIndexes.Add(key, lastIndex);
                    }
                    else
                    {
                        requestedIndexes[key] = lastIndex;
                    }
                }
                    break;

                case Algorithm.GetFirstTypeAny:
                case Algorithm.GetFirstTypeStrong:
                {
                    var idx = 0;
                    LayoutItem currentItem = layouts.GetActive();
                    result = currentItem.GetLayoutComponent(out component, ref idx, algorithm);
                }
                    break;
            }

            return result;
        }

        public override void LoadAsync(InitialParameters initialParameters, Action onComplete)
        {
            layouts.SetActive();

            LayoutItem currentItem = layouts.GetActive();
            currentItem.LoadAsync(initialParameters, this, () => { base.LoadAsync(initialParameters, onComplete); });
        }

        public int GetNextTagId(LayoutItem layoutItem)
        {
            var tagId = 1;
            while (layoutItem.components.Any(x => x.tag == tagId))
            {
                ++tagId;
            }

            return tagId;
        }

        public int GetNextLocalTagId(LayoutItem layoutItem)
        {
            var tagId = 1;
            while (layoutItem.components.Any(x => x.localTag == tagId))
            {
                ++tagId;
            }

            return tagId;
        }

        private void ValidateLayout(DirtyHelper helper, ref LayoutItem layoutItem, WindowLayout windowLayout,
            HashSet<WindowLayout> used)
        {
            used.Add(windowLayout);

            // Validate tags
            for (int j = layoutItem.components.Length - 1; j >= 0; --j)
            {
                LayoutItem.LayoutComponentItem com = layoutItem.components[j];
                if (com.localTag == 0 || layoutItem.components.Count(x => x.localTag == com.localTag) > 1)
                {
                    helper.Set(ref com.localTag, GetNextLocalTagId(layoutItem));
                }

                /*if (layoutItem.components.Count(x => x.tag == com.tag && x.windowLayout == com.windowLayout) > 1) {

                    com.tag = 0;

                }*/

                layoutItem.components[j] = com;
            }

            // Remove unused
            {
                for (var j = 0; j < layoutItem.components.Length; ++j)
                {
                    int tag = layoutItem.components[j].tag;
                    if (windowLayout.HasLayoutElementByTagId(tag) == false)
                    {
                        layoutItem.Remove(helper, tag, windowLayout);
                    }
                }
            }

            for (var j = 0; j < windowLayout.layoutElements.Length; ++j)
            {
                WindowLayoutElement layoutElement = windowLayout.layoutElements[j];
                if (layoutElement == null)
                {
                    Debug.LogError($"Layout Element is null at index {j} on window layout {windowLayout}",
                        windowLayout);
                    continue;
                }

                if (layoutElement.hideInScreen)
                {
                    layoutItem.Remove(helper, layoutElement.tagId, windowLayout);
                    continue;
                }

                if (layoutItem.GetLayoutComponentItemByTagId(layoutElement.tagId, windowLayout,
                        out LayoutItem.LayoutComponentItem layoutComponentItem) == false)
                {
                    layoutItem.Add(helper, layoutElement.tagId, windowLayout);
                }

                if (layoutElement.innerLayout != null)
                {
                    if (used.Contains(layoutElement.innerLayout) == false)
                    {
                        ValidateLayout(helper, ref layoutItem, layoutElement.innerLayout, used);
                    }
                    else
                    {
                        Debug.LogWarning(
                            $"Ignoring inner layout `{layoutElement.innerLayout}` because of a cycle. Remove innerLayout reference from {layoutElement}",
                            layoutElement);
                    }
                }
            }
        }

        public override void ValidateEditor()
        {
            base.ValidateEditor();

            List<LayoutItem> items = layouts.items;

            if (items == null)
            {
                return;
            }

            var helper = new DirtyHelper(this);

            for (var i = 0; i < items.Count; ++i)
            {
                LayoutItem layoutItem = items[i];

                if (layoutItem == null)
                {
                    helper.SetObj(ref layoutItem, new LayoutItem());
                }

                layoutItem.Validate(helper);

                WindowLayout windowLayout = layoutItem.windowLayout;

                if (windowLayout != null)
                {
                    windowLayout.ValidateEditor();

                    {
                        // Validate components list

                        for (var c = 0; c < layoutItem.components.Length; ++c)
                        {
                            ref LayoutItem.LayoutComponentItem com = ref layoutItem.components[c];
                            LayoutItem.LayoutComponentItem comLock = com;
                            if ((windowLayout != com.windowLayout ||
                                 windowLayout.HasLayoutElementByTagId(com.tag) == false) &&
                                windowLayout.layoutElements.Any(x =>
                                {
                                    return x.innerLayout != null && x.innerLayout == comLock.windowLayout &&
                                           x.innerLayout.HasLayoutElementByTagId(comLock.tag);
                                }) == false)
                            {
                                List<LayoutItem.LayoutComponentItem> list = layoutItem.components.ToList();
                                list.RemoveAt(c);
                                helper.SetObj(ref layoutItem.components, list.ToArray());
                                --c;
                            }
                        }
                    }

                    var used = new HashSet<WindowLayout>();

                    ValidateLayout(helper, ref layoutItem, windowLayout, used);

                    used.Clear();
                    ValidateLayout(helper, ref layoutItem, windowLayout, used);
                }
            }

            layouts.items = items;
            helper.Apply();
        }
    }
}