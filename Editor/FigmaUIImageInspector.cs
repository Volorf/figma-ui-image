using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Volorf.FigmaUIImage
{
    [CustomEditor(typeof(FigmaUIImage))]
    public class FigmaUiImageInspector : Editor
    { 
        VisualTreeAsset _ui;
        FigmaUIImage _figmaUiImage;
        VisualElement _previewContainer;
        VisualElement _root;
        Label _imageDescription;

        public override VisualElement CreateInspectorGUI()
        {
            _ui = Resources.Load<VisualTreeAsset>("FigmaUiImageInspector");
            _root = new VisualElement();
            _figmaUiImage = target as FigmaUIImage;
            _ui.CloneTree(_root);
            
            // Get references
            _previewContainer = _root.Q<VisualElement>("Container");
            _root.Q<TextField>("FigmaLink");
            _imageDescription = _root.Q<Label>("imageDescription");
            Button updateButton = _root.Q<Button>("UpdateButton");
            
            // Subscriptions
            updateButton.RegisterCallback<ClickEvent>(UpdateFigmaImage);
            _figmaUiImage.OnUiImageUpdated.AddListener(UpdatePreview);
            _figmaUiImage.OnUploadingFailed.AddListener(SetDefault);
            
            if (_figmaUiImage.GetRawImage() != null)
            {
                UpdatePreview(new FigmaUIImageData(_figmaUiImage.GetRawImage().texture, _figmaUiImage.GetScale()));
            }
            
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

