using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using TMPro;
using UnityEditor.Sprites;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UI.Windows;
using UnityEngine.UI.Windows.Utilities;
using UnityEngine.UI.Windows.WindowTypes;
using Object = UnityEngine.Object;

namespace UnityEditor.UI.Windows
{
    public readonly struct EditPrefabAssetScope : IDisposable
    {
        public readonly string assetPath;
        public readonly GameObject prefabRoot;

        public EditPrefabAssetScope(string assetPath)
        {
            this.assetPath = assetPath;
            prefabRoot = PrefabUtility.LoadPrefabContents(assetPath);
        }

        public void Dispose()
        {
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, assetPath);
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }
    }

    public static class StringExtensions
    {
        public static string ToSentenceCase(this string str)
        {
            if (str == null)
            {
                return string.Empty;
            }

            return Regex.Replace(str, "[a-z][A-Z]", m => $"{m.Value[0]} {char.ToLower(m.Value[1])}");
        }

        public static string UppercaseWords(this string value)
        {
            var chars = new[]
                {' ', '_', '?', '.', ',', '!', '@', '#', '$', '%', '^', '&', '*', '(', '{', '[', '/', '\\'};

            char[] array = value.ToCharArray();
            // Handle the first letter in the string.
            if (array.Length >= 1)
            {
                if (char.IsLower(array[0]))
                {
                    array[0] = char.ToUpper(array[0]);
                }
            }

            // Scan through the letters, checking for spaces.
            // ... Uppercase the lowercase letters following spaces.
            for (var i = 1; i < array.Length; ++i)
            {
                if (Array.IndexOf(chars, array[i - 1]) >= 0)
                {
                    if (char.IsLower(array[i]))
                    {
                        array[i] = char.ToUpper(array[i]);
                    }
                }
            }

            return new string(array);
        }
    }

    public struct ImageCollectionItem
    {
        public Component holder;
        public Object obj;
    }

    public static class EditorHelpers
    {
        public static Texture2D CollectImages(Object target, List<ImageCollectionItem> images)
        {
            return CollectImages(new[] {target}, images);
        }

        public static Texture2D CollectImages(Object[] targets, List<ImageCollectionItem> images)
        {
            var used = new HashSet<Object>();
            var visited = new HashSet<object>();
            var visitedFonts = new HashSet<object>();
            foreach (Object target in targets)
            {
                Component[] components = ((Component) target).gameObject.GetComponentsInChildren<Component>(true);
                foreach (Component component in components)
                {
                    FindType(component, new[] {typeof(Sprite), typeof(Texture), typeof(Texture2D)}, (fieldInfo, obj) =>
                    {
                        if (obj is Object texObj && texObj != null)
                        {
                            images.Add(new ImageCollectionItem
                            {
                                holder = component,
                                obj = texObj
                            });
                        }

                        return obj;
                    }, visited, true, new[] {typeof(TMP_FontAsset), typeof(TextMeshProUGUI)});

                    FindType(component, new[] {typeof(Font), typeof(TMP_FontAsset)}, (fieldInfo, obj) =>
                    {
                        if (obj is Object texObj && texObj != null)
                        {
                            images.Add(new ImageCollectionItem
                            {
                                holder = component,
                                obj = texObj
                            });
                        }

                        return obj;
                    }, visitedFonts, true);
                }
            }

            {
                var preview = new List<Texture2D>();
                foreach (ImageCollectionItem img in images)
                {
                    if (used.Contains(img.obj))
                    {
                        continue;
                    }

                    used.Add(img.obj);
                    var tex = img.obj as Texture2D;
                    if (img.obj is Sprite sprite)
                    {
                        tex = SpriteUtility.GetSpriteTexture(sprite, false);
                    }

                    if (tex != null)
                    {
                        Texture2D copy = CopyTexture(tex);
                        preview.Add(copy);
                    }
                }

                var previewTexture = new Texture2D(10, 10, TextureFormat.RGBA32, false);
                previewTexture.PackTextures(preview.ToArray(), 0, 4096, false);
                return previewTexture;
            }
        }

        public static Texture2D CopyTexture(Texture2D texture)
        {
            RenderTexture tmp = RenderTexture.GetTemporary(
                texture.width,
                texture.height,
                0,
                RenderTextureFormat.Default,
                RenderTextureReadWrite.Linear);
            Graphics.Blit(texture, tmp);
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = tmp;
            var myTexture2D = new Texture2D(texture.width, texture.height);
            myTexture2D.ReadPixels(new Rect(0, 0, tmp.width, tmp.height), 0, 0);
            myTexture2D.Apply();
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(tmp);

            return myTexture2D;
        }

        public static void FindType(object root, Type[] searchTypes, Func<FieldInfo, object, object> del,
            HashSet<object> visited = null, bool includeUnityObjects = false, Type[] ignoreTypes = null)
        {
            if (root == null)
            {
                return;
            }

            if (visited == null)
            {
                visited = new HashSet<object>();
            }

            if (visited.Contains(root))
            {
                return;
            }

            visited.Add(root);

            Type rootType = root.GetType();
            if (ignoreTypes != null)
            {
                if (Array.IndexOf(ignoreTypes, rootType) >= 0)
                {
                    return;
                }
            }

            FieldInfo[] fields =
                rootType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (FieldInfo field in fields)
            {
                if (field.FieldType.IsPrimitive)
                {
                    continue;
                }

                if (field.FieldType.IsPointer)
                {
                    continue;
                }

                if (typeof(Component).IsAssignableFrom(field.FieldType))
                {
                    continue;
                }

                if (Array.IndexOf(searchTypes, field.FieldType) >= 0)
                {
                    object obj = field.GetValue(root);
                    field.SetValue(root, del.Invoke(field, obj));
                }
                else if (field.FieldType.IsArray)
                {
                    var arr = (Array) field.GetValue(root);
                    if (arr != null)
                    {
                        for (var i = 0; i < arr.Length; ++i)
                        {
                            object r = arr.GetValue(i);
                            if (r != null)
                            {
                                if (Array.IndexOf(searchTypes, r.GetType()) >= 0)
                                {
                                    arr.SetValue(del.Invoke(field, r), i);
                                }
                                else
                                {
                                    if (includeUnityObjects || r is Object == false)
                                    {
                                        FindType(r, searchTypes, del, visited);
                                    }

                                    arr.SetValue(r, i);
                                }
                            }
                        }
                    }
                }
                else
                {
                    object obj = field.GetValue(root);
                    if (includeUnityObjects || obj is Object == false)
                    {
                        FindType(obj, searchTypes, del, visited);
                    }

                    field.SetValue(root, obj);
                }
            }
        }

        public static void FindType(object root, Type searchType, Func<MemberInfo, object, object> del,
            HashSet<object> visited = null, bool includeUnityObjects = false, Type[] ignoreTypes = null,
            bool getProperties = false)
        {
            if (root == null)
            {
                return;
            }

            if (visited == null)
            {
                visited = new HashSet<object>();
            }

            if (visited.Contains(root))
            {
                return;
            }

            visited.Add(root);

            bool isGeneric = searchType.IsGenericType;

            Func<Type, Type, bool> check = (t1, search) =>
            {
                if (isGeneric)
                {
                    if (t1.IsGenericType == false)
                    {
                        return false;
                    }

                    return t1.GetGenericTypeDefinition() == search;
                }

                return t1 == search;
            };

            Type rootType = root.GetType();
            if (ignoreTypes != null)
            {
                if (Array.IndexOf(ignoreTypes, rootType) >= 0)
                {
                    return;
                }
            }

            FieldInfo[] fields =
                rootType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (getProperties)
            {
                PropertyInfo[] props =
                    rootType.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                foreach (PropertyInfo field in props)
                {
                    if (field.GetMethod == null || field.SetMethod == null)
                    {
                        continue;
                    }

                    if (field.CanRead == false || field.CanWrite == false)
                    {
                        continue;
                    }

                    if (field.PropertyType.IsPrimitive)
                    {
                        continue;
                    }

                    if (field.PropertyType.IsPointer)
                    {
                        continue;
                    }

                    if (typeof(Component).IsAssignableFrom(field.PropertyType))
                    {
                        continue;
                    }

                    if (check.Invoke(field.PropertyType, searchType))
                    {
                        object obj = field.GetValue(root);
                        field.SetValue(root, del.Invoke(field, obj));
                    }
                    else if (field.PropertyType.IsArray)
                    {
                        var arr = (Array) field.GetValue(root);
                        if (arr != null)
                        {
                            for (var i = 0; i < arr.Length; ++i)
                            {
                                object r = arr.GetValue(i);
                                if (r != null)
                                {
                                    if (check.Invoke(r.GetType(), searchType))
                                    {
                                        arr.SetValue(del.Invoke(field, r), i);
                                    }
                                    else
                                    {
                                        if (includeUnityObjects || r is Object == false)
                                        {
                                            FindType(r, searchType, del, visited);
                                        }

                                        arr.SetValue(r, i);
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        object obj = field.GetValue(root);
                        if (includeUnityObjects || obj is Object == false)
                        {
                            FindType(obj, searchType, del, visited);
                        }

                        field.SetValue(root, obj);
                    }
                }
            }

            foreach (FieldInfo field in fields)
            {
                if (field.FieldType.IsPrimitive)
                {
                    continue;
                }

                if (field.FieldType.IsPointer)
                {
                    continue;
                }

                if (typeof(Component).IsAssignableFrom(field.FieldType))
                {
                    continue;
                }

                if (check.Invoke(field.FieldType, searchType))
                {
                    object obj = field.GetValue(root);
                    field.SetValue(root, del.Invoke(field, obj));
                }
                else if (field.FieldType.IsArray)
                {
                    var arr = (Array) field.GetValue(root);
                    if (arr != null)
                    {
                        for (var i = 0; i < arr.Length; ++i)
                        {
                            object r = arr.GetValue(i);
                            if (r != null)
                            {
                                if (check.Invoke(r.GetType(), searchType))
                                {
                                    arr.SetValue(del.Invoke(field, r), i);
                                }
                                else
                                {
                                    if (includeUnityObjects || r is Object == false)
                                    {
                                        FindType(r, searchType, del, visited);
                                    }

                                    arr.SetValue(r, i);
                                }
                            }
                        }
                    }
                }
                else
                {
                    object obj = field.GetValue(root);
                    if (includeUnityObjects || obj is Object == false)
                    {
                        FindType(obj, searchType, del, visited);
                    }

                    field.SetValue(root, obj);
                }
            }
        }

        public static void UpdateLayoutWindow(DirtyHelper helper, LayoutWindowType layoutWindowType)
        {
            List<LayoutItem> itemsLayout = layoutWindowType.layouts.items;
            if (itemsLayout != null)
            {
                for (var j = 0; j < itemsLayout.Count; ++j)
                {
                    LayoutItem layoutItem = itemsLayout[j];

                    if (layoutItem == null)
                    {
                        layoutItem = new LayoutItem();
                    }

                    layoutItem.Validate(helper);

                    WindowLayout windowLayoutType = layoutItem.windowLayout;
                    if (windowLayoutType != null)
                    {
                        windowLayoutType.ValidateEditor();

                        {
                            // Validate components list

                            for (var c = 0; c < layoutItem.components.Length; ++c)
                            {
                                ref LayoutItem.LayoutComponentItem com = ref layoutItem.components[c];
                                WindowSystemResourcesResourcePropertyDrawer.Validate(ref com.component,
                                    typeof(WindowComponent));
                            }
                        }
                    }
                }
            }
        }

        public static void AddSafeZone(Transform root)
        {
            var safeGo = new GameObject("SafeZone", typeof(WindowLayoutSafeZone));
            safeGo.transform.SetParent(root);
            safeGo.GetComponent<WindowLayoutSafeZone>().ValidateEditor();
            safeGo.GetComponent<WindowLayoutSafeZone>().SetTransformFullRect();

            for (int i = root.transform.childCount - 1; i >= 0; --i)
            {
                Transform child = root.transform.GetChild(i);
                Vector3 scale = child.localScale;
                child.SetParent(safeGo.transform, true);
                child.localScale = scale;
            }

            root.GetComponent<WindowLayout>().safeZone = safeGo.GetComponent<WindowLayoutSafeZone>();
        }

        public static void SetFirstSibling(Object[] objects, int siblingIndexTarget = 0)
        {
            foreach (Object obj in objects)
            {
                var comp = obj as Component;
                Component[] comps = comp.GetComponents<Component>();
                var countPrev = 0;
                foreach (Component c in comps)
                {
                    if (c == obj)
                    {
                        break;
                    }

                    ++countPrev;
                }

                --countPrev;
                int target = countPrev - siblingIndexTarget;

                for (var i = 0; i < target; ++i)
                {
                    ComponentUtility.MoveComponentUp(comp);
                }
            }
        }

        public static Rect FitRect(Rect rect, Rect root)
        {
            if (rect.width > rect.height)
            {
                rect.height = rect.height / rect.width * root.height;
                rect.width = root.width;

                rect.x = root.x;
                rect.y = root.y + root.height * 0.5f - rect.height * 0.5f;
            }
            else
            {
                rect.width = rect.width / rect.height * root.width;
                rect.height = root.height;

                rect.x = root.x + root.width * 0.5f - rect.width * 0.5f;
                rect.y = root.y;
            }

            return rect;
        }

        public static string StringToCaption(string str)
        {
            return Regex.Replace(str, "[A-Z]", match => { return " " + match.Value.Trim(); }).Trim();
        }

        public static FieldInfo GetFieldViaPath(Type type, string path)
        {
            return GetFieldViaPath(type, path, out _);
        }

        public static bool IsFieldOfTypeBeneath(Type type, Type baseType, string path)
        {
            while (type != baseType)
            {
                Type parent = type;
                FieldInfo fi = parent.GetField(path);
                string[] paths = path.Split('.');

                for (var i = 0; i < (path.Length > 0 ? 1 : paths.Length); i++)
                {
                    fi = parent.GetField(paths[i]);
                    if (fi == null)
                    {
                        return false;
                    }

                    // there are only two container field type that can be serialized:
                    // Array and List<T>
                    if (fi.FieldType.IsArray)
                    {
                        parent = fi.FieldType.GetElementType();
                        i += 2;
                        continue;
                    }

                    if (fi.FieldType.IsGenericType)
                    {
                        parent = fi.FieldType.GetGenericArguments()[0];
                        i += 2;
                        continue;
                    }

                    parent = fi.FieldType;
                }

                if (fi.DeclaringType == type)
                {
                    return true;
                }

                type = type.BaseType;
            }

            return false;
        }

        public static FieldInfo GetFieldViaPath(Type type, string path, out Type parent)
        {
            parent = type;
            FieldInfo fi = parent.GetField(path);
            string[] paths = path.Split('.');

            for (var i = 0; i < paths.Length; i++)
            {
                fi = parent.GetField(paths[i]);
                if (fi == null)
                {
                    return null;
                }

                // there are only two container field type that can be serialized:
                // Array and List<T>
                if (fi.FieldType.IsArray)
                {
                    parent = fi.FieldType.GetElementType();
                    i += 2;
                    continue;
                }

                if (fi.FieldType.IsGenericType)
                {
                    parent = fi.FieldType.GetGenericArguments()[0];
                    i += 2;
                    continue;
                }

                parent = fi.FieldType;
            }

            return fi;
        }

        public static void SetDirtyAndValidate(SerializedProperty property)
        {
            for (var i = 0; i < property.serializedObject.targetObjects.Length; ++i)
            {
                EditorUtility.SetDirty(property.serializedObject.targetObjects[i]);
            }
        }
    }
}