using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Volorf.FigmaUIImage
{
    #if UNITY_EDITOR
    
        [CustomEditor(typeof(FigmaUIImage))]
        public class FigmaUiImageInspector : Editor
        { 
            VisualTreeAsset _ui;
            FigmaUIImage _figmaUiImage;
            VisualElement _previewContainer;
            VisualElement _root;
            Label _imageDescription;
            VisualElement _explanation;

            public override VisualElement CreateInspectorGUI()
            {
                _ui = Resources.Load<VisualTreeAsset>("FigmaUiImageInspector");
                _root = new VisualElement();
                _figmaUiImage = target as FigmaUIImage;
                _ui.CloneTree(_root);
                
                // Get references
                _previewContainer = _root.Q<VisualElement>("Container");
                _imageDescription = _root.Q<Label>("imageDescription");
                Button updateButton = _root.Q<Button>("UpdateButton");
                _explanation = _root.Q<VisualElement>("explanation");
                
                // Subscriptions
                updateButton.RegisterCallback<ClickEvent>(UpdateFigmaImage);
                _figmaUiImage.OnUiImageUpdated.AddListener(UpdatePreview);
                _figmaUiImage.OnUploadingFailed.AddListener(SetDefault);
                
                if (_figmaUiImage.GetRawImage() != null)
                {
                    UpdatePreview(new FigmaUIImageData(_figmaUiImage.GetRawImage().texture, _figmaUiImage.GetScale(), FigmaUIImage.GetCurrentDateTime()));
                }

                SetDisplayStyleForExplanation();
                
                return _root;
            }

            void SetDisplayStyleForExplanation()
            {
                _explanation.style.display = _figmaUiImage.GetFigmaUIData() != null ? new StyleEnum<DisplayStyle>(DisplayStyle.None) : new StyleEnum<DisplayStyle>(DisplayStyle.Flex);
            }

            void UpdateFigmaImage(ClickEvent ev)
            {
                SetDisplayStyleForExplanation();
                StartPreloading();
                if (_figmaUiImage.GetFigmaUIData() == null)
                {
                    SetDefault();
                    Debug.LogError("Add a Figma UI Data to the Figma UI Image.");
                    return;
                }

                if (_figmaUiImage.GetFigmaUIData().figmaLink.Length <= 0)
                {
                    SetDefault();
                    Debug.LogError("Add a Figma Link to the Figma UI Data.");
                    return;
                }
                
                if (_figmaUiImage.GetFigmaUIData().token.Length <= 0)
                {
                    SetDefault();
                    Debug.LogError("Add a Figma Token to the Figma UI Data.");
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
    
    #endif
}

