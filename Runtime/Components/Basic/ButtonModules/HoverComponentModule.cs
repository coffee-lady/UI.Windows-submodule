﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine.UI.Windows {

    public class HoverComponentModule : ButtonComponentModule, UnityEngine.EventSystems.IPointerEnterHandler, UnityEngine.EventSystems.IPointerExitHandler {

        [UnityEngine.UI.Windows.Utilities.RequiredReferenceAttribute]
        public WindowComponent content;

        public override void ValidateEditor() {
            
            base.ValidateEditor();

            if (this.content != null) {

                this.content.AddEditorParametersRegistry(new WindowObject.EditorParametersRegistry() {
                    holder = this.windowComponent,
                    hiddenByDefault = true, hiddenByDefaultDescription = "Value is hold by HoverComponentModule"
                });

            }

        }

        public void OnPointerEnter(UnityEngine.EventSystems.PointerEventData eventData) {

            if (this.content != null) this.content.Show();

        }
        
        public void OnPointerExit(UnityEngine.EventSystems.PointerEventData eventData) {

            if (this.content != null) this.content.Hide();

        }

    }

}