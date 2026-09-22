using UnityEngine;

namespace Volorf.FigmaUIImage
{
    [CreateAssetMenu(fileName = "FigmaTokenAsset", menuName = "Figma Token Asset", order = 1)]
    public class FigmaTokenAsset : ScriptableObject
    {
        public string token;
    }
}

