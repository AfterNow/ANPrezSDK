using AfterNow.PrezSDK.Internal.Helpers;
using AfterNow.PrezSDK.Internal.Views;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AfterNow.PrezSDK
{
    internal class PresentationManager : MonoBehaviour
    {
        internal static LoadedSlide _slide;
        internal Location _location;
        internal static Dictionary<int, LoadedSlide> _slides;

        internal static PresentationManager _instance;

        internal static Action<ANPAnimation> callback;


        internal static List<ARPTransition> assetTransitions;
        internal static List<ARPAsset> assets;
        private static ItemTransform itemTransform;
        internal static Vector3 initialPos;
        internal static Quaternion initialRot;
        internal static Vector3 initialScale;
        internal static Dictionary<ARPAsset, GameObject> loadedObjects = new Dictionary<ARPAsset, GameObject>();
        internal static float totalLength = 3f;

        private void Awake()
        {
            _instance = this;
        }

        internal void Init(Location location)
        {
            _location = location;
            _slides = new Dictionary<int, LoadedSlide>();
        }

        internal LoadedSlide LoadSlide(int index)
        {
            if (index == 0)
            {
                _slides.Clear();
            }

            if (!_slides.TryGetValue(index, out LoadedSlide slide))
            {
                _slide = slide;
                _slide = new LoadedSlide(_location.slides[index], transform);
                _slides[index] = _slide;
            }
            else if (_slides.TryGetValue(index, out LoadedSlide loadedSlide))
            {
                _slide = loadedSlide;
            }

            _slide.LoadSlide();

            assetTransitions = _slide.Slide.assetTransitions;
            return _slide;
        }

        internal LoadedSlide GetSlideReference(int i)
        {
            if (!_slides.TryGetValue(i, out LoadedSlide slide))
                return null;
            return slide;
        }


        private void OnDestroy()
        {
            PrezAPIHelper.StopDownload();
        }

        internal class LoadedSlide
        {
            internal Slide Slide;
            internal Dictionary<ARPAsset, LoadedAsset> _assets;
            internal int loadedCount = 0;

            internal bool HasSlideLoaded => loadedCount == _assets.Count;

            internal LoadedSlide(Slide slide, Transform anchor)
            {
                //Debug.Log("slide id : " + slide.id);
                assetTransitions = slide.assetTransitions;

                _assets = new Dictionary<ARPAsset, LoadedAsset>();

                foreach (var asset in slide.assets)
                {
                    _assets[asset] = new LoadedAsset(asset, anchor, () =>
                    {
                        loadedCount++;
                    });
                }

                Slide = slide;
            }

            internal void LoadSlide()
            {
                //Debug.Log("Slidedata slidename " + Slide.name);
                foreach (var asset in _assets)
                {
                    asset.Value.LoadAsset();
                }
            }

            internal void CleanUp()
            {
                DestroyLoadedObjects();
                FindObjectOfType<PrezSDKManager>().ClearObjects();
                AssetBundleManager.Cleanup();

                foreach (Transform child in _instance.transform)
                {
                    Destroy(child.gameObject);
                }
                loadedCount = 0;

                //Dispose glb models
                GLBLoader.DisposeGltf();

                //Dispose Textures
                DisposeTextures();

                //Dispose Audioclips
                DisposeAudioClips();

                Resources.UnloadUnusedAssets();
            }

            internal void DestroyLoadedObjects()
            {
                foreach (var asset in _assets)
                {
                    asset.Value.CleanUp();
                }
            }

            internal void ShowAssets()
            {
                foreach (var asset in _assets)
                {
                    asset.Value.ShowAsset();
                }
            }

            internal class LoadedAsset
            {
                internal ARPAsset _asset;
                private GameObject _loadedObject;
                private Transform _anchor;
                private Action _onLoaded;

                private bool IsFileDownloaded => System.IO.File.Exists(_asset.AbsoluteDownloadPath(InitializeSDK.DownloadFolderPath));

                internal LoadedAsset(ARPAsset asset, Transform anchor, Action OnLoaded)
                {
                    _asset = asset;
                    _anchor = anchor;
                    _onLoaded = OnLoaded;
                }

                internal void LoadAsset()
                {
                    CoroutineRunner.Instance.StartCoroutine(LoadAssetInternal());
                }


                internal IEnumerator LoadAssetInternal()
                {
                    //Debug.Log("Slidedata assetname " + _asset.FileName());

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
                            _loadedObject.SetInitialTransform(_asset.itemTransform);
                            _asset.itemTransform.SetTransform(_loadedObject.transform);
                            _loadedObject.SetActive(false);
                            _onLoaded?.Invoke();

                            if (_asset.type == ANPAssetType.TEXT)
                            {
                                //FindObjectOfType<PrezSDKManager>().prezAssets.Add(_asset.text.value, _loadedObject);
                                if (!PrezSDKManager.prezAssets.ContainsKey(_asset.text.value))
                                {
                                    PrezSDKManager.prezAssets.Add(_asset.text.value, _loadedObject);
                                }
                            }
                            else
                            {
                                //FindObjectOfType<PrezSDKManager>().prezAssets.Add(_asset.FileName(), _loadedObject);
                                if (!PrezSDKManager.prezAssets.ContainsKey(_asset.FileName()))
                                {
                                    PrezSDKManager.prezAssets.Add(_asset.FileName(), _loadedObject);
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

                internal void ShowAsset()
                {
                    _loadedObject.SetActive(true);
                }

                internal void CleanUp()
                {
                    if (_loadedObject)
                    {
                        DestroyImmediate(_loadedObject);
                    }
                }
            }
        }

        internal static void SetInitialTransform()
        {
            initialPos = new Vector3(itemTransform.position.x, itemTransform.position.y, itemTransform.position.z);
            initialRot = new Quaternion(itemTransform.rotation.x, itemTransform.rotation.y, itemTransform.rotation.z, itemTransform.rotation.w);
            initialScale = new Vector3(itemTransform.localScale.x, itemTransform.localScale.y, itemTransform.localScale.z);

        }

        static void DisposeTextures()
        {
            foreach (var texture in AssetLoader.textures)
            {
                DestroyImmediate(texture, true);
            }
        }

        static void DisposeAudioClips()
        {
            foreach (var audioClip in AssetLoader.audioClips)
            {
                DestroyImmediate(audioClip, true);
            }
        }

        internal int GetSlideIndexFromId(string slideId)
        {
            for (int i = 0; i < _location.slides.Count; i++)
            {
                var slide = _location.slides[i];
                if (slide.id == slideId)
                {
                    return i;
                }
            }
            return -1;
        }
    }
}