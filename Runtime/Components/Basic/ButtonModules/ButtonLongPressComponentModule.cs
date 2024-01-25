using System;
using UnityEngine.EventSystems;
using UnityEngine.UI.Windows.Components;

namespace UnityEngine.UI.Windows
{
    public class ButtonLongPressComponentModule : ButtonComponentModule, IPointerDownHandler, IPointerUpHandler
    {
        public float pressTime = 2f;
        public ProgressComponent progressComponent;

        [Header("Use long press via callback, not by overriding RaiseClick()")]
        public bool callbackMode;

        private float pressTimer;
        private bool isPressed;
        private Action callback;

        public void OnPointerDown(PointerEventData eventData)
        {
            if (windowComponent.gameObject.activeSelf == false)
            {
                return;
            }

            isPressed = true;
            pressTimer = Time.realtimeSinceStartup;

            if (progressComponent != null)
            {
                progressComponent.Show();
                progressComponent.SetNormalizedValue(0f);
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            isPressed = false;

            if (progressComponent != null)
            {
                progressComponent.Hide();
            }

            if (callbackMode)
            {
                return;
            }

            if (Time.realtimeSinceStartup - pressTimer >= pressTime)
            {
                buttonComponent.RaiseClick();
            }
        }

        public override void ValidateEditor()
        {
            base.ValidateEditor();

            if (progressComponent != null)
            {
                progressComponent.hiddenByDefault = true;
            }
        }

        public override void OnHideBegin()
        {
            base.OnHideBegin();

            isPressed = false;
        }

        public override void OnDeInit()
        {
            base.OnDeInit();

            RemoveAllCallbacks();
        }

        public void SetCallback(Action callback)
        {
            this.callback = callback;
        }

        public void AddCallback(Action callback)
        {
            this.callback += callback;
        }

        public void RemoveCallback(Action callback)
        {
            this.callback -= callback;
        }

        public void RemoveAllCallbacks()
        {
            callback = null;
        }

        public override void OnInit()
        {
            base.OnInit();

            if (callbackMode == false)
            {
                buttonComponent.button.onClick.RemoveAllListeners();
            }
        }

        public void LateUpdate()
        {
            if (isPressed == false)
            {
                return;
            }

            float dt = Time.realtimeSinceStartup - pressTimer;

            if (progressComponent != null)
            {
                progressComponent.SetNormalizedValue(dt / pressTime);
            }

            if (callbackMode && dt > pressTime)
            {
                callback?.Invoke();
                isPressed = false;
            }
        }
    }
}