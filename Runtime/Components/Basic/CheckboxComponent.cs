using System;
using Sirenix.OdinInspector;

namespace UnityEngine.UI.Windows.Components
{
    public interface ICheckboxGroup
    {
        void OnChecked(CheckboxComponent checkbox);
        bool CanBeUnchecked(CheckboxComponent checkbox);
    }

    public class CheckboxComponent : ButtonComponent
    {
        public WindowComponent checkedContainer;
        public WindowComponent uncheckedContainer;
        [TabGroup("Basic")] public bool isChecked;
        [TabGroup("Basic")] public bool autoToggle = true;
        public ICheckboxGroup group;

        private Action<bool> callback;
        private Action<CheckboxComponent, bool> callbackWithInstance;

        public override void ValidateEditor()
        {
            base.ValidateEditor();

            if (checkedContainer != null)
            {
                checkedContainer.hiddenByDefault = true;
                checkedContainer.AddEditorParametersRegistry(new EditorParametersRegistry(this)
                {
                    holdHiddenByDefault = true
                });
            }

            if (uncheckedContainer != null)
            {
                uncheckedContainer.hiddenByDefault = true;
                uncheckedContainer.AddEditorParametersRegistry(new EditorParametersRegistry(this)
                {
                    holdHiddenByDefault = true
                });
            }

            //this.UpdateCheckState();
        }

        internal override void OnInitInternal()
        {
            base.OnInitInternal();

            if (autoToggle)
            {
                button.onClick.AddListener(ToggleInternal);
            }
        }

        internal override void OnDeInitInternal()
        {
            button.onClick.RemoveAllListeners();

            base.OnDeInitInternal();
        }

        internal override void OnShowBeginInternal()
        {
            base.OnShowBeginInternal();

            SetCheckedState(isChecked);
        }

        public void Toggle()
        {
            SetCheckedState(!isChecked);
        }

        internal void ToggleInternal()
        {
            SetCheckedStateInternal(!isChecked);
        }

        internal void SetCheckedStateInternal(bool state, bool call = true, bool checkGroup = true)
        {
            if (CanClick() == false)
            {
                return;
            }

            WindowSystem.InteractWith(this);

            SetCheckedState(state, call, checkGroup);
        }

        public void SetCheckedState(bool state, bool call = true, bool checkGroup = true)
        {
            bool stateChanged = isChecked != state;
            isChecked = state;
            if (checkGroup && group != null)
            {
                if (state == false)
                {
                    if (group.CanBeUnchecked(this) == false)
                    {
                        isChecked = true;
                        state = true;
                        if (stateChanged)
                        {
                            stateChanged = false;
                        }
                    }
                }
                else
                {
                    group.OnChecked(this);
                }
            }

            UpdateCheckState();

            if (call && stateChanged)
            {
                if (callback != null)
                {
                    callback.Invoke(state);
                }

                if (callbackWithInstance != null)
                {
                    callbackWithInstance.Invoke(this, state);
                }
            }
        }

        public void SetGroup(ICheckboxGroup group)
        {
            this.group = group;
        }

        private void UpdateCheckState()
        {
            if (checkedContainer != null)
            {
                checkedContainer.ShowHide(isChecked);
            }

            if (uncheckedContainer != null)
            {
                uncheckedContainer.ShowHide(isChecked == false);
            }
        }

        public void SetCallback(Action<bool> callback)
        {
            RemoveCallbacks();
            AddCallback(callback);
        }

        public void SetCallback(Action<CheckboxComponent, bool> callback)
        {
            RemoveCallbacks();
            AddCallback(callback);
        }

        public void AddCallback(Action<bool> callback)
        {
            this.callback += callback;
        }

        public void AddCallback(Action<CheckboxComponent, bool> callback)
        {
            callbackWithInstance += callback;
        }

        public void RemoveCallback(Action<bool> callback)
        {
            this.callback -= callback;
        }

        public void RemoveCallback(Action<CheckboxComponent, bool> callback)
        {
            callbackWithInstance -= callback;
        }

        public override void RemoveCallbacks()
        {
            base.RemoveCallbacks();

            callback = null;
            callbackWithInstance = null;
        }
    }
}