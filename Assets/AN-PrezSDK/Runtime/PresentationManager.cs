using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class PresentationManager : MonoBehaviour
{
    public static LoadedSlide _slide;
    public Location _location;
    public static Dictionary<int, LoadedSlide> _slides;

    public static PresentationManager _instance;

    public static Action<ANPAnimation> callback;


    public static List<ARPTransition> assetTransitions;
    public static List<ARPAsset> assets;
    private static ItemTransform itemTransform;
    public static Vector3 initialPos;
    public static Quaternion initialRot;
    public static Vector3 initialScale;
    public static Dictionary<ARPAsset, GameObject> loadedObjects = new Dictionary<ARPAsset, GameObject>();
    public static float totalLength = 3f;

    public delegate void OnSlideLoaded();
    public static event OnSlideLoaded onSlideLoaded;


    public delegate void OnObjectsDestroyed();
    public static event OnObjectsDestroyed onObjectsDestroyed;


    private void Awake()
    {
        _instance = this;
    }

    public void Init(Location location)
    {
        _location = location;
        _slides = new Dictionary<int, LoadedSlide>();
    }

    public LoadedSlide LoadSlide(int index)
    {
        Debug.Log("PresentationManager LoadSlide " + index);

        if (!_slides.TryGetValue(index, out LoadedSlide slide))
        {
            _slide = slide;
            _slide = new LoadedSlide(_location.slides[index], transform);
            _slides[index] = _slide;
        }
        _slide.LoadSlide();

        assetTransitions = _slide.Slide.assetTransitions;
        Debug.Log("slide name : " + _slide.Slide.name);
        Debug.Log("assetTransitions.Count " + assetTransitions.Count);
        //StartCoroutine(InternalLoadSlide(slide));
        return _slide;
    }

    public LoadedSlide GetSlideReference(int i)
    {
        if (!_slides.TryGetValue(i, out LoadedSlide slide))
            return null;
        return slide;
    }
    private IEnumerator InternalLoadSlide(LoadedSlide slide)
    {
        while (!slide.HasSlideLoaded)
        {
            yield return null;
        }
        slide.ShowAssets();
    }


    private void OnDestroy()
    {
        Resources.UnloadUnusedAssets();
        GC.Collect();
        PrezAPIHelper.StopDownload();

        //delete files
        DirectoryInfo directoryInfo = new DirectoryInfo(InitializeSDK.DownloadFolderPath);

        foreach (var item in directoryInfo.EnumerateFiles())
        {
            item.Delete();
        }

        foreach (var item in directoryInfo.EnumerateDirectories())
        {
            item.Delete(true);
        }

    }

    public class LoadedSlide
    {
        public Slide Slide;
        private Dictionary<ARPAsset, LoadedAsset> _assets;
        public int loadedCount = 0;

        public bool HasSlideLoaded => loadedCount == _assets.Count;

        public LoadedSlide(Slide slide, Transform anchor)
        {
            //Debug.Log("slide id : " + slide.id);
            assetTransitions = slide.assetTransitions;

            _assets = new Dictionary<ARPAsset, LoadedAsset>();

            foreach (var asset in slide.assets)
            {
                _assets[asset] = new LoadedAsset(asset, anchor, () =>
                {
                    loadedCount++;
                    Debug.Log("loadedCount " + loadedCount + " assetsCount " + _assets.Count);
                });
            }

            Slide = slide;
        }

        public void LoadSlide()
        {
            foreach (var asset in _assets)
            {
                asset.Value.LoadAsset();
            }
        }

        public void CleanUp()
        {
            DestroyLoadedObjects();
            FindObjectOfType<PrezSDKManager>().ClearObjects();
            onObjectsDestroyed();
            GC.Collect();
            Resources.UnloadUnusedAssets();
            AssetBundleManager.Cleanup();

            foreach (Transform child in _instance.transform)
            {
                Destroy(child.gameObject);
            }
            loadedCount = 0;
        }

        public void DestroyLoadedObjects()
        {
            foreach (var asset in _assets)
            {
                asset.Value.CleanUp();
            }
        }

        public void ShowAssets()
        {
            foreach (var asset in _assets)
            {
                asset.Value.ShowAsset();
            }
        }

        private class LoadedAsset
        {
            private ARPAsset _asset;
            private GameObject _loadedObject;
            private Transform _anchor;
            private Action _onLoaded;

            private bool IsFileDownloaded => System.IO.File.Exists(_asset.AbsoluteDownloadPath(InitializeSDK.DownloadFolderPath));

            public LoadedAsset(ARPAsset asset, Transform anchor, Action OnLoaded)
            {
                _asset = asset;
                _anchor = anchor;
                _onLoaded = OnLoaded;
            }

            public void LoadAsset()
            {
                CoroutineRunner.Instance.StartCoroutine(LoadAssetInternal());
            }


            public IEnumerator LoadAssetInternal()
            {
                if (_asset.type != ANPAssetType.TEXT && !IsFileDownloaded)
                {
                    string replacement = null;

                    if (_asset.type == ANPAssetType.OBJECT)
                    {
                        if (_asset.url.Contains(".unitypackage"))
                        {
                            replacement = PrezAssetHelper.ReplacementString();
                        }
                    }
                    var task = PrezWebCalls.DownloadAsset(_asset, replacement);
                    yield return new WaitForTask(task);
                }

                yield return AssetLoader.OnLoadAsset(_asset, (go) =>
                {
                    if (go != null)
                    {
                        _loadedObject = go;
                        _loadedObject.transform.SetParent(_anchor);
                        _loadedObject.SetInitialPosition(_asset.itemTransform);
                        _asset.itemTransform.SetTransform(_loadedObject.transform);
                        _loadedObject.SetActive(false);
                        _onLoaded?.Invoke();

                        if (_asset.type == ANPAssetType.TEXT)
                        {
                            //FindObjectOfType<PrezSDKManager>().prezAssets.Add(_asset.text.value, _loadedObject);
                            if (!PrezSDKManager.uDictionaryExample.prezAssets.ContainsKey(_asset.text.value))
                            {
                                PrezSDKManager.uDictionaryExample.prezAssets.Add(_asset.text.value, _loadedObject);
                            }
                        }
                        else
                        {
                            //FindObjectOfType<PrezSDKManager>().prezAssets.Add(_asset.FileName(), _loadedObject);
                            if (!PrezSDKManager.uDictionaryExample.prezAssets.ContainsKey(_asset.FileName()))
                            {
                                PrezSDKManager.uDictionaryExample.prezAssets.Add(_asset.FileName(), _loadedObject);
                            }
                        }

                        if (!loadedObjects.ContainsKey(_asset))
                        {
                            loadedObjects.Add(_asset, _loadedObject);
                        }
                    }
                    else
                    {
                        Debug.LogError("go is null");
                    }
                });

            }

            public void ShowAsset()
            {
                _loadedObject.SetActive(true);
            }

            public void CleanUp()
            {
                if (_loadedObject)
                {
                    Debug.Log("destroying " + _loadedObject.name);
                    DestroyImmediate(_loadedObject);
                }
            }
        }
    }

    public static void SetInitialTransform()
    {
        initialPos = new Vector3(itemTransform.position.x, itemTransform.position.y, itemTransform.position.z);
        initialRot = new Quaternion(itemTransform.rotation.x, itemTransform.rotation.y, itemTransform.rotation.z, itemTransform.rotation.w);
        initialScale = new Vector3(itemTransform.localScale.x, itemTransform.localScale.y, itemTransform.localScale.z);

    }

}

