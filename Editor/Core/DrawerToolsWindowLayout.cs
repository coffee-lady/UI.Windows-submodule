using System.Collections.Generic;
using Sirenix.OdinInspector.Editor;
using UnityEngine;
using UnityEngine.UI.Windows;
using Object = UnityEngine.Object;

namespace UnityEditor.UI.Windows
{
    public class DrawerToolsWindowLayout : OdinValueDrawer<ToolsWindowLayout>
    {
        private Texture2D lastImagesPreview;

        private List<ImageCollectionItem> lastImages;

        protected override void DrawPropertyLayout(GUIContent label)
        {
            Object target = Property.Tree.UnitySerializedObject.targetObject;

            GUILayoutExt.DrawButtonGenerateSafeArea(target);

            if (GUILayout.Button("Collect Images"))
            {
                var images = new List<ImageCollectionItem>();
                lastImagesPreview = EditorHelpers.CollectImages(target, images);
                lastImages = images;
            }

            GUILayoutExt.DrawImages(lastImagesPreview, lastImages);
        }
    }
}