using UnityEngine.UI.Windows.Essentials.Tutorial;
using UnityEngine.UIElements;
using UnityEngine;

namespace UnityEditor.UI.Windows.Essentials.Tutorial {

    [CustomEditor(typeof(TutorialData))]
    public class TutorialDataEditor : Editor {

        private UnityEditorInternal.ReorderableList conditions;
        private UnityEditorInternal.ReorderableList actions;
        private bool changedConditions;
        private bool changedActions;

        public override VisualElement CreateInspectorGUI() {

            var root = new VisualElement();
            var so = this.serializedObject;
            
            var forWindowType = so.FindProperty("forWindowType");
            root.Add(new UnityEditor.UIElements.PropertyField(forWindowType, "For Window"));

            {
                var conditions = so.FindProperty("conditions");
                var items = conditions.FindPropertyRelative("items");
                this.conditions = new UnityEditorInternal.ReorderableList(items.serializedObject, items);
                this.conditions.drawHeaderCallback = rect => {
                    GUI.Label(rect, "Conditions");
                }; 
                this.conditions.elementHeightCallback = index => {
                    
                    var prop = items.GetArrayElementAtIndex(index);
                    var h = 20f;
                    
                    var val = prop.GetValue();
                    if (val != null) {
                        
                        var obj = (ICondition)val;
                        var content = new GUIContent(obj.caption);
                        h += EditorStyles.boldLabel.CalcHeight(content, EditorGUIUtility.currentViewWidth);
                        content = new GUIContent(obj.text);
                        h += EditorStyles.miniLabel.CalcHeight(content, EditorGUIUtility.currentViewWidth);
                        
                    }
                    
                    if (prop.hasVisibleChildren == true) {

                        h += EditorGUI.GetPropertyHeight(prop, true);
                        /*
                        var depth = prop.depth;
                        while (prop.NextVisible(true) == true) {

                            if (prop.depth <= depth) break;

                            h += EditorGUI.GetPropertyHeight(prop);
                            
                        }*/
                        
                    }
                    
                    return h;
                    
                }; 
                this.conditions.drawElementCallback = (rect, index, active, focused) => {

                    rect.height = 20f;
                    var attr = new UnityEngine.UI.Windows.Utilities.SearchComponentsByTypePopupAttribute(typeof(ICondition));
                    var prop = items.GetArrayElementAtIndex(index);
                    WindowSystemSearchComponentsByTypePopupPropertyDrawer.DrawGUI(rect, new GUIContent("Condition"), attr, prop, () => {
                        this.changedConditions = true;
                        var method = this.conditions.GetType().GetMethod("ClearCache", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        method.Invoke(this.conditions, null);
                    }, drawLabel: false);
                    rect.y += rect.height;

                    var val = prop.GetValue();
                    if (val != null) {

                        using (new GUILayoutExt.GUIAlphaUsing(0.8f)) {

                            var obj = (ICondition)val;
                            var content = new GUIContent(obj.caption);
                            var h = EditorStyles.boldLabel.CalcHeight(content, EditorGUIUtility.currentViewWidth);
                            rect.height = h;
                            EditorGUI.LabelField(rect, content, EditorStyles.boldLabel);
                            rect.y += rect.height;

                            content = new GUIContent(obj.text);
                            h = EditorStyles.miniLabel.CalcHeight(content, EditorGUIUtility.currentViewWidth);
                            rect.height = h;
                            EditorGUI.LabelField(rect, obj.text, EditorStyles.miniLabel);
                            rect.y += rect.height;

                        }

                    }

                    if (prop.hasVisibleChildren == true) {

                        rect.height = EditorGUI.GetPropertyHeight(prop, true);
                        EditorGUI.PropertyField(rect, prop, true);
                        if (GUI.changed == true) {
                            prop.serializedObject.ApplyModifiedProperties();
                        }
                        
                        /*
                        var depth = prop.depth;
                        while (prop.NextVisible(true) == true) {

                            if (prop.depth <= depth) break;

                            rect.height = EditorGUI.GetPropertyHeight(prop);
                            EditorGUI.PropertyField(rect, prop);
                            if (GUI.changed == true) {
                                prop.serializedObject.ApplyModifiedProperties();
                            }
                            rect.y += rect.height;
                            
                        }*/
                        
                    }

                };

                var container = new IMGUIContainer(() => {
                    if (this.changedConditions == true) {
                        GUI.changed = true;
                        this.changedConditions = false;
                    }
                    this.conditions.DoLayoutList();
                });
                container.style.marginTop = 6f;
                root.Add(container);
            }
            
            {
                var actions = so.FindProperty("actions");
                var items = actions.FindPropertyRelative("items");
                this.actions = new UnityEditorInternal.ReorderableList(items.serializedObject, items);
                this.actions.drawHeaderCallback = rect => {
                    GUI.Label(rect, "Actions");
                }; 
                this.actions.elementHeightCallback = index => {
                    
                    var prop = items.GetArrayElementAtIndex(index);
                    var h = 20f;
                    
                    var val = prop.GetValue();
                    if (val != null) {
                        
                        var obj = (IAction)val;
                        var content = new GUIContent(obj.caption);
                        h += EditorStyles.boldLabel.CalcHeight(content, EditorGUIUtility.currentViewWidth);
                        content = new GUIContent(obj.text);
                        h += EditorStyles.miniLabel.CalcHeight(content, EditorGUIUtility.currentViewWidth);
                        
                    }

                    if (prop.hasVisibleChildren == true) {

                        h += EditorGUI.GetPropertyHeight(prop, true);
                        /*var depth = prop.depth;
                        while (prop.NextVisible(true) == true) {

                            if (prop.depth <= depth) break;

                            h += EditorGUI.GetPropertyHeight(prop);
                            
                        }*/
                        
                    }
                    
                    return h;
                    
                }; 
                this.actions.drawElementCallback = (rect, index, active, focused) => {

                    rect.height = 20f;
                    var attr = new UnityEngine.UI.Windows.Utilities.SearchComponentsByTypePopupAttribute(typeof(IAction));
                    var prop = items.GetArrayElementAtIndex(index);
                    WindowSystemSearchComponentsByTypePopupPropertyDrawer.DrawGUI(rect, new GUIContent("Action"), attr, prop, () => {
                        this.changedActions = true;
                        var method = this.actions.GetType().GetMethod("ClearCache", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        method.Invoke(this.actions, null);
                    }, drawLabel: false);
                    rect.y += rect.height;
                    
                    var val = prop.GetValue();
                    if (val != null) {

                        using (new GUILayoutExt.GUIAlphaUsing(0.8f)) {

                            var obj = (IAction)val;
                            var content = new GUIContent(obj.caption);
                            var h = EditorStyles.boldLabel.CalcHeight(content, EditorGUIUtility.currentViewWidth);
                            rect.height = h;
                            EditorGUI.LabelField(rect, content, EditorStyles.boldLabel);
                            rect.y += rect.height;

                            content = new GUIContent(obj.text);
                            h = EditorStyles.miniLabel.CalcHeight(content, EditorGUIUtility.currentViewWidth);
                            rect.height = h;
                            EditorGUI.LabelField(rect, obj.text, EditorStyles.miniLabel);
                            rect.y += rect.height;

                        }

                    }

                    if (prop.hasVisibleChildren == true) {

                        rect.height = EditorGUI.GetPropertyHeight(prop, true);
                        EditorGUI.PropertyField(rect, prop, true);
                        if (GUI.changed == true) {
                            prop.serializedObject.ApplyModifiedProperties();
                        }
                        /*
                        var depth = prop.depth;
                        while (prop.NextVisible(true) == true) {

                            if (prop.depth <= depth) break;
                            
                            rect.height = EditorGUI.GetPropertyHeight(prop);
                            EditorGUI.PropertyField(rect, prop, true);
                            if (GUI.changed == true) {
                                prop.serializedObject.ApplyModifiedProperties();
                            }
                            rect.y += rect.height;
                            
                        }*/
                        
                    }

                };

                var container = new IMGUIContainer(() => {
                    if (this.changedActions == true) {
                        GUI.changed = true;
                        this.changedActions = false;
                    }
                    this.actions.DoLayoutList();
                });
                container.style.marginTop = 6f;
                root.Add(container);
            }

            return root;
            
        }

    }
    
    public static class UIElementsEditorHelper
    {
        public static void FillDefaultInspector(VisualElement container, SerializedProperty property)
        {
            if (property.NextVisible(true)) // Expand first child.
            {
                do
                {
                    var field = new UnityEditor.UIElements.PropertyField(property);
                    field.name = "PropertyField:" + property.propertyPath;
 
                    container.Add(field);
                }
                while (property.NextVisible(false));
            }
        }
    }

}