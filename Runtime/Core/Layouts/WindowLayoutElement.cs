using Sirenix.OdinInspector;

namespace UnityEngine.UI.Windows
{
    public class WindowLayoutElement : WindowComponent, ILayoutInstance
    {
        [TabGroup("Basic")] public int tagId;
        [TabGroup("Basic")] public WindowLayout innerLayout;
        [TabGroup("Basic")] public bool hideInScreen;

        WindowLayout ILayoutInstance.windowLayoutInstance { get; set; }
    }
}