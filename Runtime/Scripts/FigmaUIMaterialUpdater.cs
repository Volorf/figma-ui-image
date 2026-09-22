using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Volorf.FigmaUIImage
{
    // Runs in edit mode so it can listen to FigmaUIImage, which also runs in edit mode.
    [ExecuteInEditMode]
    [RequireComponent(typeof(FigmaUIImage))]
    public class FigmaUIImageMaterialUpdater : MonoBehaviour
    {
        const string EmbeddedTextureName = "FigmaUIImage_Texture";

        [SerializeField] private Material material;

        void OnEnable()
        {
            Subscribe();
            ApplyCurrentTexture();
        }

        void OnDisable()
        {
            Unsubscribe();
        }

        void OnValidate()
        {
            // Called on script load and inspector edits. Listeners added with AddListener are
            // not serialized, so re-subscribe whenever the editor reloads this component.
            if (isActiveAndEnabled)
            {
                Subscribe();
            }
        }

        FigmaUIImage GetFigmaImage()
        {
            // Can be null while a prefab is being imported or components are reordered.
            return GetComponent<FigmaUIImage>();
        }

        void Subscribe()
        {
            FigmaUIImage figmaImage = GetFigmaImage();
            if (figmaImage == null) return;
            figmaImage.OnUiImageUpdated.RemoveListener(UpdateMaterialTexture);
            figmaImage.OnUiImageUpdated.AddListener(UpdateMaterialTexture);
        }

        void Unsubscribe()
        {
            FigmaUIImage figmaImage = GetFigmaImage();
            if (figmaImage == null) return;
            figmaImage.OnUiImageUpdated.RemoveListener(UpdateMaterialTexture);
        }

        // If the image was downloaded before this component subscribed (or the material lost its
        // reference), push the texture the RawImage already holds instead of waiting for the next update.
        void ApplyCurrentTexture()
        {
            if (material == null) return;

            FigmaUIImage figmaImage = GetFigmaImage();
            RawImage rawImage = GetComponent<RawImage>();
            if (figmaImage == null || rawImage == null) return;

            Texture current = rawImage.texture;
            if (current == null) return;
            if (current == figmaImage.GetDefaultTexture() || current == figmaImage.GetLoadingTexture()) return;
            if (material.mainTexture == current) return;

            SetMaterialTexture(current);
        }

        void UpdateMaterialTexture(FigmaUIImageData data)
        {
            if (material == null)
            {
                Debug.LogError("FigmaUIImageMaterialUpdater: No material assigned.");
                return;
            }

            SetMaterialTexture(data.GetTexture());
        }

        void SetMaterialTexture(Texture texture)
        {
            #if UNITY_EDITOR
            texture = PersistTexture(texture);
            #endif

            material.mainTexture = texture;

            #if UNITY_EDITOR
            EditorUtility.SetDirty(material);
            #endif
        }

        #if UNITY_EDITOR
        // A downloaded texture is a runtime object, and a material asset cannot keep a reference
        // to it across a save or reload. Embed the texture in the material asset so the reference persists.
        Texture PersistTexture(Texture texture)
        {
            if (texture == null || Application.isPlaying) return texture;
            if (AssetDatabase.Contains(texture)) return texture;

            string materialPath = AssetDatabase.GetAssetPath(material);
            if (string.IsNullOrEmpty(materialPath)) return texture; // Material is not an asset; a plain assignment is fine.

            // Drop the texture embedded by a previous update so the material asset does not keep growing.
            foreach (Object sub in AssetDatabase.LoadAllAssetRepresentationsAtPath(materialPath))
            {
                if (sub is Texture && sub.name == EmbeddedTextureName)
                {
                    AssetDatabase.RemoveObjectFromAsset(sub);
                }
            }

            texture.name = EmbeddedTextureName;
            AssetDatabase.AddObjectToAsset(texture, material);
            AssetDatabase.SaveAssetIfDirty(material);
            return texture;
        }
        #endif
    }
}
