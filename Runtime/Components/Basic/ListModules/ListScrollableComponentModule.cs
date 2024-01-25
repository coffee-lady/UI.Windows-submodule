using UnityEngine.UI.Windows.Utilities;

namespace UnityEngine.UI.Windows
{
    [ComponentModuleDisplayName("Scrollable Fades")]
    public class ListScrollableComponentModule : ListComponentModule
    {
        [Space(10f)] [RequiredReference] public ScrollRect scrollRect;

        [Space(10f)] public WindowComponent fadeBottom;

        public WindowComponent fadeTop;
        public WindowComponent fadeRight;
        public WindowComponent fadeLeft;

        public override void ValidateEditor()
        {
            base.ValidateEditor();

            if (fadeBottom != null)
            {
                fadeBottom.hiddenByDefault = true;
                fadeBottom.allowRegisterInRoot = false;
                fadeBottom.AddEditorParametersRegistry(new WindowObject.EditorParametersRegistry(this)
                {
                    holdHiddenByDefault = true
                });
                fadeBottom.AddEditorParametersRegistry(new WindowObject.EditorParametersRegistry(this)
                {
                    holdAllowRegisterInRoot = true
                });
            }

            if (fadeTop != null)
            {
                fadeTop.hiddenByDefault = true;
                fadeTop.allowRegisterInRoot = false;
                fadeTop.AddEditorParametersRegistry(new WindowObject.EditorParametersRegistry(this)
                {
                    holdHiddenByDefault = true
                });
                fadeTop.AddEditorParametersRegistry(new WindowObject.EditorParametersRegistry(this)
                {
                    holdAllowRegisterInRoot = true
                });
            }

            if (fadeRight != null)
            {
                fadeRight.hiddenByDefault = true;
                fadeRight.allowRegisterInRoot = false;
                fadeRight.AddEditorParametersRegistry(new WindowObject.EditorParametersRegistry(this)
                {
                    holdHiddenByDefault = true
                });
                fadeRight.AddEditorParametersRegistry(new WindowObject.EditorParametersRegistry(this)
                {
                    holdAllowRegisterInRoot = true
                });
            }

            if (fadeLeft != null)
            {
                fadeLeft.hiddenByDefault = true;
                fadeLeft.allowRegisterInRoot = false;
                fadeLeft.AddEditorParametersRegistry(new WindowObject.EditorParametersRegistry(this)
                {
                    holdHiddenByDefault = true
                });
                fadeLeft.AddEditorParametersRegistry(new WindowObject.EditorParametersRegistry(this)
                {
                    holdAllowRegisterInRoot = true
                });
            }

            if (scrollRect == null)
            {
                scrollRect = windowComponent.GetComponentInChildren<ScrollRect>(true);
            }
        }

        public override void OnInit()
        {
            base.OnInit();

            if (fadeBottom != null)
            {
                fadeBottom.Setup(GetWindow());
                fadeBottom.DoInit();
            }

            if (fadeLeft != null)
            {
                fadeLeft.Setup(GetWindow());
                fadeLeft.DoInit();
            }

            if (fadeRight != null)
            {
                fadeRight.Setup(GetWindow());
                fadeRight.DoInit();
            }

            if (fadeTop != null)
            {
                fadeTop.Setup(GetWindow());
                fadeTop.DoInit();
            }

            if (scrollRect != null)
            {
                scrollRect.onValueChanged.AddListener(OnScrollValueChanged);
                OnScrollValueChanged(scrollRect.normalizedPosition);
            }
        }

        public override void OnDeInit()
        {
            if (scrollRect != null)
            {
                scrollRect.onValueChanged.RemoveListener(OnScrollValueChanged);
            }

            if (fadeBottom != null)
            {
                fadeBottom.DoDeInit();
            }

            if (fadeLeft != null)
            {
                fadeLeft.DoDeInit();
            }

            if (fadeRight != null)
            {
                fadeRight.DoDeInit();
            }

            if (fadeTop != null)
            {
                fadeTop.DoDeInit();
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
        }

        public override void OnLayoutChanged()
        {
            base.OnLayoutChanged();

            if (scrollRect != null)
            {
                OnScrollValueChanged(scrollRect.normalizedPosition);
            }
        }

        public override void OnComponentsChanged()
        {
            base.OnComponentsChanged();

            if (scrollRect != null)
            {
                OnScrollValueChanged(scrollRect.normalizedPosition);
            }
        }

        private void OnScrollValueChanged(Vector2 position)
        {
#if UNITY_EDITOR
            if (Application.isPlaying == false)
            {
                return;
            }
#endif

            Rect contentRect = scrollRect.content.rect;
            Rect borderRect = windowComponent.rectTransform.rect;

            {
                float contentHeight = contentRect.height;
                float borderHeight = borderRect.height;

                float sizeY = borderHeight - contentHeight;
                if (sizeY >= 0f || scrollRect.vertical == false || windowComponent.IsVisible() == false)
                {
                    if (fadeTop != null)
                    {
                        fadeTop.Hide();
                    }

                    if (fadeBottom != null)
                    {
                        fadeBottom.Hide();
                    }
                }
                else
                {
                    if (position.y <= 0.01f)
                    {
                        if (fadeBottom != null)
                        {
                            fadeBottom.Hide();
                        }
                    }
                    else
                    {
                        if (fadeBottom != null)
                        {
                            fadeBottom.Show();
                        }
                    }

                    if (position.y >= 0.99f)
                    {
                        if (fadeTop != null)
                        {
                            fadeTop.Hide();
                        }
                    }
                    else
                    {
                        if (fadeTop != null)
                        {
                            fadeTop.Show();
                        }
                    }
                }
            }
            {
                float contentWidth = contentRect.width;
                float borderWidth = borderRect.width;
                float sizeX = borderWidth - contentWidth;
                if (sizeX >= 0f || scrollRect.horizontal == false || windowComponent.IsVisible() == false)
                {
                    if (fadeLeft != null)
                    {
                        fadeLeft.Hide();
                    }

                    if (fadeRight != null)
                    {
                        fadeRight.Hide();
                    }
                }
                else
                {
                    if (position.x <= 0.01f)
                    {
                        if (fadeLeft != null)
                        {
                            fadeLeft.Hide();
                        }
                    }
                    else
                    {
                        if (fadeLeft != null)
                        {
                            fadeLeft.Show();
                        }
                    }

                    if (position.x >= 0.99f)
                    {
                        if (fadeRight != null)
                        {
                            fadeRight.Hide();
                        }
                    }
                    else
                    {
                        if (fadeRight != null)
                        {
                            fadeRight.Show();
                        }
                    }
                }
            }
        }
    }
}