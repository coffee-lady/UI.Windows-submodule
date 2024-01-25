using System;
using System.Collections;
using Sirenix.OdinInspector;
using UnityEngine.UI.Windows.Modules;
using UnityEngine.UI.Windows.Utilities;

namespace UnityEngine.UI.Windows.Components
{
    public class ImageComponent : WindowComponent, ISearchComponentByTypeEditor, ISearchComponentByTypeSingleEditor
    {
        [TabGroup("Basic")] [RequiredReference]
        public Graphic graphics;

        [TabGroup("Basic")] public bool preserveAspect;

        [TabGroup("Basic")] public bool useSpriteMesh;

        [TabGroup("Basic")] public UnloadResourceEventType autoUnloadResourcesOnEvent;

        private Resource prevResourceLoad;

        Type ISearchComponentByTypeEditor.GetSearchType()
        {
            return typeof(ImageComponentModule);
        }

        IList ISearchComponentByTypeSingleEditor.GetSearchTypeArray()
        {
            return componentModules.modules;
        }

        internal override void OnDeInitInternal()
        {
            base.OnDeInitInternal();

            if (autoUnloadResourcesOnEvent == UnloadResourceEventType.OnDeInit)
            {
                UnloadCurrentResources();
            }
        }

        internal override void OnHideBeginInternal()
        {
            base.OnHideBeginInternal();

            if (autoUnloadResourcesOnEvent == UnloadResourceEventType.OnHideBegin)
            {
                UnloadCurrentResources();
            }
        }

        internal override void OnHideEndInternal()
        {
            base.OnHideEndInternal();

            if (autoUnloadResourcesOnEvent == UnloadResourceEventType.OnHideEnd)
            {
                UnloadCurrentResources();
            }
        }

        private void UnloadCurrentResources()
        {
            prevResourceLoad = default;

            WindowSystemResources resources = WindowSystem.GetResources();
            if (graphics is Image image)
            {
                Sprite obj = image.sprite;
                image.sprite = null;
                resources.Delete(this, obj);
            }
            else if (graphics is RawImage rawImage)
            {
                Texture obj = rawImage.texture;
                rawImage.texture = null;
                resources.Delete(this, obj);
            }
        }

        public void SetColor(Color color)
        {
            if (graphics == null)
            {
                return;
            }

            graphics.color = color;
        }

        public Color GetColor()
        {
            if (graphics == null)
            {
                return Color.white;
            }

            return graphics.color;
        }

        public void SetImage<T>(T provider) where T : IResourceProvider
        {
            if (provider == null)
            {
                return;
            }

            SetImage(provider.GetResource());
        }

        public void SetImage(Resource resource, bool async = true, Action onSetImageComplete = null)
        {
            if (prevResourceLoad.IsEquals(resource) == false)
            {
                WindowSystemResources resources = WindowSystem.GetResources();
                switch (resource.objectType)
                {
                    case Resource.ObjectType.Sprite:
                    {
                        if (async)
                        {
                            Coroutines.Run(resources.LoadAsync<Sprite>(this, resource, (asset, _) =>
                            {
                                SetImage(asset);
                                onSetImageComplete?.Invoke();
                            }));
                        }
                        else
                        {
                            SetImage(resources.Load<Sprite>(this, resource));
                            onSetImageComplete?.Invoke();
                        }
                    }
                        break;

                    case Resource.ObjectType.Texture:
                    {
                        if (async)
                        {
                            Coroutines.Run(resources.LoadAsync<Texture>(this, resource, (asset, _) =>
                            {
                                SetImage(asset);
                                onSetImageComplete?.Invoke();
                            }));
                        }
                        else
                        {
                            SetImage(resources.Load<Texture>(this, resource));
                            onSetImageComplete?.Invoke();
                        }
                    }
                        break;
                }

                prevResourceLoad = resource;
            }
            else
            {
                onSetImageComplete?.Invoke();
            }
        }

        public void SetImage(Sprite sprite)
        {
            UnloadCurrentResources();

            if (graphics is Image image)
            {
                image.sprite = sprite;
                image.preserveAspect = preserveAspect;
                image.useSpriteMesh = useSpriteMesh;
            }
            else if (graphics is RawImage rawImage)
            {
                WindowSystemResources resources = WindowSystem.GetResources();

                var size = new Vector2Int((int) sprite.rect.width, (int) sprite.rect.height);
                Texture2D tex =
                    resources.New<Texture2D, Texture2DConstructor>(this, new Texture2DConstructor(size.x, size.y));
                Color[] block = tex.GetPixels((int) sprite.rect.x, (int) sprite.rect.y, size.x, size.y);
                tex.SetPixels(block);
                tex.Apply();
                rawImage.texture = sprite.texture;
            }
        }

        public void SetImage(Texture texture)
        {
            UnloadCurrentResources();

            if (graphics is RawImage rawImage)
            {
                rawImage.texture = texture;
            }
            else if (graphics is Image image && texture is Texture2D tex2d)
            {
                WindowSystemResources resources = WindowSystem.GetResources();

                Sprite sprite = resources.New<Sprite, SpriteConstructor>(this,
                    new SpriteConstructor(tex2d, new Rect(0f, 0f, texture.width, texture.height),
                        new Vector2(0.5f, 0.5f)));
                image.sprite = sprite;
                image.preserveAspect = preserveAspect;
                image.useSpriteMesh = useSpriteMesh;
            }
        }

        public override void ValidateEditor()
        {
            base.ValidateEditor();

            if (graphics == null)
            {
                graphics = GetComponent<Graphic>();
            }
        }

        private readonly struct Texture2DConstructor : IResourceConstructor<Texture2D>
        {
            private readonly int x;
            private readonly int y;

            public Texture2DConstructor(int x, int y)
            {
                this.x = x;
                this.y = y;
            }

            public Texture2D Construct()
            {
                return new Texture2D(x, y);
            }

            public void Deconstruct(ref Texture2D obj)
            {
                DestroyImmediate(obj);
                obj = null;
            }
        }

        private readonly struct SpriteConstructor : IResourceConstructor<Sprite>
        {
            private readonly Texture2D tex2d;
            private readonly Rect rect;
            private readonly Vector2 pivot;

            public SpriteConstructor(Texture2D tex2d, Rect rect, Vector2 pivot)
            {
                this.tex2d = tex2d;
                this.rect = rect;
                this.pivot = pivot;
            }

            public Sprite Construct()
            {
                return Sprite.Create(tex2d, rect, pivot);
            }

            public void Deconstruct(ref Sprite obj)
            {
                DestroyImmediate(obj);
                obj = null;
            }
        }
    }
}