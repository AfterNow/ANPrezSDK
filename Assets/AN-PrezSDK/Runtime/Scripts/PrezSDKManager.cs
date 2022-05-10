using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using AfterNow.PrezSDK.Internal.Helpers;
using AfterNow.PrezSDK.Internal.Views;
using AfterNow.PrezSDK.Shared;
using AfterNow.PrezSDK.Shared.Enums;
using System.IO;


namespace AfterNow.PrezSDK
{
    public class PrezSDKManager : MonoBehaviour
    {
        #region private/internal variables

        private int slideCount = 0;
        private int SlidePoint = -1;
        private int _lastPlayedPoint = -1;
        private bool isPlaying;
        private bool isDone = false;
        private bool isWaitingForSlideDelay = false;
        private Coroutine waitingForSlideDelay;
        private bool isAnimating = false;
        private bool isSlideEnded = false;
        private int targetSlide = 0;
        private bool waitingForPresentationLoad;
        private PresentationManager.LoadedSlide previousSlide;
        private ARPAsset _asset;
        private ARPTransition _transition;
        private List<ARPAsset> _assets = new List<ARPAsset>();
        private List<ARPTransition> _transitions = new List<ARPTransition>();
        private List<AudioSource> audioSources = new List<AudioSource>();
        private readonly SlideTracker _slideTracker = new SlideTracker();
        private AnimationTimeline animationTimeline;
        private PresentationManager _manager;
        internal static Dictionary<string, GameObject> prezAssets = new Dictionary<string, GameObject>();
        #endregion

        #region serialized values

        [SerializeField] BasePrezController baseController;
        [SerializeField] GameObject presentationAnchorOverride;
        [field: SerializeField] public bool EnableClickable { get; private set; }

        #endregion

        #region non serialized public accessors
        internal float? PlayStartTime { get; private set; }
        public IEnumerator<ClickableAsset> ClickableAssets
        {
            get
            {
                foreach (var asset in AssetLoader.ClickableAssets)
                {
                    yield return asset;
                }
            }
        }
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

            if (EnableClickable)
            {
                AssetLoader.OnClickableActivate = JumpToSlide;
            }
            else
            {
                AssetLoader.OnClickableActivate = null;
            }

            baseController.OnSDKInitialize(this);
        }

        internal void Login(string username, string password)
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

        internal void Logout()
        {
            DeleteDownloadedFiles();
            baseController.Callback_OnUserLogout();
        }

        private void Quit()
        {
            StopTransitionDelayTimer();
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
            DeleteDownloadedFiles();
            //Remove PresentationManager from the presentationAnchorOverride
            Destroy(presentationAnchorOverride.GetComponent<PresentationManager>());
        }


        void Next_Slide()
        {
            StopTransitionDelayTimer();
            //Clear present slide data before playing another slide
            ClearPresentSlide();

            if (slideCount == PrezStates.CurrentSlide + 1)
            {
                _slideTracker.Clear();
                PrezStates.CurrentSlide = 0;
                targetSlide = 0;
                baseController.Callback_OnPresentationEnd();
            }
            else
            {
                _slideTracker.AddLastSlide(targetSlide);
                targetSlide = PrezStates.CurrentSlide + 1;
            }
            GoToSlide(targetSlide);
        }

        void Previous_Slide()
        {
            StopTransitionDelayTimer();
            if (PrezStates.CurrentSlide != 0)
            {
                //Clear present slide data before playing another slide
                ClearPresentSlide();

                targetSlide = _slideTracker.GetPreviousSlide();
                GoToSlide(targetSlide);
            }
        }

        void JumpToSlide(int targetSlide)
        {
            StopTransitionDelayTimer();
            if (targetSlide < 0)
            {
                Debug.LogError($"Slide index {targetSlide} not valid");
                return;
            }
            Debug.LogError(targetSlide);
            if (PrezStates.CurrentSlide != targetSlide)
            {
                ClearPresentSlide();

                this.targetSlide = targetSlide;
                GoToSlide(targetSlide);
            }
        }

        void Next_Step()
        {
            StopTransitionDelayTimer();
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
                NextStepLogic();
            }
            else if (isDone)
            {
                Next_Slide();
            }
        }

        void NextStepLogic()
        {
            if (isPlaying)
            {
                if (!animationTimeline.FirstElementAutomatic && _lastPlayedPoint == -1)
                {
                    SlidePoint = 0;
                    _lastPlayedPoint = animationTimeline.Play(SlidePoint);
                }
                else
                {
                    _lastPlayedPoint = animationTimeline.Play(SlidePoint);
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

        internal void ClearObjects()
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
            _lastPlayedPoint = SlidePoint;
            animationTimeline.Reset();
            PlayStartTime = null;
        }

        internal void StopSlide(bool now = false, Action action = null)
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

        internal void ShowChild(bool show, GameObject _prezAsset)
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

        internal bool OnStartPresentation(string presentationID)
        {
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
                CoroutineRunner.DispatchToMainThread(() =>
                {
                    waitingForPresentationLoad = false;
                    baseController.Callback_OnPresentationFailed(prezFailed);
                });
            });
            return true;
        }

        internal void GoToSlide(int slideNo)
        {
            if (PrezStates.CurrentSlide == slideNo) return;

            PrezStates.CurrentSlide = slideNo;
            StartCoroutine(LoadSlide(slideNo));
        }

        IEnumerator LoadSlide(int slideNo)
        {
            //Reset LastPlayedPoint to -1
            _lastPlayedPoint = -1;

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


            AudioSource[] audioSourceGOs = gameObject.GetComponentsInChildren<AudioSource>(true);
            foreach (AudioSource source in audioSourceGOs)
            {
                audioSources.Add(source);
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

        internal void TimelineComplete(AnimationTimeline timeilne)
        {
            isDone = true;
            isPlaying = false;

            int totalSlides = _manager._location.slides.Count;
            int currentSlide = PrezStates.CurrentSlide;
            int nextSlide = currentSlide + 1;

            if (nextSlide < totalSlides)
            {
                var slide = _manager._location.slides[nextSlide];
                if (slide.transition.startType == AnimationStartType.Automatically)
                {
                    if (slide.transition.delay <= 0)
                    {
                        Next_Slide(); //dont wait.
                        waitingForSlideDelay = null;
                    }
                    else
                    {
                        isWaitingForSlideDelay = true;
                        waitingForSlideDelay = CoroutineRunner.Instance.StartCoroutine(StartSlideDelayTimer(slide.transition.delay, () =>
                        {
                            if (isWaitingForSlideDelay)
                            {
                                isWaitingForSlideDelay = false;
                                Next_Slide();
                            }
                            waitingForSlideDelay = null;
                        }));
                    }
                }
            }
            //check if next slide present.
            //if true, check for next slide transition type.
            //if automatic, wait for the delay, then load the slide
        }

        private IEnumerator StartSlideDelayTimer(float seconds, Action action)
        {
            yield return new WaitForSeconds(seconds);
            action();
        }

        private void StopTransitionDelayTimer()
        {
            if (waitingForSlideDelay != null)
            {
                CoroutineRunner.Instance.StopCoroutine(waitingForSlideDelay);
                waitingForSlideDelay = null;
            }
        }

        internal void Play(int groupNum = -1)
        {
            //Debug.Log("PrezSDKManager Play");

            SlidePoint = groupNum;
            isDone = false;
            isWaitingForSlideDelay = false;
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

        internal void OnSyncGroup(int num, bool nextStep = true)
        {
            SlidePoint = num;
            _lastPlayedPoint = animationTimeline.Play(num, nextStep);
        }
        internal void OnSyncTimeline(int num)
        {
            OnSyncGroup(num);
        }

        static void DeleteDownloadedFiles()
        {
            try
            {
                DirectoryInfo directoryInfo = new DirectoryInfo(InitializeSDK.DownloadFolderPath);

                foreach (var item in directoryInfo.EnumerateDirectories())
                {
                    item.Delete(true);
                }
            }
            catch(Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        private void OnDestroy()
        {
            DeleteDownloadedFiles();
            InternalStates.Reset();
        }
    }
}
