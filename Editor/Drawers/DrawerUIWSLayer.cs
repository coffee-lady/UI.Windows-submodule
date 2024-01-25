using Sirenix.OdinInspector.Editor;
using UnityEngine;
using UnityEngine.UI.Windows;
using UnityEngine.UI.Windows.Utilities;

namespace UnityEditor.UI.Windows
{
    public class DrawerUIWSLayer : OdinValueDrawer<UIWSLayer>
    {
        protected override void DrawPropertyLayout(GUIContent label)
        {
            string path = Property.FindChild(inspectorProperty => inspectorProperty.Name == "value", false)
                .UnityPropertyPath;
            SerializedProperty value = Property.Tree.GetUnityPropertyForPath(path);

            WindowSystemSettings settings = WindowSystem.GetSettings();

            Rect rect = GUILayoutUtility.GetRect(label, GUIStyle.none);
            Rect buttonRect = rect;

            EditorGUI.LabelField(rect, label);

            float labelWidth = EditorGUIUtility.labelWidth;

            var normalStyle = new GUIStyle(EditorStyles.miniButton);

            var padding = 2f;
            var layersVisible = 2;

            int selectedIndex = value.intValue;
            int elementsCount = layersVisible * 2 + 1;
            buttonRect.width = (rect.width - labelWidth - padding * (elementsCount - 1)) / elementsCount;
            buttonRect.x += labelWidth;

            for (int i = selectedIndex - layersVisible; i <= layersVisible + selectedIndex; ++i)
            {
                WindowSystemSettings.Layer layerInfo = settings.GetLayerInfo(i);

                if (i == value.intValue)
                {
                    GUI.Label(buttonRect, layerInfo.name, normalStyle);
                    GUILayoutExt.DrawBoxNotFilled(buttonRect, 1f, Color.white);
                }
                else
                {
                    if (GUI.Button(buttonRect, layerInfo.name, normalStyle))
                    {
                        value.intValue = i;
                        Property.Update();
                    }
                }

                buttonRect.x += buttonRect.width + padding;
            }
        }
    }
}