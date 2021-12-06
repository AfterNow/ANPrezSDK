using AfterNow.AnPrez.SDK.Internal;
using AfterNow.AnPrez.SDK.Internal.Views;
using Siccity.GLTFUtility;
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
        private static float imageFator = 0.4f;
        private static int loadVideoFrame = 1;

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
                    TextMeshPro tm = go.GetComponent<TextMeshPro>();
                    tm.text = txt.value;
                    tm.font = txt.GetFontAsset();
                    tm.alignment = txt.GetTMPAlignment();
                    tm.color = PrezAssetHelper.GetColor(txt.color);
                    tm.faceColor = tm.color;
                    onLoaded(go);
                    break;

                case ANPAssetType.IMAGE:
                    request = Resources.LoadAsync<GameObject>("PrezImageAsset");
                    yield return request;
                    go = (GameObject)UnityEngine.Object.Instantiate(request.asset);
                    go.name = fileName;
                    CoroutineRunner.Instance.StartCoroutine(LoadImage(go, assetPath));
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
                    bool IsGLBLoading = false;
                    bool finishedAsync = false;
                    Exception exception = null;

                    string fileExtention = Path.GetExtension(assetPath).ToLower();
                    if (fileExtention == SDKConstants.GLTF || fileExtention == SDKConstants.GLB)
                    {
                        Importer.ImportGLBAsync(File.ReadAllBytes(assetPath), new ImportSettings() { useLegacyClips = true }, (obj, clips) =>
                        {
                            IsGLBLoading = false;
                            finishedAsync = true;

                            go = obj;

                            go.name = fileName;
                            go.transform.SetParent(GameObject.FindObjectOfType<PresentationManager>().transform);
                            if (go.transform.Find("Root") != null)
                            {
                                UnityEngine.Object.Destroy(go.transform.Find("Root"));
                            }

                            animationClips = clips;

                            onLoaded(go);
                        }, null,
                            (ex) =>
                            {
                                exception = ex;
                                finishedAsync = true;
                            });

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
