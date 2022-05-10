using AfterNow.PrezSDK.Internal.Helpers;
using AfterNow.PrezSDK.Internal.Views;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Playables;
using UnityEngine.Video;

namespace AfterNow.PrezSDK
{
    internal static class AssetLoader
    {
        private static float imageFator = 0.4f;
        private static int loadVideoFrame = 1;
        private static float defaultSizeFactor = 1;
        internal static bool loadComplete = false;
        internal static Action<int> OnClickableActivate;
        static AudioSource audioChannelVideo;
        internal static readonly List<Texture2D> textures = new List<Texture2D>();
        internal static readonly List<AudioClip> audioClips = new List<AudioClip>();
        internal static readonly List<ClickableAsset> ClickableAssets = new List<ClickableAsset>();
        private readonly static HashSet<CancellationTokenSource> _loadingGLBS = new HashSet<CancellationTokenSource>();
        private static int loadingAssetsCount = 0;

        internal static void StopLoadingAssets()
        {
            CoroutineRunner.Instance.StopAllCoroutines();

            //glbs are loaded in Task. cancelling it with StopCoroutine isnt possible. 
            //hence we load them up with cancellation token which
            //if cancelled before Task finishes, will result in killing of the process and destroy the instantiated glb.
            foreach (var token in _loadingGLBS)
            {
                token.Cancel();
            }
            _loadingGLBS.Clear();
            if (loadingAssetsCount > 0)
            {
                Resources.UnloadUnusedAssets();
                loadingAssetsCount = 0;
            }
        }

        internal static IEnumerator OnLoadAsset(ARPAsset asset, Action<GameObject> onLoaded)
        {
            bool isClickable = IsAssetClickable(asset.clickTarget);
            GameObject loadedAsset = null;
            string assetPath = asset.type != ANPAssetType.TEXT ? asset.AbsoluteDownloadPath(InitializeSDK.DownloadFolderPath) : null;
            string fileName = Path.GetFileName(assetPath);
            BoxCollider collider = null;
            switch (asset.type)
            {
                case ANPAssetType.TEXT:
                    ARPText txt = asset.text;
                    loadingAssetsCount++;
                    var request = Resources.LoadAsync<GameObject>("PrezTextAsset");
                    yield return request;
                    loadingAssetsCount--;
                    GameObject _text = (GameObject)UnityEngine.Object.Instantiate(request.asset);
                    _text.name = txt.value;
                    TextMeshPro tm = _text.GetComponentInChildren<TextMeshPro>();
                    tm.text = txt.value;
                    tm.font = txt.GetFontAsset();
                    tm.alignment = txt.GetTMPAlignment();
                    tm.color = PrezAssetHelper.GetColor(txt.color);
                    tm.faceColor = tm.color;
                    if (isClickable)
                    {
                        yield return null;
                        collider = tm.gameObject.AddComponent<BoxCollider>();
                        collider.center = Vector3.zero;
                        collider.size = new Vector3(collider.size.x, collider.size.y, 0.005f);
                    }
                    loadedAsset = _text;
                    //Debug.Log("objectloaded : " + _text.name + " type : TEXT");
                    break;

                case ANPAssetType.IMAGE:
                    loadingAssetsCount++;
                    request = Resources.LoadAsync<GameObject>("PrezImageAsset");
                    yield return request;
                    GameObject _image = (GameObject)UnityEngine.Object.Instantiate(request.asset);
                    _image.name = fileName;

                    // Load image in to the child of the loaded asset (that's the one which has 'MeshRenderer')
                    CoroutineRunner.Instance.StartCoroutine(LoadImage(_image.transform.GetChild(0).gameObject, assetPath));
                    loadingAssetsCount--;
                    if (isClickable)
                    {
                        collider = _image.transform.GetChild(0).gameObject.AddComponent<BoxCollider>();
                    }
                    loadedAsset = _image;
                    //Debug.Log("objectloaded : " + _image.name + " type : IMAGE");
                    break;

                case ANPAssetType.VIDEO:
                    loadingAssetsCount++;
                    request = Resources.LoadAsync<GameObject>("PrezVideoAsset");
                    yield return request;
                    GameObject _video = (GameObject)UnityEngine.Object.Instantiate(request.asset);
                    loadingAssetsCount--;
                    GameObject videoParent = new GameObject();
                    _video.transform.parent = videoParent.transform;
                    if (isClickable)
                    {
                        collider = _video.AddComponent<BoxCollider>();
                    }
                    videoParent.name = fileName;
                    if (videoParent.GetComponent<Rotate>() == null)
                        videoParent.AddComponent<Rotate>();
                    else { }

                    CoroutineRunner.Instance.StartCoroutine(HandleVideoPlayer(_video, assetPath, true));
                    loadComplete = true;

                    loadedAsset = videoParent;
                    //Debug.Log("objectloaded : " + videoParent.name + " type : VIDEO");
                    break;

                case ANPAssetType.OBJECT:
                    GameObject glbParent = new GameObject();
                    glbParent.name = fileName;
                    glbParent.AddComponent<Rotate>();
                    loadingAssetsCount++;
                    request = Resources.LoadAsync<GameObject>("PrezObjectAsset");
                    yield return request;
                    GameObject _object = (GameObject)UnityEngine.Object.Instantiate(request.asset);
                    loadingAssetsCount--;
                    _object.name = "GLTF";
                    _object.gameObject.SetActive(true);
                    _object.transform.SetParent(glbParent.transform);
                    if (isClickable)
                    {
                        collider = _object.AddComponent<BoxCollider>();
                    }
                    Exception exception = null;

                    string fileExtention = Path.GetExtension(assetPath).ToLower();
                    if (fileExtention == SDKConstants.GLTF || fileExtention == SDKConstants.GLB)
                    {
                        GameObject glb = null;

                        loadingAssetsCount++;
                        var cancellationToken = new CancellationTokenSource();
                        _loadingGLBS.Add(cancellationToken);

                        var glbLoader = GLBLoader.LoadGLTFFromURL(new Uri(assetPath).ToString(), _object.transform, cancellationToken);
                        yield return new WaitForTask(glbLoader);
                        loadingAssetsCount--;
                        glb = glbLoader.Result;

                        _loadingGLBS.Remove(cancellationToken);

                        if (glb != null)
                        {
                            Animation animation = null;

                            if (glb.GetComponent<Animation>() == null)
                            {
                                animation = glb.AddComponent<Animation>();
                            }
                            else
                            {
                                animation = glb.GetComponent<Animation>();
                            }

                            animation.playAutomatically = false;
                            if (asset != null && asset.animations != null && asset.animations.Count > 0)
                            {
                                animation.clip = null;
                            }

                            //glb.name = fileName;
                            glb.transform.SetParent(_object.transform, false);

                            //glb.transform.localPosition = Vector3.zero;

                            Camera[] cameras = glb.GetComponentsInChildren<Camera>();
                            foreach (var cam in cameras)
                            {
                                if (cam)
                                {
                                    cam.enabled = false;
                                    UnityEngine.Object.Destroy(cam.gameObject);
                                }
                            }
                            AdjustObjectScale(_object, ref collider);
                            /*if (assetGo.transform.Find("Root") != null)
                            {
                                UnityEngine.Object.Destroy(assetGo.transform.Find("Root"));
                            }*/
                            loadedAsset = glbParent;
                            //Debug.Log("objectloaded : " + _object.name + " type : GLB");

                            if (exception != null)
                            {
                                Debug.LogException(exception);
                            }
                        }
                        else if (!cancellationToken.IsCancellationRequested)
                        {
                            Debug.LogError(fileName + " not loaded");
                            UnityEngine.Object.Destroy(_object);
                            PresentationManager._slide.loadedCount++;
                        }

                    }
                    else if (fileExtention == SDKConstants.ASSETBUNDLE || fileExtention == SDKConstants.UNITYPACKAGE)
                    {
                        loadingAssetsCount++;
                        yield return AssetBundleManager.LoadAssetBundle(assetPath, (bundle) =>
                        {
                            loadingAssetsCount--;
                            if (bundle != null)
                            {
                                bundle.name = asset.FileName();
                                bundle.transform.SetParent(_object.transform, false);
                                AdjustObjectScale(_object, ref collider);
                                loadedAsset = glbParent;
                                //Debug.Log("objectloaded : " + bundle.name + " type : ASSETBUNDLE");
                            }
                            else
                            {
                                PresentationManager._slide.loadedCount++;
                                UnityEngine.Object.Destroy(_object);
                            }
                        });
                        yield return null;
                    }
                    break;

                case ANPAssetType.AUDIO:
                    loadingAssetsCount++;
                    request = Resources.LoadAsync<GameObject>("PrezAudioAsset");
                    yield return request;
                    GameObject _audio = (GameObject)UnityEngine.Object.Instantiate(request.asset);
                    _audio.name = fileName;
                    var audioSource = _audio.GetComponent<AudioSource>();
                    using (UnityWebRequest uwr = UnityWebRequestMultimedia.GetAudioClip(UriBuilderExtension.UriPath(assetPath), AudioType.UNKNOWN))
                    {
                        yield return uwr.SendWebRequest();
                        loadingAssetsCount--;
                        if (string.IsNullOrEmpty(uwr.error))
                        {
                            var clip = DownloadHandlerAudioClip.GetContent(uwr);
                            audioSource.volume = asset.volumn;
                            audioSource.clip = clip;
                            audioSource.Play();

                            audioClips.Add(clip);
                        }
                        else
                        {
                            Debug.Log("Audio error : " + uwr.error);
                        }
                    }

                    loadedAsset = _audio;
                    //Debug.Log("objectloaded : " + _audio.name + " type : AUDIO");

                    break;
            }
            bool isClickableEnabled = OnClickableActivate != null;
            if (collider != null)
            {
                collider.enabled = isClickableEnabled;
                if (isClickableEnabled)
                {
                    var clickableAsset = collider.gameObject.AddComponent<ClickableAsset>();
                    clickableAsset.Initialize(asset.clickTarget, OnClickableActivate, () => ClickableAssets.Remove(clickableAsset));
                    ClickableAssets.Add(clickableAsset);
                }
            }

            onLoaded(loadedAsset);
        }

        private static bool IsAssetClickable(string _clickTarget)
        {
            if (string.IsNullOrEmpty(_clickTarget))
            {
                return false;
            }
            if (string.IsNullOrWhiteSpace(_clickTarget))
            {
                return false;
            }
            if (_clickTarget.Equals("None"))
            {
                return false;
            }
            return true;
        }

        // ONLY used for GLB Re Scaling
        private static void AdjustObjectScale(GameObject glbObject, ref BoxCollider collider)
        {
            glbObject.SetActive(true);

            Bounds GLBBounds = CalculateLocalBounds(glbObject.transform);
            GLBBounds.center = GLBBounds.center / 2;
            GLBBounds.size = GLBBounds.size / 2;

            if (glbObject.GetComponent<BoxCollider>() == null)
            {
                collider = glbObject.AddComponent<BoxCollider>();
            }
            else
            {
                collider = glbObject.GetComponent<BoxCollider>();
            }

            float largest = Mathf.Max(GLBBounds.size.x, GLBBounds.size.y, GLBBounds.size.z) / defaultSizeFactor;
            if (largest != 0)
            {
                Transform child0 = glbObject.transform.GetChild(0);
                if (child0)
                {
                    child0.localScale /= largest;
                    collider.center = (GLBBounds.center / largest) * 2;
                    collider.size = (GLBBounds.size / largest) * 2;

                    if (glbObject != null)
                    {
                        foreach (Transform trans in glbObject.GetComponentsInChildren<Transform>())
                        {
                            trans.gameObject.layer = 0;
                        }
                    }
                }
            }
        }

        private static Vector3 Multiply(Vector3 one, Vector3 two)
        {
            one.x = one.x * two.x;
            one.y = one.y * two.y;
            one.z = one.z * two.z;
            return one;
        }

        internal static Bounds CalculateLocalBounds(Transform trans)
        {
            Quaternion currentRotation = trans.rotation;
            trans.rotation = Quaternion.Euler(0f, 0f, 0f);
            Bounds bounds = new Bounds(trans.position, Vector3.zero);
            foreach (Renderer renderer in trans.GetComponentsInChildren<Renderer>())
            {
                bounds.Encapsulate(renderer.bounds);
            }
            Vector3 localCenter = bounds.center - trans.position;
            bounds.center = localCenter;
            trans.rotation = currentRotation;
            return bounds;
        }

        private static IEnumerator HandleVideoPlayer(GameObject _video, string _assetPath, bool OnEnable)
        {
            VideoPlayer player = _video.GetComponent<VideoPlayer>();

            player.prepareCompleted += (vPlayer) =>
            {
                vPlayer.frame = loadVideoFrame;
                /*** fixing the on video aspect ratio issue ***/
                Vector3 _VPLocalScale = vPlayer.transform.localScale;
                _VPLocalScale.x = ((float)vPlayer.texture.width / (float)vPlayer.texture.width);
                _VPLocalScale.y = ((float)vPlayer.texture.height / (float)vPlayer.texture.width);
                vPlayer.transform.localScale = _VPLocalScale;
            };

            player.url = _assetPath;
            player.Prepare();
            while (!player.isPrepared)
            {
                yield return null;
            }

            if (player == null || string.IsNullOrEmpty(player.url)) yield return null;
            if (!audioChannelVideo) audioChannelVideo = player.GetComponent<AudioSource>();

            if (OnEnable)
            {
                audioChannelVideo.volume = 1;
            }
            else
            {
                player.frame = 0;
                player.Stop();
            }
        }

        static IEnumerator LoadImage(GameObject _gameObject, string url)
        {
            //Debug.Log("supports : " + SystemInfo.SupportsTextureFormat(TextureFormat.RGBA32));
            //Texture2D tex;
            //tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);

            using (UnityWebRequest webReq = UnityWebRequestTexture.GetTexture(UriBuilderExtension.UriPath(url)))
            {
                yield return webReq.SendWebRequest();
                if (webReq.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError(webReq.error + " url : " + url);
                }
                else
                {
                    var texture = DownloadHandlerTexture.GetContent(webReq);
                    if (_gameObject != null)
                    {
                        _gameObject.GetComponent<Renderer>().material.mainTexture = texture;
                        Vector3 _ImageLocalScale = _gameObject.transform.localScale;
                        _ImageLocalScale.x = ((float)texture.width / (float)texture.width) * (imageFator);
                        _ImageLocalScale.y = ((float)texture.height / (float)texture.width) * (imageFator);
                        _gameObject.transform.localScale = _ImageLocalScale;
                    }

                    textures.Add(texture);
                }
            }
        }

        internal static void OnAssetLoaded(ARPAsset _arpAsset, GameObject _objectLoaded)
        {
            if (loadComplete)
            {
                if (_arpAsset.type == ANPAssetType.AUDIO)
                {
                    _objectLoaded.GetComponent<AudioSource>().Play();
                    _objectLoaded.GetComponent<SpriteRenderer>().enabled = false;
                }
                else if (_arpAsset.type == ANPAssetType.VIDEO)
                {
                    VideoPlayer player = _objectLoaded.GetComponentInChildren<VideoPlayer>();
                    if (player)
                    {
                        player.frame = loadVideoFrame;
                        player.gameObject.GetComponent<AudioSource>().volume = _arpAsset.volumn;
                        player.Stop();
                        player.Play();
                    }
                }
                else if (_arpAsset.type == ANPAssetType.OBJECT)
                {
                }
            }
        }

    }
}
