using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using TMPro;
using System;

/// <summary>
/// Sample class on how to Authenticate to server, join a presentation and Navigate through the presentation
/// </summary>
class PrezSDKManager : MonoBehaviour
{
    //public IntReactiveProperty slideIdx = new IntReactiveProperty(-1);
    public int slideIdx = -1;

    public static VideoPlayer player;
    private int loadVideoFrame = 1;

    public static PrezSDKManager _instance = null;


    [SerializeField] TMP_Text PresentationIDText;
    [SerializeField] TMP_Text CurrentSlideText;
    [SerializeField] TMP_Text SlideLoadingStatusText;

    [HideInInspector]
    public PresentationManager _manager;
    public GameObject presentationAnchor;

    int slideCount = 0;

    IPrezController prezController;

    ARPAsset _asset;
    public static List<ARPAsset> _assets = new List<ARPAsset>();
    ARPTransition _transition;
    List<ARPTransition> _transitions = new List<ARPTransition>();

    float delay = 0;
    public bool isPlaying;

    public AnimationTimeline animationTimeline;
    int SlidePoint = -1;
    public bool isDone = false;
    private int _lastPlayedPoint = -1;
    public int LastPlayedPoint
    {
        get => _lastPlayedPoint;
        set
        {
            _lastPlayedPoint = value;
        }
    }

    public float? PlayStartTime { get; private set; }

    bool hasbeenAuthorized = false;
    bool IsAuthorized()
    {
        return hasbeenAuthorized;
    }

    int hasLoggedIn = 0;
    int HasLoggedIn()
    {
        return hasLoggedIn;
    }
    public Material transMat;

    //public List<GameObject> prezAssets = new List<GameObject>();
    //[SerializeField] public static Dictionary<string, GameObject> prezAssets = new Dictionary<string, GameObject>();
    public static UDictionaryExample uDictionaryExample { get; private set; }

    public static bool loadComplete = false;

    public Dictionary<ARPAsset, PrezVector3> initialScales = new Dictionary<ARPAsset, PrezVector3>();

    #region enums

    [Serializable]
    public enum SlideProgressionType : sbyte
    {
        PreviousSlide = -1,
        ResetSlide = 0,
        NextSlide = 1
    }
    #endregion

    private void OnEnable()
    {
        PresentationManager.onObjectsDestroyed += ObjectsDestroyed;
    }

    private void OnDisable()
    {
        PresentationManager.onObjectsDestroyed -= ObjectsDestroyed;
    }

    private void ObjectsDestroyed()
    {
        Debug.Log("All objects destroyed");
        //Play();
        Next_Slide();
    }

    public void OnAssetLoaded(ARPAsset _arpAsset, GameObject _objectLoaded)
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

    private void Awake()
    {
        _instance = this;

        uDictionaryExample = GetComponent<UDictionaryExample>();

        prezController = GetComponent<IPrezController>();
        prezController.OnNextSlide += Next_Slide;
        prezController.OnPrevSlide += Previous_Slide;
        prezController.OnNextStep += Next_Step;
        prezController.OnPresentationJoin += OnStartPresentation;
        prezController.OnAuthorized += IsAuthorized;
        prezController.OnSessionJoin += HasLoggedIn;

        var instance = CoroutineRunner.Instance;

        prezController.OnQuit += () =>
        {
            //do cleanup
            PrezStates.Reset();

            //Destroy parent on quit
            if (_manager.gameObject != null)
            {
                Destroy(_manager.gameObject);
            }
        };


        _ = PrezWebCalls.OnAuthenticationRequest((ev) =>
        {
            CoroutineRunner.DispatchToMainThread(() =>
            {
                if (ev)
                {
                    hasbeenAuthorized = true;
                }
                else
                {
                    Debug.LogError("Login failed");
                }
            });
        });
    }

    void Next_Slide()
    {
        previousSlide.DestroyLoadedObjects();
        ClearObjects();

        //slideCount = PrezStates.Presentation.locations[0].slides.Count;
        Debug.Log("CurrentSlide " + PrezStates.CurrentSlide);
        int targetSlide = slideCount == PrezStates.CurrentSlide + 1 ? 0 : PrezStates.CurrentSlide + 1;
        Debug.Log("targetSlide " + targetSlide);
        GoToSlide(targetSlide);
    }

    void Previous_Slide()
    {
        if (PrezStates.CurrentSlide != 0)
        {
            previousSlide.DestroyLoadedObjects();
            ClearObjects();

            //slideCount = PrezStates.Presentation.locations[0].slides.Count;
            Debug.Log("CurrentSlide " + PrezStates.CurrentSlide);
            int targetSlide = PrezStates.CurrentSlide == 0 ? slideCount - 1 : PrezStates.CurrentSlide - 1;
            Debug.Log("targetSlide " + targetSlide);
            GoToSlide(targetSlide);
        }
    }

    void Next_Step()
    {
        if (isPlaying)
        {
            Debug.Log("isSlidePlaying");
            NextStepLogic();
        }
        else if (isDone)
        {
            Debug.Log("isSlideDone");
            TransitionSlide(1, -1);
        }
    }

    void NextStepLogic()
    {
        if (isPlaying)
        {
            if (!animationTimeline.FirstElementAutomatic && LastPlayedPoint == -1)
            {
                SlidePoint = 0;
                LastPlayedPoint = animationTimeline.Play(SlidePoint);
            }
            else
            {
                LastPlayedPoint = animationTimeline.Play(SlidePoint);
            }
        }
        else
        {
            Play();
        }
    }

    private Coroutine slideTransition;
    private bool CanReset = false;
    private readonly LinkedList<int> _slideTracker = new LinkedList<int>();

    /// <summary>
    /// The most inporttant function
    /// </summary>
    /// <param name="nextSlide">1 to move forward, -1 to move backward, 0 to reset slide(?)</param>
    /// <param name="targetSlideIdx"></param>
    public void TransitionSlide(int nextSlide = 1, int targetSlideIdx = -1, bool shouldTrySync = true, bool clickable = false, Action OnFinish = null)
    {
        if (slideTransition != null)
        {
            StopCoroutine(slideTransition);
        }
        slideTransition = StartCoroutine(StartTransitionSlide(nextSlide, targetSlideIdx, shouldTrySync, clickable, OnFinish));
    }

    private int targetSlideIdx = 0;
    private IEnumerator StartTransitionSlide(int nextSlide, int _targetSlideIdx, bool shouldTrySync, bool clickable, Action OnFinish)
    {
        targetSlideIdx = _targetSlideIdx;

        SlideProgressionType progressionType = (SlideProgressionType)nextSlide;
        CanReset = false;
        if (!isPlaying)
        {
            bool show = targetSlideIdx == -1;
        }

        isPlaying = true;

        if (isPlaying)
        {
            if (targetSlideIdx == -1)
            {
                if (nextSlide == 1)
                {
                    //targetSlideIdx = slideIdx.Value + 1;
                    targetSlideIdx = slideIdx + 1;

                    if (targetSlideIdx == _manager._location.slides.Count)
                    {
                        _slideTracker.Clear();
                    }
                    else
                    {
                        _slideTracker.AddLast(targetSlideIdx);
                    }
                }
                else if (nextSlide == -1)
                {
                    if (_slideTracker.Count > 1)
                    {
                        targetSlideIdx = _slideTracker.Last.Previous.Value;
                        _slideTracker.RemoveLast();
                    }
                    else
                    {
                        targetSlideIdx = -1;
                    }
                }
                else
                {
                    //targetSlideIdx = slideIdx.Value;
                    targetSlideIdx = slideIdx;
                }
            }
            else if (clickable)
            {
                _slideTracker.AddLast(targetSlideIdx);
            }

            if (nextSlide == 0)
            {
                //ClearActiveSlide(true);
                yield return null;
            }


            //Debug.Log("targetSlideIdx : " + targetSlideIdx + " _manager._location.slides.Count : " + _manager._location.slides.Count);
            Debug.Log("targetSlideIdx : " + targetSlideIdx + " slides.Count : " + _manager._location.slides.Count);
            if (targetSlideIdx < _manager._location.slides.Count && targetSlideIdx >= 0)
            {
                Debug.Log("index : 1");
                //Coroutine slideLoader = GotoSlidePlayMode(targetSlideIdx);

                //if (_slide.Slide.DownloadProgress == 1f) //if current slide is loaded, animate it out
                //{
                bool hasSlideStopped = false;
                Debug.Log("delay : " + _manager._location.slides[targetSlideIdx].transition.delay);

                LeanTween.value(presentationAnchor, 0, 1, /*_manager._location.slides[targetSlideIdx].transition.delay*/0).setOnComplete(() =>
                {
                    StopSlide(false, () =>
                    {
                        if (targetSlideIdx != _manager._location.slides.Count)
                        {
                            //   slideIdx.Value = targetSlideIdx;
                            slideIdx = targetSlideIdx;
                        }
                        hasSlideStopped = true;
                    });
                });
                while (!hasSlideStopped) yield return null;
                //}
                //yield return slideLoader;
                //yield return StartCoroutine(UpdateVRBackground(newSlideController.Slide.BackgroundTexture, newSlideController.Slide.backgroundOrientation));

                //only after new slide has loaded, and old slide has finished
                //Play();

                OnSlideTransition(targetSlideIdx);
                CanReset = true;
            }
            else if (targetSlideIdx == _manager._location.slides.Count)
            {
                Debug.Log("index : 2");
                if (targetSlideIdx != _manager._location.slides.Count)
                    slideIdx = targetSlideIdx;
                // Last slide, let it do the transition..
                //if (currentSlide != null)
                //{
                    /*shouldTrySync = false;
                    StopSlide(false, () =>
                    {
                        isPlaying = false;
                        AppManager.Instance.slideNo.Value = 0;
                        eventUpdatePresValues();
                        eventUpdateMenuLayout(AppManager.Instance.appMode);
                        if (AppManager.Instance.isPresenter && AppNetworkController.Instance.channel.Value != null)
                        {
                            AppNetworkController.Instance.SelectPresentationMode(AppManager.Instance.presentationState.Value);
                        }
                        CanReset = true;
                    });*/
                //}
                /*else if (shouldTrySync)
                {
                    SyncSlide(targetSlideIdx, progressionType);
                }*/
            }
            else
            {
                Debug.Log("index : 3");
                /*currentSlide.StopSlide(false, () =>
                {
                    isPlaying = false;
                    ResetLocation();
                    AppManager.Instance.slideNo.Value = 0;
                    eventUpdatePresValues();
                    eventUpdateMenuLayout(AppManager.Instance.appMode);
                    if (AppManager.Instance.isPresenter && AppNetworkController.Instance.channel.Value != null)
                    {
                        AppNetworkController.Instance.SelectPresentationMode(AppManager.Instance.presentationState.Value);
                    }
                    CanReset = true;
                    if (targetSlideIdx != Location.slides.Count)
                        slideIdx.Value = targetSlideIdx;
                });*/
            }
        }
        slideTransition = null;
        yield return null;
        //ClearActiveSlide(false);
        //OnFirstTimeLoaded();
        OnFinish?.Invoke();
    }

    private void OnSlideTransition(int targetSlideIdx)
    {
        //AppManager.Instance.slideNo.Value = targetSlideIdx + 1;
        //AppManager.Instance.isPresPaused.Value = false;
        //            slideIdx.Value = targetSlideIdx;
        slideIdx = targetSlideIdx;
    }


    public void ClearObjects()
    {
        if (PresentationManager.loadedObjects.Count > 0)
        {
            PresentationManager.loadedObjects.Clear();
        }

        /*if (_manager._location.slides.Count > 0)
        {
            _manager._location.slides.Clear();
        }*/

        if (_assets.Count > 0)
        {
            _assets.Clear();
        }

        if (uDictionaryExample.prezAssets.Count > 0)
        {
            uDictionaryExample.prezAssets.Clear();
            Debug.Log("Cleared prezAssets");
        }

        if (uDictionaryExample.initialScales.Count > 0)
        {
            uDictionaryExample.initialScales.Clear();
            Debug.Log("Cleared initialScales");
        }
    }

    void ResetTimeline()
    {
        SlidePoint = animationTimeline.FirstElementAutomatic ? 0 : -1;
        LastPlayedPoint = SlidePoint;
        animationTimeline.Reset();
        PlayStartTime = null;
    }

    public void StopSlide(bool now = false, Action action = null)
    {
        // If stop instantly, don't do animation and just hide children
        if (now || isAnimating)
        {
            isAnimating = false;
            LeanTween.cancel(gameObject);

            if (action != null)
            {
                action.Invoke();
            }
            ResetTimeline();
            isPlaying = false;
            isDone = true;
            return;
        }


        // Animate children out
        ARPSlideTransition slideTransition = previousSlide.Slide.transition;

        Debug.Log("animation : " + slideTransition.animation);

        switch (slideTransition.animation)
        {
            case SlideAnimationType.Disappear:
                // Wait until after duration to disappear
                isAnimating = true;
                LeanTween.delayedCall(gameObject, slideTransition.animationDuration, () =>
                {
                    isAnimating = false;
                    //ShowChildren(false);
                    if (action != null)
                    {
                        action.Invoke();
                    }
                    ResetTimeline();
                    isPlaying = false;
                    isDone = true;
                    //_manager.CleanUp();
                    previousSlide.CleanUp();
                });
                break;

            case SlideAnimationType.ScaleOut:
                isAnimating = true;
                /*foreach (var audioSource in audioSources)
                {
                    if (audioSource)
                        LeanTween.value(gameObject, 1, 0, slideTransition.animationDuration).setOnUpdate((float val) =>
                        {
                            if (audioSource)
                                audioSource.volume = val;
                        }).setOnComplete(() =>
                        {
                            if (audioSource)
                                audioSource.volume = 1;
                        });
                }*/

                /*foreach (var prezAsset in prezAssets)
                {
                    prezAsset.transform.localScale = initialScale;
                    LeanTween.scale(prezAsset, Vector3.zero, slideTransition.animationDuration).setOnComplete(_ =>
                    {
                        ShowChild(false, prezAsset);
                    });
                }*/

                GameObject go = null;
                foreach (var asset in PresentationManager.assets)
                {
                    /*Debug.Log("prezAssets count : " + uDictionaryExample.prezAssets.Count);
                    string output = "";
                    foreach (KeyValuePair<string, GameObject> kvp in uDictionaryExample.prezAssets)
                    {
                        output += string.Format("Key = {0}, Value = {1}", kvp.Key, kvp.Value);
                        output += "\n";
                    }
                    Debug.Log(output);*/
                    if (asset.type == ANPAssetType.TEXT)
                    {
                        //Debug.Log("assetfilename : " + asset.text.value);
                        if (uDictionaryExample.prezAssets.TryGetValue(asset.text.value, out GameObject _go))
                        {
                            go = _go;
                            //Debug.Log("goname : " + go.name);
                        }
                    }
                    else
                    {
                        //Debug.Log("assetfilename : " + asset.FileName());
                        if (uDictionaryExample.prezAssets.TryGetValue(asset.FileName(), out GameObject _go))
                        {
                            go = _go;
                            //Debug.Log("goname : " + go.name);
                        }
                    }

                    var initialScale = PrezAssetHelper.GetVector(asset.itemTransform.localScale);
                    //Debug.Log("initialScale : " + initialScale);
                    if (go != null)
                    {
                        go.transform.localScale = initialScale;
                        LeanTween.scale(go, Vector3.zero, slideTransition.animationDuration).setOnComplete(_ =>
                        {
                            ShowChild(false, go);
                        });
                    }
                    else
                    {
                        Debug.LogError("go is null. Cannot retrieve scale.");
                    }
                }


                LeanTween.delayedCall(gameObject, slideTransition.animationDuration, () =>
                {
                    if (this)
                    {
                        isAnimating = false;
                        if (action != null)
                        {
                            action.Invoke();
                        }
                        ResetTimeline();
                        isPlaying = false;
                        //_manager.CleanUp();
                        previousSlide.CleanUp();
                        isDone = true;
                    }
                });
                break;
        }

    }

    public void ShowChild(bool show, GameObject _prezAsset)
    {
        if (_prezAsset)
        {
            if (!show)
            {
                LeanTween.cancel(_prezAsset);
            }
            _prezAsset.SetActive(show);
        }
    }

    private Coroutine SlideIndexRunner;

    /// <summary>
    /// Only used for Play mode. Edit mode should never call this directly
    /// </summary>
    /// <param name="newIdx"></param>
    /// <returns></returns>
    private Coroutine GotoSlidePlayMode(int newIdx)
    {
        SlideIndexRunner = StartCoroutine(LoadSlide(newIdx));
        return SlideIndexRunner;
    }

    public void OnStartPresentation(string presentationID)
    {
        hasLoggedIn = 0;

        //StatusText.text = null;
        _ = PrezWebCalls.JoinPresentation(presentationID, (prez) =>
        {
            CoroutineRunner.DispatchToMainThread(() =>
            {
                if (prez != null)
                {
                    PrezStates.Presentation = prez;

                    LoadPresentation(prez);

                    _manager = presentationAnchor.AddComponent<PresentationManager>();
                    _manager.Init(prez.locations[0]);
                    StartCoroutine(LoadSlide(PrezStates.CurrentSlide));
                    hasLoggedIn = 1;
                }
                else
                {
                    //StatusText.text = "Invalid Presentation ID";
                    hasLoggedIn = -1;
                }
            });
        });
    }

    void LoadPresentation(Presentation prez)
    {
        PresentationIDText.text = prez.match.shortId;
        UpdateSlideCount();
    }

    public void GoToSlide(int slideNo)
    {
        if (PrezStates.CurrentSlide == slideNo) return;

        PrezStates.CurrentSlide = slideNo;
        StartCoroutine(LoadSlide(slideNo));
    }

    public PresentationManager.LoadedSlide previousSlide;
    private bool onCommand = true;
    private int presentIndex = 0;
    private int nextIndex = 0;
    GameObject go = null;
    private GameObject _go;
    private bool isAnimating = false;

    IEnumerator LoadSlide(int slideNo)
    {
        Debug.Log("PrezSDKManager LoadSlide " + slideNo);
        /*if (previousSlide != null)
        {
            //Debug.Log("cleanup initiated for slide " + PrezStates.CurrentSlide);
            previousSlide.CleanUp();
            yield return null;
        }*/
        PrezStates.CurrentSlide = slideNo;
        previousSlide = _manager.LoadSlide(slideNo);
        UpdateSlideCount();

        //Wait till the slide completely loads
        while (!previousSlide.HasSlideLoaded)
        {
            //Debug.Log("LOADING SLIDE");
            SlideLoadingStatusText.text = "Loading slide...";
            yield return null;
        }
        //Debug.Log("LOADED SLIDE");
        SlideLoadingStatusText.text = "Slide loaded";
        yield return new WaitForSeconds(2f);
        SlideLoadingStatusText.text = "";

        PresentationManager.assets = previousSlide.Slide.assets;
        //then play slide animations
        //StartCoroutine(PlayAssetAnimations());

        // Setup animation groups
        List<ARPTransition> pTransitions = PresentationManager.assetTransitions;

        List<AnimationGroup> animationGroups = new List<AnimationGroup>();
        AnimationGroup currentGroup = null;
        int groupNum = 0;

        foreach (ARPTransition transition in PresentationManager.assetTransitions)
        {
            if (currentGroup == null || transition.startType != AnimationStartType.WithPreviousAnim)
            {
                currentGroup = new AnimationGroup(groupNum++);
                animationGroups.Add(currentGroup);

            }
            _asset = PresentationManager._slide.Slide.assets.Find(x => x.id == transition.assetId);
            _transition = transition;
            Debug.Log("assetname : " + _asset.FileName() + " :: " + " animation type : " + _transition.animation + " animation startType " + _transition.startType);

            currentGroup.AddAnimation(_transition, _asset);


            if (!_assets.Contains(_asset))
            {
                _assets.Add(_asset);
            }
            _transitions.Add(_transition);
        }


        animationTimeline = new AnimationTimeline(animationGroups);
        animationTimeline.OnGroupCompleted += OnGroupEnd;
        animationTimeline.OnTimelineComplete += TimelineComplete;

        Play();
    }

    public void TimelineComplete(AnimationTimeline timeilne)
    {
        isDone = true;
        isPlaying = false;
    }

    public void Play(int groupNum = -1)
    {
        Debug.Log("PrezSDKManager Play");
        //Debug.LogError(groupNum);

        SlidePoint = groupNum;
        isDone = false;

        // Set initial transforms for each asset in this slide before they start animating in case we updated their transform
        /*foreach (AssetController ac in assetControllers)
        {
            ac.SetInitialTransform();
        }*/
        //SetInitialTransform();

        if (animationTimeline != null)
        {
            isPlaying = true;
            /*LastPlayedPoint = */
            Debug.Log("Playing new slide");
            animationTimeline.Play(groupNum);
        }
        else
        {
            Debug.LogError("animationTimeline is null");
        }

        if (!PlayStartTime.HasValue) PlayStartTime = Time.time;

    }

    void UpdateSlideCount()
    {
        CurrentSlideText.text = (PrezStates.CurrentSlide + 1).ToString();
        slideCount = PrezStates.Presentation.locations[0].slides.Count;
    }

    private void OnGroupEnd(AnimationGroup group)
    {
        SlidePoint = group.GroupIndex + 1;
    }

    public void OnSyncGroup(int num, bool nextStep = true)
    {
        SlidePoint = num;
        LastPlayedPoint = animationTimeline.Play(num, nextStep);
    }
    public void OnSyncTimeline(int num)
    {
        OnSyncGroup(num);
    }
}
