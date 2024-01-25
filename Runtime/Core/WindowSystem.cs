using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine.UI.Windows.Components;
using UnityEngine.UI.Windows.Modules;
using UnityEngine.UI.Windows.Utilities;

namespace UnityEngine.UI.Windows
{
    public enum SequenceEvent
    {
        OnHideBegin,
        OnHideEnd
    }

    public enum UIWSCameraMode
    {
        UseSettings,
        Orthographic,
        Perspective,
        UseCameraSettings
    }

    public enum UIWSRenderMode
    {
        UseSettings,

        /// <summary>
        ///     <para>Render at the end of the Scene using a 2D Canvas.</para>
        /// </summary>
        ScreenSpaceOverlay,

        /// <summary>
        ///     <para>Render using the Camera configured on the Canvas.</para>
        /// </summary>
        ScreenSpaceCamera,

        /// <summary>
        ///     <para>Render using any Camera in the Scene that can render the layer.</para>
        /// </summary>
        WorldSpace
    }

    public interface IHasPreview
    {
    }

    public enum DontDestroy
    {
        Default = 0x0,
        Ever = -1,

        OnHideAll = 0x1
    }

    [Serializable]
    public struct WindowPreferences
    {
        [Tooltip("Window layer to draw on, all windows are sorting by layer first.")]
        public UIWSLayer layer;

        [Tooltip("Check if you need to show this window only once.")]
        public bool singleInstance;

        [Tooltip("Force sync screen load even loading set up via async WindowSystem.Show API")]
        public bool forceSyncLoad;

        [Space(10f)] [Tooltip("Add in history into current breadcrumb.")]
        public bool addInHistory;

        [Tooltip("Take focus on window open and send unfocused event to all windows behind.")]
        public bool takeFocus;

        [Space(10f)] public DontDestroy dontDestroy;

        [Space(10f)] [Tooltip("Override canvas render mode.")]
        public UIWSRenderMode renderMode;

        [Tooltip("Override camera mode. You can change camera mode at runtime.")]
        public UIWSCameraMode cameraMode;

        [Space(10f)] [Tooltip("Collect screens into queue and use this queue automatically on event.")]
        public bool showInSequence;

        public SequenceEvent showInSequenceEvent;

        [Header("Performance Options")]
        [Tooltip(
            "If this screen has full-rect opaque background you can set this option as true to deactivate render on all screens behind this.")]
        public bool fullCoverage;

        public static WindowPreferences Default => new()
        {
            layer = new UIWSLayer {value = 0},
            takeFocus = true,
            forceSyncLoad = true
        };
    }

    public struct InitialParameters
    {
        public bool overrideLayer;
        public UIWSLayer layer;

        public bool overrideSingleInstance;
        public bool singleInstance;

        public bool showSync;
    }

    [Serializable]
    public struct TransitionParametersData
    {
        internal bool resetAnimation;
        internal bool immediately;

        internal bool replaceDelay;
        internal float delay;

        internal bool replaceAffectChilds;
        internal bool affectChilds;

        internal bool replaceIgnoreTouch;
        internal bool ignoreTouch;

        internal Action callback;
    }

    internal struct TransitionInternalData
    {
        public WindowObject context;
        public TransitionParametersData data;
        public bool internalCall;
    }

    [Serializable]
    public struct TransitionParameters
    {
        internal TransitionParametersData data;

        private Action<WindowObject, TransitionParameters, bool> contextCallback;
        private TransitionInternalData internalData;

        public static TransitionParameters Default => new()
        {
            data = new TransitionParametersData {resetAnimation = false}
        };

        public void RaiseCallback()
        {
            if (contextCallback != null)
            {
                contextCallback.Invoke(internalData.context, new TransitionParameters {data = internalData.data},
                    internalData.internalCall);
            }

            if (data.callback != null)
            {
                data.callback.Invoke();
            }
        }

        public TransitionParameters ReplaceIgnoreTouch(bool state)
        {
            TransitionParameters instance = this;
            instance.data.replaceIgnoreTouch = true;
            instance.data.ignoreTouch = state;
            return instance;
        }

        public TransitionParameters ReplaceAffectChilds(bool state)
        {
            TransitionParameters instance = this;
            instance.data.replaceAffectChilds = true;
            instance.data.affectChilds = state;
            return instance;
        }

        public TransitionParameters ReplaceResetAnimation(bool state)
        {
            TransitionParameters instance = this;
            instance.data.resetAnimation = state;
            return instance;
        }

        public TransitionParameters ReplaceImmediately(bool state)
        {
            TransitionParameters instance = this;
            instance.data.immediately = state;
            return instance;
        }

        public TransitionParameters ReplaceDelay(float value)
        {
            TransitionParameters instance = this;
            instance.data.delay = value;
            instance.data.replaceDelay = value > 0f;
            return instance;
        }

        public TransitionParameters ReplaceCallback(Action callback)
        {
            TransitionParameters instance = this;
            instance.data.callback = callback;
            instance.contextCallback = null;
            return instance;
        }

        public TransitionParameters ReplaceCallbackWithContext(
            Action<WindowObject, TransitionParameters, bool> callback, WindowObject context, TransitionParameters other,
            bool internalCall)
        {
            TransitionParameters instance = this;
            instance.data.callback = null;
            instance.contextCallback = callback;
            instance.internalData = new TransitionInternalData
                {context = context, data = other.data, internalCall = internalCall};
            return instance;
        }
    }

    public enum WindowEvent
    {
        None = 0,

        OnInitialize,
        OnDeInitialize,

        OnShowBegin,
        OnShowEnd,

        OnHideBegin,
        OnHideEnd,

        OnFocusTook,
        OnFocusLost,

        OnLayoutReady
    }

    [DefaultExecutionOrder(-1000)]
    public class WindowSystem : MonoBehaviour
    {
        [Serializable]
        public struct WindowItem
        {
            public WindowBase prefab;
            public WindowBase instance;
        }

        public static Action onPointerUp;
        public static Action onPointerDown;

        [TabGroup("Start Up")] [Tooltip("Platform emulation for target filters.")]
        public bool emulatePlatform;

        [TabGroup("Start Up")] public RuntimePlatform emulateRuntimePlatform;

        [TabGroup("Start Up")] [Tooltip("Automatically show `Root Screen` on Start.")]
        public bool showRootOnStart;

        [TabGroup("Start Up")] public WindowBase rootScreen;

        [TabGroup("Start Up")] public WindowBase loaderScreen;

        [TabGroup("Start Up")] [RequiredReference]
        public WindowSystemSettings settings;

        [TabGroup("Modules")] [RequiredReference]
        public WindowSystemBreadcrumbs breadcrumbs;

        [TabGroup("Modules")] [RequiredReference]
        public WindowSystemEvents events;

        [TabGroup("Modules")] [RequiredReference]
        public WindowSystemResources resources;

        [TabGroup("Modules")] [RequiredReference]
        public WindowSystemPools pools;

        [TabGroup("Modules")] [RequiredReference]
        public Tweener tweener;

        [TabGroup("Modules")] public new WindowSystemAudio audio;

        [TabGroup("Modules")] [SearchAssetsByTypePopupAttribute(typeof(WindowSystemModule), menuName: "Modules")]
        public List<WindowSystemModule> modules = new();

        [TabGroup("Windows")] public List<WindowBase> registeredPrefabs = new();

        [TabGroup("Tools")] public ToolsWindowSystem ToolsWindowSystem;

        private readonly List<WindowItem> currentWindows = new();
        private readonly Dictionary<int, int> windowsCountByLayer = new();
        private readonly Dictionary<int, WindowBase> topWindowsByLayer = new();
        private readonly Dictionary<int, WindowBase> hashToPrefabs = new();

        private bool loaderShowBegin;
        private WindowBase loaderInstance;
        private int nextWindowId;

        private Action waitInteractableOnComplete;
        private IInteractable waitInteractable;
        private IInteractable[] waitInteractables;
        private bool lockInteractables;
        private Action<IInteractable> callbackOnAnyInteractable;
        private readonly List<WindowObject> interactablesIgnoreContainers = new();

        internal static WindowSystem _instance;

        internal static WindowSystem instance
        {
            get
            {
#if UNITY_EDITOR
                if (_instance == null && Application.isPlaying == false)
                {
                    _instance = FindObjectOfType<WindowSystem>();
                }
#endif

                return _instance;
            }
        }

#if WITHOUT_DOMAIN_RELOAD
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void CleanupInstanceOnRun() {

            WindowSystem._instance = null;

        }
#endif

        public static T GetWindowSystemModule<T>() where T : WindowSystemModule
        {
            if (instance.modules != null)
            {
                for (var i = 0; i < instance.modules.Count; ++i)
                {
                    WindowSystemModule module = instance.modules[i];
                    if (module != null && module is T moduleT)
                    {
                        return moduleT;
                    }
                }
            }

            return null;
        }

        public static void AddModule<T>(T module) where T : WindowSystemModule
        {
            if (instance.modules == null)
            {
                instance.modules = new List<WindowSystemModule>();
            }

            instance.modules.Add(module);
        }

        public static T FindOpened<T>()
        {
            foreach (WindowItem item in instance.currentWindows)
            {
                if (item.instance is T win)
                {
                    return win;
                }
            }

            return default;
        }

        public static T GetFocused<T>() where T : WindowBase
        {
            foreach (WindowItem item in instance.currentWindows)
            {
                if (item.instance is T win && win.GetFocusState() == FocusState.Focused)
                {
                    return win;
                }
            }

            return default;
        }

        public static bool HasInstance()
        {
            return instance != null;
        }

        public void Awake()
        {
#if ENABLE_INPUT_SYSTEM
            UnityEngine.InputSystem.EnhancedTouch.EnhancedTouchSupport.Enable();
#endif

            Run();
        }

        public void OnEnable()
        {
            Run();
        }

        private void Run()
        {
            if (_instance != null)
            {
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            events.Initialize();
            breadcrumbs.Initialize();

            foreach (WindowBase item in registeredPrefabs)
            {
                int key = item.GetType().GetHashCode();
                if (hashToPrefabs.ContainsKey(key))
                {
                    Debug.LogWarning($"Window with hash `{key}` already exists in windows hash map!");
                    continue;
                }

                hashToPrefabs.Add(key, item);
            }
        }

        public void Start()
        {
            if (modules != null)
            {
                for (var i = 0; i < modules.Count; ++i)
                {
                    modules[i]?.OnStart();
                }
            }

            if (showRootOnStart)
            {
                ShowRoot();
            }
        }

        public void OnDestroy()
        {
            if (modules != null)
            {
                for (int i = modules.Count - 1; i >= 0; --i)
                {
                    modules[i]?.OnDestroy();
                }
            }

            _instance = null;

            onPointerUp = null;
            onPointerDown = null;
        }

        private Vector2 pointerScreenPosition;
        private bool hasPointerUpThisFrame;
        private bool hasPointerDownThisFrame;

        public static T FindComponent<T>(Func<T, bool> filter = null) where T : WindowComponent
        {
            foreach (WindowItem window in instance.currentWindows)
            {
                if (window.instance == null)
                {
                    continue;
                }

                T component = window.instance.FindComponent(filter);
                if (component != null)
                {
                    return component;
                }
            }

            return null;
        }

        public static void LockAllInteractables()
        {
            instance.lockInteractables = true;
        }

        public static void SetCallbackOnAnyInteractable(Action<IInteractable> callback)
        {
            instance.callbackOnAnyInteractable = callback;
        }

        public static void AddWaitInteractable(Action onComplete, IInteractable interactable)
        {
            instance.waitInteractableOnComplete = onComplete;

            ref IInteractable[] arr = ref instance.waitInteractables;
            if (arr == null)
            {
                arr = new IInteractable[1]
                {
                    interactable
                };
            }
            else
            {
                List<IInteractable> list = arr.ToList();
                list.Add(interactable);
                arr = list.ToArray();
            }
        }

        public static void WaitInteractable(Action onComplete, IInteractable interactable)
        {
            instance.waitInteractableOnComplete = onComplete;
            instance.waitInteractable = interactable;
            instance.waitInteractables = null;
            instance.lockInteractables = false;
        }

        public static void WaitInteractable(Action onComplete, params IInteractable[] interactables)
        {
            instance.waitInteractableOnComplete = onComplete;
            instance.waitInteractable = null;
            instance.waitInteractables = interactables;
            instance.lockInteractables = false;
        }

        public static void CancelWaitInteractables()
        {
            instance.waitInteractable = null;
            instance.waitInteractables = null;
            instance.waitInteractableOnComplete = null;
            instance.lockInteractables = false;
        }

        public static void RaiseAndCancelWaitInteractables()
        {
            if (instance.waitInteractable != null && InteractWith(instance.waitInteractable))
            {
            }

            if (instance.waitInteractables != null)
            {
                foreach (IInteractable item in instance.waitInteractables)
                {
                    var comp = item as WindowComponent;
                    if (comp != null && comp.GetState() == ObjectState.Shown)
                    {
                        InteractWith(item);
                    }
                }
            }

            CancelWaitInteractables();
        }

        public static void AddInteractablesIgnoreContainer(WindowObject container)
        {
            instance.interactablesIgnoreContainers.Add(container);
        }

        public static void RemoveInteractablesIgnoreContainer(WindowObject container)
        {
            instance.interactablesIgnoreContainers.Remove(container);
        }

        public static bool CanInteractWith(IInteractable interactable)
        {
            if (instance.lockInteractables)
            {
                return false;
            }

            for (var i = 0; i < instance.interactablesIgnoreContainers.Count; ++i)
            {
                WindowObject container = instance.interactablesIgnoreContainers[i];
                if (container != null)
                {
                    if (interactable is WindowObject interactableObj)
                    {
                        WindowObject parent =
                            interactableObj.FindComponentParent<WindowObject, WindowObject>(container,
                                (obj, x) => { return x == obj; });
                        if (parent != null)
                        {
                            return true;
                        }
                    }
                }
            }

            if (instance.waitInteractables == null)
            {
                if (instance.waitInteractable == null)
                {
                    return true;
                }

                return instance.waitInteractable == interactable;
            }

            for (var i = 0; i < instance.waitInteractables.Length; ++i)
            {
                IInteractable interactableItem = instance.waitInteractables[i];
                if (interactableItem == interactable)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool InteractWith(IInteractable interactable)
        {
            instance.callbackOnAnyInteractable?.Invoke(interactable);

            if (instance.lockInteractables)
            {
                return false;
            }

            if (instance.waitInteractables == null)
            {
                if (instance.waitInteractable == null ||
                    instance.waitInteractable == interactable)
                {
                    instance.waitInteractableOnComplete?.Invoke();
                    return true;
                }
            }
            else
            {
                for (var i = 0; i < instance.waitInteractables.Length; ++i)
                {
                    IInteractable interactableItem = instance.waitInteractables[i];
                    if (interactableItem == interactable)
                    {
                        instance.waitInteractableOnComplete?.Invoke();
                        return true;
                    }
                }
            }

            return false;
        }

        public static Vector2 GetPointerPosition()
        {
            return instance.pointerScreenPosition;
        }

        public static bool HasPointerUpThisFrame()
        {
            return instance.hasPointerUpThisFrame;
        }

        public static bool HasPointerDownThisFrame()
        {
            return instance.hasPointerDownThisFrame;
        }

        public void Update()
        {
            if (modules != null)
            {
                for (var i = 0; i < modules.Count; ++i)
                {
                    modules[i]?.OnUpdate();
                }
            }

            hasPointerUpThisFrame = false;
            hasPointerDownThisFrame = false;

#if ENABLE_INPUT_SYSTEM
            if (UnityEngine.InputSystem.Mouse.current != null &&
                (UnityEngine.InputSystem.Mouse.current.leftButton.wasReleasedThisFrame == true ||
                UnityEngine.InputSystem.Mouse.current.rightButton.wasReleasedThisFrame == true ||
                UnityEngine.InputSystem.Mouse.current.middleButton.wasReleasedThisFrame == true)) {
                
                this.pointerScreenPosition = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
                this.hasPointerUpThisFrame = true;
                if (WindowSystem.onPointerUp != null) WindowSystem.onPointerUp.Invoke();
                
            }

            var touches = UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches;
            if (touches.Count > 0) {

                for (int i = 0; i < touches.Count; ++i) {

                    var touch = touches[i];
                    if (touch.phase == UnityEngine.InputSystem.TouchPhase.Ended || touch.phase == UnityEngine.InputSystem.TouchPhase.Canceled) {

                        this.pointerScreenPosition = touch.screenPosition;
                        this.hasPointerUpThisFrame = true;
                        if (WindowSystem.onPointerUp != null) WindowSystem.onPointerUp.Invoke();

                    }

                }
                
            }
#elif ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetMouseButtonDown(0) ||
                Input.GetMouseButtonDown(1) ||
                Input.GetMouseButtonDown(2))
            {
                pointerScreenPosition = Input.mousePosition;
                hasPointerDownThisFrame = true;
                if (onPointerDown != null)
                {
                    onPointerDown.Invoke();
                }
            }

            if (Input.GetMouseButtonUp(0) ||
                Input.GetMouseButtonUp(1) ||
                Input.GetMouseButtonUp(2))
            {
                pointerScreenPosition = Input.mousePosition;
                hasPointerUpThisFrame = true;
                if (onPointerUp != null)
                {
                    onPointerUp.Invoke();
                }
            }

            if (Input.touchCount > 0)
            {
                for (var i = 0; i < Input.touches.Length; ++i)
                {
                    Touch touch = Input.GetTouch(i);
                    if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                    {
                        pointerScreenPosition = touch.position;
                        hasPointerUpThisFrame = true;
                        if (onPointerUp != null)
                        {
                            onPointerUp.Invoke();
                        }
                    }
                }
            }
#endif
        }

        public static List<WindowItem> GetCurrentOpened()
        {
            return instance.currentWindows;
        }

        public static RuntimePlatform GetCurrentRuntimePlatform()
        {
            if (instance.emulatePlatform)
            {
                return instance.emulateRuntimePlatform;
            }

            return Application.platform;
        }

        public static TargetData GetTargetData()
        {
            return new TargetData
            {
                platform = GetCurrentRuntimePlatform(),
                screenSize = new Vector2(Screen.width, Screen.height)
            };
        }

        public static void SendEvent<T>(T data)
        {
            foreach (WindowItem item in instance.currentWindows)
            {
                item.instance.SendEvent(data);
            }
        }

        internal static void SendFullCoverageOnShowEnd(WindowBase window)
        {
            if (window.preferences.fullCoverage)
            {
                instance.TurnRenderBeneath(window, false);
            }
        }

        internal static void SendFullCoverageOnHideBegin(WindowBase window)
        {
            if (window.preferences.fullCoverage)
            {
                instance.TurnRenderBeneath(window, true);
            }
        }

        internal static void SendFocusOnShowBegin(WindowBase window)
        {
            instance.SendFocusOnShowBegin_INTERNAL(window);
        }

        internal static void SendFocusOnHideBegin(WindowBase window)
        {
            instance.SendFocusOnHideBegin_INTERNAL(window);
        }

        private void TurnRenderBeneath(WindowBase window, bool state)
        {
            IOrderedEnumerable<WindowItem> ordered = currentWindows.OrderByDescending(x => x.instance.GetDepth());
            foreach (WindowItem item in ordered)
            {
                WindowBase instance = item.instance;

                float depth = instance.GetDepth();
                if (depth < window.GetDepth())
                {
                    if (state == false)
                    {
                        instance.TurnOffRender();
                    }
                    else
                    {
                        instance.TurnOnRender();
                    }

                    // If beneath window has fullCoverage - break enumeration because beneath graph has its own coverage handler
                    if (instance.preferences.fullCoverage)
                    {
                        break;
                    }
                }
            }
        }

        private WindowBase GetTopInstanceForFocus(WindowBase ignore = null)
        {
            var maxDepth = float.MinValue;
            WindowBase topInstance = null;
            for (var i = 0; i < currentWindows.Count; ++i)
            {
                WindowBase instance = currentWindows[i].instance;
                if (instance.preferences.takeFocus == false)
                {
                    continue;
                }

                if (instance.GetState() >= ObjectState.Hiding)
                {
                    continue;
                }

                float depth = instance.GetDepth();
                if (depth > maxDepth && instance != ignore)
                {
                    maxDepth = depth;
                    topInstance = instance;
                }
            }

            return topInstance;
        }

        internal void SendFocusOnShowBegin_INTERNAL(WindowBase window)
        {
            if (window.preferences.takeFocus == false)
            {
                return;
            }

            WindowBase topInstance = GetTopInstanceForFocus();
            if (topInstance != null)
            {
                WindowBase topInstancePrev = GetTopInstanceForFocus(window);
                if (topInstancePrev != null)
                {
                    topInstancePrev.DoFocusLostInternal();
                }

                topInstance.DoFocusTookInternal();
                if (topInstance != window)
                {
                    window.DoFocusLostInternal();
                }
            }
            else
            {
                window.DoFocusTookInternal();
            }
        }

        internal void SendFocusOnHideBegin_INTERNAL(WindowBase window)
        {
            if (window.preferences.takeFocus == false)
            {
                return;
            }

            WindowBase topInstance = GetTopInstanceForFocus(window);
            window.DoFocusLostInternal();
            if (topInstance != null)
            {
                topInstance.DoFocusTookInternal();
            }
        }

        public static int GetCountByLayer(UIWSLayer layer)
        {
            if (instance.windowsCountByLayer.TryGetValue(layer.value, out int count))
            {
                return count;
            }

            return 0;
        }

        public static float GetNextDepth(UIWSLayer layer)
        {
            WindowSystemSettings settings = GetSettings();
            WindowSystemSettings.Layer layerInfo = settings.GetLayerInfo(layer.value);

            if (WindowSystem.instance.topWindowsByLayer.TryGetValue(layer.value, out WindowBase instance))
            {
                float step = (layerInfo.maxDepth - layerInfo.minDepth) / settings.windowsPerLayer;
                return instance.GetDepth() + step;
            }

            return layerInfo.minDepth;
        }

        public static int GetNextCanvasDepth(UIWSLayer layer)
        {
            WindowSystemSettings settings = GetSettings();
            WindowSystemSettings.Layer layerInfo = settings.GetLayerInfo(layer.value);

            if (WindowSystem.instance.topWindowsByLayer.TryGetValue(layer.value, out WindowBase instance))
            {
                int step = settings.windowsPerLayer;
                return instance.GetCanvasDepth() + step;
            }

            return layer.value * settings.windowsPerLayer;
        }

        public static float GetNextZDepth(UIWSLayer layer)
        {
            WindowSystemSettings settings = GetSettings();
            WindowSystemSettings.Layer layerInfo = settings.GetLayerInfo(layer.value);
            float step = (layerInfo.maxZDepth - layerInfo.minZDepth) / settings.windowsPerLayer;

            if (WindowSystem.instance.topWindowsByLayer.TryGetValue(layer.value, out WindowBase instance))
            {
                return instance.GetZDepth() + step;
            }

            return layerInfo.minZDepth;
        }

        public static void RaiseEvent(WindowObject instance, WindowEvent windowEvent)
        {
            WindowSystemEvents events = GetEvents();
            events.Raise(instance, windowEvent);
        }

        public static void ClearEvents(WindowObject instance)
        {
            WindowSystemEvents events = GetEvents();
            events.Clear(instance);
        }

        public static void RegisterActionOnce(WindowObject instance, WindowEvent windowEvent, Action callback)
        {
            WindowSystemEvents events = GetEvents();
            events.RegisterOnce(instance, windowEvent, callback);
        }

        public static void RegisterAction(WindowObject instance, WindowEvent windowEvent, Action callback)
        {
            WindowSystemEvents events = GetEvents();
            events.Register(instance, windowEvent, callback);
        }

        public static void UnRegisterAction(WindowObject instance, WindowEvent windowEvent, Action callback)
        {
            WindowSystemEvents events = GetEvents();
            events.UnRegister(instance, windowEvent, callback);
        }

        public static WindowSystemBreadcrumbs GetBreadcrumbs()
        {
            return instance?.breadcrumbs;
        }

        public static WindowSystemAudio GetAudio()
        {
            return instance?.audio;
        }

        public static WindowSystemPools GetPools()
        {
            return instance?.pools;
        }

        public static WindowSystemEvents GetEvents()
        {
            return instance?.events;
        }

        public static WindowSystemSettings GetSettings()
        {
            return instance?.settings;
        }

        public static WindowSystemResources GetResources()
        {
            return instance?.resources;
        }

        public static Tweener GetTweener()
        {
            return instance?.tweener;
        }

        public class ShowHideClosureParametersClass
        {
            public WindowObject instance;
            public TransitionParameters parameters;
            public bool internalCall;
            public bool animationComplete;
            public bool hierarchyComplete;
            public bool baseComplete;

            public void Dispose()
            {
                internalCall = default;
                animationComplete = default;
                hierarchyComplete = default;
                baseComplete = default;
                instance = null;
                parameters = default;
                PoolClass<ShowHideClosureParametersClass>.Recycle(this);
            }
        }

        public static void ShowInstance(WindowObject instance, TransitionParameters parameters,
            bool internalCall = false)
        {
            if (instance.GetState() == ObjectState.Showing || instance.GetState() == ObjectState.Shown)
            {
                parameters.RaiseCallback();
                return;
            }

            instance.SetState(ObjectState.Showing);

            {
                instance.OnShowBeginInternal();
                instance.OnShowBegin();
                RaiseEvent(instance, WindowEvent.OnShowBegin);
            }

            ShowHideClosureParametersClass closure = PoolClass<ShowHideClosureParametersClass>.Spawn();
            {
                if (instance.gameObject.activeSelf == false)
                {
                    instance.gameObject.SetActive(true);
                }

                instance.SetVisible();
                instance.SetResetState();

                closure.baseComplete = false;
                closure.animationComplete = false;
                closure.hierarchyComplete = false;
                closure.instance = instance;
                closure.parameters = parameters;
                closure.internalCall = internalCall;

                if (closure.parameters.data.replaceAffectChilds == false ||
                    closure.parameters.data.affectChilds)
                {
                    instance.BreakStateHierarchy();

                    Coroutines.CallInSequence(p =>
                    {
                        p.hierarchyComplete = true;
                        if (p.animationComplete && p.baseComplete)
                        {
                            TransitionParameters pars = p.parameters;
                            p.Dispose();
                            pars.RaiseCallback();
                        }
                    }, closure, instance.subObjects, (obj, cb, p) =>
                    {
                        if (p.parameters.data.replaceDelay)
                        {
                            if (p.internalCall)
                            {
                                obj.ShowInternal(p.parameters.ReplaceCallback(cb).ReplaceDelay(0f));
                            }
                            else
                            {
                                obj.Show(p.parameters.ReplaceCallback(cb).ReplaceDelay(0f));
                            }
                        }
                        else
                        {
                            if (p.internalCall)
                            {
                                obj.ShowInternal(p.parameters.ReplaceCallback(cb));
                            }
                            else
                            {
                                obj.Show(p.parameters.ReplaceCallback(cb));
                            }
                        }
                    });
                }
                else
                {
                    instance.BreakState();

                    closure.hierarchyComplete = true;
                }

                WindowObjectAnimation.Show(closure, instance, parameters, cParams =>
                {
                    cParams.animationComplete = true;
                    if (cParams.hierarchyComplete && cParams.baseComplete)
                    {
                        TransitionParameters pars = cParams.parameters;
                        cParams.Dispose();
                        pars.RaiseCallback();
                    }
                });
            }

            closure.baseComplete = true;
            if (closure.animationComplete && closure.hierarchyComplete)
            {
                closure.Dispose();
                parameters.RaiseCallback();
            }
        }

        internal static void SetShown(WindowObject instance, TransitionParameters parameters, bool internalCall)
        {
            if (instance.GetState() != ObjectState.Showing)
            {
                parameters.RaiseCallback();
                return;
            }

            if (internalCall)
            {
                if (instance.hiddenByDefault)
                {
                    parameters.RaiseCallback();
                    return;
                }
            }

            WindowObjectAnimation.SetState(instance, Modules.AnimationState.Show);

            TransitionParameters innerParameters = parameters.ReplaceCallback(null);
            for (var i = 0; i < instance.subObjects.Count; ++i)
            {
                SetShown(instance.subObjects[i], innerParameters, internalCall);
            }

            instance.SetState(ObjectState.Shown);

            instance.OnShowEndInternal();
            instance.OnShowEnd();
            RaiseEvent(instance, WindowEvent.OnShowEnd);

            parameters.RaiseCallback();
        }

        internal static void SetHidden(WindowObject instance, TransitionParameters parameters, bool internalCall)
        {
            if (instance.GetState() != ObjectState.Hiding)
            {
                parameters.RaiseCallback();
                return;
            }

            WindowObjectAnimation.SetState(instance, Modules.AnimationState.Hide);

            TransitionParameters innerParameters = parameters.ReplaceCallback(null);
            for (var i = 0; i < instance.subObjects.Count; ++i)
            {
                SetHidden(instance.subObjects[i], innerParameters, internalCall);
            }

            instance.SetState(ObjectState.Hidden);
            instance.SetInvisible();

            instance.OnHideEndInternal();
            instance.OnHideEnd();
            RaiseEvent(instance, WindowEvent.OnHideEnd);

            parameters.RaiseCallback();
        }

        private struct HideInstanceClosure
        {
            public WindowObject instance;
            public TransitionParameters parameters;
            public bool internalCall;
        }

        public static void HideInstance(WindowObject instance, TransitionParameters parameters,
            bool internalCall = false)
        {
            if (instance.GetState() <= ObjectState.Initializing)
            {
                Debug.LogWarning("Object is out of state: " + instance);
                return;
            }

            if (instance.GetState() == ObjectState.Hiding || instance.GetState() == ObjectState.Hidden)
            {
                parameters.RaiseCallback();
                return;
            }

            instance.SetState(ObjectState.Hiding);

            instance.OnHideBeginInternal();
            instance.OnHideBegin();
            RaiseEvent(instance, WindowEvent.OnHideBegin);

            var closureInstance = new HideInstanceClosure
            {
                instance = instance,
                parameters = parameters,
                internalCall = internalCall
            };
            Coroutines.Wait(closureInstance, inst => inst.instance.IsReadyToHide(), inst =>
            {
                {
                    ShowHideClosureParametersClass closure = PoolClass<ShowHideClosureParametersClass>.Spawn();
                    closure.animationComplete = false;
                    closure.hierarchyComplete = false;
                    closure.instance = inst.instance;
                    closure.parameters = inst.parameters;
                    closure.internalCall = inst.internalCall;

                    if (inst.parameters.data.replaceAffectChilds == false ||
                        inst.parameters.data.affectChilds)
                    {
                        inst.instance.BreakStateHierarchy();
                    }
                    else
                    {
                        inst.instance.BreakState();
                    }

                    WindowObjectAnimation.Hide(closure, inst.instance, inst.parameters, cParams =>
                    {
                        if (cParams.parameters.data.replaceAffectChilds == false ||
                            cParams.parameters.data.affectChilds)
                        {
                            Coroutines.CallInSequence(p =>
                            {
                                p.hierarchyComplete = true;

                                if (p.animationComplete)
                                {
                                    TransitionParameters pars = p.parameters;
                                    p.Dispose();
                                    pars.RaiseCallback();
                                }
                            }, cParams, cParams.instance.subObjects, (obj, cb, p) =>
                            {
                                if (p.parameters.data.replaceDelay)
                                {
                                    if (p.internalCall)
                                    {
                                        obj.HideInternal(p.parameters.ReplaceCallback(cb).ReplaceDelay(0f));
                                    }
                                    else
                                    {
                                        obj.Hide(p.parameters.ReplaceCallback(cb).ReplaceDelay(0f));
                                    }
                                }
                                else
                                {
                                    if (p.internalCall)
                                    {
                                        obj.HideInternal(p.parameters.ReplaceCallback(cb));
                                    }
                                    else
                                    {
                                        obj.Hide(p.parameters.ReplaceCallback(cb));
                                    }
                                }
                            });
                        }
                        else
                        {
                            cParams.hierarchyComplete = true;
                        }

                        cParams.animationComplete = true;
                        if (cParams.hierarchyComplete)
                        {
                            TransitionParameters pars = cParams.parameters;
                            cParams.Dispose();
                            pars.RaiseCallback();
                        }
                    });
                }
            });
        }

        /// <summary>
        ///     Clean up window instance.
        ///     This instance would be removed from pools, and all resources will be free.
        /// </summary>
        /// <param name="instance"></param>
        public static void Clean(WindowBase instance)
        {
            if (instance.GetState() != ObjectState.Hidden)
            {
                throw new Exception(
                    $"WindowSystem.Clean failed because of instance state: {instance.GetState()} (required state: Hidden)");
            }

            instance.DoDeInit();

            WindowSystemPools pools = GetPools();
            pools.RemoveInstance(instance);
        }

        public static void ShowLoader(Action<WindowObject> onInitialized = null,
            TransitionParameters transitionParameters = default, bool showSync = false)
        {
            if (instance.loaderScreen != null && instance.loaderInstance == null && instance.loaderShowBegin == false)
            {
                instance.loaderShowBegin = true;
                Show(instance.loaderScreen, new InitialParameters {showSync = showSync},
                    w =>
                    {
                        instance.loaderInstance = w;
                        instance.loaderShowBegin = false;
                        onInitialized?.Invoke(w);
                    }, transitionParameters);
            }
        }

        public static void HideLoader()
        {
            if (instance.loaderShowBegin && instance.loaderInstance == null)
            {
                Coroutines.Wait(() => instance.loaderInstance != null, HideLoader);
            }
            else
            {
                if (instance.loaderInstance != null)
                {
                    instance.loaderInstance.Hide();
                    instance.loaderInstance = null;
                    instance.loaderShowBegin = false;
                }
            }
        }

        public static void ShowRoot(TransitionParameters transitionParameters = default)
        {
            Show(instance.rootScreen, transitionParameters: transitionParameters);
        }

        public static void HideAll<T>(TransitionParameters parameters = default) where T : WindowBase
        {
            HideAll_INTERNAL(x => x is T, parameters);
        }

        public static void HideAll(TransitionParameters parameters = default)
        {
            HideAll_INTERNAL(null, parameters);
        }

        public static void HideAll(Predicate<WindowBase> predicate, TransitionParameters parameters = default)
        {
            HideAll_INTERNAL(predicate, parameters);
        }

        public static void HideAllAndClean<T>(Predicate<T> predicate, TransitionParameters parameters = default)
            where T : WindowBase
        {
            HideAllAndClean(w => w is T, parameters);
        }

        public static void HideAllAndClean(Predicate<WindowBase> predicate, TransitionParameters parameters = default)
        {
            List<WindowBase> list = PoolList<WindowBase>.Spawn();
            TransitionParameters cb = parameters.ReplaceCallback(() =>
            {
                foreach (WindowBase item in list)
                {
                    Clean(item);
                }

                parameters.RaiseCallback();
                PoolList<WindowBase>.Recycle(ref list);
            });
            HideAll_INTERNAL(predicate, cb, w => { list.Add(w); });
        }

        private static bool CanBeDestroy(DontDestroy state, DontDestroy windowInstanceFlag)
        {
            return windowInstanceFlag == DontDestroy.Default || (state & windowInstanceFlag) == 0;
        }

        private static void HideAll_INTERNAL(Predicate<WindowBase> predicate, TransitionParameters parameters,
            Action<WindowBase> onWindow = null)
        {
            List<WindowItem> currentList = instance.currentWindows;
            int count = currentList.Count;
            var filteredCount = 0;
            for (var i = 0; i < count; ++i)
            {
                WindowBase instance = currentList[i].instance;
                if ((predicate == null || predicate.Invoke(instance)) &&
                    CanBeDestroy(DontDestroy.OnHideAll, instance.preferences.dontDestroy))
                {
                    ++filteredCount;
                }
            }

            if (filteredCount == 0)
            {
                parameters.RaiseCallback();
                return;
            }

            var ptr = 0;
            TransitionParameters instanceParameters = parameters.ReplaceCallback(() =>
            {
                ++ptr;
                if (ptr == filteredCount)
                {
                    parameters.RaiseCallback();
                }
            });
            for (int i = count - 1; i >= 0; --i)
            {
                WindowBase instance = currentList[i].instance;
                if ((predicate == null || predicate.Invoke(instance)) &&
                    CanBeDestroy(DontDestroy.OnHideAll, instance.preferences.dontDestroy))
                {
                    if (instance.GetState() == ObjectState.Hiding ||
                        instance.GetState() == ObjectState.Hidden)
                    {
                        instanceParameters.RaiseCallback();
                        continue;
                    }

                    instance.BreakStateHierarchy();
                    onWindow?.Invoke(instance);
                    instance.Hide(instanceParameters);
                }
            }
        }

        public static T ShowSync<T>(T source, InitialParameters initialParameters, Action<T> onInitialized = null,
            TransitionParameters transitionParameters = default) where T : WindowBase
        {
            T instance = default;
            initialParameters.showSync = true;
            WindowSystem.instance.Show_INTERNAL<T>(source, initialParameters, w =>
            {
                instance = w;
                onInitialized?.Invoke(w);
            }, transitionParameters);
            return instance;
        }

        /// <summary>
        ///     Initializing window in sync mode.
        ///     Just returns instance immediately, but still stay in async mode for layout because of Addressable assets.
        /// </summary>
        /// <param name="initialParameters"></param>
        /// <param name="onInitialized"></param>
        /// <param name="transitionParameters"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T ShowSync<T>(InitialParameters initialParameters, Action<T> onInitialized = null,
            TransitionParameters transitionParameters = default) where T : WindowBase
        {
            return ShowSync(instance.GetSource<T>(), initialParameters, onInitialized, transitionParameters);
        }

        public static T ShowSync<T>(Action<T> onInitialized = null, TransitionParameters transitionParameters = default)
            where T : WindowBase
        {
            return ShowSync(default, onInitialized, transitionParameters);
        }

        public static void Show<T>(Action<T> onInitialized = null, TransitionParameters transitionParameters = default)
            where T : WindowBase
        {
            instance.Show_INTERNAL(new InitialParameters(), onInitialized, transitionParameters);
        }

        public static void Show(WindowBase source, Action<WindowBase> onInitialized = null,
            TransitionParameters transitionParameters = default)
        {
            instance.Show_INTERNAL(source, new InitialParameters(), onInitialized, transitionParameters);
        }

        public static void Show<T>(WindowBase source, Action<T> onInitialized = null,
            TransitionParameters transitionParameters = default) where T : WindowBase
        {
            instance.Show_INTERNAL(source, new InitialParameters(), onInitialized, transitionParameters);
        }

        public static void Show<T>(InitialParameters initialParameters, Action<T> onInitialized = null,
            TransitionParameters transitionParameters = default) where T : WindowBase
        {
            instance.Show_INTERNAL(initialParameters, onInitialized, transitionParameters);
        }

        public static void Show(WindowBase source, InitialParameters initialParameters,
            Action<WindowBase> onInitialized = null, TransitionParameters transitionParameters = default)
        {
            instance.Show_INTERNAL(source, initialParameters, onInitialized, transitionParameters);
        }

        public static void Show<T>(WindowBase source, InitialParameters initialParameters,
            Action<T> onInitialized = null, TransitionParameters transitionParameters = default) where T : WindowBase
        {
            instance.Show_INTERNAL(source, initialParameters, onInitialized, transitionParameters);
        }

        public static void RemoveWindow(WindowBase instance)
        {
            var uiws = WindowSystem.instance;
            if (uiws.windowsCountByLayer.TryGetValue(instance.preferences.layer.value, out int count))
            {
                uiws.windowsCountByLayer[instance.preferences.layer.value] = count - 1;
            }

            uiws.RemoveWindow_INTERNAL(instance);
        }

        private void RemoveWindow_INTERNAL(WindowBase instance)
        {
            UIWSLayer layer = instance.preferences.layer;
            for (var i = 0; i < currentWindows.Count; ++i)
            {
                if (currentWindows[i].instance == instance)
                {
                    currentWindows.RemoveAt(i);
                    if (instance.preferences.addInHistory)
                    {
                        breadcrumbs.OnWindowRemoved(instance);
                    }

                    break;
                }
            }

            if (topWindowsByLayer.TryGetValue(layer.value, out WindowBase topInstance))
            {
                if (topInstance == instance)
                {
                    topWindowsByLayer.Remove(layer.value);
                    for (var i = 0; i < currentWindows.Count; ++i)
                    {
                        TryAddTopWindow(currentWindows[i].instance.preferences.layer, currentWindows[i].instance);
                    }
                }
            }
        }

        public static WindowBase GetSource(int windowSourceId)
        {
            if (instance.hashToPrefabs.TryGetValue(windowSourceId, out WindowBase prefab))
            {
                return prefab;
            }

            return null;
        }

        private T GetSource<T>() where T : WindowBase
        {
            int hash = typeof(T).GetHashCode();
            if (hashToPrefabs.TryGetValue(hash, out WindowBase prefab))
            {
                return (T) prefab;
            }

            return default;
        }

        public static bool IsOpenedBySource(WindowBase source)
        {
            return instance.IsOpenedBySource_INTERNAL(source);
        }

        private bool IsOpenedBySource_INTERNAL(WindowBase source)
        {
            return IsOpenedBySource_INTERNAL(source, out _);
        }

        private bool IsOpenedBySource_INTERNAL(WindowBase source, out WindowBase result,
            ObjectState minimumState = ObjectState.NotInitialized, ObjectState maximumState = ObjectState.DeInitialized,
            bool last = false)
        {
            if (last)
            {
                for (int i = currentWindows.Count - 1; i >= 0; --i)
                {
                    WindowItem win = currentWindows[i];
                    if (win.prefab == source && win.instance.GetState() >= minimumState &&
                        win.instance.GetState() <= maximumState)
                    {
                        result = win.instance;
                        return true;
                    }
                }
            }
            else
            {
                for (var i = 0; i < currentWindows.Count; ++i)
                {
                    WindowItem win = currentWindows[i];
                    if (win.prefab == source && win.instance.GetState() >= minimumState &&
                        win.instance.GetState() <= maximumState)
                    {
                        result = win.instance;
                        return true;
                    }
                }
            }

            result = default;
            return false;
        }

        private void TryAddTopWindow(UIWSLayer layer, WindowBase instance)
        {
            if (topWindowsByLayer.TryGetValue(layer.value, out WindowBase topInstance))
            {
                if (instance.GetDepth() > topInstance.GetDepth())
                {
                    topWindowsByLayer[layer.value] = instance;
                }
            }
            else
            {
                topWindowsByLayer.Add(layer.value, instance);
            }
        }

        public static WindowItem GetPreviousWindow(WindowBase instance)
        {
            WindowSystemBreadcrumbs breadcrumbs = GetBreadcrumbs();
            return breadcrumbs.GetPrevious(instance);
        }

        private void Show_INTERNAL<T>(InitialParameters initialParameters, Action<T> onInitialized = null,
            TransitionParameters transitionParameters = default) where T : WindowBase
        {
            var source = GetSource<T>();
            Show_INTERNAL(source, initialParameters, onInitialized, transitionParameters);
        }

        private void Show_INTERNAL<T>(WindowBase source, InitialParameters initialParameters, Action<T> onInitialized,
            TransitionParameters transitionParameters) where T : WindowBase
        {
            if (source == null)
            {
                throw new Exception("Window Source is null, did you forget to collect your screens?");
            }

            if (source.preferences.forceSyncLoad && initialParameters.showSync == false)
            {
                initialParameters.showSync = true;
            }

            WindowBase instance;
            bool singleInstance = source.preferences.singleInstance;
            if (initialParameters.overrideSingleInstance)
            {
                singleInstance = initialParameters.singleInstance;
            }

            if (singleInstance)
            {
                if (IsOpenedBySource_INTERNAL(source, out instance, maximumState: ObjectState.Shown))
                {
                    if (onInitialized != null)
                    {
                        onInitialized.Invoke((T) instance);
                    }
                    else
                    {
                        instance.OnEmptyPass();
                    }

                    return;
                }
            }

            if (source.createPool)
            {
                pools.CreatePool(source);
            }

            instance = pools.Spawn(source, null, out bool fromPool);
            instance.identifier = ++nextWindowId;
            instance.windowSourceId = source.GetType().GetHashCode();
            instance.windowId = instance.identifier;
#if UNITY_EDITOR
            instance.name = "[" + instance.identifier.ToString("00") + "] " + source.name;
#endif
            instance.SetInitialParameters(initialParameters);
            DontDestroyOnLoad(instance.gameObject);

            if (instance.preferences.showInSequence)
            {
                if (IsOpenedBySource_INTERNAL(source, out WindowBase existInstance, ObjectState.Hiding, last: true))
                {
                    var state = WindowEvent.None;
                    switch (instance.preferences.showInSequenceEvent)
                    {
                        case SequenceEvent.OnHideBegin:
                            state = WindowEvent.OnHideBegin;
                            break;

                        case SequenceEvent.OnHideEnd:
                            state = WindowEvent.OnHideEnd;
                            break;
                    }

                    RegisterActionOnce(existInstance, state,
                        () =>
                        {
                            Show_INTERNAL(existInstance, initialParameters, onInitialized, transitionParameters);
                        });

                    return;
                }
            }

            var item = new WindowItem
            {
                prefab = source,
                instance = instance
            };

            if (instance.preferences.addInHistory)
            {
                breadcrumbs.Add(item);
            }

            currentWindows.Add(item);

            DontDestroyOnLoad(instance.gameObject);

            {
                // Setup for each instance

                instance.Setup(instance);
                TryAddTopWindow(instance.preferences.layer, instance);
                if (windowsCountByLayer.TryGetValue(instance.preferences.layer.value, out int count))
                {
                    windowsCountByLayer[instance.preferences.layer.value] = count + 1;
                }
                else
                {
                    windowsCountByLayer.Add(instance.preferences.layer.value, 1);
                }
            }

            if (initialParameters.showSync)
            {
                if (onInitialized != null)
                {
                    onInitialized.Invoke((T) instance);
                }
                else
                {
                    instance.OnEmptyPass();
                }
            }

            instance.LoadAsync(initialParameters, () =>
            {
                if (initialParameters.showSync == false)
                {
                    if (onInitialized != null)
                    {
                        onInitialized.Invoke((T) instance);
                    }
                    else
                    {
                        instance.OnEmptyPass();
                    }
                }

                TransitionParameters tr = transitionParameters.ReplaceCallback(() =>
                {
                    Coroutines.Run(WaitForLayoutBuildComplete(instance));
                    transitionParameters.RaiseCallback();
                });

                instance.DoInit(() => instance.ShowInternal(tr));
            });
        }

        private static IEnumerator WaitForLayoutBuildComplete(WindowBase instance)
        {
            yield return new WaitForEndOfFrame();
            instance.DoLayoutReady();
        }
    }
}