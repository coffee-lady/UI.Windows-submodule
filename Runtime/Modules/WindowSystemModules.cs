using System;
using System.Collections;
using UnityEngine.UI.Windows.Utilities;

namespace UnityEngine.UI.Windows.Modules
{
    public abstract class WindowModule : WindowLayout
    {
        [SerializeReference] public Parameters parameters;

        [Serializable]
        public class Parameters
        {
            public int defaultOrder;
            public bool applyCanvasScaler = true;

            public virtual void Apply(WindowModule instance)
            {
                instance.SetCanvasOrder(instance.GetWindow().GetCanvasOrder() + defaultOrder);
            }
        }
    }

    [Serializable]
    public class WindowModules
    {
        public WindowModuleInfo[] modules;

        private int loadingCount;

        public WindowModules()
        {
            modules = new WindowModuleInfo[] { };
        }

        public void Unload()
        {
            for (var i = 0; i < modules.Length; ++i)
            {
                modules[i].moduleInstance = null;
            }
        }

        public void LoadAsync(InitialParameters initialParameters, WindowBase window, Action onComplete)
        {
            Coroutines.Run(InitModules(initialParameters, window, onComplete));
        }

        public T Get<T>() where T : WindowModule
        {
            for (var i = 0; i < modules.Length; ++i)
            {
                if (modules[i].moduleInstance is T module)
                {
                    return module;
                }
            }

            return null;
        }

        private IEnumerator InitModules(InitialParameters initialParameters, WindowBase window, Action onComplete)
        {
            WindowSystemResources resources = WindowSystem.GetResources();
            TargetData targetData = WindowSystem.GetTargetData();

            for (var i = 0; i < modules.Length; ++i)
            {
                WindowModuleInfo moduleInfo = modules[i];
                if (moduleInfo.targets.IsValid(targetData) == false)
                {
                    continue;
                }

                if (moduleInfo.moduleInstance != null)
                {
                    continue;
                }

                WindowModule.Parameters parameters = moduleInfo.parameters;
                var data = new LoadingClosure
                {
                    windowModules = this,
                    index = i,
                    parameters = parameters,
                    window = window,
                    initialParameters = initialParameters
                };
                ++loadingCount;
                Coroutines.Run(resources.LoadAsync<WindowModule, LoadingClosure>(
                    new WindowSystemResources.LoadParameters {async = !initialParameters.showSync}, window, data,
                    moduleInfo.module, (asset, closure) =>
                    {
                        if (asset.createPool)
                        {
                            WindowSystem.GetPools().CreatePool(asset);
                        }

                        WindowModule instance = WindowSystem.GetPools().Spawn(asset, closure.window.transform);
                        instance.Setup(closure.window);
                        if (closure.parameters != null)
                        {
                            closure.parameters.Apply(instance);
                        }

                        instance.SetResetState();

                        if (closure.parameters != null && closure.parameters.applyCanvasScaler)
                        {
                            WindowLayoutPreferences layoutPreferences = closure.window.GetCurrentLayoutPreferences();
                            if (layoutPreferences != null && instance.canvasScaler != null)
                            {
                                layoutPreferences.Apply(instance.canvasScaler);
                            }
                        }

                        closure.window.RegisterSubObject(instance);

                        closure.windowModules.modules[closure.index].moduleInstance = instance;

                        instance.DoLoadScreenAsync(closure.initialParameters,
                            () => { --closure.windowModules.loadingCount; });
                    }));
            }

            while (loadingCount > 0)
            {
                yield return null;
            }

            onComplete.Invoke();
        }

        [Serializable]
        public struct WindowModuleInfo
        {
            public WindowSystemTargets targets;

            [ResourceType(typeof(WindowModule))] public Resource module;

            [SerializeReference] public WindowModule.Parameters parameters;

            [NonSerialized] internal WindowModule moduleInstance;
        }

        private struct LoadingClosure
        {
            public WindowBase window;
            public WindowModule.Parameters parameters;
            public WindowModules windowModules;
            public InitialParameters initialParameters;
            public int index;
        }
    }
}