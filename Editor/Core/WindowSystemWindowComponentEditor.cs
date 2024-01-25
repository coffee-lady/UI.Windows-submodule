using Sirenix.OdinInspector.Editor;
using UnityEngine.UI.Windows;

namespace UnityEditor.UI.Windows
{
    [CustomEditor(typeof(WindowComponent), true)]
    [CanEditMultipleObjects]
    public class WindowSystemWindowComponentEditor : OdinEditor
    {
        public override void OnInspectorGUI()
        {
            GUILayoutExt.DrawComponentHeader(Tree.UnitySerializedObject, "C",
                () =>
                {
                    GUILayoutExt.DrawComponentHeaderItem("State", ((WindowObject) target).GetState().ToString());
                });

            base.OnInspectorGUI();
        }
    }
}