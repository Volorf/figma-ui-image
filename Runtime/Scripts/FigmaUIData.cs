using UnityEngine;

namespace Volorf.FigmaUIImage
{
    [CreateAssetMenu(fileName = "Figma UI Data", menuName = "Figma UI/Create Figma UI Data", order = 1)]
    public class FigmaUIData : ScriptableObject
    {
        [TextArea(3, 6)] public string token;
        [TextArea(3, 6)] public string figmaLink;
    }
}


