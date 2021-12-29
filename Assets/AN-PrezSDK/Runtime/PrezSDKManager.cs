using AfterNow.AnPrez.SDK.Internal;
using AfterNow.AnPrez.SDK.Internal.Views;
using AfterNow.AnPrez.SDK.Unity.Interfaces;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using static AfterNow.AnPrez.SDK.Unity.PresentationManager;
using TMPro;
using Assets.AN_PrezSDK.Runtime;

namespace AfterNow.AnPrez.SDK.Unity
{
    /// <summary>
    /// Sample class on how to Authenticate to server, join a presentation and Navigate through the presentation
    /// </summary>
    class PrezSDKManager : MonoBehaviour
    {
        [SerializeField] TMP_Text PresentationIDText;
        [SerializeField] TMP_Text CurrentSlideText;

        [HideInInspector]
        public PresentationManager _manager;
        public GameObject presentationAnchor;

        int slideCount = 0;

        IPrezController prezController;

        ARPAsset _asset;
        List<ARPAsset> _assets = new List<ARPAsset>();
        ARPTransition _transition;
        List<ARPTransition> _transitions = new List<ARPTransition>();
        float delay = 0;


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

        private void Awake()
        {
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

        void ClearObjects()
        {
            if (loadedObjects.Count > 0)
            {
                loadedObjects.Clear();
            }

            if (_assets.Count > 0)
            {
                _assets.Clear();
            }

        }

        void Next_Slide()
        {
            ClearObjects();
            //slideCount = PrezStates.Presentation.locations[0].slides.Count;
            int targetSlide = slideCount == PrezStates.CurrentSlide + 1 ? 0 : PrezStates.CurrentSlide + 1;
            GoToSlide(targetSlide);
        }

        void Previous_Slide()
        {
            ClearObjects();
            //slideCount = PrezStates.Presentation.locations[0].slides.Count;
            int targetSlide = PrezStates.CurrentSlide == 0 ? slideCount - 1 : PrezStates.CurrentSlide - 1;
            GoToSlide(targetSlide);
        }

        void Next_Step()
        {
            nextIndex = presentIndex + 1;
            StopAllCoroutines();
            if (nextIndex < assets.Count)
            {
                StartCoroutine(PlayAnim(nextIndex));
            }
            else
            {
                Next_Slide();
            }
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

        private LoadedSlide previousSlide;
        private bool onCommand = true;
        private int presentIndex = 0;
        private int nextIndex = 0;
        GameObject go = null;
        private GameObject _go;

        IEnumerator LoadSlide(int slideNo)
        {
            //If this is first slide, disable the "previous slide" button
            /*if (slideNo == 0)
                PreviousSlide.gameObject.SetActive(false);
            else
                PreviousSlide.gameObject.SetActive(true);*/

            //If this is last slide, disable the "next slide" button
            /*if (slideNo == slideCount - 1)
                NextSlide.gameObject.SetActive(false);
            else
                NextSlide.gameObject.SetActive(true);*/

            if (previousSlide != null)
            {
                //Debug.Log("cleanup initiated for slide " + PrezStates.CurrentSlide);
                previousSlide.CleanUp();
                yield return null;
            }
            PrezStates.CurrentSlide = slideNo;
            previousSlide = _manager.LoadSlide(slideNo);
            UpdateSlideCount();

            //Wait till the slide completely loads
            while (!previousSlide.HasSlideLoaded)
            {
                yield return null;
            }

            assets = previousSlide.Slide.assets;

            //then play slide animations
            //StartCoroutine(PlayAssetAnimations());

            // Setup animation groups
            List<ARPTransition> pTransitions = assetTransitions;
            List<AnimationGroup> animationGroups = new List<AnimationGroup>();
            AnimationGroup currentGroup = null;
            int groupNum = 0;

            foreach (ARPTransition transition in assetTransitions)
            {
                if (currentGroup == null || transition.startType != AnimationStartType.WithPreviousAnim)
                {
                    currentGroup = new AnimationGroup(groupNum++);
                    animationGroups.Add(currentGroup);

                }

                _asset = _slide.Slide.assets.Find(x => x.id == transition.assetId);
                _transition = transition;
                //Debug.Log("a : " + _asset.FileName() + " :: " + "t : " + _transition.animation + " :: " + _transition.startType);

                currentGroup.AddAnimation(_transition, _asset);


                if (!_assets.Contains(_asset))
                {
                    _assets.Add(_asset);
                }
                _transitions.Add(_transition);
            }

            for (int i = 0; i < assets.Count; i++)
            {
                yield return new WaitForSeconds(1f);

                if (loadedObjects.TryGetValue(_assets[i], out _go))
                {
                    go = _go;
                }
                else
                {
                    Debug.LogErrorFormat("Cannot find {0} ", _assets[i].FileName());
                }

                go.SetActive(true);
                if (_assets[i].type == ANPAssetType.VIDEO)
                {
                    yield return null;
                    var videoPlayer = go.GetComponent<VideoPlayer>();
                    videoPlayer.Play();
                }

                var currentAsset = _assets[i];
                var currentTransition = _transitions[i];

                DoRegularAnimation(go, currentAsset, currentTransition, false, currentTransition.atTime, currentTransition.animationDuration);
            }

        }

        IEnumerator PlayAssetAnimations()
        {
            for (int i = 0; i < loadedObjects.Count && i < assetTransitions.Count; i++)
            {

                switch (assetTransitions[i].startType)
                {
                    case AnimationStartType.None:
                        yield return null;
                        break;
                    case AnimationStartType.OnCommand:
                        yield return null;
                        break;
                    case AnimationStartType.Automatically:
                        yield return null;
                        break;
                    case AnimationStartType.WithPreviousAnim:
                        yield return new WaitForSeconds(0f);
                        break;
                    case AnimationStartType.AfterPreviousAnim:
                        yield return new WaitForSeconds(3f);
                        break;
                    default:
                        break;
                }

                presentIndex = i;

                yield return new WaitForSeconds(assetTransitions[i].atTime);
                StartCoroutine(PlayAnim(presentIndex));

            }
        }

        IEnumerator PlayAnim(int index)
        {
            var go = loadedObjects[assets[index]];

//            Debug.Log("object : " + loadedObjects[assets[index]] + " : " + );
            if (go != null)
            {
                go.SetActive(true);
                if (assets[index].type == ANPAssetType.VIDEO)
                {
                    yield return null;
                    var videoPlayer = go.GetComponent<VideoPlayer>();
                    videoPlayer.Play();
                }


                //DoRegularAnimation(go, assets[index], assetTransitions[index], false, 0f, 0f);
            }
            else
            {
                Debug.LogError("gameobject not found");
            }
        }

        void UpdateSlideCount()
        {
            CurrentSlideText.text = (PrezStates.CurrentSlide + 1).ToString();
            slideCount = PrezStates.Presentation.locations[0].slides.Count;
        }

        void DestroyObjects()
        {
            int childs = _manager.transform.childCount;
            for (int i = childs - 1; i > 0; i--)
            {
                GameObject.Destroy(transform.GetChild(i).gameObject);
            }
        }
    }
}
