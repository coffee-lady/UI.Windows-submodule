// using System;
// using System.Collections.Generic;
// using Sirenix.OdinInspector.Editor;
// using UnityEngine;
// using UnityEngine.UI.Windows;
// using UnityEngine.UI.Windows.Modules;
//
// namespace UnityEditor.UI.Windows
// {
//     [CustomEditor(typeof(WindowModule), true)]
//     [CanEditMultipleObjects]
//     public class WindowSystemWindowModuleEditor : OdinEditor
//     {
//         private InspectorProperty createPool;
//
//         private InspectorProperty animationParameters;
//         private InspectorProperty subObjects;
//
//         private InspectorProperty renderBehaviourOnHidden;
//
//         private InspectorProperty allowRegisterInRoot;
//         private InspectorProperty autoRegisterSubObjects;
//         private InspectorProperty hiddenByDefault;
//
//         private InspectorProperty parameters;
//
//         private InspectorProperty useSafeZone;
//         private InspectorProperty safeZone;
//
//         private Texture2D lastImagesPreview;
//         private List<ImageCollectionItem> lastImages;
//
//         private int selectedTab
//         {
//             get => EditorPrefs.GetInt("UnityEditor.UI.Windows.WindowModule.TabIndex");
//             set => EditorPrefs.SetInt("UnityEditor.UI.Windows.WindowModule.TabIndex", value);
//         }
//
//         private Vector2 tabScrollPosition
//         {
//             get =>
//                 new(
//                     EditorPrefs.GetFloat("UnityEditor.UI.Windows.WindowModule.TabScrollPosition.X"),
//                     EditorPrefs.GetFloat("UnityEditor.UI.Windows.WindowModule.TabScrollPosition.Y")
//                 );
//             set
//             {
//                 EditorPrefs.SetFloat("UnityEditor.UI.Windows.WindowModule.TabScrollPosition.X", value.x);
//                 EditorPrefs.SetFloat("UnityEditor.UI.Windows.WindowModule.TabScrollPosition.Y", value.y);
//             }
//         }
//
//         public void OnEnable()
//         {
//             try
//             {
// #pragma warning disable
//                 SerializedObject _ = serializedObject;
// #pragma warning restore
//             }
//             catch (Exception)
//             {
//                 return;
//             }
//
//             createPool = Tree.GetPropertyAtUnityPath("createPool");
//
//             animationParameters = Tree.GetPropertyAtUnityPath("animationParameters");
//             renderBehaviourOnHidden = Tree.GetPropertyAtUnityPath("renderBehaviourOnHidden");
//
//             subObjects = Tree.GetPropertyAtUnityPath("subObjects");
//
//             allowRegisterInRoot = Tree.GetPropertyAtUnityPath("allowRegisterInRoot");
//             autoRegisterSubObjects = Tree.GetPropertyAtUnityPath("autoRegisterSubObjects");
//             hiddenByDefault = Tree.GetPropertyAtUnityPath("hiddenByDefault");
//
//             parameters = Tree.GetPropertyAtUnityPath("parameters");
//
//             useSafeZone = Tree.GetPropertyAtUnityPath("useSafeZone");
//             safeZone = Tree.GetPropertyAtUnityPath("safeZone");
//
//             EditorHelpers.SetFirstSibling(targets);
//         }
//
//         private void DrawParameters()
//         {
//             GUILayoutExt.DrawHeader("Module Options (Default)");
//             parameters.Draw();
//         }
//
//         public override void OnInspectorGUI()
//         {
//             Tree.UpdateTree();
//
//             Tree.BeginDraw(false);
//
//             GUILayoutExt.DrawComponentHeader(serializedObject, "M",
//                 () => { GUILayoutExt.DrawComponentHeaderItem("State", ((WindowObject) target).GetState().ToString()); },
//                 new Color(1f, 0.6f, 1f, 0.4f));
//
//             GUILayout.Space(5f);
//
//             Vector2 scroll = tabScrollPosition;
//             selectedTab = GUILayoutExt.DrawTabs(
//                 selectedTab,
//                 ref scroll,
//                 new GUITab("Basic", () =>
//                 {
//                     GUILayoutExt.DrawHeader("Main");
//                     hiddenByDefault.Draw();
//                     animationParameters.Draw();
//                     subObjects.Draw();
//
//                     GUILayoutExt.DrawHeader("Performance Options");
//                     createPool.Draw();
//
//                     DrawParameters();
//                 }),
//                 new GUITab("Advanced", () =>
//                 {
//                     GUILayoutExt.DrawHeader("Render Behaviour");
//                     renderBehaviourOnHidden.Draw();
//
//                     GUILayoutExt.DrawHeader("Animation");
//                     animationParameters.Draw();
//
//                     GUILayoutExt.DrawHeader("Graph");
//                     allowRegisterInRoot.Draw();
//                     autoRegisterSubObjects.Draw();
//                     hiddenByDefault.Draw();
//                     subObjects.Draw();
//
//                     GUILayoutExt.DrawHeader("Performance Options");
//                     createPool.Draw();
//
//                     DrawParameters();
//                 }),
//                 new GUITab("Tools", () =>
//                 {
//                     GUILayoutExt.Box(4f, 4f, () =>
//                     {
//                         if (GUILayout.Button("Collect Images", GUILayout.Height(30f)))
//                         {
//                             var images = new List<ImageCollectionItem>();
//                             lastImagesPreview = EditorHelpers.CollectImages(target, images);
//                             lastImages = images;
//                         }
//
//                         GUILayoutExt.DrawImages(lastImagesPreview, lastImages);
//                     });
//                 })
//             );
//             tabScrollPosition = scroll;
//
//             GUILayout.Space(10f);
//
//             if (targets.Length == 1)
//             {
//                 useSafeZone.Draw();
//                 safeZone.Draw();
//
//                 GUILayoutExt.DrawButtonGenerateSafeArea(target);
//             }
//
//             GUILayout.Space(10f);
//
//             GUILayoutExt.DrawFieldsBeneath(Tree, typeof(WindowModule));
//
//             Tree.EndDraw();
//         }
//     }
// }