using System;
using System.Collections;
using Sirenix.OdinInspector;
using UnityEngine.UI.Windows.Utilities;

namespace UnityEngine.UI.Windows.Components
{
    public interface IInteractable
    {
        bool IsInteractable();
        void SetInteractable(bool state);
    }

    public interface IInteractableButton : IInteractable
    {
        void SetCallback(Action callback);
        void AddCallback(Action callback);
        void RemoveCallback(Action callback);

        void SetCallback(Action<ButtonComponent> callback);
        void AddCallback(Action<ButtonComponent> callback);
        void RemoveCallback(Action<ButtonComponent> callback);

        void RemoveCallbacks();
    }

    public class ButtonComponent : GenericComponent, IInteractableButton, ISearchComponentByTypeEditor,
        ISearchComponentByTypeSingleEditor
    {
        [TabGroup("Basic")] [RequiredReference]
        public Button button;

        private CallbackRegistries callbackRegistries;

        public void SetInteractable(bool state)
        {
            button.interactable = state;
            componentModules.OnInteractableChanged(state);
        }

        public bool IsInteractable()
        {
            return button.interactable;
        }

        public void SetCallback(Action callback)
        {
            RemoveCallbacks();
            AddCallback(callback);
        }

        public void SetCallback(Action<ButtonComponent> callback)
        {
            RemoveCallbacks();
            AddCallback(callback);
        }

        public void AddCallback(Action callback)
        {
            callbackRegistries.Add(callback);
        }

        public void AddCallback(Action<ButtonComponent> callback)
        {
            AddCallback(new WithInstance {component = this, action = callback}, cb => cb.action.Invoke(cb.component));
        }

        public void RemoveCallback(Action callback)
        {
            callbackRegistries.Remove(callback);
        }

        public void RemoveCallback(Action<ButtonComponent> callback)
        {
            callbackRegistries.Remove(new WithInstance {component = this, action = callback}, null);
        }

        public virtual void RemoveCallbacks()
        {
            callbackRegistries.Clear();
        }

        Type ISearchComponentByTypeEditor.GetSearchType()
        {
            return typeof(ButtonComponentModule);
        }

        IList ISearchComponentByTypeSingleEditor.GetSearchTypeArray()
        {
            return componentModules.modules;
        }

        internal override void OnInitInternal()
        {
            button.onClick.AddListener(DoClickInternal);
            callbackRegistries.Initialize();

            base.OnInitInternal();
        }

        internal override void OnDeInitInternal()
        {
            base.OnDeInitInternal();

            ResetInstance();
            callbackRegistries.DeInitialize();
        }

        private void ResetInstance()
        {
            button.onClick.RemoveAllListeners();
            RemoveCallbacks();
        }

        public void RaiseClick()
        {
            DoClick();
        }

        public bool CanClick()
        {
            if (GetWindow().GetState() != ObjectState.Showing &&
                GetWindow().GetState() != ObjectState.Shown)
            {
                Debug.LogWarning("Couldn't send click because window is in `" + GetWindow().GetState() + "` state.",
                    this);
                return false;
            }

            if (GetState() != ObjectState.Showing &&
                GetState() != ObjectState.Shown)
            {
                Debug.LogWarning("Couldn't send click because component is in `" + GetState() + "` state.", this);
                return false;
            }

            return WindowSystem.CanInteractWith(this);
        }

        internal void DoClickInternal()
        {
            if (callbackRegistries.Count == 0)
            {
                return;
            }

            if (CanClick())
            {
                WindowSystem.InteractWith(this);

                DoClick();
            }
        }

        protected virtual void DoClick()
        {
            callbackRegistries.Invoke();
        }

        public void SetCallback<TState>(TState state, Action<TState> callback) where TState : IEquatable<TState>
        {
            RemoveCallbacks();
            AddCallback(state, callback);
        }

        public void AddCallback<TState>(TState state, Action<TState> callback) where TState : IEquatable<TState>
        {
            callbackRegistries.Add(state, callback);
        }

        public void RemoveCallback<TState>(TState state) where TState : IEquatable<TState>
        {
            callbackRegistries.Remove(state, null);
        }

        public void RemoveCallback<TState>(Action<TState> callback) where TState : IEquatable<TState>
        {
            callbackRegistries.Remove(default, callback);
        }

        public override void ValidateEditor()
        {
            base.ValidateEditor();

            if (button == null)
            {
                button = GetComponent<Button>();
            }
        }

        private struct WithInstance : IEquatable<WithInstance>
        {
            public ButtonComponent component;
            public Action<ButtonComponent> action;

            public bool Equals(WithInstance other)
            {
                return component == other.component && action == other.action;
            }
        }
    }
}