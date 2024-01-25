using System;
using System.Collections;
using Sirenix.OdinInspector;
using UnityEngine.Events;
using UnityEngine.UI.Windows.Utilities;

namespace UnityEngine.UI.Windows.Components
{
    public class InputFieldComponent : GenericComponent, IInteractable, ISearchComponentByTypeEditor,
        ISearchComponentByTypeSingleEditor
    {
        [TabGroup("Basic")] [RequiredReference]
        public Selectable inputField;

        [TabGroup("Basic")] public TextComponent placeholder;

        private Action<string> callbackOnEditEnd;
        private Action<string> callbackOnChanged;
        private Func<string, int, char, char> callbackValidateChar;

        public void SetInteractable(bool state)
        {
            inputField.interactable = state;
        }

        public bool IsInteractable()
        {
            return inputField.interactable;
        }

        Type ISearchComponentByTypeEditor.GetSearchType()
        {
            return typeof(InputFieldComponentModule);
        }

        IList ISearchComponentByTypeSingleEditor.GetSearchTypeArray()
        {
            return componentModules.modules;
        }

        private void AddValueChangedListener(UnityAction<string> action)
        {
            if (this.inputField is InputField inputField)
            {
                inputField.onValueChanged.AddListener(action);
            }

#if TEXTMESHPRO_SUPPORT
            if (this.inputField is TMPro.TMP_InputField tmpInputField) {
                
                tmpInputField.onValueChanged.AddListener(action);
                
            }
#endif
        }

        private void AddEndEditListener(UnityAction<string> action)
        {
            if (this.inputField is InputField inputField)
            {
                inputField.onEndEdit.AddListener(action);
            }

#if TEXTMESHPRO_SUPPORT
            if (this.inputField is TMPro.TMP_InputField tmpInputField) {
                
                tmpInputField.onEndEdit.AddListener(action);
                
            }
#endif
        }

        private void AddValidateCharListener()
        {
            if (this.inputField is InputField inputField)
            {
                inputField.onValidateInput += OnValidateChar;
            }

#if TEXTMESHPRO_SUPPORT
            if (this.inputField is TMPro.TMP_InputField tmpInputField) {
                
                tmpInputField.onValidateInput += this.OnValidateChar;
                
            }
#endif
        }

        private void RemoveValueChangedListener(UnityAction<string> action)
        {
            if (this.inputField is InputField inputField)
            {
                inputField.onValueChanged.RemoveListener(action);
            }

#if TEXTMESHPRO_SUPPORT
            if (this.inputField is TMPro.TMP_InputField tmpInputField) {
                
                tmpInputField.onValueChanged.RemoveListener(action);
                
            }
#endif
        }

        private void RemoveEndEditListener(UnityAction<string> action)
        {
            if (this.inputField is InputField inputField)
            {
                inputField.onEndEdit.RemoveListener(action);
            }

#if TEXTMESHPRO_SUPPORT
            if (this.inputField is TMPro.TMP_InputField tmpInputField) {
                
                tmpInputField.onEndEdit.RemoveListener(action);
                
            }
#endif
        }

        private void RemoveValidateCharListener()
        {
            if (this.inputField is InputField inputField)
            {
                inputField.onValidateInput -= OnValidateChar;
            }

#if TEXTMESHPRO_SUPPORT
            if (this.inputField is TMPro.TMP_InputField tmpInputField) {
                
                tmpInputField.onValidateInput -= this.OnValidateChar;
                
            }
#endif
        }

        internal override void OnInitInternal()
        {
            base.OnInitInternal();

            AddValueChangedListener(OnValueChanged);
            AddEndEditListener(OnEndEdit);
            AddValidateCharListener();
        }

        internal override void OnDeInitInternal()
        {
            base.OnDeInitInternal();

            RemoveValueChangedListener(OnValueChanged);
            RemoveEndEditListener(OnEndEdit);
            RemoveValidateCharListener();

            RemoveCallbacks();
        }

        public void Clear()
        {
            SetText(string.Empty);
        }

        public bool IsFocused()
        {
            if (this.inputField is InputField inputField)
            {
                return inputField.isFocused;
            }

#if TEXTMESHPRO_SUPPORT
            if (this.inputField is TMPro.TMP_InputField tmpInputField) {

                return tmpInputField.isFocused;

            }
#endif

            return false;
        }

        public void SetFocus()
        {
            this.inputField.Select();
            if (this.inputField is InputField inputField)
            {
                inputField.ActivateInputField();
            }

#if TEXTMESHPRO_SUPPORT
            if (this.inputField is TMPro.TMP_InputField tmpInputField) {

                tmpInputField.ActivateInputField();

            }
#endif
        }

        public T GetSource<T>() where T : Selectable
        {
            return (T) inputField;
        }

        private void OnValueChanged(string value)
        {
            if (callbackOnChanged != null)
            {
                callbackOnChanged.Invoke(value);
            }
        }

        private void OnEndEdit(string value)
        {
            if (callbackOnEditEnd != null)
            {
                callbackOnEditEnd.Invoke(value);
            }
        }

        private char OnValidateChar(string value, int charIndex, char addedChar)
        {
            if (callbackValidateChar != null)
            {
                return callbackValidateChar.Invoke(value, charIndex, addedChar);
            }

            return addedChar;
        }

        public string GetText()
        {
            if (this.inputField is InputField inputField)
            {
                return inputField.text;
            }

#if TEXTMESHPRO_SUPPORT
            if (this.inputField is TMPro.TMP_InputField tmpInputField) {

                return tmpInputField.text;

            }
#endif

            return null;
        }

        public void SetText(string text)
        {
            if (text == null)
            {
                text = string.Empty;
            }

            if (this.inputField is InputField inputField)
            {
                inputField.text = text;
            }

#if TEXTMESHPRO_SUPPORT
            if (this.inputField is TMPro.TMP_InputField tmpInputField) {

                tmpInputField.text = text;

            }
#endif
        }

        public string GetPlaceholderText()
        {
            if (placeholder != null)
            {
                return placeholder.GetText();
            }

            return null;
        }

        public void SetPlaceholderText(string text)
        {
            if (placeholder != null)
            {
                placeholder.SetText(text);
            }
        }

        public void SetCallbackValueChanged(Action<string> callback)
        {
            callbackOnChanged = null;
            callbackOnChanged += callback;
        }

        public void AddCallbackValueChanged(Action<string> callback)
        {
            callbackOnChanged += callback;
        }

        public void RemoveCallbackValueChanged(Action<string> callback)
        {
            callbackOnChanged -= callback;
        }

        public void SetCallbackEditEnd(Action<string> callback)
        {
            callbackOnEditEnd = null;
            callbackOnEditEnd += callback;
        }

        public void AddCallbackEditEnd(Action<string> callback)
        {
            callbackOnEditEnd += callback;
        }

        public void RemoveCallbackEditEnd(Action<string> callback)
        {
            callbackOnEditEnd -= callback;
        }

        public void SetCallbackValidateChar(Func<string, int, char, char> callback)
        {
            callbackValidateChar = null;
            callbackValidateChar += callback;
        }

        public void AddCallbackEditEnd(Func<string, int, char, char> callback)
        {
            callbackValidateChar += callback;
        }

        public void RemoveCallbackEditEnd(Func<string, int, char, char> callback)
        {
            callbackValidateChar -= callback;
        }

        public virtual void RemoveCallbacksValueChanged()
        {
            callbackOnChanged = null;
        }

        public virtual void RemoveCallbacksEditEnd()
        {
            callbackOnEditEnd = null;
        }

        public virtual void RemoveCallbacksValidateChar()
        {
            callbackValidateChar = null;
        }

        public virtual void RemoveCallbacks()
        {
            callbackOnChanged = null;
            callbackOnEditEnd = null;
            callbackValidateChar = null;
        }

        public override void ValidateEditor()
        {
            base.ValidateEditor();

            if (inputField == null)
            {
                inputField = GetComponent<InputField>();
            }
#if TEXTMESHPRO_SUPPORT
            if (this.inputField == null) this.inputField = this.GetComponent<TMPro.TMP_InputField>();
#endif
        }
    }
}