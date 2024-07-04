using System;
using System.Collections;
using System.Collections.Specialized;
using System.Web;
using UnityEngine;
using UnityEngine.Networking;
using SimpleJSON;
using UnityEditor;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Volorf.FigmaUIImage
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(RawImage))]
    [AddComponentMenu("Volorf/Figma UI Image")]
    public class FigmaUIImage : MonoBehaviour, IFigmaImageUpdatable
    {
        public FigmaUIImageEvent OnUiImageUpdated = new FigmaUIImageEvent();
        public UnityEvent OnUploadingFailed = new UnityEvent();
        
        [SerializeField] float imageScale = 2f;
        [SerializeField] FigmaUIData figmaUIData;
        
        const string MainFigmaLinkPart = "https://www.figma.com/design/";
        const string BaseFigmaImageUrl = "https://api.figma.com/v1/images/";
        const string BaseFigmaDocumentUrl = "https://api.figma.com/v1/files/";
        
        string _figmaFileKey;
        bool _isLinkValid;

        public Texture texture
        {
            get
            {
                return _texture;
            }
            set
            {
                _texture = value;
                SetRawImage(value);
            }
        }
        
        Texture _texture = default;
        Texture _loadingTexture = default;
        Texture _defaultTexture = default;
        RawImage _rawImage;
        float _textureRatio;
        Vector2 _textureSize;
        string _figmaSelectionName;

        public FigmaUIData GetFigmaUIData() => figmaUIData;
        
        public void UpdateFigmaImage()
        {
            if (figmaUIData == null)
            {
                Debug.LogError("Add a Figma UI Data to the Figma UI Image");
                return;
            }
            if (figmaUIData.token.Length <= 0)
            {
                Debug.LogError("Add a Figma token to the Figma UI Data");
                return;
            }

            if (figmaUIData.figmaLink.Length <= 0)
            {
                Debug.LogError("Add a Figma Link to the Figma UI Data");
                return;
            }
            
            #if UNITY_EDITOR
                texture = GetPreview("FigImagePlaceholder");
            #endif
            
            _isLinkValid = true;
            SetImageFromFigma();
        }

        void SetFigmageName(string name)
        {
            transform.name = name;
        }

        public float GetScale() => imageScale;

        void Awake()
        {
            _rawImage = GetComponent<RawImage>();
            
            #if UNITY_EDITOR
                _defaultTexture = GetPreview("FigImagePlaceholder");
                _loadingTexture = GetPreview("FigImageLoading");
            #endif
            
            texture = _rawImage.texture == null ? _defaultTexture : _rawImage.texture;
        }

        void Start()
        {
            // Debug.LogError("figmaLink from start " + figmaUIData.figmaLink);
            // Debug.LogError("token from start " + figmaUIData.token);
            
            if (_rawImage.texture == null)
            {
                UpdateFigmaImage(); 
            }
        }

        public RawImage GetRawImage() => _rawImage;

        public Texture GetLoadingTexture() => _loadingTexture;

        public Texture GetDefaultTexture() => _defaultTexture;

        void SetImageFromFigma()
        {
            if (_rawImage == null) _rawImage = GetComponent<RawImage>();
            
            string fileKey = GetFileKey(figmaUIData.figmaLink);

            if (_isLinkValid)
            {
                string nodeId = GetNodeId(figmaUIData.figmaLink);

                string finalImageUrl = CombineImageUrl(BaseFigmaImageUrl, fileKey, nodeId, imageScale);
                StartCoroutine(RequestImageLinkFromFigma(finalImageUrl));

                string finalDocUrl = CombineDocumentUrl(BaseFigmaDocumentUrl, fileKey, nodeId);
                StartCoroutine(RequestDocumentFromFigma(finalDocUrl));
            }
            else
            {
                #if UNITY_EDITOR
                    texture = GetPreview("FigImagePlaceholder");
                #endif
            }
        }

        string CombineImageUrl(string baseURL, string fileKey, string nodeId, float scale)
        {
            NameValueCollection parsedParams = System.Web.HttpUtility.ParseQueryString(String.Empty);
            parsedParams.Add("ids", nodeId);
            parsedParams.Add("scale", scale.ToString());
            return baseURL + fileKey + "?" + parsedParams.ToString();
        }
        
        string CombineDocumentUrl(string baseURL, string fileKey, string nodeId)
        {
            NameValueCollection parsedParams = System.Web.HttpUtility.ParseQueryString(String.Empty);
            parsedParams.Add("ids", nodeId);
            return baseURL + fileKey + "/" + "nodes" + "?" + parsedParams.ToString();
        }

        string GetFileKey(string link)
        {
            string cutFirstPartFigmaLink = link.Replace(MainFigmaLinkPart, "");
            int removeIndex = cutFirstPartFigmaLink.IndexOf("/");

            if (removeIndex < 0)
            {
                Debug.LogError("Got an invalid link. Can't parse it.");
                _isLinkValid = false;
                return link;
            }
            return cutFirstPartFigmaLink.Substring(0, removeIndex);
        }

        string GetNodeId(string link)
        {
            Uri uri = new Uri(link);
            return HttpUtility.ParseQueryString(uri.Query).Get("node-id");
        }
        
        IEnumerator RequestImageLinkFromFigma(string url)
        {
            using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
            {
                webRequest.SetRequestHeader("X-FIGMA-TOKEN", figmaUIData.token);
                yield return webRequest.SendWebRequest();

                switch (webRequest.result)
                {
                    case UnityWebRequest.Result.ConnectionError:
                        OnUploadingFailed.Invoke();
                        break;
                    case UnityWebRequest.Result.DataProcessingError:
                        OnUploadingFailed.Invoke();
                        Debug.LogError("Error: " + webRequest.error);
                        break;
                    case UnityWebRequest.Result.ProtocolError:
                        OnUploadingFailed.Invoke();
                        Debug.LogError("HTTP Error: " + webRequest.error);
                        break;
                    case UnityWebRequest.Result.Success:
                        string js = webRequest.downloadHandler.text;
                        JSONNode info = JSON.Parse(js);
                        string linkToImage = info[1][0];
                        StartCoroutine(RequestImage(linkToImage));
                        break;
                }
            }
        }
        
        IEnumerator RequestDocumentFromFigma(string url)
        {
            using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
            {
                webRequest.SetRequestHeader("X-FIGMA-TOKEN", figmaUIData.token);
                yield return webRequest.SendWebRequest();

                switch (webRequest.result)
                {
                    case UnityWebRequest.Result.ConnectionError:
                        OnUploadingFailed.Invoke();
                        break;
                    case UnityWebRequest.Result.DataProcessingError:
                        OnUploadingFailed.Invoke();
                        Debug.LogError("Error: " + webRequest.error);
                        break;
                    case UnityWebRequest.Result.ProtocolError:
                        OnUploadingFailed.Invoke();
                        Debug.LogError("HTTP Error: " + webRequest.error);
                        break;
                    case UnityWebRequest.Result.Success:
                        string js = webRequest.downloadHandler.text;
                        JSONNode info = JSON.Parse(js);
                        // Debug.Log(info);
                        // 7 is nodes
                        _figmaSelectionName = info[7][0][0][1];
                        SetFigmageName(_figmaSelectionName);
                        break;
                }
            }
        }

        void SetRawImage(Texture t)
        {
            _rawImage.rectTransform.sizeDelta = new Vector2(t.width / imageScale,
                t.height / imageScale);
            _rawImage.texture = t;
        }

        IEnumerator RequestImage(string url)
        {
            UnityWebRequest request = UnityWebRequestTexture.GetTexture(url);
            yield return request.SendWebRequest();

            switch (request.result)
            {
                case UnityWebRequest.Result.InProgress:
                    break;
                case UnityWebRequest.Result.Success:
                    Texture tempTex = DownloadHandlerTexture.GetContent(request);
                    tempTex.filterMode = FilterMode.Bilinear;
                    texture = tempTex;
                    SetRawImage(texture);
                    
                    FigmaUIImageData figmaUiImageData = new FigmaUIImageData(texture, imageScale, GetCurrentDateTime());

                    // print("tex width: " + figmaImageData.GetWidth());
                    // print("tex height: " + figmaImageData.GetHeight());
                    // print("texRatio: " + figmaImageData.GetRatio());
                    
                    OnUiImageUpdated.Invoke(figmaUiImageData);
                    Debug.Log($"{_figmaSelectionName} has been updated.");

                    break;
                case UnityWebRequest.Result.ConnectionError:
                    OnUploadingFailed.Invoke();
                    Debug.LogError("Connection Error");
                    break;
                case UnityWebRequest.Result.ProtocolError:
                    OnUploadingFailed.Invoke();
                    Debug.LogError("Protocol Error");
                    break;
                case UnityWebRequest.Result.DataProcessingError:
                    OnUploadingFailed.Invoke();
                    Debug.LogError("Data Processing Error");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        static Texture GetPreview(string assetName)
        {
            Texture preview = null;
            
            #if UNITY_EDITOR
                string[] links = AssetDatabase.FindAssets(assetName, null);
                
                if (links != null)
                {
                    string path = AssetDatabase.GUIDToAssetPath(links[0]);
                    preview = (Texture)AssetDatabase.LoadAssetAtPath(path, typeof(Texture));
                }
            #endif

            return preview;
        }

        public static string GetCurrentDateTime()
        {
            DateTime curDT = DateTime.Now;
            string strD= $"{curDT.Year}.{curDT.Month}.{curDT.Day}";
            string strT = $"{curDT.Hour}:{curDT.Minute}:{curDT.Second}";
            return $"{strD} {strT}";
        }
    }
}