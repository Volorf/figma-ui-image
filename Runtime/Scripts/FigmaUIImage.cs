using System;
using System.Collections;
using System.Collections.Specialized;
using System.IO;
using System.Threading.Tasks;
using System.Web;
using UnityEngine;
using UnityEngine.Networking;
using SimpleJSON;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine.Events;
using UnityEngine.UI;

namespace Volorf.FigmaUIImage
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(RawImage))]
    [AddComponentMenu("Volorf/Figma UI Image")]
    public class FigmaUIImage : MonoBehaviour, IFigmaImageUpdatable
    {
        #region EVENTS

        public FigmaUIImageEvent OnUiImageUpdated = new FigmaUIImageEvent();
        public UnityEvent OnUploadingFailed = new UnityEvent();

        #endregion

        #region SERIALIZED FIELDS

        [SerializeField] float imageScale = 2f;
        [SerializeField] [TextArea(3, 6)] string figmaLink;
        // [SerializeField] FigmaUIData figmaUIData;

        #endregion

        #region CONSTANTS

        const string MainFigmaLinkPart = "https://www.figma.com/design/";
        const string BaseFigmaImageUrl = "https://api.figma.com/v1/images/";
        const string BaseFigmaDocumentUrl = "https://api.figma.com/v1/files/";

        #endregion

        #region STATE

        RawImage _rawImage;
        Texture _texture = default;
        Texture _loadingTexture = default;
        Texture _defaultTexture = default;

        string _token = string.Empty;
        string _figmaFileKey;
        string _figmaSelectionName;
        bool _isLinkValid;
        bool _updateOnStart;

        float _textureRatio;
        Vector2 _textureSize;

        #endregion

        #region PROPERTIES

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

        #endregion

        #region UNITY CALLBACKS
        
        void Start()
        {
            // Debug.LogError("figmaLink from start " + figmaUIData.figmaLink);
            // Debug.LogError("token from start " + figmaUIData.token);
            
            _rawImage = GetComponent<RawImage>();
            _token = GetToken();
            
            #if UNITY_EDITOR
            _defaultTexture = GetPreview("FigImagePlaceholder");
            _loadingTexture = GetPreview("FigImageLoading");
            #endif
            
            texture = _rawImage.texture == null ? _defaultTexture : _rawImage.texture;
            
            if (_rawImage.texture == null || _updateOnStart)
            {
                UpdateFigmaImage(); 
            }
        }
        
        #endregion

        #region PUBLIC API

        public void UpdateFigmaImage()
        {
            if (string.IsNullOrEmpty(_token))
            {
                _token = GetToken();
                
                if (string.IsNullOrEmpty(_token))
                {
                    Debug.LogError("Figma Token field is empty");
                    return;
                }
            }
            
            if (string.IsNullOrEmpty(figmaLink))
            {
                Debug.LogError("Figma Link field is empty");
                return;
            }

            #if UNITY_EDITOR
                texture = GetPreview("FigImagePlaceholder");
            #endif
            
            _isLinkValid = true;
            SetImageFromFigma();
        }

        public void SaveAsAsset()
        {
            if (_texture == null)
            {
                Debug.LogError("Figma Image texture is empty");
                return;
            }
            
            string path = "Assets/Volorf/Figma UI Image/";
            string fileName = $"FigmaUIImage_{System.DateTime.Now:yyyyMMdd_HHmmss}.png";
            
            Texture2D texture2D = _texture as Texture2D;

            if (texture2D == null)
            {
                Debug.LogError("Figma Image texture is not a Texture2D and cannot be saved.");
                return;
            }

            SaveTextureAsAsset(texture2D, path, fileName).ContinueWith(task =>
            {
                if (task.IsFaulted)
                {
                    Debug.LogError($"Failed to save image: {task.Exception}");
                }
                else
                {
                    Debug.Log($"Image saved as {path}/{fileName}");
                }
            });
        }

        public void SetToken(string token)
        {
            _token = token;
        }
        
        public void SetUpdateOnStart(bool updateOnStart)
        {
            _updateOnStart = updateOnStart;
        }

        public string GetToken()
        {
            string tempToken = PlayerPrefs.GetString("FIGMA_TOKEN");;
            if (String.IsNullOrEmpty(_token) || _token != tempToken)
            {
                _token = tempToken;
            }
            
            return _token;
        }

        public string GetFigmaLink()
        {
            return figmaLink;
        }

        public float GetScale() => imageScale;

        public RawImage GetRawImage() => _rawImage;

        public Texture GetLoadingTexture() => _loadingTexture;

        public Texture GetDefaultTexture() => _defaultTexture;

        public static string GetCurrentDateTime()
        {
            DateTime curDT = DateTime.Now;
            string strD= $"{curDT.Year}.{curDT.Month}.{curDT.Day}";
            string strT = $"{curDT.Hour}:{curDT.Minute}:{curDT.Second}";
            return $"{strD} {strT}";
        }

        #endregion

        #region LINK PARSING

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

        #endregion

        #region FIGMA REQUESTS

        void SetImageFromFigma()
        {
            if (_rawImage == null) _rawImage = GetComponent<RawImage>();
            
            string fileKey = GetFileKey(figmaLink);

            if (_isLinkValid)
            {
                string nodeId = GetNodeId(figmaLink);

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

        IEnumerator RequestImageLinkFromFigma(string url)
        {
            using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
            {
                webRequest.SetRequestHeader("X-FIGMA-TOKEN", _token);
                yield return webRequest.SendWebRequest();

                if (webRequest.result != UnityWebRequest.Result.Success)
                {
                    OnUploadingFailed.Invoke();
                    Debug.LogError(DescribeRequestError(webRequest, "image render"));
                    yield break;
                }

                string js = webRequest.downloadHandler.text;
                JSONNode info = JSON.Parse(js);
                string linkToImage = info[1][0];
                StartCoroutine(RequestImage(linkToImage));
            }
        }
        
        // Only used to name the GameObject after the Figma node. A failure here must not
        // cancel the image update, so it logs a warning and leaves the current name in place.
        IEnumerator RequestDocumentFromFigma(string url)
        {
            using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
            {
                webRequest.SetRequestHeader("X-FIGMA-TOKEN", _token);
                yield return webRequest.SendWebRequest();

                if (webRequest.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogWarning(DescribeRequestError(webRequest, "node name") + ". Keeping the current object name.");
                    yield break;
                }

                string js = webRequest.downloadHandler.text;
                JSONNode info = JSON.Parse(js);
                // 7 is nodes
                _figmaSelectionName = info[7][0][0][1];
                SetFigmageName(_figmaSelectionName);
            }
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
                    string updatedName = string.IsNullOrEmpty(_figmaSelectionName) ? name : _figmaSelectionName;
                    Debug.Log($"{updatedName} has been updated.");

                    break;
                case UnityWebRequest.Result.ConnectionError:
                case UnityWebRequest.Result.ProtocolError:
                case UnityWebRequest.Result.DataProcessingError:
                    OnUploadingFailed.Invoke();
                    Debug.LogError(DescribeRequestError(request, "image download"));
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        // One line per failed request that names the request and, for 429, tells when Figma allows a retry.
        static string DescribeRequestError(UnityWebRequest request, string requestName)
        {
            string message = $"Figma {requestName} request failed: {request.error}";

            if (request.responseCode == 429)
            {
                string retryAfter = request.GetResponseHeader("Retry-After");
                message += string.IsNullOrEmpty(retryAfter)
                    ? " (rate limited by Figma, wait a minute before updating again)"
                    : $" (rate limited by Figma, retry after {retryAfter} s)";
            }

            return message;
        }

        #endregion

        #region HELPERS

        void SetRawImage(Texture t)
        {
            _rawImage.rectTransform.sizeDelta = new Vector2(t.width / imageScale,
                t.height / imageScale);
            _rawImage.texture = t;
        }

        void SetFigmageName(string name)
        {
            transform.name = name;
        }

        static Texture GetPreview(string assetName)
        {
            Texture preview = null;
            
            #if UNITY_EDITOR
                string[] links = AssetDatabase.FindAssets(assetName, null);
                
                if (links != null && links.Length > 0)
                {
                    string path = AssetDatabase.GUIDToAssetPath(links[0]);
                    preview = (Texture)AssetDatabase.LoadAssetAtPath(path, typeof(Texture));
                }
            #endif

            return preview;
        }
        
        private async Task SaveTextureAsAsset(Texture2D texture, string path, string fileName)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            
            string fullPath = Path.Combine(path, fileName);
            await File.WriteAllBytesAsync(fullPath, texture.EncodeToPNG());
            
            #if UNITY_EDITOR
            AssetDatabase.Refresh();
            #endif
        }

        #endregion
    }
}
