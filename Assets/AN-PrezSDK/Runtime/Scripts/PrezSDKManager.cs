using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;
using Unity.Linq;
using System.Linq;
using AfterNow.PrezSDK.Internal.Helpers;
using AfterNow.PrezSDK.Internal.Views;
using AfterNow.PrezSDK.Shared;
using AfterNow.PrezSDK.Shared.Enums;
using System.IO;

/// <summary>
/// Sample class on how to Authenticate to server, join a presentation and Navigate through the presentation
/// </summary>
class PrezSDKManager : MonoBehaviour
{
    #region private variables

    private int slideIdx = -1;
    private int slideCount = 0;
    private int SlidePoint = -1;
    private int _lastPlayedPoint = -1;
    private bool isPlaying;
    private bool isDone = false;
    private bool isAnimating = false;
    private bool isSlideEnded = false;
    private int targetSlide = 0;
    private bool CanReset = false;
    private int targetSlideIdx = 0;
    private bool waitingForPresentationLoad;
    private PresentationManager.LoadedSlide previousSlide;
    private Coroutine slideTransition;
    private ARPAsset _asset;
    private ARPTransition _transition;
    private List<ARPAsset> _assets = new List<ARPAsset>();
    private List<ARPTransition> _transitions = new List<ARPTransition>();
    private List<AudioSource> audioSources = new List<AudioSource>();
    private readonly LinkedList<int> _slideTracker = new LinkedList<int>();

    #endregion

    #region public/serialized variables

    [SerializeField] BasePrezController baseController;
    public GameObject presentationAnchorOverride;
    public AnimationTimeline animationTimeline;
    public static PrezSDKManager _instance = null;
    [HideInInspector] public PresentationManager _manager;
    public static Dictionary<string, GameObject> prezAssets = new Dictionary<string, GameObject>();

    #endregion

    #region UI



    #endregion

    #region private properties

    #endregion

    #region public properties

    public int LastPlayedPoint
    {
        get => _lastPlayedPoint;
        set
        {
            _lastPlayedPoint = value;
        }
    }
    public float? PlayStartTime { get; private set; }

    #endregion

    #region public events

    public static event Action OnPresentationEnded;

    #endregion

    #region enums

    [Serializable]
    public enum SlideProgressionType : sbyte
    {
        PreviousSlide = -1,
        ResetSlide = 0,
        NextSlide = 1
    }

    #endregion

    private void Awake()
    {
        _instance = this;

        baseController.AssignEvents(OnStartPresentation, Next_Step, Next_Slide, Previous_Slide, Quit);

        var instance = CoroutineRunner.Instance;

        if (presentationAnchorOverride == null)
        {
            presentationAnchorOverride = new GameObject("Presentation Anchor");
            presentationAnchorOverride.transform.SetParent(transform, false);
        }

        baseController.Callback_OnUserLoginFromEditor((username, password) =>
        {
            if (string.IsNullOrEmpty(username) && string.IsNullOrEmpty(password))
                return;

            PrezWebCalls.user_email = username;
            PrezWebCalls.user_password = password;

            _ = PrezWebCalls.OnAuthenticationRequest((ev) =>
            {
                CoroutineRunner.DispatchToMainThread(() =>
                {
                    if (ev)
                    {
                        baseController.Callback_OnAuthorized(true);
                    }
                    else
                    {
                        baseController.Callback_OnAuthorized(false);
                    }
                });
            });

        });
    }

    public void Login(string username, string password)
    {
        PrezWebCalls.user_email = username;
        PrezWebCalls.user_password = password;

        _ = PrezWebCalls.OnAuthenticationRequest((ev) =>
        {
            CoroutineRunner.DispatchToMainThread(() =>
            {
                if (ev)
                {
                    baseController.Callback_OnAuthorized(true);
                }
                else
                {
                    baseController.Callback_OnAuthorized(false);
                }
            });
        });
    }

    public void Logout()
    {
        baseController.Callback_OnUserLogout();
    }

    private void Quit()
    {
        //Terminate the asset loading process
        AssetLoader.StopLoadingAssets();

        //do cleanup
        PrezStates.Reset();

        //Destroy assets on quit
        foreach (var asset in prezAssets)
        {
            Destroy(asset.Value.gameObject);
        }

        prezAssets.Clear();
        _manager._location = null;

        //Remove PresentationManager from the presentationAnchorOverride
        Destroy(presentationAnchorOverride.GetComponent<PresentationManager>());
    }


    void Next_Slide()
    {
        //Clear present slide data before playing another slide
        ClearPresentSlide();

        if (slideCount == PrezStates.CurrentSlide + 1)
        {
            PrezStates.CurrentSlide = 0;
            targetSlide = 0;
            slideIdx = -1;
            baseController.Callback_OnPresentationEnd();
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
            //Clear present slide data before playing another slide
            ClearPresentSlide();

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

    /// <summary>
    /// Clear the data related to the present slide if User decides to change to another slide
    /// </summary>
    private void ClearPresentSlide()
    {
        //Terminate the asset loading process
        AssetLoader.StopLoadingAssets();

        //Cleanup assetbundles
        AssetBundleManager.Cleanup();

        //Destroy the already loaded assets
        previousSlide.DestroyLoadedObjects();

        //Clear lists of assets
        ClearObjects();
    }


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

            LeanTween.value(presentationAnchorOverride, 0, 1, /*_manager._location.slides[targetSlideIdx].transition.delay*/0).setOnComplete(() =>
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
            if (targetSlideIdx != _manager._location.slides.Count)
                slideIdx = targetSlideIdx;
            //Last slide, let it do the transition..

            shouldTrySync = false;
            StopSlide(false, () =>
            {
                isPlaying = false;
                CanReset = true;
            });
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
                    previousSlide.CleanUp();
                    Next_Slide();
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
                        previousSlide.CleanUp();
                        Next_Slide();
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

    public bool OnStartPresentation(string presentationID)
    {
        slideIdx = -1;
        if (waitingForPresentationLoad) return false;
        //StatusText.text = null;
        waitingForPresentationLoad = true;

        _ = PrezWebCalls.JoinPresentation(presentationID, (prez) =>
        {
            CoroutineRunner.DispatchToMainThread(() =>
            {
                waitingForPresentationLoad = false;
                if (prez != null)
                {
                    PrezStates.Presentation = prez;

                    UpdateSlideCount();

                    _manager = presentationAnchorOverride.AddComponent<PresentationManager>();
                    _manager.Init(prez.locations[0]);
                    StartCoroutine(LoadSlide(PrezStates.CurrentSlide));
                    baseController.Callback_OnPresentationJoin(PresentationJoinStatus.SUCCESS, prez.match.shortId);
                }
                else
                {
                    //StatusText.text = "Invalid Presentation ID";
                    baseController.Callback_OnPresentationJoin(PresentationJoinStatus.FAILED, null);
                }
            });
        }, (prezFailed) =>
        {
            waitingForPresentationLoad = false;
            baseController.Callback_OnPresentationFailed(prezFailed);
        });
        return true;
    }

    public void GoToSlide(int slideNo)
    {
        if (PrezStates.CurrentSlide == slideNo) return;

        PrezStates.CurrentSlide = slideNo;
        StartCoroutine(LoadSlide(slideNo));
    }

    IEnumerator LoadSlide(int slideNo)
    {
        isSlideEnded = false;
        PrezStates.CurrentSlide = slideNo;
        previousSlide = _manager.LoadSlide(slideNo);

        previousSlide.loadedCount = 0;

        UpdateSlideCount();
        //Debug.Log("LOADING SLIDE");    
        baseController.Callback_OnSlideStatusUpdate(AfterNow.PrezSDK.Shared.Enums.SlideStatusUpdate.LOADING);
        //Wait till the slide completely loads
        while (!previousSlide.HasSlideLoaded)
        {
            yield return null;
        }

        baseController.Callback_OnSlideStatusUpdate(AfterNow.PrezSDK.Shared.Enums.SlideStatusUpdate.LOADED);
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
        baseController.Callback_OnSlideChange(PrezStates.CurrentSlide + 1);
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

    public static void DeleteDownloadedFiles()
    {
        DirectoryInfo directoryInfo = new DirectoryInfo(InitializeSDK.DownloadFolderPath);

        foreach (var item in directoryInfo.EnumerateDirectories())
        {
            item.Delete(true);
        }

    }
}
