using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Volorf.FigmaUIImage
{
    #if UNITY_EDITOR
        // Creates a GameObject in a scene via the context menu
        public class FigmaUIImageUtility : MonoBehaviour
        {
            #if UNITY_EDITOR
                [MenuItem("GameObject/UI/Figma UI Image")]
            #endif
            public static void AddFigImage()
            {
                GameObject newObject = new GameObject("Figma UI Image");
                RawImage rawImage = newObject.AddComponent<RawImage>();
                FigmaUIImage figmaUiImage = newObject.AddComponent<FigmaUIImage>();
                
                Debug.Log("Figma UI Image has been added");
                
                if (Selection.activeGameObject == null)
                {
                    Canvas canvas = GameObject.FindObjectOfType<Canvas>();
                
                    if(canvas != null)
                    {
                        newObject.transform.SetParent(canvas.transform);
                        newObject.transform.localPosition = Vector3.zero;
                        newObject.transform.localScale = Vector3.one;
                    }
                }
                else
                {
                    newObject.transform.SetParent(Selection.activeGameObject.transform);
                    newObject.transform.localPosition = Vector3.zero;
                    newObject.transform.localScale = Vector3.one;
                }
            
                Undo.RegisterCreatedObjectUndo(newObject,"Create a Figma UI Image");
            
                Selection.activeGameObject = newObject;
            }
        } 
        
    #endif
}

