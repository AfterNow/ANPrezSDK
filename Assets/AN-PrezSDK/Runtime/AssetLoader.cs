﻿using System;
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

public static class AssetLoader
{
    private static GameObject assetGo; // The relating objectLoaded that corresponds to the type of asset (Image, Video, Audio, GLTF).

    private static float imageFator = 0.4f;
    private static int loadVideoFrame = 1;
    private static float defaultSizeFactor = 1;

    static AudioSource audioChannelVideo;
    private static AnimationClip[] animationClips;
    //static GameObject objectLoaded;
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
                GameObject _text = (GameObject)UnityEngine.Object.Instantiate(request.asset);
                _text.name = txt.value;
                TextMeshPro tm = _text.GetComponentInChildren<TextMeshPro>();
                tm.text = txt.value;
                tm.font = txt.GetFontAsset();
                tm.alignment = txt.GetTMPAlignment();
                tm.color = PrezAssetHelper.GetColor(txt.color);
                tm.faceColor = tm.color;
                //yield return null;
                var collider = _text.AddComponent<BoxCollider>();
                collider.center = Vector3.zero;
                collider.size = new Vector3(collider.size.x, collider.size.y, 0.005f);
                //yield return null;

                onLoaded(_text);
                Debug.Log("objectLoaded : " + _text.name + " TYPE : TEXT");
                break;

            case ANPAssetType.IMAGE:
                request = Resources.LoadAsync<GameObject>("PrezImageAsset");
                yield return request;
                GameObject _image = (GameObject)UnityEngine.Object.Instantiate(request.asset);
                _image.name = fileName;

                // Load image in to the child of the loaded asset (that's the one which has 'MeshRenderer')
                CoroutineRunner.Instance.StartCoroutine(LoadImage(_image.transform.GetChild(0).gameObject, assetPath));
                onLoaded(_image);
                Debug.Log("objectLoaded : " + _image.name + " TYPE : IMAGE");
                break;

            case ANPAssetType.VIDEO:

                request = Resources.LoadAsync<GameObject>("PrezVideoAsset");
                yield return request;
                GameObject _video = (GameObject)UnityEngine.Object.Instantiate(request.asset);
                _video.name = fileName;

                if (PrezSDKManager.player == null)
                {
                    PrezSDKManager.player = _video.GetComponent<VideoPlayer>();
                }

                bool transVideo = Path.GetFileNameWithoutExtension(fileName).Substring(fileName.LastIndexOf('-') + 1).Equals("alpha");

                if (transVideo)
                {
                    PrezSDKManager.player.GetComponent<Renderer>().material = GameObject.FindObjectOfType<PrezSDKManager>().transMat;
                }
                PrezSDKManager.player.prepareCompleted += (vPlayer) =>
                {
                    vPlayer.frame = loadVideoFrame;
                    /*** fixing the on video aspect ratio issue ***/
                    Vector3 _VPLocalScale = vPlayer.transform.localScale;
                    _VPLocalScale.x = ((float)vPlayer.texture.width / (float)vPlayer.texture.width);
                    _VPLocalScale.y = ((float)vPlayer.texture.height / (float)vPlayer.texture.width);
                    vPlayer.transform.localScale = _VPLocalScale;
                };

                PrezSDKManager.player.url = assetPath;
                PrezSDKManager.player.Prepare();
                while (!PrezSDKManager.player.isPrepared)
                {
                    yield return null;
                }

                HandleVideoPlayer(true);
                PrezSDKManager.loadComplete = true;

                onLoaded(_video);
                Debug.Log("objectLoaded : " + _video.name + " TYPE : VIDEO");
                break;

            case ANPAssetType.OBJECT:
                request = Resources.LoadAsync<GameObject>("PrezObjectAsset");
                yield return request;
                assetGo = (GameObject)UnityEngine.Object.Instantiate(request.asset);
                assetGo.name = fileName;
                assetGo.gameObject.SetActive(true);

                bool IsGLBLoading = false;
                bool finishedAsync = false;
                Exception exception = null;

                string fileExtention = Path.GetExtension(assetPath).ToLower();
                if (fileExtention == SDKConstants.GLTF || fileExtention == SDKConstants.GLB)
                {
                    GameObject glb = null;

                    while (IsGLBLoading)
                    {
                        yield return null;
                    }

                    IsGLBLoading = true;
                    var glbLoader = GLBLoader.LoadGLTF(File.ReadAllBytes(assetPath), assetPath, assetGo.transform);
                    yield return new WaitForTask(glbLoader);

                    glb = glbLoader.Result;

                    if (glb == null)
                    {
                        Debug.LogError("GLB not loaded");
                        yield return null;
                    }

                    IsGLBLoading = false;
                    finishedAsync = true;

                    glb.name = fileName;
                    glb.transform.SetParent(assetGo.transform, false);


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
                    AdjustObjectScale(glb);
                    /*if (assetGo.transform.Find("Root") != null)
                    {
                        UnityEngine.Object.Destroy(assetGo.transform.Find("Root"));
                    }*/
                    onLoaded(assetGo);
                    Debug.Log("objectLoaded : " + assetGo.name + " TYPE : GLB");

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
                        bundle.name = asset.FileName();
                        onLoaded(bundle);
                        Debug.Log("objectLoaded : " + bundle.name + " TYPE : ASSETBUNDLE");
                    });
                }
                break;

            case ANPAssetType.AUDIO:
                request = Resources.LoadAsync<GameObject>("PrezAudioAsset");
                yield return request;
                GameObject _audio = (GameObject)UnityEngine.Object.Instantiate(request.asset);
                _audio.name = fileName;
                var audioSource = _audio.GetComponent<AudioSource>();
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

                onLoaded(_audio);
                Debug.Log("objectLoaded : " + _audio.name + " TYPE : AUDIO");

                break;
        }
        yield return null;
    }

    // ONLY used for GLB Re Scaling
    private static void AdjustObjectScale(GameObject glbObject)
    {
        assetGo.SetActive(true);

        Bounds GLBBounds = CalculateLocalBounds(assetGo.transform);
        BoxCollider GLBBoxCollider = assetGo.AddComponent<BoxCollider>();

        float largest = Mathf.Max(GLBBounds.size.x, GLBBounds.size.y, GLBBounds.size.z) / defaultSizeFactor;
        if (largest != 0)
        {
            Transform child0 = assetGo.transform.GetChild(0);
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
    private static void HandleVideoPlayer(bool OnEnable)
    {
        if (PrezSDKManager.player == null || string.IsNullOrEmpty(PrezSDKManager.player.url)) return;
        if (!audioChannelVideo) audioChannelVideo = PrezSDKManager.player.GetComponent<AudioSource>();

        if (OnEnable)
        {
            audioChannelVideo.volume = 1;
        }
        else
        {
            PrezSDKManager.player.frame = 0;
            PrezSDKManager.player.Stop();
        }
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
    }

}
