using AfterNow.PrezSDK.Internal.Helpers;
using AfterNow.PrezSDK.Shared;
using AfterNow.PrezSDK.Shared.Enums;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AfterNow.PrezSDK.Runtime.Examples
{
    public class BasePrezController : BaseController
    {
        [SerializeField] Button nextSlide;
        [SerializeField] Button previousSlide;
        [SerializeField] Button nextStep;
        [SerializeField] Button Quit;
        [SerializeField] Button LoadPresentation;
        [SerializeField] Button Login;
        [SerializeField] Button Logout;

        private void Start()
        {
            //Clear Status Texts
            userLoginStatusText.text = string.Empty;
            presentationLoadStatusText.text = string.Empty;
            slideLoadingStatusText.text = string.Empty;

            nextSlide.onClick.AddListener(() =>
            {
                NextSlide();
            });

            previousSlide.onClick.AddListener(() =>
            {
                PreviousSlide();
            });

            nextStep.onClick.AddListener(() =>
            {
                NextStep();
            });

            //LoginUI.SetActive(false);
            PlayPresentationUI.SetActive(false);

            Quit.onClick.AddListener(() =>
            {
                ReturnToLoadPresentationScreen();
                LoadPresentation.interactable = true;
            });

            LoadPresentation.onClick.AddListener(() =>
            {
                string presentationID = presentationIdInput.text;
                if (int.TryParse(presentationID.Trim(), out int integerPresentID))
                {
                    JoinPresentation(presentationID);
                }
                else
                {
                    Callback_OnPresentationFailed("Presentation ID should only contain numbers");
                }
            });

            Login.onClick.AddListener(() =>
            {
                UserLogin();
            });

            Logout.onClick.AddListener(() =>
            {
                UserLogout();
            });
        }

        public override void UserLogin()
        {
            userLoginStatusText.text = "Authenticating...";
            userLoginStatusText.color = Color.black;
            PrezSDKManager._instance.Login(userEmailIdInput.text, userPasswordInput.text);
        }

        public override void UserLogout()
        {
            PrezSDKManager._instance.Logout();
        }

        public override void Callback_OnAuthenticationFailed(string authenticationFailedReason)
        {
            throw new NotImplementedException();
        }

        public override void Callback_OnAuthorized(bool result)
        {
            if (result)
            {
                userLoginStatusText.text = "Login Success";
                userLoginStatusText.color = Color.green;
                if (!string.IsNullOrEmpty(defaultPresentationID))
                {
                    if (int.TryParse(defaultPresentationID.Trim(), out int integerPresentationID))
                    {
                        JoinPresentation(defaultPresentationID);
                    }
                    else
                    {
                        Callback_OnPresentationFailed("Presentation ID should only contain numbers");
                    }
                }
                else
                {
                    //Enable Presentation UI one second after the user is authorized successfully
                    Invoke(nameof(EnableLoadPresentationUI), 2f);
                }

                UserLoginUI.SetActive(false);
            }
            else
            {
                Callback_OnAuthenticationFailed("Incorrect Email or Password");
            }
        }

        public override void Callback_OnPresentationEnd()
        {
            ReturnToLoadPresentationScreen();
            LoadPresentation.interactable = true;
        }

        public override void Callback_OnPresentationFailed(string presentationFailedReason)
        {
            throw new NotImplementedException();
        }

        public override void Callback_OnPresentationJoin(PresentationStatus joinStatus, string presentationID)
        {
            this.presentationID = presentationID;

            if (joinStatus == PresentationStatus.SUCCESS)
            {
                presentationLoadStatusText.text = "Loading Presentation...";
                presentationLoadStatusText.color = Color.green;
                Invoke(nameof(EnablePlayPresentationUI), 2f);
            }
            else
            {
                Callback_OnPresentationFailed("Presentation ID invalid");
            }
        }

        public override void Callback_OnSlideChange(int newSlide)
        {
            currentSlideNumberText.text = newSlide.ToString();
        }

        public override void Callback_OnSlideStatusUpdate(SlideStatusUpdate slideStatus)
        {
            switch (slideStatus)
            {
                case SlideStatusUpdate.LOADING:
                    hasSlideLoaded = false;
                    StartCoroutine(ShowSlideLoadingStatus("Loading slide...", Color.black));
                    break;

                case SlideStatusUpdate.LOADED:
                    hasSlideLoaded = true;
                    StartCoroutine(ShowSlideLoadingStatus("Loaded", Color.green));
                    break;
            }
        }

        public override void Callback_OnUserLoginFromEditor(Action<string, string> userCredentials)
        {

        }

        public override void Callback_OnUserLogout()
        {
            InternalStates.Reset();

            //Delete downloaded files
            PrezSDKManager.DeleteDownloadedFiles();

            //Clear email and password fields
            userEmailIdInput.text = string.Empty;
            userPasswordInput.text = string.Empty;

            //Clear user login status
            userLoginStatusText.text = string.Empty;
            userLoginStatusText.color = Color.white;

            //Enable user login screen
            EnableUserLoginUI();
        }

        IEnumerator ShowSlideLoadingStatus(string slideLoadingStatus, Color slideLoadingStatusColor)
        {
            slideLoadingStatusText.text = slideLoadingStatus;
            slideLoadingStatusText.color = slideLoadingStatusColor;

            if (hasSlideLoaded)
            {
                yield return new WaitForSeconds(2f);
                slideLoadingStatusText.text = string.Empty;
                slideLoadingStatusText.color = Color.white;
            }
        }
    }
}