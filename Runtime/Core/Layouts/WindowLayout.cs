using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;

namespace UnityEngine.UI.Windows
{
    [RequireComponent(typeof(Canvas))]
    [RequireComponent(typeof(CanvasScaler))]
    public class WindowLayout : WindowObject, IHasPreview
    {
        [HideInInspector] public Canvas canvas;
        [HideInInspector] public CanvasScaler canvasScaler;
        [HideInInspector] public bool isRootLayout = true;
        [HideInInspector] public WindowLayoutElement[] layoutElements;

        [TabGroup("Basic")] public bool useSafeZone;

        [TabGroup("Basic")] [ShowIf(nameof(useSafeZone))]
        public WindowLayoutSafeZone safeZone;

        private readonly Dictionary<int, WindowComponent> loadedComponents = new();
        [TabGroup("Tools")] public ToolsWindowLayout tools;

        private int order;

        public void SetLoadedComponent(int tag, WindowComponent instance)
        {
            loadedComponents.Add(tag, instance);
        }

        public WindowComponent GetLoadedComponent(int tag)
        {
            if (loadedComponents.TryGetValue(tag, out WindowComponent component))
            {
                return component;
            }

            return null;
        }

        public override void OnInit()
        {
            base.OnInit();

            if (useSafeZone && isRootLayout)
            {
                safeZone.Apply();
            }
        }

        public override void OnDeInit()
        {
            base.OnDeInit();

            loadedComponents.Clear();
        }

        public override void OnShowBegin()
        {
            base.OnShowBegin();

            SetTransformFullRect();
        }

        public void SetCanvasOrder(int order)
        {
            this.order = order;
            canvas.sortingOrder = order;
        }

        public int GetCanvasOrder()
        {
            return order;
        }

        public Canvas GetCanvas()
        {
            return canvas;
        }

        internal override void Setup(WindowBase source)
        {
            base.Setup(source);

            ApplyRenderMode();

            canvas.worldCamera = source.workCamera;

            if (canvas.isRootCanvas == false)
            {
                canvasScaler.enabled = false;
            }

            SetCanvasOrder(order);
        }

        internal void ApplyRenderMode()
        {
            switch (window.preferences.renderMode)
            {
                case UIWSRenderMode.UseSettings:
                    ApplyRenderMode(WindowSystem.GetSettings().canvas.renderMode);
                    break;

                case UIWSRenderMode.WorldSpace:
                    ApplyRenderMode(RenderMode.WorldSpace);
                    break;

                case UIWSRenderMode.ScreenSpaceCamera:
                    ApplyRenderMode(RenderMode.ScreenSpaceCamera);
                    break;

                case UIWSRenderMode.ScreenSpaceOverlay:
                    ApplyRenderMode(RenderMode.ScreenSpaceOverlay);
                    break;
            }
        }

        internal void ApplyRenderMode(RenderMode mode)
        {
            switch (mode)
            {
                case RenderMode.WorldSpace:
                    canvas.renderMode = RenderMode.WorldSpace;
                    window.workCamera.enabled = true;
                    break;

                case RenderMode.ScreenSpaceCamera:
                    canvas.renderMode = RenderMode.ScreenSpaceCamera;
                    window.workCamera.enabled = true;
                    break;

                case RenderMode.ScreenSpaceOverlay:
                    canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    window.workCamera.enabled = false;
                    break;
            }
        }

        public override void ValidateEditor()
        {
            base.ValidateEditor();

            canvas = GetComponent<Canvas>();
            canvasScaler = GetComponent<CanvasScaler>();
            WindowLayoutElement[] prevElements = layoutElements;
            layoutElements = GetComponentsInChildren<WindowLayoutElement>(true);

            if (WindowSystem.HasInstance())
            {
                canvas.renderMode = WindowSystem.GetSettings().canvas.renderMode;
            }

            ApplyTags(prevElements);
        }

        private void ApplyTags(WindowLayoutElement[] prevElements)
        {
            foreach (WindowLayoutElement element in layoutElements)
            {
                if (element.tagId != 0)
                {
                    if (layoutElements.Count(x => x.tagId == element.tagId) > 1 &&
                        prevElements.Contains(element) == false)
                    {
                        element.tagId = 0;
                    }
                }
            }

            var localTagId = 0;
            foreach (WindowLayoutElement element in layoutElements)
            {
                element.windowId = windowId;
                if (element.tagId == 0)
                {
                    int reqId = ++localTagId;
                    while (layoutElements.Any(x => x.tagId == reqId))
                    {
                        ++reqId;
                    }

                    element.tagId = ++localTagId;
                }
                else
                {
                    localTagId = element.tagId;
                }
            }
        }

        public WindowLayoutElement GetLayoutElementByTagId(int tagId)
        {
            for (var i = 0; i < layoutElements.Length; ++i)
            {
                if (layoutElements[i].tagId == tagId)
                {
                    return layoutElements[i];
                }
            }

            return default;
        }

        public bool HasLayoutElementByTagId(int tagId)
        {
            return GetLayoutElementByTagId(tagId) != null;
        }
    }
}