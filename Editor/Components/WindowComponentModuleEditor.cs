using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector.Editor;
using UnityEngine;
using UnityEditor;

namespace UnityEditor.UI.Windows {

    using UnityEngine.UI.Windows;

    [CustomEditor(typeof(WindowComponentModule), editorForChildClasses: true)]
    [CanEditMultipleObjects]
    public class WindowComponentModuleEditor : OdinEditor {

        public void OnEnable() {

            var comp = (Component)this.target;
            comp.hideFlags = HideFlags.HideInInspector;

        }

    }

}
