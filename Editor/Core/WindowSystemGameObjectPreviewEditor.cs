using UnityEngine;
using UnityEngine.UI.Windows;

namespace UnityEditor.UI.Windows
{
    [CustomPreview(typeof(GameObject))]
    public class WindowSystemGameObjectPreviewEditor : ObjectPreview
    {
        private static Editor editor;
        private static Object obj;

        public override void Initialize(Object[] targets)
        {
            base.Initialize(targets);

            ValidateEditor(targets);
        }

        private void ValidateEditor(Object[] targets)
        {
            if (targets.Length > 1)
            {
                Reset();
                return;
            }

            var targetGameObject = target as GameObject;
            if (targetGameObject == null)
            {
                Reset();
                return;
            }

            var hasPreview = targetGameObject.GetComponent<IHasPreview>();
            if (hasPreview == null)
            {
                Reset();
                return;
            }

            if (editor == null || obj != targetGameObject)
            {
                obj = targetGameObject;
                editor = Editor.CreateEditor((Object) hasPreview);
            }
        }

        private void Reset()
        {
            obj = null;
            editor = null;
        }

        public override GUIContent GetPreviewTitle()
        {
            if (editor != null)
            {
                return editor.GetPreviewTitle();
            }

            return base.GetPreviewTitle();
        }

        public override bool HasPreviewGUI()
        {
            return editor != null && editor.HasPreviewGUI();
        }

        public override void OnInteractivePreviewGUI(Rect r, GUIStyle background)
        {
            if (editor != null)
            {
                editor.OnInteractivePreviewGUI(r, background);
            }
        }

        public override void OnPreviewGUI(Rect r, GUIStyle background)
        {
            if (editor != null)
            {
                editor.OnPreviewGUI(r, background);
            }
        }
    }
}