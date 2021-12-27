using AfterNow.AnPrez.SDK.Internal;
using AfterNow.AnPrez.SDK.Internal.Views;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Playables;
using UnityEngine.Video;

namespace AfterNow.AnPrez.SDK.Unity
{
    public static class AssetLoader
    {
        private static GameObject assetGO; // The relating GO that corresponds to the type of asset (Image, Video, Audio, GLTF).
        public static GameObject GLTF; // This is the GLB asset ref from the asset prefab.

        private static float imageFator = 0.4f;
        private static int loadVideoFrame = 1;
        private static float defaultSizeFactor = 1;

        static VideoPlayer player;
        static AudioSource audioChannelVideo;
        private static AnimationClip[] animationClips;
        static GameObject go;
        private static string assetname;
        private static PlayableDirector director;

        public static IEnumerator OnLoadAsset(ARPAsset asset, Action<GameObject> onLoaded)
        {

            string assetPath = asset.type != ANPAssetType.TEXT ? asset.AbsoluteDownloadPath(InitializeSDK.DownloadFolderPath) : null;
            string fileName = Path.GetFileName(assetPath);

            switch (asset.type)
            {
                case ANPAssetType.TEXT:
                    ARPText txt = asset.text;
                    var request = Resources.LoadAsync<GameObject>("PrezTextAsset");
                    yield return request;
                    go = (GameObject)UnityEngine.Object.Instantiate(request.asset);
                    go.name = txt.value;
                    TextMeshPro tm = go.GetComponentInChildren<TextMeshPro>();
                    tm.text = txt.value;
                    tm.font = txt.GetFontAsset();
                    tm.alignment = txt.GetTMPAlignment();
                    tm.color = PrezAssetHelper.GetColor(txt.color);
                    tm.faceColor = tm.color;
                    //yield return null;
                    var collider = go.AddComponent<BoxCollider>();
                    collider.center = Vector3.zero;
                    collider.size = new Vector3(collider.size.x, collider.size.y, 0.005f);
                    //yield return null;
                    onLoaded(go);
                    break;

                case ANPAssetType.IMAGE:
                    request = Resources.LoadAsync<GameObject>("PrezImageAsset");
                    yield return request;
                    go = (GameObject)UnityEngine.Object.Instantiate(request.asset);
                    go.name = fileName;

                    // Load image in to the child of the loaded asset (that's the one which has 'MeshRenderer')
                    CoroutineRunner.Instance.StartCoroutine(LoadImage(go.transform.GetChild(0).gameObject, assetPath));
                    onLoaded(go);
                    break;

                case ANPAssetType.VIDEO:
                    request = Resources.LoadAsync<GameObject>("PrezVideoAsset");
                    yield return request;
                    go = (GameObject)UnityEngine.Object.Instantiate(request.asset);
                    go.name = fileName;
                    player = go.GetComponent<VideoPlayer>();
                    player.url = assetPath;
                    player.prepareCompleted += (vPlayer) =>
                    {
                        vPlayer.frame = loadVideoFrame;
                        /*** fixing the on video aspect ratio issue ***/
                        Vector3 _VPLocalScale = vPlayer.transform.localScale;
                        _VPLocalScale.x = ((float)vPlayer.texture.width / (float)vPlayer.texture.width);
                        _VPLocalScale.y = ((float)vPlayer.texture.height / (float)vPlayer.texture.width);
                        vPlayer.transform.localScale = _VPLocalScale;
                    };
                    player.Stop();
                    player.frame = player.frameCount > 200 ? 200 : (long)(player.frameCount - 1);
                    onLoaded(go);
                    break;

                case ANPAssetType.OBJECT:
                    request = Resources.LoadAsync<GameObject>("PrezObjectAsset");
                    yield return request;
                    GLTF = (GameObject)UnityEngine.Object.Instantiate(request.asset);

                    assetGO = GLTF;
                    assetGO.gameObject.SetActive(true);
                    
                    bool IsGLBLoading = false;
                    bool finishedAsync = false;
                    Exception exception = null;

                    string fileExtention = Path.GetExtension(assetPath).ToLower();
                    if (fileExtention == SDKConstants.GLTF || fileExtention == SDKConstants.GLB)
                    {
                        assetGO.gameObject.SetActive(true);

                        GameObject glb = null;

                        while (IsGLBLoading)
                        {
                            yield return null;
                        }
                        IsGLBLoading = true;
                        var glbLoader = GLBLoader.LoadGLTF(File.ReadAllBytes(assetPath), assetPath, assetGO.transform);
                        yield return new WaitForTask(glbLoader);
                        glb = glbLoader.Result;
                        IsGLBLoading = false;
                        finishedAsync = true;

                        glb.name = fileName;
                        glb.transform.SetParent(assetGO.transform, false);
                        glb.transform.localPosition = Vector3.zero;

                        Camera[] cameras = glb.GetComponentsInChildren<Camera>();
                        foreach (var cam in cameras)
                        {
                            if (cam)
                            {
                                cam.enabled = false;
                                UnityEngine.Object.Destroy(cam.gameObject);
                            }
                        }
                        AdjustObjectScale(glb);
                        if (assetGO.transform.Find("Root") != null)
                        {
                            UnityEngine.Object.Destroy(assetGO.transform.Find("Root"));
                        }
                        onLoaded(assetGO);

                        /*Importer.ImportGLBAsync(File.ReadAllBytes(assetPath), new ImportSettings() { useLegacyClips = true }, (obj, clips) =>
                        {
                            IsGLBLoading = false;
                            finishedAsync = true;

                            glb = obj;

                            glb.name = fileName;
                            assetGO = new GameObject();
                            assetGO.name = glb.name;
                            glb.transform.SetParent(assetGO.transform, false);
                            glb.transform.localPosition = Vector3.zero;

                            Camera[] cameras = glb.GetComponentsInChildren<Camera>();
                            foreach (var cam in cameras)
                            {
                                if (cam)
                                {
                                    cam.enabled = false;
                                    UnityEngine.Object.Destroy(cam.gameObject);
                                }
                            }
                            AdjustObjectScale(glb);

                            if (assetGO.transform.Find("Root") != null)
                            {
                                UnityEngine.Object.Destroy(assetGO.transform.Find("Root"));
                            }

                            animationClips = clips;

                            onLoaded(assetGO);
                        }, null,
                            (ex) =>
                            {
                                exception = ex;
                                finishedAsync = true;
                            });*/

                        if (exception != null)
                        {
                            Debug.LogException(exception);
                        }

                        while (IsGLBLoading)
                        {
                            yield return null;
                        }
                    }
                    else if (fileExtention == SDKConstants.ASSETBUNDLE || fileExtention == SDKConstants.UNITYPACKAGE)
                    {
                        yield return AssetBundleManager.LoadAssetBundle(assetPath, (bundle) =>
                        {
                            onLoaded(bundle);
                        });
                    }
                    break;

                case ANPAssetType.AUDIO:
                    request = Resources.LoadAsync<GameObject>("PrezAudioAsset");
                    yield return request;
                    go = (GameObject)UnityEngine.Object.Instantiate(request.asset);
                    go.name = fileName;
                    var audioSource = go.GetComponent<AudioSource>();
                    using (UnityWebRequest uwr = UnityWebRequestMultimedia.GetAudioClip(assetPath, AudioType.UNKNOWN))
                    {
                        yield return uwr.SendWebRequest();
                        if (string.IsNullOrEmpty(uwr.error))
                        {
                            var clip = DownloadHandlerAudioClip.GetContent(uwr);
                            audioSource.clip = clip;
                            audioSource.Play();
                        }
                        else
                        {
                            Debug.Log(uwr.error);
                        }
                    }
                    onLoaded(go);

                    break;
            }
            yield return null;
        }

        // ONLY used for GLB Re Scaling
        private static void AdjustObjectScale(GameObject glbObject)
        {
            assetGO.SetActive(true);

            Bounds GLBBounds = CalculateLocalBounds(assetGO.transform);
            BoxCollider GLBBoxCollider = assetGO.AddComponent<BoxCollider>();

            float largest = Mathf.Max(GLBBounds.size.x, GLBBounds.size.y, GLBBounds.size.z) / defaultSizeFactor;
            if (largest != 0)
            {
                Transform child0 = assetGO.transform.GetChild(0);
                if (child0)
                {
                    child0.localScale /= largest;
                    GLBBoxCollider.center = (GLBBounds.center / largest) * 2;
                    GLBBoxCollider.size = (GLBBounds.size / largest) * 2;
                   /* Vector3 defaultSize = new Vector3(defaultSizeFactor / largest, defaultSizeFactor / largest, defaultSizeFactor / largest) / 2;
                    child0.localScale = defaultSize;
                    GLBBoxCollider.center = Multiply(GLBBounds.center, defaultSize);
                    GLBBoxCollider.size = Multiply(GLBBounds.size, defaultSize) * 2;*/

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

        public static Bounds CalculateLocalBounds(Transform trans)
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

        static IEnumerator LoadImage(GameObject _gameObject, string url)
        {
            //Debug.Log("supports : " + SystemInfo.SupportsTextureFormat(TextureFormat.RGBA32));
            //Texture2D tex;
            //tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);

            using (UnityWebRequest webReq = UnityWebRequestTexture.GetTexture("file://" + url))
            {
                yield return webReq.SendWebRequest();
                if (webReq.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError(webReq.error + " url : " + url);
                }
                else
                {
                    var texture = DownloadHandlerTexture.GetContent(webReq);
                    _gameObject.GetComponent<Renderer>().material.mainTexture = texture;
                    Vector3 _ImageLocalScale = _gameObject.transform.localScale;
                    _ImageLocalScale.x = ((float)texture.width / (float)texture.width) * (imageFator);
                    _ImageLocalScale.y = ((float)texture.height / (float)texture.width) * (imageFator);
                    _gameObject.transform.localScale = _ImageLocalScale;
                }
            }

            /*using (WWW www = new WWW(url))
            {
                yield return www;
                if (www.error != null)
                {
                    Debug.LogError("Error : " + www.error);
                }
                else
                {
                    www.LoadImageIntoTexture(tex);
                    Vector3 _ImageLocalScale = _gameObject.transform.localScale;
                    _ImageLocalScale.x = ((float)tex.width / (float)tex.width) * (imageFator);
                    _ImageLocalScale.y = ((float)tex.height / (float)tex.width) * (imageFator);
                    _gameObject.transform.localScale = _ImageLocalScale;
                    _gameObject.GetComponent<Renderer>().material.mainTexture = tex;
                }
            }*/
        }

    }
}
