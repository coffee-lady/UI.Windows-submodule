using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;

namespace UnityEngine.UI.Windows.Components
{
    public class GenericComponent : WindowComponent
    {
        [TitleGroup("Generic Component")] public bool grabComponentsOnValidate = true;
        [TitleGroup("Generic Component")] public WindowComponent[] components;

        public override void ValidateEditor()
        {
            base.ValidateEditor();

            if (grabComponentsOnValidate)
            {
                components = gameObject.GetComponentsInChildren<WindowComponent>().Skip(1).ToArray();
            }
        }

        public T Get<T>() where T : WindowComponent
        {
            for (var i = 0; i < components.Length; ++i)
            {
                WindowComponent comp = components[i];
                if (comp is T c)
                {
                    return c;
                }
            }

            return default;
        }

        public IEnumerable<T> GetAll<T>() where T : WindowComponent
        {
            for (var i = 0; i < components.Length; ++i)
            {
                WindowComponent comp = components[i];

                if (comp is T c)
                {
                    yield return c;
                }
            }
        }

        public T GetStrong<T>() where T : WindowComponent
        {
            Type type = typeof(T);

            for (var i = 0; i < components.Length; ++i)
            {
                WindowComponent comp = components[i];
                if (comp != null && comp.GetType() == type)
                {
                    return comp as T;
                }
            }

            return default;
        }

        public IEnumerable<T> GetAllStrong<T>() where T : WindowComponent
        {
            Type type = typeof(T);

            for (var i = 0; i < components.Length; ++i)
            {
                WindowComponent comp = components[i];
                if (comp != null && comp.GetType() == type)
                {
                    yield return comp as T;
                }
            }
        }
    }
}