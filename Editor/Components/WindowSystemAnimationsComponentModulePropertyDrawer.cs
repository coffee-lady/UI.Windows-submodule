// using System.Collections.Generic;
// using UnityEditor.AddressableAssets.GUI;
// using UnityEditorInternal;
// using UnityEngine;
// using UnityEngine.UI.Windows;
// using UnityEngine.UI.Windows.Modules;
//
// namespace UnityEditor.UI.Windows
// {
//     [CustomPropertyDrawer(typeof(AnimationsComponentModule.States))]
//     public class WindowSystemAnimationsComponentModulePropertyDrawer : PropertyDrawer
//     {
//         private readonly Dictionary<string, ReorderableList> dicList = new();
//
//         private ReorderableList Init(SerializedProperty property, GUIContent label)
//         {
//             string key = property.propertyPath + ":" + property.serializedObject.targetObject.GetInstanceID();
//             if (dicList.TryGetValue(key, out ReorderableList list) == false)
//             {
//                 SerializedProperty items = property.FindPropertyRelative("items");
//                 list = new ReorderableList(property.serializedObject, items, true, false, true, true);
//                 list.drawHeaderCallback = rect => { GUI.Label(rect, label); };
//                 list.onAddCallback = list =>
//                 {
//                     property.serializedObject.Update();
//                     items.arraySize = items.arraySize + 1;
//                     SerializedProperty item = items.GetArrayElementAtIndex(items.arraySize - 1);
//                     item.FindPropertyRelative("parameters").FindPropertyRelative("items").ClearArray();
//                     property.serializedObject.ApplyModifiedProperties();
//                 };
//                 list.elementHeightCallback = index =>
//                 {
//                     var h = 0f;
//                     SerializedProperty prop = items.GetArrayElementAtIndex(index);
//                     prop.NextVisible(true);
//                     int depth = prop.depth;
//                     do
//                     {
//                         if (prop.depth != depth)
//                         {
//                             break;
//                         }
//
//                         h += EditorGUI.GetPropertyHeight(prop, true);
//                     } while (prop.NextVisible(false));
//
//                     return h;
//                 };
//                 list.drawElementCallback = (rect, index, active, focused) =>
//                 {
//                     property.serializedObject.Update();
//                     Rect splitterRect = rect;
//                     splitterRect.height = 1f;
//                     SplitterGUI.Splitter(splitterRect);
//                     SerializedProperty prop = items.GetArrayElementAtIndex(index);
//                     prop.NextVisible(true);
//                     int depth = prop.depth;
//                     do
//                     {
//                         if (prop.depth != depth)
//                         {
//                             break;
//                         }
//
//                         float h = EditorGUI.GetPropertyHeight(prop, true);
//                         rect.height = h;
//                         EditorGUI.PropertyField(rect, prop, true);
//                         rect.y += h;
//                     } while (prop.NextVisible(false));
//
//                     property.serializedObject.ApplyModifiedProperties();
//                 };
//                 list.onRemoveCallback = list =>
//                 {
//                     property.serializedObject.Update();
//                     int idx = list.index;
//                     SerializedProperty item = items.GetArrayElementAtIndex(idx);
//                     var lbl = string.Empty;
//                     var state = item.GetActualObjectForSerializedProperty<AnimationsComponentModule.State>(fieldInfo,
//                         ref lbl);
//                     if (state.parameters.items != null)
//                     {
//                         foreach (AnimationParameters anim in state.parameters.items)
//                         {
//                             // if (anim != null) {
//                             //
//                             //     Object.DestroyImmediate(anim, true);
//                             //
//                             // }
//                         }
//                     }
//
//                     items.DeleteArrayElementAtIndex(idx);
//                     property.serializedObject.ApplyModifiedProperties();
//                 };
//                 dicList.Add(key, list);
//             }
//             else if (list == null || list.serializedProperty == null)
//             {
//                 dicList.Remove(key);
//                 return Init(property, label);
//             }
//
//             return list;
//         }
//
//         public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
//         {
//             ReorderableList list = Init(property, label);
//             float height = list.GetHeight();
//             return height;
//         }
//
//         public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
//         {
//             ReorderableList list = Init(property, label);
//             list.DoList(position);
//         }
//     }
// }