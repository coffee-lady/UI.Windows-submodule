using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Sirenix.OdinInspector.Editor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEditor.U2D;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI.Windows;
using UnityEngine.UI.Windows.Modules;
using UnityEngine.UI.Windows.Utilities;
using UnityEngine.UI.Windows.WindowTypes;
using Object = UnityEngine.Object;

namespace UnityEditor.UI.Windows
{
    public class DrawerToolsWindowSystem : OdinValueDrawer<ToolsWindowSystem>
    {
        private readonly Dictionary<WindowBase, HashSet<UsedResource>> usedResources = new();
        private readonly HashSet<AtlasData> usedAtlases = new();
        private bool dependenciesState;

        private Texture2D lastImagesPreview;

        private List<ImageCollectionItem> lastImages;

        private SerializedProperty registeredPrefabs;

        private ReorderableList listModules;
        private Type textureUtils;
        private MethodInfo getTextureSizeMethod;

        private void Collect()
        {
            textureUtils = typeof(Editor).Assembly.GetType("UnityEditor.TextureUtil");
            getTextureSizeMethod =
                textureUtils.GetMethod("GetStorageMemorySize", BindingFlags.Public | BindingFlags.Static);

            registeredPrefabs = Property.Tree.UnitySerializedObject.FindProperty("registeredPrefabs");
        }

        
        protected override void DrawPropertyLayout(GUIContent label)
        {
            Collect();
            
            GUILayout.Space(10f);
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Collect prefabs", GUILayout.Width(200f), GUILayout.Height(30f)) == true) {

                var list = new List<WindowBase>();
                var gameObjects = AssetDatabase.FindAssets("t:GameObject");
                foreach (var guid in gameObjects) {

                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    var win = asset.GetComponent<WindowBase>();
                    if (win != null) {
                                
                        list.Add(win);
                                
                    }

                }
                        
                this.registeredPrefabs.ClearArray();
                this.registeredPrefabs.arraySize = list.Count;
                for (int i = 0; i < list.Count; ++i) {

                    this.registeredPrefabs.GetArrayElementAtIndex(i).objectReferenceValue = list[i];


                }

            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            
            GUILayout.Space(10f);
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Make Addressables", GUILayout.Width(200f), GUILayout.Height(30f)))
            {
                try
                {
                    for (var i = 0; i < registeredPrefabs.arraySize; ++i)
                    {
                        SerializedProperty element = registeredPrefabs.GetArrayElementAtIndex(i);
                        var window = element.objectReferenceValue as WindowBase;
                        if (window != null)
                        {
                            EditorUtility.DisplayProgressBar("Updating Addressables", window.ToString(),
                                i / (float) registeredPrefabs.arraySize);

                            string path = AssetDatabase.GetAssetPath(window);
                            string sourceDir = Path.GetDirectoryName(path);
                            string dir = sourceDir.Replace("/Screens", "");
                            dir = dir.Replace("\\Screens", "");
                            string mainDir = dir;

                            if (File.Exists(mainDir + "/UIWS-IgnoreAddressables.txt"))
                            {
                                continue;
                            }

                            if (File.Exists(mainDir + "\\UIWS-IgnoreAddressables.txt"))
                            {
                                continue;
                            }

                            var name = $"UIWS-{window.name}-AddressablesGroup";
                            AddressableAssetSettings aaSettings = AddressableAssetSettingsDefaultObject.Settings;
                            var groupPath = $"{mainDir}/{name}.asset";
                            AddressableAssetGroup group;
                            if (File.Exists(groupPath) == false)
                            {
                                group = aaSettings.CreateGroup(name, false, false, true, null);
                                var scheme = group.AddSchema<BundledAssetGroupSchema>();
                                BundledAssetGroupSchema schemeInstance = WindowSystemEditor.Instantiate(scheme);
                                schemeInstance.name = "BundledAssetGroupSchema";
                                AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(scheme));
                                AssetDatabase.AddObjectToAsset(schemeInstance, group);
                                group.Schemas.Clear();
                                group.Schemas.Add(schemeInstance);
                                AssetDatabase.MoveAsset(AssetDatabase.GetAssetPath(group), groupPath);
                            }

                            group = AssetDatabase.LoadAssetAtPath<AddressableAssetGroup>(groupPath);

                            dir = sourceDir.Replace("/Screens", "/Components");
                            dir = dir.Replace("\\Screens", "\\Components");
                            string[] components = AssetDatabase.FindAssets("t:GameObject", new[] {dir});
                            foreach (string guid in components)
                            {
                                string p = AssetDatabase.GUIDToAssetPath(guid);
                                var componentGo = AssetDatabase.LoadAssetAtPath<GameObject>(p);
                                var component = componentGo.GetComponent<WindowComponent>();
                                if (component != null)
                                {
                                    componentGo.SetAddressableID(p, group);
                                    EditorUtility.SetDirty(componentGo);
                                }
                            }
                        }

                        if (window is LayoutWindowType layoutWindowType)
                        {
                            var helper = new DirtyHelper(layoutWindowType);
                            EditorHelpers.UpdateLayoutWindow(helper, layoutWindowType);
                            helper.Apply();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }

                EditorUtility.ClearProgressBar();
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.Label("Make all component in all registered screens as Addressables",
                EditorStyles.centeredGreyMiniLabel);

            GUILayout.Space(10f);
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Validate Resources", GUILayout.Width(200f), GUILayout.Height(30f)))
            {
                EditorApplication.delayCall += () =>
                {
                    try
                    {
                        var isBreak = false;
                        var markDirtyCount = 0;
                        string[] gos = AssetDatabase.FindAssets("t:GameObject");
                        var visited = new HashSet<object>();
                        var visitedGeneric = new HashSet<object>();
                        var i = 0;
                        foreach (string guid in gos)
                        {
                            string path = AssetDatabase.GUIDToAssetPath(guid);
                            var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                            if (EditorUtility.DisplayCancelableProgressBar("Validating Resources 1 / 2", path,
                                    i / (float) gos.Length))
                            {
                                isBreak = true;
                                break;
                            }

                            {
                                Component[] allComponents = go.GetComponentsInChildren<Component>(true);
                                foreach (Component component in allComponents)
                                {
                                    EditorHelpers.FindType(component, typeof(Resource), (fieldInfo, res) =>
                                    {
                                        Type resType = null;
                                        object[] resTypeAttrs =
                                            fieldInfo.GetCustomAttributes(typeof(ResourceTypeAttribute), true);
                                        if (resTypeAttrs.Length > 0)
                                        {
                                            resType = ((ResourceTypeAttribute) resTypeAttrs[0]).type;
                                        }

                                        var r = (Resource) res;
                                        WindowSystemResourcesResourcePropertyDrawer.Validate(ref r, resType);
                                        ++markDirtyCount;
                                        EditorUtility.SetDirty(component.gameObject);
                                        return r;
                                    }, visited);
                                }

                                foreach (Component component in allComponents)
                                {
                                    EditorHelpers.FindType(component, typeof(Resource<>), (fieldInfo, res) =>
                                    {
                                        FieldInfo rField =
                                            res.GetType().GetField("data",
                                                BindingFlags.Instance | BindingFlags.NonPublic);
                                        var r = (Resource) rField.GetValue(res);
                                        Type type = null;
                                        var _fieldInfo = (FieldInfo) fieldInfo;
                                        if (_fieldInfo.FieldType.IsArray)
                                        {
                                            type = _fieldInfo.FieldType.GetElementType().GetGenericArguments()[0];
                                        }
                                        else
                                        {
                                            type = _fieldInfo.FieldType.GetGenericArguments()[0];
                                        }

                                        WindowSystemResourcesResourcePropertyDrawer.Validate(ref r, type);
                                        ++markDirtyCount;
                                        rField.SetValue(res, r);
                                        EditorUtility.SetDirty(component.gameObject);
                                        return res;
                                    }, visitedGeneric);
                                }
                            }

                            ++i;
                        }

                        if (isBreak == false)
                        {
                            string[] sos = AssetDatabase.FindAssets("t:ScriptableObject");
                            i = 0;
                            foreach (string guid in sos)
                            {
                                string path = AssetDatabase.GUIDToAssetPath(guid);
                                var go = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                                if (EditorUtility.DisplayCancelableProgressBar("Validating Resources 2 / 2", path,
                                        i / (float) sos.Length))
                                {
                                    isBreak = true;
                                    break;
                                }

                                {
                                    EditorHelpers.FindType(go, typeof(Resource), (fieldInfo, res) =>
                                    {
                                        Type resType = null;
                                        object[] resTypeAttrs =
                                            fieldInfo.GetCustomAttributes(typeof(ResourceTypeAttribute), true);
                                        if (resTypeAttrs.Length > 0)
                                        {
                                            resType = ((ResourceTypeAttribute) resTypeAttrs[0]).type;
                                        }

                                        var r = (Resource) res;
                                        WindowSystemResourcesResourcePropertyDrawer.Validate(ref r, resType);
                                        ++markDirtyCount;
                                        EditorUtility.SetDirty(go);
                                        return r;
                                    }, visited);
                                    EditorHelpers.FindType(go, typeof(Resource<>), (fieldInfo, res) =>
                                    {
                                        FieldInfo rField =
                                            res.GetType().GetField("data",
                                                BindingFlags.Instance | BindingFlags.NonPublic);
                                        var r = (Resource) rField.GetValue(res);
                                        var _fieldInfo = (FieldInfo) fieldInfo;
                                        Type type = null;
                                        if (_fieldInfo.FieldType.IsArray)
                                        {
                                            type = _fieldInfo.FieldType.GetElementType().GetGenericArguments()[0];
                                        }
                                        else
                                        {
                                            type = _fieldInfo.FieldType.GetGenericArguments()[0];
                                        }

                                        WindowSystemResourcesResourcePropertyDrawer.Validate(ref r, type);
                                        ++markDirtyCount;
                                        rField.SetValue(res, r);
                                        EditorUtility.SetDirty(go);
                                        return res;
                                    }, visitedGeneric);
                                }

                                ++i;
                            }

                            Debug.Log("Done. Updated: " + markDirtyCount);
                        }

                        if (isBreak)
                        {
                            Debug.Log("Break. Updated: " + markDirtyCount);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogException(ex);
                    }

                    EditorUtility.ClearProgressBar();
                };
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.Label("Find and Validate all Resource objects.", EditorStyles.centeredGreyMiniLabel);

            GUILayout.Space(10f);
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Validate WindowObjects", GUILayout.Width(200f), GUILayout.Height(30f)))
            {
                EditorApplication.delayCall += () =>
                {
                    try
                    {
                        var isBreak = false;
                        var markDirtyCount = 0;
                        string[] gos = AssetDatabase.FindAssets("t:GameObject");
                        var i = 0;
                        foreach (string guid in gos)
                        {
                            string path = AssetDatabase.GUIDToAssetPath(guid);
                            var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                            if (EditorUtility.DisplayCancelableProgressBar("Validating Objects", path,
                                    i / (float) gos.Length))
                            {
                                isBreak = true;
                                break;
                            }

                            {
                                WindowObject[] allComponents = go.GetComponentsInChildren<WindowObject>(true);
                                foreach (WindowObject component in allComponents)
                                {
                                    component.ValidateEditor();
                                    EditorUtility.SetDirty(component);
                                    EditorUtility.SetDirty(go);
                                    ++markDirtyCount;
                                }
                            }

                            ++i;
                        }

                        if (isBreak)
                        {
                            Debug.Log("Break. Updated: " + markDirtyCount);
                        }
                        else
                        {
                            Debug.Log("Done. Updated: " + markDirtyCount);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogException(ex);
                    }

                    EditorUtility.ClearProgressBar();
                };
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.Label("Find and Validate all WindowObject objects.", EditorStyles.centeredGreyMiniLabel);

            GUILayout.Space(10f);
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Find Direct References", GUILayout.Width(200f), GUILayout.Height(30f)))
            {
                try
                {
                    /*var listAtlases = new List<UnityEngine.U2D.SpriteAtlas>();
                    System.Action<UnityEngine.U2D.SpriteAtlas> action = (atlas) => {
                        listAtlases.Add(atlas);
                        Debug.Log("Reg atlas: " + atlas);
                    };*/
                    SpriteAtlasUtility.PackAllAtlases(EditorUserBuildSettings.activeBuildTarget);

                    var listAtlases = new List<AtlasData>();
                    string[] atlasesGUID = AssetDatabase.FindAssets("t:spriteatlas");
                    foreach (string atlasGUID in atlasesGUID)
                    {
                        var atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(
                            AssetDatabase.GUIDToAssetPath(atlasGUID));
                        if (atlas != null)
                        {
                            MethodInfo previews = typeof(SpriteAtlasExtensions).GetMethod("GetPreviewTextures",
                                BindingFlags.Static | BindingFlags.NonPublic);
                            listAtlases.Add(new AtlasData
                            {
                                atlas = atlas,
                                previews = (Texture2D[]) previews.Invoke(null, new[] {atlas})
                            });
                        }
                    }

                    usedAtlases.Clear();
                    usedResources.Clear();
                    for (var i = 0; i < registeredPrefabs.arraySize; ++i)
                    {
                        SerializedProperty element = registeredPrefabs.GetArrayElementAtIndex(i);
                        var window = element.objectReferenceValue as WindowBase;
                        if (window != null)
                        {
                            string path = AssetDatabase.GetAssetPath(window);
                            EditorUtility.DisplayProgressBar("Validating Resources", path,
                                i / (float) registeredPrefabs.arraySize);

                            string dir = Path.GetDirectoryName(path);
                            string componentsDir = dir.Replace("/Screens", "/Components");
                            string[] components = AssetDatabase.FindAssets("t:GameObject", new[] {componentsDir});
                            foreach (string guid in components)
                            {
                                string p = AssetDatabase.GUIDToAssetPath(guid);
                                var componentGo = AssetDatabase.LoadAssetAtPath<GameObject>(p);
                                var component = componentGo.GetComponent<WindowComponent>();
                                if (component != null)
                                {
                                    EditorHelpers.FindType(component,
                                        new[] {typeof(Sprite), typeof(Texture), typeof(Texture2D)}, (fieldInfo, res) =>
                                        {
                                            var r = (Object) res;
                                            if (r != null)
                                            {
                                                if (res is Sprite sprite)
                                                {
                                                    if (sprite != null)
                                                    {
                                                        foreach (AtlasData atlas in listAtlases)
                                                        {
                                                            if (atlas.atlas.CanBindTo(sprite))
                                                            {
                                                                if (usedAtlases.Contains(atlas) == false)
                                                                {
                                                                    usedAtlases.Add(atlas);
                                                                }

                                                                break;
                                                            }
                                                        }
                                                    }
                                                }

                                                var usedRes = new UsedResource(window, component, r);
                                                if (usedResources.TryGetValue(window, out HashSet<UsedResource> hs))
                                                {
                                                    if (hs.Contains(usedRes) == false)
                                                    {
                                                        hs.Add(usedRes);
                                                    }
                                                }
                                                else
                                                {
                                                    hs = new HashSet<UsedResource>();
                                                    hs.Add(usedRes);
                                                    usedResources.Add(window, hs);
                                                }
                                            }

                                            return r;
                                        });
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }

                EditorUtility.ClearProgressBar();
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.Label("Find all used direct reference resources and group them by screen.",
                EditorStyles.centeredGreyMiniLabel);

            if (usedResources.Count > 0)
            {
                GUILayoutExt.FoldOut(ref dependenciesState, "Direct References", () =>
                {
                    GUILayout.Space(10f);
                    GUILayout.Label($"Used atlases ({usedAtlases.Count}):");
                    var usedSize = 0;
                    foreach (AtlasData atlas in usedAtlases)
                    {
                        GUILayout.BeginHorizontal();
                        EditorGUILayout.ObjectField(atlas.atlas, typeof(Object), false);
                        var atlasSize = 0;
                        foreach (Texture2D tex in atlas.previews)
                        {
                            var size = (int) getTextureSizeMethod.Invoke(null, new[] {(Texture) tex});
                            atlasSize += size;
                            usedSize += size;
                        }

                        string str = EditorUtility.FormatBytes(atlasSize);
                        GUILayout.Label(str, GUILayout.Width(70f));
                        GUILayout.EndHorizontal();
                    }

                    GUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    GUILayout.Label($"Size: {EditorUtility.FormatBytes(usedSize)}");
                    GUILayout.EndHorizontal();

                    GUILayout.Space(10f);
                    GUILayout.Label("Used resources:");
                    foreach (KeyValuePair<WindowBase, HashSet<UsedResource>> kv in usedResources)
                    {
                        GUILayoutExt.DrawHeader(kv.Key.name);
                        foreach (UsedResource res in kv.Value)
                        {
                            GUILayout.BeginHorizontal();
                            EditorGUILayout.ObjectField(res.component, typeof(Object), false);
                            EditorGUILayout.ObjectField(res.resource, typeof(Object), false);
                            GUILayout.EndHorizontal();
                        }
                    }
                });
            }
        }

        private readonly struct UsedResource
        {
            public readonly WindowBase screen;
            public readonly Component component;
            public readonly Object resource;

            public UsedResource(WindowBase screen, Component component, Object resource)
            {
                this.screen = screen;
                this.component = component;
                this.resource = resource;
            }

            public override int GetHashCode()
            {
                return resource.GetHashCode();
            }

            public override string ToString()
            {
                return $"Screen: {screen}, Component: {component}, Resource: {resource}";
            }

            public string ToSmallString()
            {
                return $"Component: {component}, Resource: {resource}";
            }
        }

        public struct AtlasData
        {
            public SpriteAtlas atlas;
            public Texture2D[] previews;
        }
    }
}