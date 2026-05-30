using UnityEngine;

namespace Volorf.FigmaUIImage
{
    [RequireComponent(typeof(FigmaUIImage))]
    public class FigmaUIImageMaterialUpdater : MonoBehaviour
    {
        [SerializeField] private Material material;

        void OnValidate()
        {
            GetComponent<FigmaUIImage>().OnUiImageUpdated.RemoveListener(UpdateMaterialTexture);
            GetComponent<FigmaUIImage>().OnUiImageUpdated.AddListener(UpdateMaterialTexture);
        }

        void OnEnable()
        {
            GetComponent<FigmaUIImage>().OnUiImageUpdated.AddListener(UpdateMaterialTexture);
        }

        void OnDisable()
        {
            GetComponent<FigmaUIImage>().OnUiImageUpdated.RemoveListener(UpdateMaterialTexture);
        }
        
        void UpdateMaterialTexture(FigmaUIImageData data)
        {
            if (material == null)
            {
                Debug.LogError("FigmaUIImageMaterialUpdater: No material assigned.");
                return;
            }
            material.mainTexture = data.GetTexture();
            #if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(material);
            #endif
        }
    }
}


