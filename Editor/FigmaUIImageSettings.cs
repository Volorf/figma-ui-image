using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Volorf.FigmaUIImage
{
    public class FigmaUIImageSettings : EditorWindow
    {
        VisualTreeAsset _ui;
        static FigmaUIImageSettings _wnd;
        TextField _field;
        const string FigmaTokenKeyName = "FIGMA_TOKEN";

        [MenuItem("Tools/Figma UI Image/Settings", false, 1)]
        public static void ShowSettings()
        {
            _wnd = GetWindow<FigmaUIImageSettings>();
            _wnd.titleContent = new GUIContent("Figma UI Image Settings");
        }

        [MenuItem("Tools/Figma UI Image/Update All", false, 2)]
        public static void UpdateAll()
        {
            IFigmaImageUpdatable[] figmaImageUpdatables =
                FindObjectsOfType<MonoBehaviour>(false).OfType<IFigmaImageUpdatable>().ToArray();

            foreach (IFigmaImageUpdatable updatable in figmaImageUpdatables)
            {
                updatable.UpdateFigmaImage();
            }

            Debug.Log("All textures have been updated.");
        }

        public void CreateGUI()
        {
            string tokenValue = PlayerPrefs.GetString(FigmaTokenKeyName);
            Debug.Log(tokenValue);
            
            _ui = Resources.Load<VisualTreeAsset>("FigmaUIImageSettings");
            VisualElement root = rootVisualElement;
            VisualElement labelFromUXML = _ui.Instantiate();
            root.Add(labelFromUXML);

            Button apply = root.Q<Button>("Apply");
            _field = root.Q<TextField>("Token");
            _field.value = tokenValue;

            apply.RegisterCallback<ClickEvent>(SaveFigmaToken);
        }

        void SaveFigmaToken(ClickEvent ev)
        {
            PlayerPrefs.SetString(FigmaTokenKeyName, _field.value);
            _wnd.Close();
        }

    }
}
