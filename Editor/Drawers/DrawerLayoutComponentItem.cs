using Sirenix.OdinInspector.Editor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UI.Windows;
using UnityEngine.UI.Windows.WindowTypes;

namespace UnityEditor.UI.Windows
{
    public class DrawerLayoutComponentItem : OdinValueDrawer<LayoutItem.LayoutComponentItem>
    {
        private ReorderableList list;

        protected override void DrawPropertyLayout(GUIContent label)
        {
            InspectorProperty propertyComponent = Property;

            LayoutItem.LayoutComponentItem layoutComponentItem =
                Property.TryGetTypedValueEntry<LayoutItem.LayoutComponentItem>().SmartValue;

            Rect rect = GUILayoutUtility.GetRect(propertyComponent.Label, GUIStyle.none);

            var captionRect = new Rect(rect.x, rect.y, rect.width, 18f);

            var layoutElementName = string.Empty;

            int tagId = layoutComponentItem.tag;

            WindowLayout windowLayout = layoutComponentItem.windowLayout;

            if (windowLayout != null)
            {
                WindowLayoutElement layoutElement = windowLayout.GetLayoutElementByTagId(tagId);

                if (layoutElement != null)
                {
                    layoutElementName = layoutElement.name;
                }
            }

            var captionStyle = new GUIStyle(EditorStyles.boldLabel);
            GUI.Label(captionRect, EditorHelpers.StringToCaption(layoutElementName), captionStyle);

            var tagStyle = new GUIStyle(EditorStyles.label);
            tagStyle.alignment = TextAnchor.MiddleRight;

            GUI.Label(captionRect, "Tag: " + tagId, tagStyle);

            propertyComponent.Children.Get("component").Draw();
        }
    }
}

// using System.Collections.Generic;
// using Sirenix.OdinInspector.Editor;
// using Sirenix.Utilities.Editor;
// using UnityEditorInternal;
// using UnityEngine;
// using UnityEngine.UI.Windows;
// using UnityEngine.UI.Windows.WindowTypes;
//
// namespace UnityEditor.UI.Windows
// {
//     public class WindowSystemLayoutsPropertyDrawer : OdinValueDrawer<Layouts>
//     {
//         private ReorderableList list;
//
//         protected override void DrawPropertyLayout(GUIContent label)
//         {
//             InspectorProperty propertyItems = Property.Children.Get("items");
//             IPropertyValueEntry<List<LayoutItem>> entryItemsLayouts =
//                 propertyItems.TryGetTypedValueEntry<List<LayoutItem>>();
//
//             List<LayoutItem> items = entryItemsLayouts.SmartValue;
//
//             if (items.Count == 0)
//             {
//                 GUILayout.BeginHorizontal();
//                 GUILayout.FlexibleSpace();
//
//                 if (GUILayout.Button("Add Layout", GUILayout.Height(30f), GUILayout.Width(120f)))
//                 {
//                     AddNewLayout(entryItemsLayouts);
//                 }
//
//                 GUILayout.FlexibleSpace();
//                 GUILayout.EndHorizontal();
//                 GUILayout.Space(20f);
//                 return;
//             }
//
//             GUITabGroup tabGroup = SirenixEditorGUI.CreateAnimatedTabGroup("Layouts");
//
//             tabGroup.BeginGroup();
//
//             for (var i = 0; i < items.Count; ++i)
//             {
//                 LayoutItem layoutItem = items[i];
//
//                 DrawLayoutItem(layoutItem, tabGroup, entryItemsLayouts, i);
//             }
//
//             tabGroup.EndGroup();
//
//             if (GUILayout.Button("Add Layout"))
//             {
//                 AddNewLayout(entryItemsLayouts);
//             }
//         }
//
//         private void AddNewLayout(IPropertyValueEntry<List<LayoutItem>> entryItemsLayouts)
//         {
//             var layoutItems = new List<LayoutItem>();
//             layoutItems.AddRange(entryItemsLayouts.SmartValue);
//             layoutItems.Add(new LayoutItem());
//             entryItemsLayouts.SmartValue = layoutItems;
//             entryItemsLayouts.ApplyChanges();
//             Property.Update(true);
//         }
//
//         private void RemoveLayout(IPropertyValueEntry<List<LayoutItem>> entryItemsLayouts, LayoutItem layoutItem)
//         {
//             var layoutItems = new List<LayoutItem>();
//             layoutItems.AddRange(entryItemsLayouts.SmartValue);
//             layoutItems.Remove(layoutItem);
//             entryItemsLayouts.SmartValue = layoutItems;
//             entryItemsLayouts.ApplyChanges();
//             Property.Update(true);
//         }
//
//         private void DrawLayoutItem(LayoutItem layoutItem, GUITabGroup tabGroup,
//             IPropertyValueEntry<List<LayoutItem>> entryItemsLayouts, int i)
//         {
//             WindowLayout windowLayout = layoutItem.windowLayout;
//             string tabName = windowLayout?.name ?? $"Layout (Empty) [{i}]";
//
//             GUITabPage guiTabPage = tabGroup.RegisterTab(tabName);
//             guiTabPage.BeginPage();
//
//             InspectorProperty propertyLayout = entryItemsLayouts.Property.Children.Get(i);
//             InspectorProperty propertyComponents = propertyLayout.Children.Get("components");
//
//             SirenixEditorGUI.BeginVerticalList();
//
//             LayoutItem.LayoutComponentItem[] layoutItemComponents = layoutItem.components;
//
//             if (layoutItemComponents != null)
//             {
//                 for (var j = 0; j < layoutItemComponents.Length; j++)
//                 {
//                     LayoutItem.LayoutComponentItem layoutItemComponent = layoutItemComponents[j];
//
//                     DrawLayoutItemComponent(propertyComponents, j, layoutItemComponent, windowLayout);
//                 }
//             }
//
//             if (GUILayout.Button("Remove Layout"))
//             {
//                 RemoveLayout(entryItemsLayouts, layoutItem);
//             }
//
//             SirenixEditorGUI.EndVerticalList();
//
//             guiTabPage.EndPage();
//         }
//
//         private void DrawLayoutItemComponent(InspectorProperty propertyComponents, int j,
//             LayoutItem.LayoutComponentItem layoutItemComponent, WindowLayout windowLayout)
//         {
//             SirenixEditorGUI.BeginListItem();
//
//             InspectorProperty propertyComponent = propertyComponents.Children.Get(j);
//
//             Rect rect = GUILayoutUtility.GetRect(propertyComponent.Label, GUIStyle.none);
//
//             var captionRect = new Rect(rect.x, rect.y, rect.width, 18f);
//
//             var layoutElementName = string.Empty;
//
//             int tagId = layoutItemComponent.tag;
//
//             if (windowLayout != null)
//             {
//                 WindowLayoutElement layoutElement = windowLayout.GetLayoutElementByTagId(tagId);
//
//                 if (layoutElement != null)
//                 {
//                     layoutElementName = layoutElement.name;
//                 }
//             }
//
//             var captionStyle = new GUIStyle(EditorStyles.boldLabel);
//             captionStyle.padding = new RectOffset(5, 0, 0, 0);
//             GUI.Label(captionRect, EditorHelpers.StringToCaption(layoutElementName), captionStyle);
//
//             var tagStyle = new GUIStyle(EditorStyles.label);
//             tagStyle.alignment = TextAnchor.MiddleRight;
//             tagStyle.padding = new RectOffset(0, 5, 0, 0);
//
//             GUI.Label(captionRect, "Tag: " + tagId, tagStyle);
//
//             propertyComponent.Children.Get("component").Draw();
//
//             SirenixEditorGUI.EndListItem();
//         }
//     }
// }
//