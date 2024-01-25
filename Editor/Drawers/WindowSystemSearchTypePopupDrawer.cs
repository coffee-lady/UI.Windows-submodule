using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UI.Windows;
using UnityEngine.UI.Windows.Utilities;
using Object = UnityEngine.Object;

namespace UnityEditor.UI.Windows
{
    [CustomPropertyDrawer(typeof(SearchAssetsByTypePopupAttribute))]
    public class WindowSystemSearchAssetsByTypePopupPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var attr = (SearchAssetsByTypePopupAttribute) attribute;

            SerializedProperty target = string.IsNullOrEmpty(attr.innerField)
                ? property
                : property.FindPropertyRelative(attr.innerField);
            EditorGUI.LabelField(position, label);
            position.x += EditorGUIUtility.labelWidth;
            position.width -= EditorGUIUtility.labelWidth;
            if (GUILayoutExt.DrawDropdown(position,
                    new GUIContent(target.objectReferenceValue != null
                        ? EditorHelpers.StringToCaption(target.objectReferenceValue.name)
                        : attr.noneOption), FocusType.Passive, target.objectReferenceValue))
            {
                Rect rect = position;
                Vector2 vector = GUIUtility.GUIToScreenPoint(new Vector2(rect.x, rect.y));
                rect.x = vector.x;
                rect.y = vector.y;

                var popup = new Popup
                {
                    title = attr.menuName, autoClose = true,
                    screenRect = new Rect(rect.x, rect.y + rect.height, rect.width, 200f)
                };
                string[] objects = AssetDatabase.FindAssets(
                    "t:" + (attr.filterType != null ? attr.filterType.Name : "Object"),
                    attr.filterDir == null ? null : new[] {attr.filterDir});
                if (string.IsNullOrEmpty(attr.noneOption) == false)
                {
                    popup.Item(attr.noneOption, null, searchable: false, action: item =>
                    {
                        property.serializedObject.Update();
                        target.objectReferenceValue = null;
                        property.serializedObject.ApplyModifiedProperties();
                    }, order: -1);
                }

                for (var i = 0; i < objects.Length; ++i)
                {
                    string path = AssetDatabase.GUIDToAssetPath(objects[i]);
                    var asset = AssetDatabase.LoadAssetAtPath<Object>(path);
                    popup.Item(EditorHelpers.StringToCaption(asset.name), () =>
                    {
                        property.serializedObject.Update();
                        target.objectReferenceValue = asset;
                        property.serializedObject.ApplyModifiedProperties();
                    });
                }

                popup.Show();
            }
        }
    }

    [CustomPropertyDrawer(typeof(SearchComponentsByTypePopupAttribute))]
    public class WindowSystemSearchComponentsByTypePopupPropertyDrawer : PropertyDrawer
    {
        private static bool changed;

        public override bool CanCacheInspectorGUI(SerializedProperty property)
        {
            return false;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            DrawGUI(position, label, (SearchComponentsByTypePopupAttribute) attribute, property);
        }

        public static void DrawGUI(Rect position, GUIContent label, SearchComponentsByTypePopupAttribute attr,
            SerializedProperty property, Action onChanged = null, bool drawLabel = true)
        {
            Type searchType = attr.baseType;
            var useSearchTypeOverride = false;
            if (attr.allowClassOverrides &&
                property.serializedObject.targetObject is ISearchComponentByTypeEditor searchComponentByTypeEditor)
            {
                useSearchTypeOverride = true;
                searchType = searchComponentByTypeEditor.GetSearchType();
            }

            IList searchArray = null;
            var singleOnly = false;
            if (attr.singleOnly &&
                property.serializedObject.targetObject is ISearchComponentByTypeSingleEditor
                    searchComponentByTypeSingleEditor)
            {
                searchArray = searchComponentByTypeSingleEditor.GetSearchTypeArray();
                singleOnly = true;
            }

            SerializedProperty target = string.IsNullOrEmpty(attr.innerField)
                ? property.Copy()
                : property.FindPropertyRelative(attr.innerField);
            var displayName = string.Empty;
            Object selectButtonObj = null;
            if (target.propertyType == SerializedPropertyType.ObjectReference && target.objectReferenceValue != null)
            {
                selectButtonObj = target.objectReferenceValue;
                object[] compDisplayAttrs = target.objectReferenceValue.GetType()
                    .GetCustomAttributes(typeof(ComponentModuleDisplayNameAttribute), true);
                if (compDisplayAttrs.Length > 0)
                {
                    var compDisplayAttr = (ComponentModuleDisplayNameAttribute) compDisplayAttrs[0];
                    displayName = compDisplayAttr.name;
                }
                else
                {
                    displayName = EditorHelpers.StringToCaption(target.objectReferenceValue.GetType().Name);
                }
            }

            if (target.propertyType == SerializedPropertyType.ManagedReference)
            {
                GetTypeFromManagedReferenceFullTypeName(target.managedReferenceFullTypename, out Type type);
                displayName = EditorHelpers.StringToCaption(type != null ? type.Name : string.Empty);
            }

            if (drawLabel)
            {
                EditorGUI.LabelField(position, label);
            }

            Rect rectPosition = position;
            if (drawLabel)
            {
                position.x += EditorGUIUtility.labelWidth;
                position.width -= EditorGUIUtility.labelWidth;
            }

            position.height = EditorGUIUtility.singleLineHeight;
            if (GUILayoutExt.DrawDropdown(position,
                    new GUIContent(string.IsNullOrEmpty(displayName) == false ? displayName : attr.noneOption),
                    FocusType.Passive, selectButtonObj))
            {
                Rect rect = position;
                Vector2 vector = GUIUtility.GUIToScreenPoint(new Vector2(rect.x, rect.y));
                rect.x = vector.x;
                rect.y = vector.y;

                var popup = new Popup
                {
                    title = attr.menuName, autoClose = true,
                    screenRect = new Rect(rect.x, rect.y + rect.height, rect.width, 200f)
                };
                if (string.IsNullOrEmpty(attr.noneOption) == false)
                {
                    popup.Item(attr.noneOption, null, searchable: false, action: item =>
                    {
                        property.serializedObject.Update();
                        if (target.propertyType == SerializedPropertyType.ObjectReference)
                        {
                            if (property.objectReferenceValue != null)
                            {
                                Object.DestroyImmediate(property.objectReferenceValue, true);
                                property.objectReferenceValue = null;
                            }
                        }
                        else if (target.propertyType == SerializedPropertyType.ManagedReference)
                        {
                            target.managedReferenceValue = null;
                            property.isExpanded = true;
                            property.serializedObject.SetIsDifferentCacheDirty();
                            GUI.changed = true;
                            changed = true;
                            onChanged?.Invoke();
                        }

                        property.serializedObject.ApplyModifiedProperties();
                    }, order: -1);
                }

                Type[] allTypes =
                    AppDomain.CurrentDomain.GetAssemblies().SelectMany(x => x.GetTypes())
                        .ToArray(); //searchType.Assembly.GetTypes();
                foreach (Type type in allTypes)
                {
                    if (
                        (((useSearchTypeOverride == false || searchType != attr.baseType) &&
                          searchType.IsAssignableFrom(type)) || attr.baseType == type.BaseType) &&
                        type.IsInterface == false &&
                        type.IsAbstract == false)
                    {
                        Type itemType = type;
                        if (singleOnly)
                        {
                            var found = false;
                            foreach (object item in searchArray)
                            {
                                if (item == null)
                                {
                                    continue;
                                }

                                if (itemType.IsAssignableFrom(item.GetType()))
                                {
                                    found = true;
                                    break;
                                }
                            }

                            if (found)
                            {
                                continue;
                            }
                        }

                        object[] compDisplayAttrs =
                            type.GetCustomAttributes(typeof(ComponentModuleDisplayNameAttribute), true);
                        if (compDisplayAttrs.Length > 0)
                        {
                            var compDisplayAttr = (ComponentModuleDisplayNameAttribute) compDisplayAttrs[0];
                            displayName = compDisplayAttr.name;
                        }
                        else
                        {
                            displayName = EditorHelpers.StringToCaption(type.Name);
                        }

                        popup.Item(displayName, () =>
                        {
                            target.serializedObject.ApplyModifiedProperties();
                            target.serializedObject.Update();
                            if (target.propertyType == SerializedPropertyType.ObjectReference)
                            {
                                GameObject go = (target.serializedObject.targetObject as Component)?.gameObject;
                                if (target.objectReferenceValue != null)
                                {
                                    Object.DestroyImmediate(target.objectReferenceValue, true);
                                    target.objectReferenceValue = null;
                                }

                                if (go != null)
                                {
                                    target.objectReferenceValue = go.AddComponent(itemType);
                                }
                            }
                            else if (target.propertyType == SerializedPropertyType.ManagedReference)
                            {
                                target.managedReferenceValue = Activator.CreateInstance(itemType);
                                property.isExpanded = true;
                                property.serializedObject.SetIsDifferentCacheDirty();
                                GUI.changed = true;
                                changed = true;
                                onChanged?.Invoke();
                            }

                            target.serializedObject.ApplyModifiedProperties();
                        });
                    }
                }

                popup.Show();
            }

            /*if (target.propertyType == SerializedPropertyType.ManagedReference) {

                position.y += position.height;
                position.height = rectPosition.height;
                var depth = property.depth;

                var lbl = new GUIContent(property.displayName);
                property.isExpanded = true;
                EditorGUI.PropertyField(position, property, lbl, true);

            }*/

            if (changed)
            {
                GUI.changed = true;
                changed = false;
            }
        }

        internal static bool GetTypeFromManagedReferenceFullTypeName(string managedReferenceFullTypename,
            out Type managedReferenceInstanceType)
        {
            managedReferenceInstanceType = null;
            string[] parts = managedReferenceFullTypename.Split(' ');
            if (parts.Length == 2)
            {
                string assemblyPart = parts[0];
                string nsClassnamePart = parts[1];
                managedReferenceInstanceType = Type.GetType($"{nsClassnamePart}, {assemblyPart}");
            }

            return managedReferenceInstanceType != null;
        }
    }
}