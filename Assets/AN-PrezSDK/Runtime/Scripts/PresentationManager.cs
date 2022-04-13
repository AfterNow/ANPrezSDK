using AfterNow.PrezSDK.Internal.Helpers;
using AfterNow.PrezSDK.Internal.Views;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace AfterNow.PrezSDK
{
    /// <summary>
    /// Class responsible for managing the presentation loaded by a user. This is an internal script added in the scene in runtime.
    /// User does not have to interact with it.
    /// </summary>
    internal class PresentationManager : MonoBehaviour
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

        public LoadedSlide GetSlideReference(int i)
        {
            if (!_slides.TryGetValue(i, out LoadedSlide slide))
                return null;
            return slide;
        }


        private void OnDestroy()
        {
            PrezAPIHelper.StopDownload();

            //delete files
            PrezSDKManager.DeleteDownloadedFiles();
        }

        public class LoadedSlide
        {
            public Slide Slide;
            public Dictionary<ARPAsset, LoadedAsset> _assets;
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
                    });
                }

                Slide = slide;
            }

            public void LoadSlide()
            {
                //Debug.Log("Slidedata slidename " + Slide.name);
                foreach (var asset in _assets)
                {
                    asset.Value.LoadAsset();
                }
            }

            /// <summary>
            /// This method handles cleanup of the slide including destroying the loaded assets, cleaning up assetbundles, disposing models, textures etc.
            /// </summary>
            public void CleanUp()
            {
                DestroyLoadedObjects();
                FindObjectOfType<PrezSDKManager>().ClearObjects();

                //Clean up assetbundles
                AssetBundleManager.Cleanup();

                //Destroy the gameobjects of loaded assets of a slide
                foreach (Transform child in _instance.transform)
                {
                    Destroy(child.gameObject);
                }

                //Reset the loaded count to 0
                loadedCount = 0;

                //Dispose glb models
                GLBLoader.DisposeGltf();

                //Dispose Textures
                DisposeTextures();

                //Dispose Audioclips
                DisposeAudioClips();

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

            public class LoadedAsset
            {
                public ARPAsset _asset;
                private GameObject _loadedObject;
                private Transform _anchor;
                private Action _onLoaded;

                private bool IsFileDownloaded => File.Exists(_asset.AbsoluteDownloadPath(InitializeSDK.DownloadFolderPath));

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
                    //Debug.Log("Slidedata assetname " + _asset.FileName());

                    if (_asset.type != ANPAssetType.TEXT && !IsFileDownloaded)
                    {
                        string replacement = null;

                        if (_asset.type == ANPAssetType.OBJECT)
                        {
                            if (_asset.url.Contains(".unitypackage"))
                            {
                                replacement = AssetHelper.ReplacementString();
                            }
                        }
                        var task = _asset.DownloadAsset(replacement);
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

                public void ShowAsset()
                {
                    _loadedObject.SetActive(true);
                }

                public void CleanUp()
                {
                    if (_loadedObject)
                    {
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

        /// <summary>
        /// Dispose the textures of the loaded assets. This is called when the slide is unloaded.
        /// </summary>
        static void DisposeTextures()
        {
            foreach (var texture in AssetLoader.textures)
            {
                DestroyImmediate(texture, true);
            }
        }

        /// <summary>
        /// Dispose the audio clips of the loaded assets. This is called when the slide is unloaded.
        /// </summary>
        static void DisposeAudioClips()
        {
            foreach (var audioClip in AssetLoader.audioClips)
            {
                DestroyImmediate(audioClip, true);
            }
        }
    }
}