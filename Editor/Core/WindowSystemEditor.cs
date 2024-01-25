using Sirenix.OdinInspector.Editor;
using UnityEngine;
using UnityEngine.UI.Windows;

namespace UnityEditor.UI.Windows
{
    [CustomEditor(typeof(WindowSystem), true)]
    [CanEditMultipleObjects]
    public class WindowSystemEditor : OdinEditor
    {
        public override void OnInspectorGUI()
        {
            GUILayoutExt.DrawComponentHeader(serializedObject, "UI",
                () => { GUILayout.Label("Window System", GUILayout.Height(36f)); }, new Color(0.3f, 0.4f, 0.6f, 0.4f));

            base.OnInspectorGUI();
        }
    }
}