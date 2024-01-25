using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEditor;

namespace UnityEngine.UI.Windows
{
    [DisallowMultipleComponent]
    public class WindowComponent : WindowObject, IHasPreview
    {
        [TabGroup("Modules")] [OdinSerialize] public ComponentModules componentModules;

        public T GetModule<T>()
        {
            return componentModules.GetModule<T>();
        }

        public T[] GetModules<T>()
        {
            return componentModules.GetModules<T>();
        }

        public override void ValidateEditor()
        {
            base.ValidateEditor();

            if (componentModules.windowComponent == null)
            {
                componentModules.windowComponent = this;
            }

            componentModules.ValidateEditor();
        }

        public override void OnPoolAdd()
        {
            base.OnPoolAdd();

            componentModules.OnPoolAdd();
        }

        internal override void OnInitInternal()
        {
            base.OnInitInternal();

            componentModules.OnInit();
        }

        internal override void OnDeInitInternal()
        {
            base.OnDeInitInternal();

            componentModules.OnDeInit();
        }

        internal override void OnShowBeginInternal()
        {
            base.OnShowBeginInternal();

            componentModules.OnShowBegin();
        }

        internal override void OnHideBeginInternal()
        {
            base.OnHideBeginInternal();

            componentModules.OnHideBegin();
        }

        internal override void OnShowEndInternal()
        {
            base.OnShowEndInternal();

            componentModules.OnShowEnd();
        }

        internal override void OnHideEndInternal()
        {
            base.OnHideEndInternal();

            componentModules.OnHideEnd();
        }

        [Serializable]
        public class ComponentModules
        {
            [HideInInspector] public WindowComponent windowComponent;

            public WindowComponentModule[] modules;

            public ComponentModules()
            {
                modules = new WindowComponentModule[] { };
            }

            public T GetModule<T>()
            {
                if (modules == null)
                {
                    return default;
                }

                for (var i = 0; i < modules.Length; ++i)
                {
                    if (modules[i] is T module)
                    {
                        return module;
                    }
                }

                return default;
            }

            public T[] GetModules<T>()
            {
                var modulesOfGivenType = new List<T>();

                if (modules == null)
                {
                    return modulesOfGivenType.ToArray();
                }

                for (var i = 0; i < modules.Length; ++i)
                {
                    if (modules[i] is T module)
                    {
                        modulesOfGivenType.Add(module);
                    }
                }

                return modulesOfGivenType.ToArray();
            }

            public void ValidateEditor()
            {
                if (modules == null)
                {
                    return;
                }

                for (var i = 0; i < modules.Length; ++i)
                {
                    if (modules[i] != null)
                    {
                        modules[i].windowComponent = windowComponent;
                        modules[i].ValidateEditor();
                    }
                }
            }

            public void OnInteractableChanged(bool state)
            {
                if (modules == null)
                {
                    return;
                }

                for (var i = 0; i < modules.Length; ++i)
                {
                    if (modules[i] != null)
                    {
                        modules[i].OnInteractableChanged(state);
                    }
                }
            }

            public void OnLayoutChanged()
            {
                if (modules == null)
                {
                    return;
                }

                for (var i = 0; i < modules.Length; ++i)
                {
                    if (modules[i] != null)
                    {
                        modules[i].OnLayoutChanged();
                    }
                }
            }

            public void OnPoolAdd()
            {
                if (modules == null)
                {
                    return;
                }

                for (var i = 0; i < modules.Length; ++i)
                {
                    if (modules[i] != null)
                    {
                        modules[i].OnPoolAdd();
                    }
                }
            }

            public void OnInit()
            {
                if (modules == null)
                {
                    return;
                }

                for (var i = 0; i < modules.Length; ++i)
                {
                    if (modules[i] != null)
                    {
                        modules[i].OnInit();
                    }
                }
            }

            public void OnDeInit()
            {
                if (modules == null)
                {
                    return;
                }

                for (var i = 0; i < modules.Length; ++i)
                {
                    if (modules[i] != null)
                    {
                        modules[i].OnDeInit();
                    }
                }
            }

            public void OnShowBegin()
            {
                if (modules == null)
                {
                    return;
                }

                for (var i = 0; i < modules.Length; ++i)
                {
                    if (modules[i] != null)
                    {
                        modules[i].OnShowBegin();
                    }
                }
            }

            public void OnHideBegin()
            {
                if (modules == null)
                {
                    return;
                }

                for (var i = 0; i < modules.Length; ++i)
                {
                    if (modules[i] != null)
                    {
                        modules[i].OnHideBegin();
                    }
                }
            }

            public void OnShowEnd()
            {
                if (modules == null)
                {
                    return;
                }

                for (var i = 0; i < modules.Length; ++i)
                {
                    if (modules[i] != null)
                    {
                        modules[i].OnShowEnd();
                    }
                }
            }

            public void OnHideEnd()
            {
                if (modules == null)
                {
                    return;
                }

                for (var i = 0; i < modules.Length; ++i)
                {
                    if (modules[i] != null)
                    {
                        modules[i].OnHideEnd();
                    }
                }
            }
        }
    }

    public class ComponentModuleDisplayNameAttribute : Attribute
    {
        public string name;

        public ComponentModuleDisplayNameAttribute(string name)
        {
            this.name = name;
        }
    }

    [Serializable]
    public abstract class WindowComponentModule : IHolder
    {
        public WindowComponent windowComponent;

        public virtual void ValidateEditor()
        {
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
#endif

        public WindowBase GetWindow()
        {
            return windowComponent.GetWindow();
        }

        public virtual void OnInteractableChanged(bool state)
        {
        }

        public virtual void OnLayoutChanged()
        {
        }

        public virtual void OnPoolAdd()
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

        public virtual void OnHideBegin()
        {
        }

        public virtual void OnShowEnd()
        {
        }

        public virtual void OnHideEnd()
        {
        }
    }
}