using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Volorf.Figmage
{
    [CustomEditor(typeof(FigmaUIImage))]
    public class FigmaUiImageInspector : Editor
    { 
        VisualTreeAsset ui;
        
        FigmaUIImage _figmaUiImage;
        VisualElement _previewContainer;
        VisualElement _root;
        Label _imageDescription;

        public override VisualElement CreateInspectorGUI()
        {
            // ui = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Editor/MyCustomEditor.uxml")
            ui = Resources.Load<VisualTreeAsset>("FigmaUiImageInspector");
            Debug.Log(ui.name);
            // string[] uiPaths = AssetDatabase.FindAssets("FigmaUiImageInspector", null);
            // Debug.Log(uiPaths[0]);
            // string[] links = 
            //
            // if (links != null)
            // {
            //     string path = AssetDatabase.GUIDToAssetPath(links[0]);
            //     preview = (Texture)AssetDatabase.LoadAssetAtPath(path, typeof(Texture));
            // }
            
            // Root UI
            _root = new VisualElement();
            
            // Get an instance of the main script
            _figmaUiImage = target as FigmaUIImage;

            ui.CloneTree(_root);
            
            // Get references
            _previewContainer = _root.Q<VisualElement>("Container");
            _root.Q<TextField>("FigmaLink");
            _imageDescription = _root.Q<Label>("imageDescription");
            Button updateButton = _root.Q<Button>("UpdateButton");
            
            // Subscriptions
            updateButton.RegisterCallback<ClickEvent>(UpdateFigmaImage);
            _figmaUiImage.OnUiImageUpdated.AddListener(UpdatePreview);
            _figmaUiImage.OnUploadingFailed.AddListener(SetDefault);
            
            // vel.RegisterCallback<TransitionEndEvent>(evt => vel.ToggleInClassList("preloadingAnimator"));
            // root.schedule.Execute(() => vel.ToggleInClassList("preloadingAnimator")).StartingIn(100);

            
            // Have it here because it wired up to editor's updates, which might happen often
            if (_figmaUiImage.GetRawImage() != null)
            {
                UpdatePreview(new FigmaUIImageData(_figmaUiImage.GetRawImage().texture, _figmaUiImage.GetScale()));
            }
                
            // fignity.UpdateFigmaImage();
            return _root;
        }

        void UpdateFigmaImage(ClickEvent ev)
        {
            
            StartPreloading();
            string ft = PlayerPrefs.GetString("FIGMA_TOKEN");
            if (ft == null || ft == String.Empty)
            {
                FigmaUIImageSettings.ShowSettings();
                SetDefault();
                Debug.LogError("Please, enter your Figma Access Token.");
                return;
            }
            
            _figmaUiImage.UpdateFigmaImage();
        }

        void StartPreloading()
        {
            _imageDescription.text = "Loading...";
            _previewContainer.style.backgroundImage = new StyleBackground(_figmaUiImage.GetLoadingTexture() as Texture2D);
        }

        void StopPreloading()
        {
            
        }

        void SetDefault()
        {
            _imageDescription.text = "No Figma Image";
            _previewContainer.style.backgroundImage = new StyleBackground(_figmaUiImage.GetDefaultTexture() as Texture2D);
        }

        void UpdatePreview(FigmaUIImageData fd)
        {
            _previewContainer.style.backgroundImage = new StyleBackground(fd.GetTexture() as Texture2D);

            string descText = $"Original size is {fd.GetOriginalWidth()}x{fd.GetOriginalHeight()}. Ratio is {fd.GetRatio().ToString("0.###")}";
            _imageDescription.text = descText;
            _previewContainer.style.width = fd.GetOriginalWidth();
            _previewContainer.style.height = fd.GetOriginalHeight();
            
            StopPreloading();
        }
    }
}

