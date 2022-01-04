using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

    public class PresentationManager : MonoBehaviour
    {
        public static LoadedSlide _slide;
        public Location _location;
        public Dictionary<int, LoadedSlide> _slides;

        public static PresentationManager _instance;

        //public static Action<ANPAnimation> callback;


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

        /*public void CleanUp()
        {
            StartCoroutine(InternalCleanup());
        }

        private IEnumerator InternalCleanup()
        {
            foreach (var slide in _slides)
            {
                if (slide.Value != null)
                {
                    slide.Value.CleanUp();
                }
            }
            yield return null;
            Resources.UnloadUnusedAssets();
            GC.Collect();
        }*/

        public LoadedSlide LoadSlide(int index)
        {
            if (!_slides.TryGetValue(index, out LoadedSlide slide))
            {
                _slide = slide;
                _slide = new LoadedSlide(_location.slides[index], transform);
                _slides[index] = _slide;
            }
            _slide.LoadSlide();

            assetTransitions = _slide.Slide.assetTransitions;

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
            /*foreach (var file in Directory.EnumerateFiles(InitializeSDK.DownloadFolderPath))
            {
                System.IO.File.Delete(file);
            }*/
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
            private int loadedCount = 0;

            public bool HasSlideLoaded => loadedCount == _assets.Count;

            public LoadedSlide(Slide slide, Transform anchor)
            {
                //Debug.Log("slide id : " + slide.id);
                assetTransitions = slide.assetTransitions;

                _assets = new Dictionary<ARPAsset, LoadedAsset>();
                foreach (var asset in slide.assets)
                {
                    _assets[asset] = new LoadedAsset(asset, anchor, () => { loadedCount++; });
                }

                //Debug.Log("slidetransitionanimation " + slide.transition.animation);
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
                foreach (var asset in _assets)
                {
                    asset.Value.CleanUp();
                }
                GC.Collect();
                Resources.UnloadUnusedAssets();
                AssetBundleManager.Cleanup();
                foreach (Transform child in _instance.transform)
                {
                    Destroy(child.gameObject);
                }
                loadedCount = 0;
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
                            //Debug.Log("go loaded " + go.name);
                            _loadedObject = go;
                            _loadedObject.transform.SetParent(_anchor);
                            //_loadedObject.transform.localPosition = Vector3.zero;

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

                                    //Debug.Log("Adding " + "key : " + _asset.text.value + " " + "value : " + _loadedObject.name);
                                }
                            }
                            else
                            {
                                //FindObjectOfType<PrezSDKManager>().prezAssets.Add(_asset.FileName(), _loadedObject);
                                if (!PrezSDKManager.uDictionaryExample.prezAssets.ContainsKey(_asset.FileName()))
                                {
                                    PrezSDKManager.uDictionaryExample.prezAssets.Add(_asset.FileName(), _loadedObject);

                                    //Debug.Log("Adding " + "key : " + _asset.FileName() + " " + "value : " + _loadedObject.name);
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

                    //onSlideLoaded();
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

        public static void DoRegularAnimation(GameObject go, ARPAsset asset, ARPTransition transition, bool skipToEnd, float _delay, float _animationDuration)
        {
            //Debug.Log("go : " + go.name);
            //Debug.Log("transition : " + transition.animation);

            _delay = skipToEnd ? 0 : _delay;
            _animationDuration = skipToEnd ? 0 : transition.animationDuration;
            AnimationStartType animationStartType = transition.startType;

            itemTransform = asset.GetItemTransform();
            SetInitialTransform();

            switch (transition.animation)
            {
                case AnimationType.None:
                    // Shouldn't ever be able to reach here
                    Debug.LogError("Animation is none, this shouldn't be possible");
                    break;
                case AnimationType.Appear:
                    if (skipToEnd)
                    {
                        Complete();
                    }
                    else
                    {
                        LeanTween.value(go, 0, 1, totalLength - _delay).setOnComplete(Complete);
                    }
                    break;
                case AnimationType.FadeIn:
                    break;
                case AnimationType.ScaleIn:
                    if (!skipToEnd)
                    {
                        go.transform.localScale = Vector3.zero;
                    }

                    LeanTween.scale(go, initialScale, _animationDuration).setOnComplete(() =>
                    {
                        if (skipToEnd)
                        {
                            Complete();
                        }
                    });

                    if (!skipToEnd)
                    {
                        LeanTween.value(go, 0, 1, totalLength - _delay).setOnComplete(Complete);
                    }
                    break;
                case AnimationType.BlurIn:
                    Complete();
                    break;
                case AnimationType.PopIn:
                    if (!skipToEnd)
                    {
                        LeanTween.value(go, 0, 1, totalLength - _delay).setOnComplete(Complete);
                    }
                    else
                    {
                        Complete();
                    }
                    break;
                case AnimationType.LeftSwooshIn:
                    Vector3 rightSwooshPos = initialPos;
                    rightSwooshPos.x += 1;

                    if (!skipToEnd)
                    {
                        go.transform.localPosition = rightSwooshPos;
                    }
                    LeanTween.moveLocal(go, initialPos, _animationDuration).setOnComplete(() =>
                    {
                        if (skipToEnd)
                        {
                            Complete();
                        }
                    });
                    if (!skipToEnd)
                    {
                        LeanTween.value(go, 0, 1, totalLength - _delay).setOnComplete(Complete);
                    }
                    break;
                case AnimationType.RightSwooshIn:
                    Vector3 leftSwooshPos = initialPos;
                    leftSwooshPos.x -= 1;

                    if (!skipToEnd)
                    {
                        go.transform.localPosition = leftSwooshPos;
                    }

                    LeanTween.moveLocal(go, initialPos, _animationDuration).setOnComplete(() =>
                    {
                        if (skipToEnd)
                        {
                            Complete();
                        }
                    });
                    if (!skipToEnd)
                    {
                        LeanTween.value(go, 0, 1, totalLength - _delay).setOnComplete(Complete);
                    }
                    break;
                case AnimationType.RightSpinIn:
                    break;
                case AnimationType.LeftSpinIn:
                    if (!skipToEnd)
                    {
                        LeanTween.value(go, 0, 1, totalLength - _delay).setOnComplete(Complete);
                    }
                    else
                    {
                        Complete();
                    }
                    break;
                case AnimationType.Disappear:
                    go.SetActive(false);
                    Complete();
                    break;
                case AnimationType.FadeOut:
                    break;
                case AnimationType.ScaleOut:
                    if (!skipToEnd)
                    {
                        go.transform.localScale = initialScale;
                    }

                    LeanTween.scale(go, Vector3.zero, _animationDuration).setOnComplete(() =>
                    {
                        go.SetActive(false);
                        Complete();
                    });
                    break;
                case AnimationType.BlurOut:
                    Complete();
                    break;
                case AnimationType.PopOut:
                    go.SetActive(false);
                    Complete();
                    break;
                case AnimationType.LeftSwooshOut:
                    Vector3 rightSwooshOutPos = initialPos;
                    rightSwooshOutPos.x += 1;

                    if (!skipToEnd)
                    {
                        go.transform.localPosition = initialPos;
                    }

                    LeanTween.moveLocal(go, rightSwooshOutPos, _animationDuration).setOnComplete(() =>
                    {
                        go.SetActive(false);
                        Complete();
                    });
                    break;
                case AnimationType.RightSwooshOut:
                    Vector3 leftSwooshOutPos = initialPos;
                    leftSwooshOutPos.x -= 1;

                    if (!skipToEnd)
                    {
                        go.transform.localPosition = initialPos;
                    }

                    LeanTween.moveLocal(go, leftSwooshOutPos, _animationDuration).setOnComplete(() =>
                    {
                        go.SetActive(false);
                        Complete();
                    });
                    break;
                case AnimationType.RightSpinOut:
                    go.SetActive(false);
                    Complete();
                    break;
                case AnimationType.LeftSpinOut:
                    go.SetActive(false);
                    Complete();
                    break;
                case AnimationType.TopSwooshIn:
                    Vector3 topSwooshPos = initialPos;
                    topSwooshPos.y += 1;

                    if (!skipToEnd)
                    {
                        go.transform.localPosition = topSwooshPos;
                    }

                    LeanTween.moveLocal(go, initialPos, _animationDuration).setOnComplete(() =>
                    {
                        if (skipToEnd)
                        {
                            Complete();
                        }
                    });

                    if (!skipToEnd)
                    {
                        LeanTween.value(go, 0, 1, totalLength - _delay).setOnComplete(Complete);
                    }
                    break;
                case AnimationType.TopSwooshOut:
                    Vector3 topSwooshOutPos = initialPos;
                    topSwooshOutPos.y += 1;

                    if (!skipToEnd)
                    {
                        go.transform.localPosition = initialPos;
                    }

                    LeanTween.moveLocal(go, topSwooshOutPos, _animationDuration).setOnComplete(() =>
                    {
                        go.SetActive(false);
                        Complete();
                    });
                    break;
                case AnimationType.BottomSwooshIn:
                    Vector3 bottomSwooshPos = initialPos;
                    bottomSwooshPos.y -= 1;

                    if (!skipToEnd)
                    {
                        go.transform.localPosition = bottomSwooshPos;
                    }

                    LeanTween.moveLocal(go, initialPos, _animationDuration).setOnComplete(() =>
                    {
                        if (skipToEnd)
                        {
                            Complete();
                        }
                    });

                    if (!skipToEnd)
                    {
                        LeanTween.value(go, 0, 1, totalLength - _delay).setOnComplete(Complete);
                    }
                    break;
                case AnimationType.BottomSwooshOut:
                    Vector3 bottomSwooshOutPos = initialPos;
                    bottomSwooshOutPos.y -= 1;

                    if (!skipToEnd)
                    {
                        go.transform.localPosition = initialPos;
                    }

                    LeanTween.moveLocal(go, bottomSwooshOutPos, _animationDuration).setOnComplete(() =>
                    {
                        go.SetActive(false);
                        Complete();
                    });
                    break;
                case AnimationType.StartRotationRight:
                    /*assetController.rotate.shouldUpdate = true;
                    assetController.rotate.speed = asset.transition.animationDuration;
                    Complete(); */
                    break;
                case AnimationType.StopRotation:
                    /* assetController.rotate.shouldUpdate = false;
                     Complete(); */
                    break;
                case AnimationType.StartRotationLeft:
                    /*assetController.rotate.shouldUpdate = true;
                    assetController.rotate.speed = -asset.transition.animationDuration;
                    Complete();*/
                    break;
                default:
                    break;

            }
        }

        public static void SetInitialTransform()
        {
            initialPos = new Vector3(itemTransform.position.x, itemTransform.position.y, itemTransform.position.z);
            initialRot = new Quaternion(itemTransform.rotation.x, itemTransform.rotation.y, itemTransform.rotation.z, itemTransform.rotation.w);
            initialScale = new Vector3(itemTransform.localScale.x, itemTransform.localScale.y, itemTransform.localScale.z);

        }

        private static void Complete()
        {
            //if (callback == null) return;
            //callback.Invoke();
        }

    }

