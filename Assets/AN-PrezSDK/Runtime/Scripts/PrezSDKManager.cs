using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using TMPro;
using System;
using Unity.Linq;
using System.Linq;
using AfterNow.PrezSDK.Internal.Helpers;
using AfterNow.PrezSDK.Internal.Views;

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
    [SerializeField] public TMP_Text AssetLoadingStatusText;

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
    public static Dictionary<string, GameObject> prezAssets = new Dictionary<string, GameObject>();

    public static bool loadComplete = false;
    private List<AudioSource> audioSources = new List<AudioSource>();

    public static event Action OnPresentationEnded;


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

            //Destroy assets on quit
            foreach (var asset in prezAssets)
            {
                Destroy(asset.Value.gameObject);
            }

            prezAssets.Clear();
            _manager._location = null;

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

    int targetSlide = 0;

    void Next_Slide()
    {
        previousSlide.DestroyLoadedObjects();
        ClearObjects();

        //slideCount = PrezStates.Presentation.locations[0].slides.Count;
        if (slideCount == PrezStates.CurrentSlide + 1)
        {
            PrezStates.CurrentSlide = 0;
            targetSlide = 0;
            slideIdx = -1;
            OnPresentationEnded.Invoke();
        }
        else
        {
            targetSlide = PrezStates.CurrentSlide + 1;
        }
        GoToSlide(targetSlide);
    }

    void Previous_Slide()
    {
        if (PrezStates.CurrentSlide != 0)
        {
            //PresentationManager._slides.Remove(PrezStates.CurrentSlide);
            previousSlide.DestroyLoadedObjects();
            ClearObjects();

            //slideCount = PrezStates.Presentation.locations[0].slides.Count;
            targetSlide = PrezStates.CurrentSlide == 0 ? slideCount - 1 : PrezStates.CurrentSlide - 1;
            GoToSlide(targetSlide);
        }
    }

    void Next_Step()
    {
        if (isSlideEnded)
        {
            //Debug.Log("nextstep isSlideEnded");
            foreach (Transform child in PresentationManager._instance.transform)
            {
                Destroy(child.gameObject);
            }
            isSlideEnded = false;
            return;
        }

        if (isPlaying)
        {
            //Debug.Log("nextstep isPlaying");
            NextStepLogic();
        }
        else if (isDone)
        {
            //Debug.Log("nextstep isDone");
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
            //Debug.Log("stopping coroutine");
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


        //isPlaying = true;

        //if (isPlaying)
        //{
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
        //Debug.Log("targetSlideIdx : " + targetSlideIdx + " slides.Count : " + _manager._location.slides.Count);
        if (targetSlideIdx < _manager._location.slides.Count && targetSlideIdx >= 0)
        {
            //Coroutine slideLoader = GotoSlidePlayMode(targetSlideIdx);

            //if (_slide.Slide.DownloadProgress == 1f) //if current slide is loaded, animate it out
            //{
            bool hasSlideStopped = false;

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
            /*if (targetSlideIdx != _manager._location.slides.Count)
                slideIdx = targetSlideIdx;*/
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
        //}
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

        if (_assets.Count > 0)
        {
            _assets.Clear();
        }

        if (prezAssets.Count > 0)
        {
            prezAssets.Clear();
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

        switch (slideTransition.animation)
        {
            case SlideAnimationType.Disappear:
                isSlideEnded = false;
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
                isSlideEnded = true;
                isAnimating = true;
                foreach (var audioSource in audioSources)
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
                }

                GameObject go = null;
                foreach (var asset in PresentationManager.assets)
                {
                    if (asset.type == ANPAssetType.TEXT)
                    {
                        if (prezAssets.TryGetValue(asset.text.value, out GameObject _go))
                        {
                            go = _go;
                        }
                    }
                    else
                    {
                        if (prezAssets.TryGetValue(asset.FileName(), out GameObject _go))
                        {
                            go = _go;
                        }
                    }

                    var initialScale = PrezAssetHelper.GetVector(asset.itemTransform.localScale);
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
                //Debug.Log("scaling out " + _prezAsset.name);
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
        slideIdx = -1;
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
    public bool isSlideEnded = false;

    IEnumerator LoadSlide(int slideNo)
    {
        isSlideEnded = false;
        PrezStates.CurrentSlide = slideNo;
        previousSlide = _manager.LoadSlide(slideNo);

        previousSlide.loadedCount = 0;

        UpdateSlideCount();

        Debug.Log("_assetscount : " + previousSlide._assets.Count);
        Debug.Log("loadedcount : " + previousSlide.loadedCount);

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


        IEnumerable<GameObject> audioSourceGOs = gameObject.DescendantsAndSelf().Where(x => x.GetComponent<AudioSource>());
        foreach (GameObject audioSourceGO in audioSourceGOs)
        {
            audioSources.Add(audioSourceGO.GetComponent<AudioSource>());
        }

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
            //Debug.Log("assetname : " + _asset.FileName() + " :: " + " animation type : " + _transition.animation + " animation startType " + _transition.startType);

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
        //Debug.Log("PrezSDKManager Play");

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
            //Debug.Log("Playing new slide");
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
    public IEnumerator ShowErrors(string value, Color color)
    {
        AssetLoadingStatusText.text = value;
        AssetLoadingStatusText.color = color;

        yield return new WaitForSeconds(3f);

        AssetLoadingStatusText.text = string.Empty;
        AssetLoadingStatusText.color = Color.white;
    }

}
