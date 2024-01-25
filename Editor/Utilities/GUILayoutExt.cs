using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Sirenix.OdinInspector.Editor;
using UnityEditor.Experimental;
using UnityEngine;
using UnityEngine.UI.Windows;
using Object = UnityEngine.Object;

namespace UnityEditor.UI.Windows
{
    public static class SplitterGUI
    {
        public static readonly GUIStyle splitter;

        private static readonly Color splitterColor =
            EditorGUIUtility.isProSkin ? new Color(0.3f, 0.3f, 0.3f) : new Color(0.5f, 0.5f, 0.5f);

        static SplitterGUI()
        {
            //GUISkin skin = GUI.skin;

            splitter = new GUIStyle();
            splitter.normal.background = EditorGUIUtility.whiteTexture;
            splitter.stretchWidth = true;
            splitter.margin = new RectOffset(0, 0, 7, 7);
        }

        // GUILayout Style
        public static void Splitter(Color rgb, float thickness = 1)
        {
            Rect position = GUILayoutUtility.GetRect(GUIContent.none, splitter, GUILayout.Height(thickness));

            if (Event.current.type == EventType.Repaint)
            {
                Color restoreColor = GUI.color;
                GUI.color = rgb;
                splitter.Draw(position, false, false, false, false);
                GUI.color = restoreColor;
            }
        }

        public static void Splitter(float thickness, GUIStyle splitterStyle)
        {
            Rect position = GUILayoutUtility.GetRect(GUIContent.none, splitterStyle, GUILayout.Height(thickness));

            if (Event.current.type == EventType.Repaint)
            {
                Color restoreColor = GUI.color;
                GUI.color = splitterColor;
                splitterStyle.Draw(position, false, false, false, false);
                GUI.color = restoreColor;
            }
        }

        public static void Splitter(float thickness = 1)
        {
            Splitter(thickness, splitter);
        }

        // GUI Style
        public static void Splitter(Rect position)
        {
            if (Event.current.type == EventType.Repaint)
            {
                Color restoreColor = GUI.color;
                GUI.color = splitterColor;
                splitter.Draw(position, false, false, false, false);
                GUI.color = restoreColor;
            }
        }
    }

    public abstract class CustomEditorAttribute : Attribute
    {
        public Type type;
        public int order;

        protected CustomEditorAttribute(Type type, int order = 0)
        {
            this.type = type;
            this.order = order;
        }
    }

    public class ComponentCustomEditorAttribute : CustomEditorAttribute
    {
        public ComponentCustomEditorAttribute(Type type, int order = 0) : base(type, order)
        {
        }
    }

    public struct GUITab
    {
        public string caption;
        public Action onDraw;
        public float customWidth;

        public static GUITab none => new(null, null);

        public GUITab(string caption, Action onDraw, float customWidth = 0f)
        {
            this.caption = caption;
            this.onDraw = onDraw;
            this.customWidth = customWidth;
        }
    }

    public static class GUILayoutExt
    {
        private static int foldOutLevel;

        public static GUIBackgroundColorUsing GUIBackgroundColor(Color color)
        {
            return new GUIBackgroundColorUsing(color);
        }

        public static GUIColorUsing GUIColor(Color color)
        {
            return new GUIColorUsing(color);
        }

        public static void DrawImages(Texture2D preview, List<ImageCollectionItem> images)
        {
            if (images != null)
            {
                Box(4f, 4f, () =>
                {
                    if (preview != null)
                    {
                        var labelStyle = new GUIStyle(EditorStyles.label);
                        labelStyle.fontStyle = FontStyle.Bold;
                        labelStyle.alignment = TextAnchor.LowerCenter;
                        float w = EditorGUIUtility.currentViewWidth - 80f;
                        float h = w / preview.width * preview.height;
                        GUILayout.Label(string.Empty, GUILayout.MinWidth(w), GUILayout.MinHeight(h), GUILayout.Width(w),
                            GUILayout.Height(h));
                        Rect lastRect = GUILayoutUtility.GetLastRect();
                        lastRect.width = w;
                        lastRect.height = h;

                        EditorGUI.DrawTextureTransparent(lastRect, preview);
                        EditorGUI.DropShadowLabel(lastRect, preview.width + "x" + preview.height, labelStyle);
                    }

                    foreach (ImageCollectionItem img in images)
                    {
                        EditorGUI.BeginDisabledGroup(true);
                        EditorGUILayout.ObjectField(img.holder, typeof(Component), true);
                        EditorGUILayout.ObjectField(img.obj, typeof(Object), true);
                        Separator();
                        EditorGUI.EndDisabledGroup();
                    }
                });
            }
        }

        public static void DrawFieldsBeneath(PropertyTree propertyTree, Type baseType)
        {
            Type baseClassType = null;

            foreach (InspectorProperty property in propertyTree.RootProperty.Children)
            {
                Type objType = propertyTree.UnitySerializedObject.targetObject.GetType();
                string unityPropertyPath = property.UnityPropertyPath;

                if (EditorHelpers.IsFieldOfTypeBeneath(objType, baseType, unityPropertyPath))
                {
                    Type newBaseClassType = EditorHelpers
                        .GetFieldViaPath(objType, unityPropertyPath).DeclaringType;

                    if (newBaseClassType != null && newBaseClassType != baseClassType)
                    {
                        DrawSplitter(newBaseClassType.Name);
                        baseClassType = newBaseClassType;
                    }

                    property.Draw();
                }
            }
        }

        public static void DrawFieldsBeneath(SerializedObject serializedObject, Type baseType)
        {
            SerializedProperty iter = serializedObject.GetIterator();
            iter.NextVisible(true);
            Type baseClassType = null;
            do
            {
                if (EditorHelpers.IsFieldOfTypeBeneath(serializedObject.targetObject.GetType(), baseType,
                        iter.propertyPath))
                {
                    Type newBaseClassType = EditorHelpers
                        .GetFieldViaPath(serializedObject.targetObject.GetType(), iter.propertyPath).DeclaringType;
                    if (newBaseClassType != null && newBaseClassType != baseClassType)
                    {
                        DrawSplitter(newBaseClassType.Name);
                        baseClassType = newBaseClassType;
                    }

                    EditorGUILayout.PropertyField(iter);
                }
            } while (iter.NextVisible(false));
        }

        public static void DrawSplitter(string label)
        {
            string[] splitted = label.Split('`');
            if (splitted.Length > 1)
            {
                label = splitted[0];
            }

            GUIStyle labelStyle = EditorStyles.centeredGreyMiniLabel;
            Vector2 size = labelStyle.CalcSize(new GUIContent(label.ToSentenceCase()));
            GUILayout.Label(label.ToSentenceCase().UppercaseWords(), labelStyle);
            Rect lastRect = GUILayoutUtility.GetLastRect();
            SplitterGUI.Splitter(new Rect(lastRect.x, lastRect.y + lastRect.height * 0.5f,
                lastRect.width * 0.5f - size.x * 0.5f, 1f));
            SplitterGUI.Splitter(new Rect(lastRect.x + lastRect.width * 0.5f + size.x * 0.5f,
                lastRect.y + lastRect.height * 0.5f, lastRect.width * 0.5f - size.x * 0.5f, 1f));
        }

        public static void DrawSafeAreaFields(Object target, SerializedProperty useSafeZone,
            SerializedProperty safeZone)
        {
            EditorGUILayout.PropertyField(useSafeZone);
            if (useSafeZone.boolValue)
            {
                Box(6f, 2f, () =>
                {
                    EditorGUILayout.PropertyField(safeZone);
                    if (safeZone.objectReferenceValue == null)
                    {
                        Box(2f, 2f, () =>
                        {
                            GUILayout.BeginHorizontal();
                            GUILayout.FlexibleSpace();

                            if (GUILayout.Button("Generate", GUILayout.Width(80f), GUILayout.Height(30f)))
                            {
                                var obj = (Component) target;
                                if (PrefabUtility.IsPartOfAnyPrefab(obj))
                                {
                                    string path = AssetDatabase.GetAssetPath(obj.gameObject);
                                    using (var edit = new EditPrefabAssetScope(path))
                                    {
                                        EditorHelpers.AddSafeZone(edit.prefabRoot.transform);
                                    }
                                }
                                else
                                {
                                    GameObject root = obj.gameObject;
                                    EditorHelpers.AddSafeZone(root.transform);
                                }
                            }

                            GUILayout.FlexibleSpace();
                            GUILayout.EndHorizontal();
                        }, GUIStyle.none);
                    }
                });
            }
        }

        public static void DrawButtonGenerateSafeArea(Object target)
        {
            if (GUILayout.Button("Generate Safe Area"))
            {
                var obj = (Component) target;
                if (PrefabUtility.IsPartOfAnyPrefab(obj))
                {
                    string path = AssetDatabase.GetAssetPath(obj.gameObject);
                    using (var edit = new EditPrefabAssetScope(path))
                    {
                        EditorHelpers.AddSafeZone(edit.prefabRoot.transform);
                    }
                }
                else
                {
                    GameObject root = obj.gameObject;
                    EditorHelpers.AddSafeZone(root.transform);
                }
            }
        }

        public static string GetPropertyToString(SerializedProperty property)
        {
            if (property.hasMultipleDifferentValues)
            {
                return "?";
            }

            var str = string.Empty;
            switch (property.propertyType)
            {
                case SerializedPropertyType.Enum:
                    str = property.enumDisplayNames[property.enumValueIndex];
                    break;

                case SerializedPropertyType.Boolean:
                    str = property.boolValue ? "True" : "False";
                    break;

                case SerializedPropertyType.Integer:
                    str = property.intValue.ToString();
                    break;

                case SerializedPropertyType.String:
                    str = property.stringValue;
                    break;
            }

            return str;
        }

        public static void DrawProperty(SerializedProperty property)
        {
            SerializedProperty prop = property.serializedObject.FindProperty(property.propertyPath);
            prop.NextVisible(true);
            do
            {
                if (prop.propertyPath.StartsWith(property.propertyPath + ".") == false)
                {
                    break;
                }

                EditorGUILayout.PropertyField(prop, true);
            } while (prop.NextVisible(false));
        }

        public static void DrawProperty(Rect rect, SerializedProperty property, float elementHeight)
        {
            SerializedProperty prop = property.serializedObject.FindProperty(property.propertyPath);
            prop.NextVisible(true);
            do
            {
                if (prop.propertyPath.StartsWith(property.propertyPath + ".") == false)
                {
                    break;
                }

                EditorGUI.PropertyField(rect, prop, true);

                rect.y += elementHeight;
            } while (prop.NextVisible(false));
        }

        public static void DrawStateButtons(Object[] targets)
        {
            Padding(10f, 10f, () =>
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Show"))
                {
                    for (var i = 0; i < targets.Length; ++i)
                    {
                        var wo = targets[i] as WindowObject;
                        if (wo != null)
                        {
                            wo.Show();
                        }
                    }
                }

                if (GUILayout.Button("Hide"))
                {
                    for (var i = 0; i < targets.Length; ++i)
                    {
                        var wo = targets[i] as WindowObject;
                        if (wo != null)
                        {
                            wo.Hide();
                        }
                    }
                }

                GUILayout.EndHorizontal();
            });
        }

        public static void DrawComponentHeaderItem(string caption, string value)
        {
            Padding(16f, 4f, () =>
            {
                using (new GUIColorUsing(new Color(1f, 1f, 1f, 0.5f)))
                {
                    GUILayout.Label(caption, EditorStyles.miniBoldLabel, GUILayout.Height(16f));
                }

                GUILayout.Space(-6f);
                using (new GUIColorUsing(new Color(1f, 1f, 1f, 1f)))
                {
                    Padding(5f, 0f,
                        () => { GUILayout.Label(EditorHelpers.StringToCaption(value), EditorStyles.label); },
                        GUIStyle.none);
                }
            }, GUIStyle.none);
        }

        public static void DrawComponentHeader(SerializedObject serializedObject, string caption, Action onDraw)
        {
            DrawComponentHeader(serializedObject, caption, onDraw, new Color(0f, 0.6f, 1f, 0.4f));
        }

        public static void DrawComponentHeader(SerializedObject serializedObject, string caption, Action onDraw,
            Color color)
        {
            var colorCaption = new Color(0f, 0f, 0f, 0.1f);

            var width = 40f;

            GUILayout.BeginVertical();
            {
                Separator(color);
                GUILayout.BeginHorizontal();
                {
                    Rect rect = EditorGUILayout.GetControlRect(GUILayout.Width(width), GUILayout.ExpandHeight(true));
                    rect.y -= 2f;
                    rect.x -= 3f;
                    rect.height += 4f;
                    EditorGUI.DrawRect(rect, color);
                    {
                        var style = new GUIStyle(EditorStyles.whiteLargeLabel);
                        style.alignment = TextAnchor.MiddleCenter;
                        style.normal.textColor = colorCaption;
                        style.fontStyle = FontStyle.Bold;
                        style.fontSize = 30;
                        rect.height = 40f;
                        GUI.Label(rect, caption, style);
                    }

                    GUILayout.BeginHorizontal();
                    {
                        onDraw.Invoke();

                        if (EditorApplication.isPlaying)
                        {
                            var isValid = true;
                            for (var i = 0; i < serializedObject.targetObjects.Length; ++i)
                            {
                                if (serializedObject.targetObjects[i] is WindowObject == false ||
                                    PrefabUtility.IsPartOfPrefabAsset(serializedObject.targetObjects[i]))
                                {
                                    isValid = false;
                                    break;
                                }
                            }

                            if (isValid)
                            {
                                DrawStateButtons(serializedObject.targetObjects);
                            }
                        }
                    }
                    GUILayout.EndHorizontal();
                }
                GUILayout.EndHorizontal();
                Separator(color);
            }
            GUILayout.EndVertical();
        }

        public static int DrawTabs(int selectedIndex, ref Vector2 scrollPosition, params GUITab[] tabs)
        {
            var color = new Color(0f, 0.6f, 1f, 0.4f);
            var selectedColor = new Color(0f, 0f, 0f, 0.2f);

            var hasFlex = false;
            scrollPosition = GUILayout.BeginScrollView(scrollPosition);
            GUILayout.BeginHorizontal(GUILayout.ExpandWidth(true));
            {
                var normalStyle = new GUIStyle(EditorStyles.toolbarButton);
                var selectedStyle = new GUIStyle(EditorStyles.toolbarButton);
                selectedStyle.normal = normalStyle.active;
                selectedStyle.onNormal = normalStyle.onActive;
                selectedStyle.normal.background = Texture2D.whiteTexture;
                selectedStyle.normal.textColor = Color.white;

                var attrs = new List<GUILayoutOption>();
                GUILayoutOption[] attrsArr = null;
                for (var i = 0; i < tabs.Length; ++i)
                {
                    GUITab tab = tabs[i];
                    if (string.IsNullOrEmpty(tab.caption))
                    {
                        if (selectedIndex == i)
                        {
                            --selectedIndex;
                        }

                        continue;
                    }

                    attrs.Clear();
                    if (tab.customWidth > 0f)
                    {
                        GUILayout.FlexibleSpace();
                        hasFlex = true;

                        attrs.Add(GUILayout.Width(tab.customWidth));
                        attrs.Add(GUILayout.ExpandWidth(false));
                    }
                    else
                    {
                        attrs.Add(GUILayout.ExpandWidth(false));
                    }

                    attrsArr = attrs.ToArray();

                    if (selectedIndex == i)
                    {
                        GUILayout.BeginVertical();
                        {
                            Separator(color, 2f);
                            Color bc = GUI.backgroundColor;
                            GUI.backgroundColor = selectedColor;
                            GUILayout.Label(" " + tab.caption + " ", selectedStyle, attrsArr);
                            GUI.backgroundColor = bc;
                        }
                        GUILayout.EndVertical();
                    }
                    else
                    {
                        GUILayout.BeginVertical();
                        {
                            Separator();
                            if (GUILayout.Button(" " + tab.caption + " ", normalStyle, attrsArr))
                            {
                                selectedIndex = i;
                            }
                        }
                        GUILayout.EndVertical();
                    }
                }
            }

            if (hasFlex == false)
            {
                GUILayout.FlexibleSpace();
            }

            GUILayout.EndHorizontal();
            GUILayout.EndScrollView();

            if (tabs[selectedIndex].onDraw != null)
            {
                GUILayout.BeginVertical();
                {
                    Separator(selectedColor);
                    //++EditorGUI.indentLevel;
                    if (selectedIndex >= 0 && selectedIndex < tabs.Length)
                    {
                        Box(8f, 0f, tabs[selectedIndex].onDraw);
                    }

                    //--EditorGUI.indentLevel;
                }
                GUILayout.EndVertical();
            }

            return selectedIndex;
        }

        public static void DrawGradient(float height, Color from, Color to, string labelFrom, string labelTo)
        {
            var tex = new Texture2D(2, 1, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.SetPixel(0, 0, from);
            tex.SetPixel(1, 0, to);
            tex.Apply();

            Rect rect = EditorGUILayout.GetControlRect(false, height);
            rect.height = height;
            EditorGUI.DrawTextureTransparent(rect, tex, ScaleMode.StretchToFill);

            GUILayout.BeginHorizontal();
            {
                GUILayout.Label(labelFrom);
                GUILayout.FlexibleSpace();
                GUILayout.Label(labelTo);
            }
            GUILayout.EndHorizontal();
        }

        public static Rect ProgressBar(float value, float max, bool drawLabel = false, float height = 4f)
        {
            return ProgressBar(value, max, new Color(0f, 0f, 0f, 0.3f), new Color32(104, 148, 192, 255), drawLabel,
                height);
        }

        public static Rect ProgressBar(float value, float max, Color back, Color fill, bool drawLabel = false,
            float height = 4f)
        {
            float progress = value / max;
            float lineHeight = drawLabel ? height * 2f : height;
            Rect rect = EditorGUILayout.GetControlRect(false, lineHeight);
            rect.height = lineHeight;
            Rect fillRect = rect;
            fillRect.width = progress * rect.width;
            EditorGUI.DrawRect(rect, back);
            EditorGUI.DrawRect(fillRect, fill);

            if (drawLabel)
            {
                EditorGUI.LabelField(rect, string.Format("{0}/{1}", value, max), EditorStyles.centeredGreyMiniLabel);
            }

            return rect;
        }

        public static bool ToggleLeft(ref bool state, ref bool isDirty, string caption, string text)
        {
            var labelRich = new GUIStyle(EditorStyles.label);
            labelRich.richText = true;

            var isLocalDirty = false;
            bool flag = EditorGUILayout.ToggleLeft(caption, state, labelRich);
            if (flag != state)
            {
                isLocalDirty = true;
                isDirty = true;
                state = flag;
            }

            if (string.IsNullOrEmpty(text) == false)
            {
                SmallLabel(text);
            }

            EditorGUILayout.Space();

            return isLocalDirty;
        }

        public static LayerMask DrawLayerMaskField(string label, LayerMask layerMask)
        {
            var layers = new List<string>();
            var layerNumbers = new List<int>();

            for (var i = 0; i < 32; i++)
            {
                string layerName = LayerMask.LayerToName(i);
                if (layerName != "")
                {
                    layers.Add(layerName);
                    layerNumbers.Add(i);
                }
            }

            var maskWithoutEmpty = 0;
            for (var i = 0; i < layerNumbers.Count; i++)
            {
                if (((1 << layerNumbers[i]) & layerMask.value) > 0)
                {
                    maskWithoutEmpty |= 1 << i;
                }
            }

            maskWithoutEmpty = EditorGUILayout.MaskField(label, maskWithoutEmpty, layers.ToArray());
            var mask = 0;
            for (var i = 0; i < layerNumbers.Count; i++)
            {
                if ((maskWithoutEmpty & (1 << i)) > 0)
                {
                    mask |= 1 << layerNumbers[i];
                }
            }

            layerMask.value = mask;
            return layerMask;
        }

        public static void DrawHeader(string caption)
        {
            GUIStyle style = GUIStyle.none; //new GUIStyle("In BigTitle");
            //new Editor().DrawHeader();

            GUILayout.Space(4f);
            Separator();
            Padding(
                16f, 4f,
                () => { GUILayout.Label(caption, EditorStyles.boldLabel); }, style);
            Separator(new Color(0.2f, 0.2f, 0.2f, 1f));
        }

        public static void SmallLabel(string text)
        {
            var labelRich = new GUIStyle(EditorStyles.miniLabel);
            labelRich.richText = true;
            labelRich.wordWrap = true;

            Color oldColor = GUI.color;
            Color c = oldColor;
            c.a = 0.5f;
            GUI.color = c;

            EditorGUILayout.LabelField(text, labelRich);

            GUI.color = oldColor;
        }

        public static int GetFieldsCount(object instance)
        {
            FieldInfo[] fields = instance.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public);
            return fields.Length;
        }

        public static void Icon(string path, float width = 32f, float height = 32f)
        {
            var icon = new GUIStyle();
            icon.normal.background = EditorResources.Load<Texture2D>(path);
            EditorGUILayout.LabelField(string.Empty, icon, GUILayout.Width(width), GUILayout.Height(height));
        }

        private static bool HasBaseType(this Type type, Type baseType)
        {
            return baseType.IsAssignableFrom(type);
        }

        private static bool HasInterface(this Type type, Type interfaceType)
        {
            return interfaceType.IsAssignableFrom(type);
        }

        public static void DataLabel(string content, params GUILayoutOption[] options)
        {
            var style = new GUIStyle(EditorStyles.label);
            Rect rect = GUILayoutUtility.GetRect(new GUIContent(content), style, options);
            style.richText = true;
            style.stretchHeight = false;
            style.fixedHeight = 0f;
            EditorGUI.SelectableLabel(rect, content, style);
        }

        public static string GetTypeLabel(Type type)
        {
            string output = type.Name;
            string[] sOutput = output.Split('`');
            if (sOutput.Length > 0)
            {
                output = sOutput[0];
            }

            Type[] genericTypes = type.GenericTypeArguments;
            if (genericTypes != null && genericTypes.Length > 0)
            {
                var sTypes = string.Empty;
                for (var i = 0; i < genericTypes.Length; ++i)
                {
                    sTypes += (i > 0 ? ", " : string.Empty) + genericTypes[i].Name;
                }

                output += "<" + sTypes + ">";
            }

            return output;
        }

        public static void TypeLabel(Type type, params GUILayoutOption[] options)
        {
            DataLabel(GetTypeLabel(type), options);
        }

        public static void Separator()
        {
            Separator(new Color(0.1f, 0.1f, 0.1f, 0.2f));
        }

        public static void Separator(Color color, params GUILayoutOption[] options)
        {
            Separator(color, 1f, options);
        }

        public static void Separator(Color color, float height, params GUILayoutOption[] options)
        {
            GUIStyle horizontalLine;
            horizontalLine = new GUIStyle();
            horizontalLine.normal.background = EditorGUIUtility.whiteTexture;
            horizontalLine.margin = new RectOffset(0, 0, 0, 0);
            horizontalLine.fixedHeight = height;

            Color c = GUI.color;
            GUI.color = color;
            GUILayout.Box(GUIContent.none, horizontalLine);
            GUI.color = c;
        }

        public static void DrawBoxNotFilled(Rect rect, float size, Color color, float padding = 0f)
        {
            var s1 = new Rect(rect);
            s1.height = size;
            s1.y += padding;
            s1.x += padding + 1f;
            s1.width -= padding * 2f + 2f;

            var s2 = new Rect(rect);
            s2.y += rect.height - size - padding;
            s2.height = size;
            s2.x += padding + 1f;
            s2.width -= padding * 2f + 2f;

            var s3 = new Rect(rect);
            s3.width = size;
            s3.x += padding;
            s3.y += padding;
            s3.height -= padding * 2f;

            var s4 = new Rect(rect);
            s4.width = size;
            s4.x += rect.width - size - padding;
            s4.y += padding;
            s4.height -= padding * 2f;

            DrawRect(s1, color);
            DrawRect(s2, color);
            DrawRect(s3, color);
            DrawRect(s4, color);
        }

        public static void DrawRect(Rect rect, Color color)
        {
            GUIStyle horizontalLine;
            horizontalLine = new GUIStyle();
            horizontalLine.normal.background = EditorGUIUtility.whiteTexture;
            horizontalLine.margin = new RectOffset(0, 0, 0, 0);

            Color c = GUI.color;
            GUI.color = color;
            GUI.Box(rect, GUIContent.none, horizontalLine);
            GUI.color = c;
        }

        public static void TableCaption(string content, GUIStyle style)
        {
            style = new GUIStyle(style);
            style.alignment = TextAnchor.MiddleCenter;
            style.stretchWidth = true;
            style.stretchHeight = true;

            GUILayout.Label(content, style);
        }

        public static void FoldOut(ref bool state, string content, Action onContent, GUIStyle style = null,
            Action<Rect> onHeader = null)
        {
            if (style == null)
            {
                style = new GUIStyle(EditorStyles.foldoutHeader);
                style.fixedWidth = 0f;
                style.stretchWidth = true;

                if (foldOutLevel == 0)
                {
                    style.fixedHeight = 24f;
                    style.richText = true;
                    content = "<b>" + content + "</b>";
                }
                else
                {
                    style.fixedHeight = 16f;
                    style.richText = true;
                }
            }

            ++foldOutLevel;
            state = BeginFoldoutHeaderGroup(state, new GUIContent(content), style, onHeader);
            if (state)
            {
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Space(10f);
                    GUILayout.BeginVertical();
                    onContent.Invoke();
                    GUILayout.EndVertical();
                }
                GUILayout.EndHorizontal();
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
            --foldOutLevel;
        }

        public static bool BeginFoldoutHeaderGroup(
            bool foldout,
            GUIContent content,
            GUIStyle style = null,
            Action<Rect> menuAction = null,
            GUIStyle menuIcon = null)
        {
            return BeginFoldoutHeaderGroup(GUILayoutUtility.GetRect(content, style), foldout, content, style,
                menuAction, menuIcon);
        }

        public static bool BeginFoldoutHeaderGroup(
            Rect position,
            bool foldout,
            GUIContent content,
            GUIStyle style = null,
            Action<Rect> menuAction = null,
            GUIStyle menuIcon = null)
        {
            //if (EditorGUIUtility.hierarchyMode) position.xMin -= (float)(EditorStyles.inspectorDefaultMargins.padding.left - EditorStyles.inspectorDefaultMargins.padding.right);
            if (style == null)
            {
                style = EditorStyles.foldoutHeader;
            }

            var position1 = new Rect
            {
                x = (float) (position.xMax - (double) style.padding.right - 16.0),
                y = position.y + style.padding.top,
                size = Vector2.one * 16f
            };
            bool isHover = position1.Contains(Event.current.mousePosition);
            bool isActive = isHover && Event.current.type == EventType.MouseDown && Event.current.button == 0;
            if (menuAction != null && isActive)
            {
                menuAction(position1);
                Event.current.Use();
            }

            foldout = GUI.Toggle(position, foldout, content, style);
            if (menuAction != null && Event.current.type == EventType.Repaint)
            {
                if (menuIcon == null)
                {
                    menuIcon = EditorStyles.foldoutHeaderIcon;
                }

                menuIcon.Draw(position1, isHover, isActive, false, false);
            }

            return foldout;
        }

        public static void Box(float padding, float margin, Action onContent, GUIStyle style = null,
            params GUILayoutOption[] options)
        {
            Padding(margin, () =>
            {
                if (style == null)
                {
                    style = "GroupBox";
                }
                else
                {
                    style = new GUIStyle(style);
                }

                style.padding = new RectOffset();
                style.margin = new RectOffset();

                GUILayout.BeginVertical(style, options);
                {
                    Padding(padding, onContent);
                }
                GUILayout.EndVertical();
            }, options);
        }

        public static void Padding(float padding, Action onContent, params GUILayoutOption[] options)
        {
            Padding(padding, padding, onContent, options);
        }

        public static void Padding(float paddingX, float paddingY, Action onContent, params GUILayoutOption[] options)
        {
            Padding(paddingX, paddingY, onContent, GUIStyle.none, options);
        }

        public static void Padding(float paddingX, float paddingY, Action onContent, GUIStyle style,
            params GUILayoutOption[] options)
        {
            GUILayout.BeginVertical(style, options);
            {
                GUILayout.Space(paddingY);
                GUILayout.BeginHorizontal(options);
                {
                    GUILayout.Space(paddingX);
                    {
                        GUILayout.BeginVertical(options);
                        onContent.Invoke();
                        GUILayout.EndVertical();
                    }
                    GUILayout.Space(paddingX);
                }
                GUILayout.EndHorizontal();
                GUILayout.Space(paddingY);
            }
            GUILayout.EndVertical();
        }

        public static bool DrawDropdown(Rect position, GUIContent content, FocusType focusType,
            Object selectButton = null)
        {
            if (selectButton != null)
            {
                var selectButtonWidth = 80f;
                var space = 4f;
                var rect = new Rect(position.x, position.y, position.width - selectButtonWidth - space,
                    position.height);
                bool result = EditorGUI.DropdownButton(rect, content, focusType);
                if (GUI.Button(
                        new Rect(position.x + rect.width + space, position.y, selectButtonWidth, position.height),
                        "Select"))
                {
                    Selection.activeObject = selectButton;
                }

                return result;
            }

            return EditorGUI.DropdownButton(position, content, focusType);
        }

        public struct GUIBackgroundColorUsing : IDisposable
        {
            private readonly Color oldColor;

            public GUIBackgroundColorUsing(Color color)
            {
                oldColor = GUI.backgroundColor;
                GUI.backgroundColor = color;
            }

            public void Dispose()
            {
                GUI.backgroundColor = oldColor;
            }
        }

        public struct GUIAlphaUsing : IDisposable
        {
            private readonly Color oldColor;

            public GUIAlphaUsing(float alpha)
            {
                oldColor = GUI.color;
                GUI.color = new Color(oldColor.r, oldColor.g, oldColor.b, alpha);
            }

            public void Dispose()
            {
                GUI.color = oldColor;
            }
        }

        public struct GUIColorUsing : IDisposable
        {
            private readonly Color oldColor;

            public GUIColorUsing(Color color)
            {
                oldColor = GUI.color;
                GUI.color = color;
            }

            public void Dispose()
            {
                GUI.color = oldColor;
            }
        }
    }

    public class PopupWindowAnim : EditorWindow
    {
        private const float defaultWidth = 150;
        private const float defaultHeight = 250;
        private const float elementHeight = 20;

        /// <summary> Стили, используемые для визуализации попапа </summary>
        private static Styles styles;

        private static bool s_DirtyList = true;

        /// <summary> Прямоугольник, в котором будет отображен попап </summary>
        public Rect screenRect;

        /// <summary> Указывает, что является разделителем в пути </summary>
        public char separator = '/';

        /// <summary> Позволяет использовать/убирать поиск </summary>
        public bool useSearch = true;

        /// <summary> Название рута </summary>
        public new string title = "Menu";

        //Поиск
        /// <summary> Строка поиска </summary>
        public string searchText = "";

        public bool autoHeight;
        public bool autoClose;

        /// <summary> Хранит контекст элементов (нужно при заполнении попапа) </summary>
        private readonly List<string> folderStack = new();

        /// <summary> Хранит контексты элементов (после вызова Show) </summary>
        private readonly List<GroupElement> _stack = new();

        //Анимация
        private float _anim;
        private int _animTarget = 1;
        private long _lastTime;

        //Элементы
        /// <summary> Список конечных элементов (до вызова Show) </summary>
        private List<PopupItem> submenu = new();

        /// <summary> Список элементов (после вызова Show) </summary>
        private Element[] _tree;

        /// <summary> Список элементов, подходящих под условия поиска </summary>
        private Element[] _treeSearch;

        /// <summary> Указывает, нуждается ли выбор нового элемента в прокрутке </summary>
        private bool scrollToSelected;

        private int maxElementCount = 1;

        public new string name
        {
            get => title;
            set => title = value;
        }

        /// <summary> Активен ли поиск? </summary>
        private bool hasSearch => useSearch && !string.IsNullOrEmpty(searchText);

        private Element[] activeTree => !hasSearch ? _tree : _treeSearch;

        private GroupElement activeParent => _stack[_stack.Count - 2 + _animTarget];

        private Element activeElement
        {
            get
            {
                if (activeTree == null)
                {
                    return null;
                }

                List<Element> childs = GetChildren(activeTree, activeParent);
                if (childs.Count == 0)
                {
                    return null;
                }

                return childs[activeParent.selectedIndex];
            }
        }

        public void OnGUI()
        {
            if (_tree == null)
            {
                Close();
                return;
            }

            //Создание стиля
            if (styles == null)
            {
                styles = new Styles();
            }

            //Фон
            if (s_DirtyList)
            {
                CreateComponentTree();
            }

            HandleKeyboard();
            GUI.Label(new Rect(0, 0, position.width, position.height), GUIContent.none, styles.background);

            //Поиск
            if (useSearch)
            {
                GUILayout.Space(7f);
                Rect rectSearch = GUILayoutUtility.GetRect(10f, 20f);
                rectSearch.x += 8f;
                rectSearch.width -= 16f;
                EditorGUI.FocusTextInControl("ComponentSearch");
                GUI.SetNextControlName("ComponentSearch");
                if (SearchField(rectSearch, ref searchText))
                {
                    RebuildSearch();
                }
            }

            //Элементы
            ListGUI(activeTree, _anim, GetElementRelative(0), GetElementRelative(-1));
            if (_anim < 1f && _stack.Count > 1)
            {
                ListGUI(activeTree, _anim + 1f, GetElementRelative(-1), GetElementRelative(-2));
            }

            if (_anim != _animTarget && Event.current.type == EventType.Repaint)
            {
                long ticks = DateTime.Now.Ticks;
                float coef = (ticks - _lastTime) / 1E+07f;
                _lastTime = ticks;
                _anim = Mathf.MoveTowards(_anim, _animTarget, coef * 4f);
                if (_animTarget == 0 && _anim == 0f)
                {
                    _anim = 1f;
                    _animTarget = 1;
                    _stack.RemoveAt(_stack.Count - 1);
                }

                Repaint();
            }
        }

        /// <summary> Создание окна </summary>
        public static PopupWindowAnim Create(Rect screenRect, bool useSearch = true)
        {
            var popup = CreateInstance<PopupWindowAnim>();
            popup.screenRect = screenRect;
            popup.useSearch = useSearch;
            return popup;
        }

        /// <summary> Создание окна </summary>
        public static PopupWindowAnim CreateByPos(Vector2 pos, bool useSearch = true)
        {
            return Create(new Rect(pos.x, pos.y, defaultWidth, defaultHeight), useSearch);
        }

        /// <summary> Создание окна </summary>
        public static PopupWindowAnim CreateByPos(Vector2 pos, float width, bool useSearch = true)
        {
            return Create(new Rect(pos.x, pos.y, width, defaultHeight), useSearch);
        }

        /// <summary> Создание окна. Вызывается из OnGUI()! </summary>
        public static PopupWindowAnim CreateBySize(Vector2 size, bool useSearch = true)
        {
            Vector2 screenPos = GUIUtility.GUIToScreenPoint(Event.current.mousePosition);
            return Create(new Rect(screenPos.x, screenPos.y, size.x, size.y), useSearch);
        }

        /// <summary> Создание окна. Вызывается из OnGUI()! </summary>
        public static PopupWindowAnim Create(float width, bool useSearch = true)
        {
            return CreateBySize(new Vector2(width, defaultHeight), useSearch);
        }

        /// <summary> Создание окна. Вызывается из OnGUI()! </summary>
        public static PopupWindowAnim Create(bool useSearch = true)
        {
            return CreateBySize(new Vector2(defaultWidth, defaultHeight), useSearch);
        }

        /// <summary> Отображает попап </summary>
        public new void Show()
        {
            if (submenu.Count == 0)
            {
                DestroyImmediate(this);
            }
            else
            {
                Init();
            }
        }

        /// <summary> Отображает попап </summary>
        public void ShowAsDropDown()
        {
            Show();
        }

        public void SetHeightByElementCount(int elementCount)
        {
            screenRect.height = elementCount * elementHeight + (useSearch ? 30f : 0f) + 26f;
        }

        public void SetHeightByElementCount()
        {
            SetHeightByElementCount(maxElementCount);
        }

        public void BeginRoot(string folderName)
        {
            string previous = folderStack.Count != 0 ? folderStack[folderStack.Count - 1] : "";
            if (string.IsNullOrEmpty(folderName))
            {
                folderName = "<Noname>";
            }

            if (!string.IsNullOrEmpty(previous))
            {
                folderStack.Add(previous + separator + folderName);
            }
            else
            {
                folderStack.Add(folderName);
            }
        }

        public void EndRoot()
        {
            if (folderStack.Count > 0)
            {
                folderStack.RemoveAt(folderStack.Count - 1);
            }
            else
            {
                throw new Exception("Excess call EndFolder()");
            }
        }

        public void EndRootAll()
        {
            while (folderStack.Count > 0)
            {
                folderStack.RemoveAt(folderStack.Count - 1);
            }
        }

        public void Item(string title, Texture2D image, Action action, int order)
        {
            var folder = "";
            if (folderStack.Count > 0)
            {
                folder = folderStack[folderStack.Count - 1] ?? "";
            }

            submenu.Add(string.IsNullOrEmpty(folder)
                ? new PopupItem(this.title + separator + title, action) {image = image, order = order}
                : new PopupItem(this.title + separator + folder + separator + title, action)
                    {image = image, order = order});
        }

        public void Item(string title, Texture2D image, Action<PopupItem> action, bool searchable)
        {
            var folder = "";
            if (folderStack.Count > 0)
            {
                folder = folderStack[folderStack.Count - 1] ?? "";
            }

            submenu.Add(string.IsNullOrEmpty(folder)
                ? new PopupItem(this.title + separator + title, action) {image = image, searchable = searchable}
                : new PopupItem(this.title + separator + folder + separator + title, action)
                    {image = image, searchable = searchable});
        }

        public void Item(string title, Texture2D image, Action<PopupItem> action, bool searchable, int order)
        {
            var folder = "";
            if (folderStack.Count > 0)
            {
                folder = folderStack[folderStack.Count - 1] ?? "";
            }

            submenu.Add(string.IsNullOrEmpty(folder)
                ? new PopupItem(this.title + separator + title, action)
                    {image = image, searchable = searchable, order = order}
                : new PopupItem(this.title + separator + folder + separator + title, action)
                    {image = image, searchable = searchable, order = order});
        }

        public void Item(string title, Action action)
        {
            var folder = "";
            if (folderStack.Count > 0)
            {
                folder = folderStack[folderStack.Count - 1] ?? "";
            }

            submenu.Add(string.IsNullOrEmpty(folder)
                ? new PopupItem(this.title + separator + title, action)
                : new PopupItem(this.title + separator + folder + separator + title, action));
        }

        public void Item(string title)
        {
            var folder = "";
            if (folderStack.Count > 0)
            {
                folder = folderStack[folderStack.Count - 1] ?? "";
            }

            submenu.Add(string.IsNullOrEmpty(folder)
                ? new PopupItem(this.title + separator + title, () => { })
                : new PopupItem(this.title + separator + folder + separator + title, () => { }));
        }

        public void ItemByPath(string path, Texture2D image, Action action)
        {
            if (string.IsNullOrEmpty(path))
            {
                path = "<Noname>";
            }

            submenu.Add(new PopupItem(title + separator + path, action) {image = image});
        }

        public void ItemByPath(string path, Action action)
        {
            if (string.IsNullOrEmpty(path))
            {
                path = "<Noname>";
            }

            submenu.Add(new PopupItem(title + separator + path, action));
        }

        public void ItemByPath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                path = "<Noname>";
            }

            submenu.Add(new PopupItem(title + separator + path, () => { }));
        }

        private void Init()
        {
            CreateComponentTree();
            if (autoHeight)
            {
                SetHeightByElementCount();
            }

            ShowAsDropDown(new Rect(screenRect.x, screenRect.y, 1, 1),
                new Vector2(screenRect.width, screenRect.height));
            Focus();
            wantsMouseMove = true;
        }

        private void CreateComponentTree()
        {
            var list = new List<string>();
            var elements = new List<Element>();

            submenu = submenu.OrderBy(x => x.order).ThenBy(x => x.path).ToList();

            for (var i = 0; i < submenu.Count; i++)
            {
                PopupItem submenuItem = submenu[i];
                string menuPath = submenuItem.path;
                char[] separators = {separator};
                string[] pathParts = menuPath.Split(separators);

                while (pathParts.Length - 1 < list.Count)
                {
                    list.RemoveAt(list.Count - 1);
                }

                while (list.Count > 0 && pathParts[list.Count - 1] != list[list.Count - 1])
                {
                    list.RemoveAt(list.Count - 1);
                }

                while (pathParts.Length - 1 > list.Count)
                {
                    elements.Add(new GroupElement(list.Count, pathParts[list.Count]));
                    list.Add(pathParts[list.Count]);
                }

                elements.Add(new CallElement(list.Count, pathParts[pathParts.Length - 1], submenuItem));
            }

            _tree = elements.ToArray();
            for (var i = 0; i < _tree.Length; i++)
            {
                List<Element> elChilds = GetChildren(_tree, _tree[i]);
                if (elChilds.Count > maxElementCount)
                {
                    maxElementCount = elChilds.Count;
                }
            }

            if (_stack.Count == 0)
            {
                _stack.Add(_tree[0] as GroupElement);
                goto to_research;
            }

            var parent = _tree[0] as GroupElement;
            var level = 0;
            to_startCycle:
            GroupElement stackElement = _stack[level];
            _stack[level] = parent;
            if (_stack[level] != null)
            {
                _stack[level].selectedIndex = stackElement.selectedIndex;
                _stack[level].scroll = stackElement.scroll;
            }

            level++;
            if (level != _stack.Count)
            {
                List<Element> childs = GetChildren(activeTree, parent);
                Element child = childs.FirstOrDefault(x => _stack[level].name == x.name);
                if (child is GroupElement)
                {
                    parent = child as GroupElement;
                }
                else
                {
                    while (_stack.Count > level)
                    {
                        _stack.RemoveAt(level);
                    }
                }

                goto to_startCycle;
            }

            to_research:
            s_DirtyList = false;
            RebuildSearch();
        }

        private void RebuildSearch()
        {
            if (!hasSearch)
            {
                _treeSearch = null;
                if (_stack[_stack.Count - 1].name == "Search")
                {
                    _stack.Clear();
                    _stack.Add(_tree[0] as GroupElement);
                }

                _animTarget = 1;
                _lastTime = DateTime.Now.Ticks;
            }
            else
            {
                char[] separatorSearch = {' ', separator};
                string[] searchLowerWords = searchText.ToLower().Split(separatorSearch);
                var firstElements = new List<Element>();
                var otherElements = new List<Element>();
                foreach (Element element in _tree)
                {
                    if (!(element is CallElement))
                    {
                        continue;
                    }

                    if (element.searchable == false)
                    {
                        continue;
                    }

                    string elementNameShortLower = element.name.ToLower().Replace(" ", string.Empty);
                    var itsSearchableItem = true;
                    var firstContainsFlag = false;
                    for (var i = 0; i < searchLowerWords.Length; i++)
                    {
                        string searchLowerWord = searchLowerWords[i];
                        if (elementNameShortLower.Contains(searchLowerWord))
                        {
                            if (i == 0 && elementNameShortLower.StartsWith(searchLowerWord))
                            {
                                firstContainsFlag = true;
                            }
                        }
                        else
                        {
                            itsSearchableItem = false;
                            break;
                        }
                    }

                    if (itsSearchableItem)
                    {
                        if (firstContainsFlag)
                        {
                            firstElements.Add(element);
                        }
                        else
                        {
                            otherElements.Add(element);
                        }
                    }
                }

                firstElements.Sort();
                otherElements.Sort();

                var searchElements = new List<Element>
                    {new GroupElement(0, "Search")};
                searchElements.AddRange(firstElements);
                searchElements.AddRange(otherElements);
                //            searchElements.Add(_tree[_tree.Length - 1]);
                _treeSearch = searchElements.ToArray();
                _stack.Clear();
                _stack.Add(_treeSearch[0] as GroupElement);
                if (GetChildren(activeTree, activeParent).Count >= 1)
                {
                    activeParent.selectedIndex = 0;
                }
                else
                {
                    activeParent.selectedIndex = -1;
                }
            }
        }

        private void HandleKeyboard()
        {
            Event current = Event.current;
            if (current.type == EventType.KeyDown)
            {
                if (current.keyCode == KeyCode.DownArrow)
                {
                    activeParent.selectedIndex++;
                    activeParent.selectedIndex = Mathf.Min(activeParent.selectedIndex,
                        GetChildren(activeTree, activeParent).Count - 1);
                    scrollToSelected = true;
                    current.Use();
                }

                if (current.keyCode == KeyCode.UpArrow)
                {
                    GroupElement element2 = activeParent;
                    element2.selectedIndex--;
                    activeParent.selectedIndex = Mathf.Max(activeParent.selectedIndex, 0);
                    scrollToSelected = true;
                    current.Use();
                }

                if (current.keyCode == KeyCode.Return || current.keyCode == KeyCode.KeypadEnter)
                {
                    GoToChild(activeElement, true);
                    current.Use();
                }

                if (!hasSearch)
                {
                    if (current.keyCode == KeyCode.LeftArrow || current.keyCode == KeyCode.Backspace)
                    {
                        GoToParent();
                        current.Use();
                    }

                    if (current.keyCode == KeyCode.RightArrow)
                    {
                        GoToChild(activeElement, false);
                        current.Use();
                    }

                    if (current.keyCode == KeyCode.Escape)
                    {
                        Close();
                        current.Use();
                    }
                }
            }
        }

        private static bool SearchField(Rect position, ref string text)
        {
            Rect rectField = position;
            rectField.width -= 15f;
            string startText = text;
            text = GUI.TextField(rectField, startText ?? "", styles.searchTextField);

            Rect rectCancel = position;
            rectCancel.x += position.width - 15f;
            rectCancel.width = 15f;
            GUIStyle styleCancel = text == "" ? styles.searchCancelButtonEmpty : styles.searchCancelButton;
            if (GUI.Button(rectCancel, GUIContent.none, styleCancel) && text != "")
            {
                text = "";
                GUIUtility.keyboardControl = 0;
            }

            return startText != text;
        }

        private void ListGUI(Element[] tree, float anim, GroupElement parent, GroupElement grandParent)
        {
            anim = Mathf.Floor(anim) + Mathf.SmoothStep(0f, 1f, Mathf.Repeat(anim, 1f));
            Rect rectArea = position;
            rectArea.x = position.width * (1f - anim) + 1f;
            rectArea.y = useSearch ? 30f : 0;
            rectArea.height -= useSearch ? 30f : 0;
            rectArea.width -= 2f;
            GUILayout.BeginArea(rectArea);
            {
                Rect rectHeader = GUILayoutUtility.GetRect(10f, 25f);
                string nameHeader = parent.name;
                GUI.Label(rectHeader, nameHeader, styles.header);
                if (grandParent != null)
                {
                    var rectHeaderBackArrow = new Rect(rectHeader.x + 4f, rectHeader.y + 7f, 13f, 13f);
                    if (Event.current.type == EventType.Repaint)
                    {
                        styles.leftArrow.Draw(rectHeaderBackArrow, false, false, false, false);
                    }

                    if (Event.current.type == EventType.MouseDown && rectHeader.Contains(Event.current.mousePosition))
                    {
                        GoToParent();
                        Event.current.Use();
                    }
                }

                ListGUI(tree, parent);
            }
            GUILayout.EndArea();
        }

        private void ListGUI(Element[] tree, GroupElement parent)
        {
            parent.scroll = GUILayout.BeginScrollView(parent.scroll);
            EditorGUIUtility.SetIconSize(new Vector2(16f, 16f));
            List<Element> children = GetChildren(tree, parent);
            var rect = new Rect();
            for (var i = 0; i < children.Count; i++)
            {
                Element e = children[i];
                GUILayoutOption[] options = {GUILayout.ExpandWidth(true)};
                Rect rectElement = GUILayoutUtility.GetRect(16f, elementHeight, options);
                if ((Event.current.type == EventType.MouseMove || Event.current.type == EventType.MouseDown)
                    && parent.selectedIndex != i && rectElement.Contains(Event.current.mousePosition))
                {
                    parent.selectedIndex = i;
                    Repaint();
                }

                var on = false;
                if (i == parent.selectedIndex)
                {
                    on = true;
                    rect = rectElement;
                }

                if (Event.current.type == EventType.Repaint)
                {
                    (e.content.image != null ? styles.componentItem : styles.groupItem).Draw(rectElement, e.content,
                        false, false, on, on);
                    if (!(e is CallElement))
                    {
                        var rectElementForwardArrow = new Rect(rectElement.x + rectElement.width - 13f,
                            rectElement.y + 4f, 13f, 13f);
                        styles.rightArrow.Draw(rectElementForwardArrow, false, false, false, false);
                    }
                }

                if (Event.current.type == EventType.MouseDown && rectElement.Contains(Event.current.mousePosition))
                {
                    Event.current.Use();
                    parent.selectedIndex = i;
                    GoToChild(e, true);
                }
            }

            EditorGUIUtility.SetIconSize(Vector2.zero);
            GUILayout.EndScrollView();
            if (scrollToSelected && Event.current.type == EventType.Repaint)
            {
                scrollToSelected = false;
                Rect lastRect = GUILayoutUtility.GetLastRect();
                if (rect.yMax - lastRect.height > parent.scroll.y)
                {
                    parent.scroll.y = rect.yMax - lastRect.height;
                    Repaint();
                }

                if (rect.y < parent.scroll.y)
                {
                    parent.scroll.y = rect.y;
                    Repaint();
                }
            }
        }

        private void GoToParent()
        {
            if (_stack.Count <= 1)
            {
                return;
            }

            _animTarget = 0;
            _lastTime = DateTime.Now.Ticks;
        }

        private void GoToChild(Element e, bool addIfComponent)
        {
            var element = e as CallElement;
            if (element != null)
            {
                if (!addIfComponent)
                {
                    return;
                }

                element.action();
                if (autoClose)
                {
                    Close();
                }
            }
            else if (!hasSearch)
            {
                _lastTime = DateTime.Now.Ticks;
                if (_animTarget == 0)
                {
                    _animTarget = 1;
                }
                else if (_anim == 1f)
                {
                    _anim = 0f;
                    _stack.Add(e as GroupElement);
                }
            }
        }

        private List<Element> GetChildren(Element[] tree, Element parent)
        {
            var list = new List<Element>();
            int num = -1;
            var index = 0;
            while (index < tree.Length)
            {
                if (tree[index] == parent)
                {
                    num = parent.level + 1;
                    index++;
                    break;
                }

                index++;
            }

            if (num == -1)
            {
                return list;
            }

            while (index < tree.Length)
            {
                Element item = tree[index];
                if (item.level < num)
                {
                    return list;
                }

                if (item.level <= num || hasSearch)
                {
                    list.Add(item);
                }

                index++;
            }

            return list;
        }

        private GroupElement GetElementRelative(int rel)
        {
            int num = _stack.Count + rel - 1;
            return num < 0 ? null : _stack[num];
        }

        private class CallElement : Element
        {
            public readonly Action action;

            public CallElement(int level, string name, PopupItem item)
            {
                this.level = level;
                content = new GUIContent(name, item.image);
                action = () =>
                {
                    item.action();
                    content = new GUIContent(name, item.image);
                };
                searchable = item.searchable;
            }
        }

        [Serializable]
        private class GroupElement : Element
        {
            public Vector2 scroll;
            public int selectedIndex;

            public GroupElement(int level, string name)
            {
                this.level = level;
                content = new GUIContent(name);
                searchable = true;
            }
        }

        private class Element : IComparable
        {
            public GUIContent content;
            public int level;
            public bool searchable;

            public string name => content.text;

            public int CompareTo(object o)
            {
                return string.Compare(name, ((Element) o).name, StringComparison.Ordinal);
            }
        }

        private class Styles
        {
            public readonly GUIStyle searchTextField = "SearchTextField";
            public readonly GUIStyle searchCancelButton = "SearchCancelButton";
            public readonly GUIStyle searchCancelButtonEmpty = "SearchCancelButtonEmpty";
            public readonly GUIStyle background = "grey_border";
            public readonly GUIStyle componentItem = new("PR Label");
            public readonly GUIStyle groupItem;
            public readonly GUIStyle header = new("In BigTitle");
            public readonly GUIStyle leftArrow = "AC LeftArrow";
            public readonly GUIStyle rightArrow = "AC RightArrow";

            public Styles()
            {
                header.font = EditorStyles.boldLabel.font;
                header.richText = true;
                componentItem.alignment = TextAnchor.MiddleLeft;
                componentItem.padding.left -= 15;
                componentItem.fixedHeight = 20f;
                componentItem.richText = true;
                groupItem = new GUIStyle(componentItem);
                groupItem.padding.left += 0x11;
                groupItem.richText = true;
            }
        }

        public class PopupItem
        {
            public int order;
            public string path;
            public Texture2D image;
            public Action action;
            public bool searchable;

            public PopupItem(string path, Action action)
            {
                this.path = path;
                this.action = action;
                searchable = true;
            }

            public PopupItem(string path, Action<PopupItem> action)
            {
                this.path = path;
                this.action = () => { action(this); };
                searchable = true;
            }
        }
    }

    public class Popup
    {
        /// <summary> Окно, которое связано с попапом </summary>
        internal PopupWindowAnim window;

        /// <summary> Прямоугольник, в котором будет отображен попап </summary>
        public Rect screenRect
        {
            get => window.screenRect;
            set => window.screenRect = value;
        }

        /// <summary> Указывает, что является разделителем в пути </summary>
        public char separator
        {
            get => window.separator;
            set => window.separator = value;
        }

        /// <summary> Позволяет использовать/убирать поиск </summary>
        public bool useSearch
        {
            get => window.useSearch;
            set => window.useSearch = value;
        }

        /// <summary> Название рута </summary>
        public string title
        {
            get => window.title;
            set => window.title = value;
        }

        /// <summary> Название рута </summary>
        public string searchText
        {
            get => window.searchText;
            set => window.searchText = value;
        }

        /// <summary> Автоматически установить размер по высоте, узнав максимальное количество видимых элементов </summary>
        public bool autoHeight
        {
            get => window.autoHeight;
            set => window.autoHeight = value;
        }

        public bool autoClose
        {
            get => window.autoClose;
            set => window.autoClose = value;
        }

        /// <summary> Создание окна </summary>
        public Popup(Rect screenRect, bool useSearch = true, string title = "Menu", char separator = '/')
        {
            window = PopupWindowAnim.Create(screenRect, useSearch);
            this.title = title;
            this.separator = separator;
        }

        /// <summary> Создание окна </summary>
        public Popup(Vector2 size, bool useSearch = true, string title = "Menu", char separator = '/')
        {
            window = PopupWindowAnim.CreateBySize(size, useSearch);
            this.title = title;
            this.separator = separator;
        }

        /// <summary> Создание окна </summary>
        public Popup(float width, bool useSearch = true, string title = "Menu", char separator = '/',
            bool autoHeight = true)
        {
            window = PopupWindowAnim.Create(width, useSearch);
            this.title = title;
            this.separator = separator;
            this.autoHeight = autoHeight;
        }

        /// <summary> Создание окна </summary>
        public Popup(bool useSearch = true, string title = "Menu", char separator = '/', bool autoHeight = true)
        {
            window = PopupWindowAnim.Create(useSearch);
            this.title = title;
            this.separator = separator;
            this.autoHeight = autoHeight;
        }

        public void BeginFolder(string folderName)
        {
            window.BeginRoot(folderName);
        }

        public void EndFolder()
        {
            window.EndRoot();
        }

        public void EndFolderAll()
        {
            window.EndRootAll();
        }

        public void Item(string name)
        {
            window.Item(name);
        }

        public void Item(string name, Action action, int order = 0)
        {
            window.Item(name, null, action, order);
        }

        public void Item(string name, Texture2D image, Action action, int order = 0)
        {
            window.Item(name, image, action, order);
        }

        public void Item(string name, Texture2D image, Action<PopupWindowAnim.PopupItem> action, bool searchable = true)
        {
            window.Item(name, image, action, searchable);
        }

        public void Item(string name, Texture2D image, Action<PopupWindowAnim.PopupItem> action, bool searchable,
            int order)
        {
            window.Item(name, image, action, searchable, order);
        }

        public void ItemByPath(string path)
        {
            window.ItemByPath(path);
        }

        public void ItemByPath(string path, Action action)
        {
            window.ItemByPath(path, action);
        }

        public void ItemByPath(string path, Texture2D image, Action action)
        {
            window.ItemByPath(path, image, action);
        }

        public void Show()
        {
            window.Show();
        }

        public static void DrawInt(GUIContent label, string selected, Action<int> onResult, GUIContent[] options,
            int[] keys)
        {
            DrawInt_INTERNAL(new Rect(), selected, label, onResult, options, keys, true);
        }

        public static void DrawInt(Rect rect, string selected, GUIContent label, Action<int> onResult,
            GUIContent[] options, int[] keys)
        {
            DrawInt_INTERNAL(rect, selected, label, onResult, options, keys, false);
        }

        private static void DrawInt_INTERNAL(Rect rect, string selected, GUIContent label, Action<int> onResult,
            GUIContent[] options, int[] keys, bool layout)
        {
            var state = false;
            if (layout)
            {
                GUILayout.BeginHorizontal();
                if (label != null)
                {
                    GUILayout.Label(label);
                }

                if (GUILayout.Button(selected, EditorStyles.popup))
                {
                    state = true;
                }

                GUILayout.EndHorizontal();
            }
            else
            {
                if (label != null)
                {
                    rect = EditorGUI.PrefixLabel(rect, label);
                }

                if (GUI.Button(rect, selected, EditorStyles.popup))
                {
                    state = true;
                }
            }

            if (state)
            {
                Popup popup = null;
                if (layout)
                {
                    popup = new Popup
                    {
                        title = label == null ? string.Empty : label.text,
                        screenRect = new Rect(rect.x, rect.y + rect.height, rect.width, 200f)
                    };
                }
                else
                {
                    Vector2 vector = GUIUtility.GUIToScreenPoint(new Vector2(rect.x, rect.y));
                    rect.x = vector.x;
                    rect.y = vector.y;

                    popup = new Popup
                    {
                        title = label == null ? string.Empty : label.text,
                        screenRect = new Rect(rect.x, rect.y + rect.height, rect.width, 200f)
                    };
                }

                for (var i = 0; i < options.Length; ++i)
                {
                    GUIContent option = options[i];
                    int result = keys[i];
                    popup.ItemByPath(option.text, () => { onResult(result); });
                }

                popup.Show();
            }
        }
    }
}