using System;
using Sirenix.OdinInspector;
using UnityEngine.UI.Windows.Modules;
using UnityEngine.UI.Windows.Utilities;

namespace UnityEngine.UI.Windows
{
    public enum FocusState
    {
        None,
        Focused,
        Unfocused
    }

    public abstract class WindowBase : WindowObject, IHasPreview
    {
        [TabGroup("Basic")] public WindowPreferences preferences = WindowPreferences.Default;
        [TabGroup("Modules")] public WindowModules modules = new();

        [HideInInspector] public int identifier;
        [HideInInspector] public int windowSourceId;

        [HideInInspector] public Camera workCamera;

        private float currentDepth;
        private float currentZDepth;
        private int currentCanvasDepth;

        private FocusState focusState;

        public virtual void OnParametersPass()
        {
        }

        public virtual void OnEmptyPass()
        {
        }

        public FocusState GetFocusState()
        {
            return focusState;
        }

        internal override void OnDeInitInternal()
        {
            modules.Unload();

            base.OnDeInitInternal();
        }

        public void SetAsPerspective()
        {
            preferences.cameraMode = UIWSCameraMode.Perspective;
            ApplyCamera();
        }

        public void SetAsOrthographic()
        {
            preferences.cameraMode = UIWSCameraMode.Orthographic;
            ApplyCamera();
        }

        public override void Hide(TransitionParameters parameters)
        {
            parameters = parameters.ReplaceIgnoreTouch(true);
            TransitionParameters cbParameters = parameters.ReplaceCallback(() =>
            {
                PushToPool();
                parameters.RaiseCallback();
            });

            if (cbParameters.data.replaceDelay)
            {
                Tweener tweener = WindowSystem.GetTweener();
                tweener.Add(this, cbParameters.data.delay, 0f, 0f).Tag(this).OnComplete(obj =>
                {
                    base.Hide(cbParameters.ReplaceDelay(0f));
                });
            }
            else
            {
                base.Hide(cbParameters);
            }
        }

        public virtual int GetCanvasOrder()
        {
            return 0;
        }

        public virtual Canvas GetCanvas()
        {
            return null;
        }

        public void SetInitialParameters(InitialParameters parameters)
        {
            {
                if (parameters.overrideLayer)
                {
                    preferences.layer = parameters.layer;
                }

                if (parameters.overrideSingleInstance)
                {
                    preferences.singleInstance = parameters.singleInstance;
                }
            }

            ApplyDepth();
            ApplyCamera();
        }

        internal void DoFocusTookInternal()
        {
            if (focusState == FocusState.Focused)
            {
                return;
            }

            focusState = FocusState.Focused;

            DoSendFocusTook();
        }

        internal void DoFocusLostInternal()
        {
            if (focusState == FocusState.Unfocused)
            {
                return;
            }

            focusState = FocusState.Unfocused;

            DoSendFocusLost();
        }

        internal override void OnHideEndInternal()
        {
            WindowSystem.RemoveWindow(this);

            base.OnHideEndInternal();
        }

        internal override void OnShowEndInternal()
        {
            WindowSystem.SendFullCoverageOnShowEnd(this);

            base.OnShowEndInternal();
        }

        internal override void OnShowBeginInternal()
        {
            WindowSystem.SendFocusOnShowBegin(this);

            base.OnShowBeginInternal();
        }

        internal override void OnHideBeginInternal()
        {
            WindowSystem.SendFullCoverageOnHideBegin(this);
            WindowSystem.SendFocusOnHideBegin(this);

            base.OnHideBeginInternal();
        }

        public float GetZDepth()
        {
            return currentZDepth;
        }

        public float GetDepth()
        {
            return currentDepth;
        }

        public int GetCanvasDepth()
        {
            return currentCanvasDepth;
        }

        internal void ApplyCamera()
        {
            WindowSystemSettings settings = WindowSystem.GetSettings();
            if (preferences.cameraMode == UIWSCameraMode.UseSettings)
            {
                if (settings.camera.orthographicDefault)
                {
                    ApplyCameraSettings(UIWSCameraMode.Orthographic);
                }
                else
                {
                    ApplyCameraSettings(UIWSCameraMode.Perspective);
                }

                return;
            }

            ApplyCameraSettings(preferences.cameraMode);
        }

        private void ApplyCameraSettings(UIWSCameraMode mode)
        {
            WindowSystemSettings settings = WindowSystem.GetSettings();
            switch (mode)
            {
                case UIWSCameraMode.Orthographic:
                    workCamera.orthographic = true;
                    workCamera.orthographicSize = settings.camera.orthographicSize;
                    workCamera.nearClipPlane = settings.camera.orthographicNearClippingPlane;
                    workCamera.farClipPlane = settings.camera.orthographicFarClippingPlane;
                    if (settings.canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                    {
                        workCamera.enabled = false;
                    }

                    break;

                case UIWSCameraMode.Perspective:
                    workCamera.orthographic = false;
                    workCamera.fieldOfView = settings.camera.perspectiveSize;
                    workCamera.nearClipPlane = settings.camera.perspectiveNearClippingPlane;
                    workCamera.farClipPlane = settings.camera.perspectiveFarClippingPlane;
                    break;
            }
        }

        public override void TurnOffRender()
        {
            base.TurnOffRender();

            workCamera.enabled = false;
        }

        public override void TurnOnRender()
        {
            base.TurnOnRender();

            WindowSystemSettings settings = WindowSystem.GetSettings();
            if (settings.canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                workCamera.enabled = false;
            }
            else
            {
                workCamera.enabled = true;
            }
        }

        internal void ApplyDepth()
        {
            float depth = WindowSystem.GetNextDepth(preferences.layer);
            int canvasDepth = WindowSystem.GetNextCanvasDepth(preferences.layer);
            float zDepth = WindowSystem.GetNextZDepth(preferences.layer);

            currentDepth = depth;
            currentZDepth = zDepth;
            currentCanvasDepth = canvasDepth;

            Transform tr = transform;
            workCamera.depth = depth;
            Vector3 pos = tr.position;
            pos.z = zDepth;
            tr.position = pos;
        }

#if UNITY_EDITOR
        public override void ValidateEditor()
        {
            base.ValidateEditor();

            var helper = new DirtyHelper(this);
            if (workCamera == null)
            {
                helper.SetObj(ref workCamera, GetComponent<Camera>());
            }

            if (workCamera == null)
            {
                helper.SetObj(ref workCamera, GetComponentInChildren<Camera>(true));
            }

            if (workCamera != null)
            {
                CameraClearFlags workCameraClearFlags = workCamera.clearFlags;
                if (helper.SetEnum(ref workCameraClearFlags, CameraClearFlags.Depth))
                {
                    workCamera.clearFlags = workCameraClearFlags;
                }
            }

            helper.Apply();
        }
#endif

        public virtual void LoadAsync(InitialParameters initialParameters, Action onComplete)
        {
            modules.LoadAsync(initialParameters, this, onComplete);
        }

        public virtual WindowLayoutPreferences GetCurrentLayoutPreferences()
        {
            return null;
        }
    }
}