using Sirenix.OdinInspector.Editor;
using UnityEngine;
using UnityEngine.UI.Windows;

namespace UnityEditor.UI.Windows
{
    [CustomEditor(typeof(WindowBase), true)]
    [CanEditMultipleObjects]
    public class WindowSystemWindowBaseEditor : OdinEditor
    {
        public override void OnInspectorGUI()
        {
            GUILayoutExt.DrawComponentHeader(Tree.UnitySerializedObject, "S", () =>
            {
                GUILayoutExt.DrawComponentHeaderItem("State", ((WindowObject) target).GetState().ToString());
                GUILayoutExt.DrawComponentHeaderItem("Focus", ((WindowBase) target).GetFocusState().ToString());
            }, new Color(0f, 0.6f, 0f, 0.4f));

            base.OnInspectorGUI();
        }
    }
}