using UnityEngine;

namespace Volorf.FigmaUIImage
{
    [RequireComponent(typeof(FigmaUIImage))]
    public class FigmaTokenProvider : MonoBehaviour
    {
        [SerializeField] FigmaTokenAsset _tokenAsset;

        void Awake()
        {
            var figmaImage = GetComponent<FigmaUIImage>();
            if (figmaImage != null)
            {
                figmaImage.SetToken(_tokenAsset.token);
                figmaImage.SetUpdateOnStart(true);
            }
        }
    }
}

