using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEditor;
using UnityEngine.UI.Windows.Modules;
using UnityEngine.UI.Windows.Utilities;

namespace UnityEngine.UI.Windows
{
    public enum ObjectState
    {
        NotInitialized,
        Initializing,
        Loading,
        Loaded,
        Initialized,
        Showing,
        Shown,
        Hiding,
        Hidden,
        DeInitializing,
        DeInitialized
    }

    public enum RenderBehaviourSettings
    {
        UseSettings = 0,
        TurnOffRenderers = 1,
        HideGameObject = 2,
        Nothing = 3
    }

    public enum RenderBehaviour
    {
        TurnOffRenderers = 1,
        HideGameObject = 2,
        Nothing = 3
    }

    public interface ILayoutInstance
    {
        WindowLayout windowLayoutInstance { get; set; }
    }

    [Serializable]
    public struct EditorRefLocks
    {
#if USE_PROPERTY_DRAWERS_OVERRIDE
        [InfoBox("Only this directories must be used for resources.")]
#else
        [InfoBox(
            "Only this directories must be used for resources. Add USE_PROPERTY_DRAWERS_OVERRIDE define to use this feature.")]
#endif
        [FolderPath]
        public string[] directories;
    }

    [Serializable]
    public struct DebugStateLog
    {
        public List<Item> items;

        public void Add(ObjectState state)
        {
            if (items == null)
            {
                items = new List<Item>();
            }

            string trace = StackTraceUtility.ExtractStackTrace();
            items.Add(new Item
            {
                state = state,
                stackTrace = trace
            });
        }

        [Serializable]
        public struct Item
        {
            public ObjectState state;
            public string stackTrace;
        }
    }

    public interface IHolder
    {
        void ValidateEditor();
    }

    public interface ILoadable
    {
        void Load(Action onComplete);
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public abstract class WindowObject : SerializedMonoBehaviour, IOnPoolGet, IOnPoolAdd,
        ISearchComponentByTypeSingleEditor,
        IHolder
    {
        [HideInInspector] [SerializeField] internal int windowId;
        [HideInInspector] [SerializeField] internal WindowBase window;
        [HideInInspector] public RectTransform rectTransform;
        [HideInInspector] public bool hasObjectCanvas;
        [HideInInspector] public Canvas objectCanvas;
        [HideInInspector] public RenderItem[] canvasRenderers;
        [HideInInspector] public bool isObjectRoot;
        [HideInInspector] public WindowObject rootObject;
        [HideInInspector] public List<EditorParametersRegistry> registry;

        [TabGroup("Basic")]
        [Tooltip(
            "Make this object is hidden by default.\nWorks only on window showing state, check if this object must be hidden by default it breaks branch graph on this node. After it works current object state will be Initialized.")]
        public bool hiddenByDefault;

        [TabGroup("Advanced")] public List<WindowObject> subObjects = new();

        [TabGroup("Basic")]
        [Tooltip(
            "Should this object return in pool when window is hidden? Object will returns into pool only if parent object is not mark as `createPool`.")]
        public bool createPool;

        [TabGroup("Advanced")] public int canvasSortingOrderDelta;

        [TabGroup("Advanced")] [Tooltip("Render behaviour when hidden state set or if hiddenByDefault is true.")]
        public RenderBehaviourSettings renderBehaviourOnHidden = RenderBehaviourSettings.UseSettings;

        [TabGroup("Advanced")] [Tooltip("Allow root to register this object in subObjects array.")]
        public bool allowRegisterInRoot = true;

        [TabGroup("Advanced")]
        [Tooltip(
            "Auto register sub objects in this object.\nIf it turned off you need to set up subObjects array manually in inspector or use API.")]
        public bool autoRegisterSubObjects = true;

        [TabGroup("Animations")] [OdinSerialize]
        public AnimationParametersContainer animationParameters;

        [TabGroup("Debug")] public DebugStateLog debugStateLog;

        [TabGroup("Audio")] public ComponentAudio audioEvents;

        [TabGroup("Tools")] public EditorRefLocks editorRefLocks;

        internal bool internalManualShow;
        internal bool internalManualHide;
        private bool readyToHide = true;

        private bool isActiveSelf;
        private ObjectState objectState;

        public virtual void ValidateEditor()
        {
#if UNITY_EDITOR
            string path = AssetDatabase.GetAssetPath(gameObject);
            if (path.Contains("Packages/"))
            {
                return;
            }
#endif

            rectTransform = GetComponent<RectTransform>();

            ValidateEditor(false, true);
        }

        void IOnPoolAdd.OnPoolAdd()
        {
            OnPoolAdd();

            internalManualHide = false;
            internalManualShow = false;

            for (var i = 0; i < subObjects.Count; ++i)
            {
                if (CheckSubObject(subObjects, ref i) == false)
                {
                    continue;
                }

                ((IOnPoolAdd) subObjects[i]).OnPoolAdd();
            }
        }

        void IOnPoolGet.OnPoolGet()
        {
            OnPoolGet();

            for (var i = 0; i < subObjects.Count; ++i)
            {
                if (CheckSubObject(subObjects, ref i) == false)
                {
                    continue;
                }

                ((IOnPoolGet) subObjects[i]).OnPoolGet();
            }
        }

        IList ISearchComponentByTypeSingleEditor.GetSearchTypeArray()
        {
            return animationParameters.items;
        }

        [ContextMenu("Validate")]
        public void ContextValidateEditor()
        {
            ValidateEditor();
        }

        public bool IsReadyToHide()
        {
            return readyToHide;
        }

        public void SetReadyToHide(bool state)
        {
            readyToHide = state;
        }

        public void SetState(ObjectState state)
        {
            bool isDebug = WindowSystem.GetSettings().collectDebugInfo;
            if (isDebug)
            {
                debugStateLog.Add(state);
            }

            objectState = state;

            if (state == ObjectState.Initializing)
            {
                SetResetState();
            }
        }

        public void AddEditorParametersRegistry(EditorParametersRegistry param)
        {
            if (registry == null)
            {
                registry = new List<EditorParametersRegistry>();
            }

            if (registry.Any(x => x.IsEquals(param)) == false)
            {
                registry.Add(param);
            }
        }

        private void ValidateRegistry(DirtyHelper dirtyHelper)
        {
            if (registry != null && registry.Count > 0)
            {
                var holders = new List<IHolder>();
                foreach (EditorParametersRegistry reg in registry)
                {
                    if (reg.GetHolder() != null)
                    {
                        holders.Add(reg.GetHolder());
                    }
                }

                List<EditorParametersRegistry> prevRegistry = registry.ToList();
                registry.Clear();
                foreach (IHolder holder in holders)
                {
                    if ((MonoBehaviour) holder != this)
                    {
                        holder.ValidateEditor();
                    }
                }

                List<EditorParametersRegistry> newRegistry = registry;
                registry = prevRegistry;
                dirtyHelper.Set(ref registry, newRegistry);
            }
        }

        public virtual void OnPoolGet()
        {
        }

        public virtual void OnPoolAdd()
        {
        }

        public void SetTransformAs(RectTransform rectTransform)
        {
            if (rectTransform == null)
            {
                return;
            }

            RectTransform rect = this.rectTransform;
            rect.localScale = rectTransform.localScale;
            rect.anchorMin = rectTransform.anchorMin;
            rect.anchorMax = rectTransform.anchorMax;
            rect.sizeDelta = rectTransform.sizeDelta;
            rect.anchoredPosition = rectTransform.anchoredPosition;
        }

        public void SetTransformFullRect()
        {
            RectTransform rect = rectTransform;
            rect.localScale = Vector3.one;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;
        }

        public virtual void BreakState()
        {
            WindowObjectAnimation.BreakState(this);
        }

        public virtual void BreakStateHierarchy()
        {
            BreakState();
            for (var i = 0; i < subObjects.Count; ++i)
            {
                if (CheckSubObject(subObjects, ref i) == false)
                {
                    continue;
                }

                subObjects[i].BreakState();
            }
        }

        public bool IsVisible()
        {
            return IsVisibleSelf() && (rootObject != null ? rootObject.IsVisible() : true);
        }

        public bool IsVisibleSelf()
        {
            return objectState == ObjectState.Showing || objectState == ObjectState.Shown;
        }

        public virtual T FindComponent<T>(Func<T, bool> filter = null) where T : WindowObject
        {
            return FindComponent<T, Func<T, bool>>(filter, (fn, c) =>
            {
                if (fn != null)
                {
                    return fn.Invoke(c);
                }

                return c;
            });
        }

        public virtual T FindComponent<T, TState>(TState state, Func<TState, T, bool> filter = null)
            where T : WindowObject
        {
            if (this is T instance)
            {
                if (filter == null || filter.Invoke(state, instance))
                {
                    return instance;
                }
            }

            for (var i = 0; i < subObjects.Count; ++i)
            {
                if (CheckSubObject(subObjects, ref i) == false)
                {
                    continue;
                }

                WindowObject obj = subObjects[i];
                T comp = obj.FindComponent(state, filter);
                if (comp != null)
                {
                    return comp;
                }
            }

            return null;
        }

        public virtual T FindComponentParent<T>(Func<T, bool> filter = null) where T : WindowObject
        {
            return FindComponentParent<T, Func<T, bool>>(filter, (fn, c) =>
            {
                if (fn != null)
                {
                    return fn.Invoke(c);
                }

                return c;
            });
        }

        public virtual T FindComponentParent<T, TState>(TState state, Func<TState, T, bool> filter = null)
            where T : WindowObject
        {
            if (this is T instance)
            {
                if (filter == null || filter.Invoke(state, instance))
                {
                    return instance;
                }
            }

            if (rootObject != null)
            {
                T comp = rootObject.FindComponentParent(state, filter);
                if (comp != null)
                {
                    return comp;
                }
            }

            return null;
        }

        public void ValidateEditor(bool updateParentObjects, bool updateChildObjects = false)
        {
            var helper = new DirtyHelper(this);
            ValidateEditor(helper, updateParentObjects, updateChildObjects);
            helper.Apply();
        }

        public void ValidateEditor(DirtyHelper dirtyHelper, bool updateParentObjects, bool updateChildObjects = false)
        {
            ValidateRegistry(dirtyHelper);

            dirtyHelper.SetEnum(ref objectState, ObjectState.NotInitialized);
            WindowObject[] up = transform.parent != null
                ? transform.parent.GetComponentsInParent<WindowObject>(true)
                : null;
            dirtyHelper.Set(ref isObjectRoot, up == null || up.Length == 0);
            dirtyHelper.SetObj(ref objectCanvas, GetComponent<Canvas>());
            dirtyHelper.Set(ref hasObjectCanvas, objectCanvas != null);

            {
                // Collect render items

                CanvasRenderer[] canvasRenderers = GetComponentsInChildren<CanvasRenderer>(true)
                    .Where(x => x.GetComponentsInParent<WindowObject>(true)[0] == this).ToArray();
                for (var i = 0; i < canvasRenderers.Length; ++i)
                {
                    if (canvasRenderers[i].GetComponent<Graphic>() == null)
                    {
                        DestroyImmediate(canvasRenderers[i], true);
                        canvasRenderers[i] = null;
                    }
                }

                canvasRenderers = canvasRenderers.Where(x => x != null).ToArray();
                RectMask2D[] rectMasks = GetComponentsInChildren<RectMask2D>()
                    .Where(x => x.GetComponentsInParent<WindowObject>(true)[0] == this).ToArray();
                var newCanvasRenderers = new RenderItem[canvasRenderers.Length + rectMasks.Length];
                var k = 0;
                for (var i = 0; i < newCanvasRenderers.Length; ++i)
                {
                    if (i < rectMasks.Length)
                    {
                        newCanvasRenderers[i] = new RenderItem
                        {
                            rectMask = rectMasks[i]
                        };
                    }
                    else
                    {
                        newCanvasRenderers[i] = new RenderItem
                        {
                            canvasRenderer = canvasRenderers[k],
                            graphics = canvasRenderers[k].GetComponent<Graphic>()
                        };

                        ++k;
                    }
                }

                if (this.canvasRenderers != null)
                {
                    for (var i = 0; i < this.canvasRenderers.Length; ++i)
                    {
                        if (this.canvasRenderers[i].canvasRenderer == null)
                        {
                            this.canvasRenderers[i].canvasRenderer = null;
                        }
                    }
                }

                dirtyHelper.Set(ref this.canvasRenderers, newCanvasRenderers);
            }

            WindowObject[] roots = GetComponentsInParent<WindowObject>(true);
            if (roots.Length >= 2)
            {
                dirtyHelper.SetObj(ref rootObject, roots[1]);
            }

            if (autoRegisterSubObjects)
            {
                List<WindowObject> newSubObjects = GetComponentsInChildren<WindowObject>(true).Where(x =>
                {
                    if (x.allowRegisterInRoot == false)
                    {
                        return false;
                    }

                    WindowObject[] comps = x.GetComponentsInParent<WindowObject>(true);
                    if (comps.Length < 2)
                    {
                        return false;
                    }

                    WindowObject c = comps[1];
                    return x != this && c == this;
                }).ToList();
                dirtyHelper.SetObj(ref subObjects, newSubObjects);
            }

            if (updateChildObjects)
            {
                WindowObject[] childObjects = GetComponentsInChildren<WindowObject>(true);
                foreach (WindowObject obj in childObjects)
                {
                    if (obj != this)
                    {
                        obj.ValidateEditor(false, true);
                    }
                }
            }

            if (updateParentObjects)
            {
                WindowObject[] topObjects = GetComponentsInParent<WindowObject>(true);
                foreach (WindowObject obj in topObjects)
                {
                    if (obj != this)
                    {
                        obj.ValidateEditor(false);
                    }
                }
            }

#if UNITY_EDITOR
            if (dirtyHelper.isDirty)
            {
                EditorUtility.SetDirty(gameObject);
            }
#endif
        }

        public void PushToPool()
        {
            if (createPool == false)
            {
                for (int i = subObjects.Count - 1; i >= 0; --i)
                {
                    if (CheckSubObject(subObjects, ref i) == false)
                    {
                        continue;
                    }

                    subObjects[i].PushToPool();
                }
            }

            if (isObjectRoot)
            {
                if (rootObject != null)
                {
                    rootObject.RemoveSubObject(this);
                }

                window = null;
                rootObject = null;
                WindowSystem.GetPools().Despawn(this, obj => { obj.DoDeInit(); });
            }
            else
            {
                window = null;
                rootObject = null;
            }
        }

        internal void DoLoadScreenAsync(InitialParameters initialParameters, Action onComplete)
        {
            Coroutines.CallInSequence(closure => { OnLoadScreenAsync(closure, onComplete); }, initialParameters,
                subObjects, (x, cb, closure) => x.OnLoadScreenAsync(closure, cb));
        }

        protected virtual void OnLoadScreenAsync(InitialParameters initialParameters, Action onComplete)
        {
            if (onComplete != null)
            {
                onComplete.Invoke();
            }
        }

        public ObjectState GetState()
        {
            return objectState;
        }

        /// <summary>
        ///     Just turn off all canvases
        /// </summary>
        public virtual void TurnOffRender()
        {
            if (hasObjectCanvas)
            {
                objectCanvas.enabled = false;
            }

            for (var i = 0; i < subObjects.Count; ++i)
            {
                if (CheckSubObject(subObjects, ref i) == false)
                {
                    continue;
                }

                subObjects[i].TurnOffRender();
            }
        }

        /// <summary>
        ///     Just turn on all canvases
        /// </summary>
        public virtual void TurnOnRender()
        {
            if (hasObjectCanvas)
            {
                objectCanvas.enabled = true;
            }

            for (var i = 0; i < subObjects.Count; ++i)
            {
                if (CheckSubObject(subObjects, ref i) == false)
                {
                    continue;
                }

                subObjects[i].TurnOnRender();
            }
        }

        public bool IsActive()
        {
            return isActiveSelf && (rootObject == null || rootObject.IsActive());
        }

        public void SetVisibleHierarchy()
        {
            SetVisible();

            for (var i = 0; i < subObjects.Count; ++i)
            {
                if (CheckSubObject(subObjects, ref i) == false)
                {
                    continue;
                }

                subObjects[i].SetVisible();
            }
        }

        public void SetVisible()
        {
            isActiveSelf = true;

            if (IsActive())
            {
                var renderBehaviourOnHidden = RenderBehaviour.Nothing;
                if (this.renderBehaviourOnHidden == RenderBehaviourSettings.UseSettings)
                {
                    WindowSystemSettings settings = WindowSystem.GetSettings();
                    renderBehaviourOnHidden = settings.components.renderBehaviourOnHidden;
                }
                else
                {
                    renderBehaviourOnHidden = (RenderBehaviour) this.renderBehaviourOnHidden;
                }

                switch (renderBehaviourOnHidden)
                {
                    case RenderBehaviour.TurnOffRenderers:

                        for (var i = 0; i < canvasRenderers.Length; ++i)
                        {
                            canvasRenderers[i].SetCull(false);
                        }

                        break;

                    case RenderBehaviour.HideGameObject:

                        gameObject.SetActive(true);

                        break;
                }
            }
        }

        public void SetInvisibleHierarchy()
        {
            SetInvisible();

            for (var i = 0; i < subObjects.Count; ++i)
            {
                if (CheckSubObject(subObjects, ref i) == false)
                {
                    continue;
                }

                subObjects[i].SetVisible();
            }
        }

        public void SetInvisible()
        {
            if (this == null || gameObject == null)
            {
                return;
            }

            isActiveSelf = false;

            var renderBehaviourOnHidden = RenderBehaviour.Nothing;
            if (this.renderBehaviourOnHidden == RenderBehaviourSettings.UseSettings)
            {
                WindowSystemSettings settings = WindowSystem.GetSettings();
                renderBehaviourOnHidden = settings.components.renderBehaviourOnHidden;
            }
            else
            {
                renderBehaviourOnHidden = (RenderBehaviour) this.renderBehaviourOnHidden;
            }

            switch (renderBehaviourOnHidden)
            {
                case RenderBehaviour.TurnOffRenderers:

                    for (var i = 0; i < canvasRenderers.Length; ++i)
                    {
                        canvasRenderers[i].SetCull(true);
                    }

                    break;

                case RenderBehaviour.HideGameObject:

                    gameObject.SetActive(false);

                    break;
            }
        }

        public void SetSortingOrderDelta(int value)
        {
            if (hasObjectCanvas)
            {
                Canvas windowCanvas = window.GetCanvas();
                if (windowCanvas != null)
                {
                    objectCanvas.overrideSorting = true;
                    objectCanvas.sortingOrder = windowCanvas.sortingOrder + value;
                    objectCanvas.sortingLayerName = windowCanvas.sortingLayerName;
                    objectCanvas.sortingLayerID = windowCanvas.sortingLayerID;
                }
            }
        }

        public void Setup(WindowComponent component)
        {
            Setup(component.GetWindow());
        }

        internal virtual void Setup(WindowBase source)
        {
            if (hasObjectCanvas)
            {
                Canvas windowCanvas = source.GetCanvas();
                if (windowCanvas != null)
                {
                    objectCanvas.overrideSorting = true;
                    objectCanvas.sortingOrder = windowCanvas.sortingOrder + canvasSortingOrderDelta;
                    objectCanvas.sortingLayerName = windowCanvas.sortingLayerName;
                    objectCanvas.sortingLayerID = windowCanvas.sortingLayerID;
                }
            }

            for (var i = 0; i < subObjects.Count; ++i)
            {
                if (CheckSubObject(subObjects, ref i) == false)
                {
                    continue;
                }

                subObjects[i].Setup(source);
            }

            for (var i = 0; i < canvasRenderers.Length; ++i)
            {
                canvasRenderers[i].SetCullTransparentMesh(true);
            }

            windowId = source.windowId;
            window = source;
        }

        private bool CheckSubObject(List<WindowObject> subObjects, ref int index)
        {
            if (subObjects[index] == null)
            {
                Debug.LogError(
                    $"Null subObject encountered on window [{(window == null ? "Null" : window.name)}], object [{name}], index [{index}] (previous subObject is [{(index > 0 ? subObjects[index - 1].name : "Empty")}], next subObject is [{(index < subObjects.Count - 1 ? subObjects[index + 1].name : "Empty")}])");
                this.subObjects.RemoveAt(index);
                --index;
                return false;
            }

            return true;
        }

        public bool RegisterSubObject(WindowObject windowObject)
        {
            windowObject.Setup(window);

            if (subObjects.Contains(windowObject) == false)
            {
                subObjects.Add(windowObject);
                windowObject.rootObject = this;

                if (windowObject.GetState() == ObjectState.NotInitialized)
                {
                    windowObject.DoInit(() => AdjustObjectState(windowObject));
                }
                else
                {
                    AdjustObjectState(windowObject);
                }

                return true;
            }

            return false;
        }

        private void AdjustObjectState(WindowObject windowObject)
        {
            switch (objectState)
            {
                case ObjectState.Initializing:
                case ObjectState.Initialized:

                    if (windowObject.GetState() != ObjectState.Initialized)
                    {
                        Debug.LogError("WindowObject must be initialized before AdjustObjectState");
                    }

                    break;

                case ObjectState.Showing:

                    if (windowObject.hiddenByDefault == false)
                    {
                        WindowSystem.ShowInstance(windowObject, TransitionParameters.Default.ReplaceImmediately(true),
                            true);
                    }

                    break;
                case ObjectState.Shown:

                    if (windowObject.hiddenByDefault == false)
                    {
                        WindowSystem.ShowInstance(
                            windowObject,
                            TransitionParameters.Default.ReplaceImmediately(true).ReplaceCallback(() =>
                            {
                                WindowSystem.SetShown(windowObject,
                                    TransitionParameters.Default.ReplaceImmediately(true), true);
                            }), true);
                    }

                    break;

                case ObjectState.Hiding:

                    windowObject.SetState(objectState);

                    break;
                case ObjectState.Hidden:

                    windowObject.SetState(objectState);

                    break;

                case ObjectState.DeInitializing:
                case ObjectState.DeInitialized:

                    windowObject.DoDeInit();
                    break;
            }
        }

        public bool RemoveSubObject(WindowObject windowObject)
        {
            if (subObjects.Remove(windowObject))
            {
                windowObject.rootObject = null;
                return true;
            }

            return false;
        }

        public bool UnRegisterSubObject(WindowObject windowObject)
        {
            if (RemoveSubObject(windowObject))
            {
                switch (windowObject.objectState)
                {
                    case ObjectState.Initializing:
                    case ObjectState.Initialized:
                        windowObject.DoDeInit();
                        break;

                    case ObjectState.Showing:
                    case ObjectState.Shown:

                        // after OnShowEnd
                        windowObject.Hide(TransitionParameters.Default.ReplaceImmediately(true));
                        windowObject.DoDeInit();
                        break;

                    case ObjectState.Hiding:

                        // after OnHideBegin
                        WindowSystem.SetHidden(windowObject, TransitionParameters.Default.ReplaceImmediately(true),
                            true);
                        windowObject.DoDeInit();
                        break;

                    case ObjectState.Hidden:

                        // after OnHideEnd
                        windowObject.DoDeInit();
                        break;
                }

                return true;
            }

            return false;
        }

        public WindowBase GetWindow()
        {
#if UNITY_EDITOR
            if (Application.isPlaying == false && window == null)
            {
                return GetComponentInParent<WindowBase>();
            }
#endif

            return window;
        }

        public T GetWindow<T>() where T : WindowBase
        {
            return (T) GetWindow();
        }

        public void SetResetStateHierarchy()
        {
            SetResetState();

            for (var i = 0; i < subObjects.Count; ++i)
            {
                subObjects[i].SetResetState();
            }
        }

        public void SetResetState()
        {
            WindowObjectAnimation.SetResetState(this);
        }

        protected internal virtual void SendEvent<T>(T data)
        {
            OnEvent(data);

            for (var i = 0; i < subObjects.Count; ++i)
            {
                if (CheckSubObject(subObjects, ref i) == false)
                {
                    continue;
                }

                subObjects[i].SendEvent(data);
            }
        }

        public virtual void OnEvent<T>(T data)
        {
        }

        public void DoSendFocusLost()
        {
            OnFocusLost();
            WindowSystem.RaiseEvent(this, WindowEvent.OnFocusLost);

            for (var i = 0; i < subObjects.Count; ++i)
            {
                if (CheckSubObject(subObjects, ref i) == false)
                {
                    continue;
                }

                subObjects[i].DoSendFocusLost();
            }
        }

        public void DoSendFocusTook()
        {
            OnFocusTook();
            WindowSystem.RaiseEvent(this, WindowEvent.OnFocusTook);

            for (var i = 0; i < subObjects.Count; ++i)
            {
                if (CheckSubObject(subObjects, ref i) == false)
                {
                    continue;
                }

                subObjects[i].DoSendFocusTook();
            }
        }

        public void DoInit(Action callback = null)
        {
            Coroutines.Run(DoInitAsync(callback));
        }

        private IEnumerator DoInitAsync(Action callback = null)
        {
            if (objectState < ObjectState.Initializing)
            {
                SetState(ObjectState.Initializing);

                if (this is ILoadable loadable)
                {
                    SetState(ObjectState.Loading);

                    var loaded = false;
                    loadable.Load(() => loaded = true);
                    yield return new WaitUntil(() => loaded);
                }

                SetState(ObjectState.Loaded);

                audioEvents.Initialize(this);

                OnInitInternal();
                OnInit();
                WindowSystem.RaiseEvent(this, WindowEvent.OnInitialize);

                SetState(ObjectState.Initialized);
            }

            List<IEnumerator> coroutines = PoolList<IEnumerator>.Spawn();

            for (var i = 0; i < subObjects.Count; ++i)
            {
                if (CheckSubObject(subObjects, ref i) == false)
                {
                    continue;
                }

                coroutines.Add(subObjects[i].DoInitAsync());
            }

            var moveNext = true;
            while (moveNext)
            {
                moveNext = false;
                foreach (IEnumerator coroutine in coroutines)
                {
                    moveNext = moveNext || coroutine.MoveNext();
                }
            }

            PoolList<IEnumerator>.Recycle(coroutines);

            callback?.Invoke();
        }

        public void DoDeInit()
        {
            if (objectState >= ObjectState.DeInitializing)
            {
                Debug.LogWarning("Object is out of state: " + this);
                return;
            }

            SetState(ObjectState.DeInitializing);

            for (var i = 0; i < subObjects.Count; ++i)
            {
                if (CheckSubObject(subObjects, ref i) == false)
                {
                    continue;
                }

                subObjects[i].DoDeInit();
            }

            WindowSystemResources resources = WindowSystem.GetResources();
            resources.DeleteAll(this);

            WindowSystem.RaiseEvent(this, WindowEvent.OnDeInitialize);
            OnDeInit();
            OnDeInitInternal();

            audioEvents.DeInitialize(this);

            SetState(ObjectState.DeInitialized);

            WindowSystem.ClearEvents(this);

            SetState(ObjectState.NotInitialized);
        }

        private bool IsInternalManualTouch(TransitionParameters parameters)
        {
            if (parameters.data.replaceIgnoreTouch)
            {
                if (parameters.data.ignoreTouch)
                {
                    return false;
                }
            }

            return internalManualHide || internalManualShow;
        }

        internal void ShowInternal(TransitionParameters parameters = default)
        {
            if (hiddenByDefault || IsInternalManualTouch(parameters))
            {
                if (internalManualShow == false)
                {
                    HideInternal(TransitionParameters.Default.ReplaceImmediately(true));
                    SetInvisible();
                }

                parameters.RaiseCallback();
                return;
            }

            if (objectState <= ObjectState.Initializing)
            {
                Debug.LogWarning("Object is out of state: " + this);
                return;
            }

            WindowObject cObj = this;
            TransitionParameters cParams = parameters;
            TransitionParameters cbParameters =
                parameters.ReplaceCallbackWithContext(WindowSystem.SetShown, cObj, cParams, true);
            WindowSystem.ShowInstance(this, cbParameters, true);
        }

        internal void HideInternal(TransitionParameters parameters = default)
        {
            if (IsInternalManualTouch(parameters))
            {
                parameters.RaiseCallback();
                return;
            }

            WindowObject cObj = this;
            TransitionParameters cParams = parameters;
            TransitionParameters cbParameters =
                parameters.ReplaceCallbackWithContext(WindowSystem.SetHidden, cObj, cParams, true);
            WindowSystem.HideInstance(this, cbParameters, true);
        }

        public void ShowHide(bool state)
        {
            ShowHide(state, default);
        }

        public void ShowHide(bool state, TransitionParameters parameters)
        {
            if (state)
            {
                Show(parameters);
            }
            else
            {
                Hide(parameters);
            }
        }

        public void Show()
        {
            Show(default);
        }

        public virtual void Show(TransitionParameters parameters)
        {
            if (objectState <= ObjectState.Initializing)
            {
                if (objectState == ObjectState.NotInitialized)
                {
                    TransitionParameters copy = parameters;
                    DoInit(() => Show(copy));
                    return;
                }

                Debug.LogWarning("Object is out of state: " + this);
                return;
            }

            if (objectState == ObjectState.Showing || objectState == ObjectState.Shown)
            {
                parameters.RaiseCallback();
                return;
            }

            internalManualShow = true;

            WindowObject cObj = this;
            TransitionParameters cParams = parameters;
            TransitionParameters cbParameters = parameters.ReplaceCallbackWithContext(
                static (obj, tr, internalCall) => WindowSystem.SetShown(obj, tr, internalCall), cObj, cParams, false);
            WindowSystem.ShowInstance(this, cbParameters, true);
        }

        public void Hide()
        {
            Hide(default);
        }

        public virtual void Hide(TransitionParameters parameters)
        {
            if (objectState <= ObjectState.Initializing)
            {
                if (objectState == ObjectState.NotInitialized)
                {
                    TransitionParameters copy = parameters;
                    DoInit(() => Hide(copy));
                    return;
                }

                Debug.LogWarning("Object is out of state: " + this);
                return;
            }

            if (objectState == ObjectState.Hiding || objectState == ObjectState.Hidden)
            {
                parameters.RaiseCallback();
                return;
            }

            internalManualHide = true;

            WindowObject cObj = this;
            TransitionParameters cParams = parameters;
            TransitionParameters cbParameters = parameters.ReplaceCallbackWithContext(
                static (obj, tr, internalCall) => WindowSystem.SetHidden(obj, tr, internalCall), cObj, cParams, false);
            WindowSystem.HideInstance(this, cbParameters, true);
        }

        public void HideCurrentWindow()
        {
            window.Hide();
        }

        public void LoadAsync<T>(Resource resource, Action<T> onComplete = null, bool async = true)
            where T : WindowObject
        {
            Coroutines.Run(LoadAsync_YIELD(resource, onComplete, async));
        }

        private IEnumerator LoadAsync_YIELD<T>(Resource resource, Action<T> onComplete = null, bool async = true)
            where T : WindowObject
        {
            WindowSystemResources resources = WindowSystem.GetResources();
            var data = new LoadAsyncClosure<T>
            {
                component = this,
                onComplete = onComplete
            };
            yield return resources.LoadAsync<T, LoadAsyncClosure<T>>(
                new WindowSystemResources.LoadParameters {async = async}, GetWindow(), data, resource,
                (asset, closure) =>
                {
                    if (asset != null)
                    {
                        T instance = closure.component.Load(asset);
                        if (closure.onComplete != null)
                        {
                            closure.onComplete.Invoke(instance);
                        }
                    }
                    else
                    {
                        if (closure.onComplete != null)
                        {
                            closure.onComplete.Invoke(null);
                        }
                    }
                });
        }

        public T Load<T>(T prefab) where T : WindowObject
        {
            if (prefab.createPool)
            {
                WindowSystem.GetPools().CreatePool(prefab);
            }

            T instance = WindowSystem.GetPools().Spawn(prefab, transform);
            instance.Setup(GetWindow());
            instance.SetInvisible();
            RegisterSubObject(instance);

            return instance;
        }

        public void UnloadSubObjects()
        {
            if (subObjects.Count == 0)
            {
                return;
            }

            WindowSystemPools pools = WindowSystem.GetPools();

            for (int i = subObjects.Count - 1; i >= 0; i--)
            {
                WindowObject subObject = subObjects[i];

                UnRegisterSubObject(subObject);
                pools.Despawn(subObject);
            }
        }

        public void DoLayoutReady()
        {
            OnLayoutReady();
            WindowSystem.RaiseEvent(this, WindowEvent.OnLayoutReady);

            for (var i = 0; i < subObjects.Count; ++i)
            {
                if (CheckSubObject(subObjects, ref i) == false)
                {
                    continue;
                }

                subObjects[i].DoLayoutReady();
            }
        }

        internal virtual void OnInitInternal()
        {
        }

        internal virtual void OnDeInitInternal()
        {
        }

        internal virtual void OnShowBeginInternal()
        {
        }

        internal virtual void OnShowEndInternal()
        {
        }

        internal virtual void OnHideBeginInternal()
        {
        }

        internal virtual void OnHideEndInternal()
        {
        }

        [Serializable]
        public struct RenderItem : IEquatable<RenderItem>
        {
            public CanvasRenderer canvasRenderer;
            public Graphic graphics;
            public RectMask2D rectMask;

            public bool Equals(RenderItem other)
            {
                return Equals(canvasRenderer, other.canvasRenderer) && Equals(graphics, other.graphics) &&
                       Equals(rectMask, other.rectMask);
            }

            public void SetCull(bool state)
            {
                if (canvasRenderer != null)
                {
                    canvasRenderer.SetAlpha(state ? 0f : 1f);
                    canvasRenderer.cull = state;
                    if (graphics != null)
                    {
                        CanvasUpdateRegistry.RegisterCanvasElementForGraphicRebuild(graphics);
                    }
                }
                else if (rectMask != null)
                {
                    rectMask.enabled = !state;
                }
            }

            public void SetCullTransparentMesh(bool state)
            {
                if (canvasRenderer != null)
                {
                    canvasRenderer.cullTransparentMesh = state;
                }
            }

            public override bool Equals(object obj)
            {
                return obj is RenderItem other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int hashCode = canvasRenderer != null ? canvasRenderer.GetHashCode() : 0;
                    hashCode = (hashCode * 397) ^ (graphics != null ? graphics.GetHashCode() : 0);
                    hashCode = (hashCode * 397) ^ (rectMask != null ? rectMask.GetHashCode() : 0);
                    return hashCode;
                }
            }
        }

        [Serializable]
        public struct AnimationParametersContainer
        {
            // [SearchComponentsByTypePopupAttribute(typeof(AnimationParameters), "Animations", singleOnly: true)]
            public AnimationParameters[] items;
        }

        [Serializable]
        public struct EditorParametersRegistry : IEquatable<EditorParametersRegistry>
        {
            [SerializeField] private WindowObject holder;

            public bool holdHiddenByDefault;
            public bool holdAllowRegisterInRoot;

            [SerializeField] private WindowComponentModule moduleHolder;

            public EditorParametersRegistry(WindowObject holder)
            {
                this.holder = holder;
                moduleHolder = null;

                holdHiddenByDefault = default;
                holdAllowRegisterInRoot = default;
            }

            public EditorParametersRegistry(WindowComponentModule holder)
            {
                this.holder = holder.windowComponent;
                moduleHolder = holder;

                holdHiddenByDefault = default;
                holdAllowRegisterInRoot = default;
            }

            public bool Equals(EditorParametersRegistry other)
            {
                return Equals(holder, other.holder) && Equals(moduleHolder, other.moduleHolder) &&
                       holdHiddenByDefault == other.holdHiddenByDefault &&
                       holdAllowRegisterInRoot == other.holdAllowRegisterInRoot;
            }

            public IHolder GetHolder()
            {
                if (holder == null)
                {
                    return moduleHolder;
                }

                return holder;
            }

            public string GetHolderName()
            {
                if (moduleHolder != null)
                {
                    return moduleHolder.windowComponent.name;
                }

                return holder.name;
            }

            public bool IsEquals(EditorParametersRegistry other)
            {
                return holder == other.holder &&
                       holdHiddenByDefault == other.holdHiddenByDefault &&
                       holdAllowRegisterInRoot == other.holdAllowRegisterInRoot;
            }

            public override bool Equals(object obj)
            {
                return obj is EditorParametersRegistry other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int hashCode = holder != null ? holder.GetHashCode() : 0;
                    hashCode = (hashCode * 397) ^ (moduleHolder != null ? moduleHolder.GetHashCode() : 0);
                    hashCode = (hashCode * 397) ^ holdHiddenByDefault.GetHashCode();
                    hashCode = (hashCode * 397) ^ holdAllowRegisterInRoot.GetHashCode();
                    return hashCode;
                }
            }
        }

        private struct LoadAsyncClosure<T>
        {
            public WindowObject component;
            public Action<T> onComplete;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (Application.isPlaying == false)
            {
                if (WindowSystem.HasInstance() == false)
                {
                    return;
                }

                EditorApplication.delayCall += () =>
                {
                    if (this != null)
                    {
                        ValidateEditor();
                    }
                };
            }
        }

        private void OnTransformChildrenChanged()
        {
            OnValidate();
        }

        private void OnTransformParentChanged()
        {
            OnValidate();
        }
#endif

        #region Public Override Events

        public virtual void OnLayoutReady()
        {
        }

        public virtual void OnInit()
        {
        }

        public virtual void OnDeInit()
        {
        }

        public virtual void OnShowBegin()
        {
        }

        public virtual void OnShowEnd()
        {
        }

        public virtual void OnHideBegin()
        {
        }

        public virtual void OnHideEnd()
        {
        }

        public virtual void OnFocusTook()
        {
        }

        public virtual void OnFocusLost()
        {
        }

        #endregion
    }
}