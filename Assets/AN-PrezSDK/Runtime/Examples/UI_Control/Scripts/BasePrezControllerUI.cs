using AfterNow.PrezSDK.Shared;
using AfterNow.PrezSDK.Shared.Enums;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AfterNow.PrezSDK.Runtime.Examples
{
    public class BasePrezControllerUI : BaseController
    {
        public Func<string, bool> loadPresentationFromId;
        public Action<PresentationStatus, string> onPresentationJoin;
        public Action<bool> onAuthorized;
        public Action<string> onAuthorizationFailed;
        public Action<string> onAuthorizationSucceeded;

        string presentationSuccessMessage = string.Empty;
        string presentationFailedMessage = string.Empty;
        string presentationEndedMessage = string.Empty;

        private void OnEnable()
        {
            PrezSDKManager.OnSlideChange += ShowCurrentSlideNumber;
        }

        public override void UserLogin()
        {
            PrezSDKManager._instance.Login(userEmailIdInput.text, userPasswordInput.text);
        }

        public override void UserLogout()
        {
            PrezSDKManager._instance.Logout();
        }
        
        public void LoadPresentationFromId()
        {
            string presentationId = presentationIdInput.text;
            _onJoinPresentation?.Invoke(presentationId);
        }

        public void OnAuthorized(bool isauthorized)
        {
            onAuthorized?.Invoke(isauthorized);
        }

        public void OnAuthorizationFailed(string message)
        {
            onAuthorizationFailed?.Invoke(message);
        }

        public void OnAuthorizationSucceeded(string message)
        {
            onAuthorizationSucceeded?.Invoke(message);
        }

        public void OnPresentationJoin(PresentationStatus presentationJoinStatus, string presentationId)
        {
            onPresentationJoin?.Invoke(presentationJoinStatus, presentationId);
        }

        public void PresentationSuccessMessage(string _presentationSuccessMessage)
        {
            presentationSuccessMessage = _presentationSuccessMessage;
        }

        public void PresentationFailedMessage(string _presentationFailedMessage)
        {
            presentationFailedMessage = _presentationFailedMessage;
        }

        public void PresentationEndedMessage(string _presentationEndedMessage)
        {
            presentationEndedMessage = _presentationEndedMessage;
        }

        void ShowCurrentSlideNumber(int currentSlideNumber)
        {
            currentSlideNumberText.text = currentSlideNumber.ToString();
        }

        public override void Callback_OnPresentationJoin(PresentationStatus joinStatus, string presentationID)
        {
            this.presentationID = presentationID;
            if (presentationLoadStatusText != null)
            {
                switch (joinStatus)
                {
                    case PresentationStatus.SUCCESS:
                        presentationLoadStatusText.text = presentationSuccessMessage;
                        presentationLoadStatusText.color = Color.green;
                        Invoke(nameof(EnablePlayPresentationUI), 2f);
                        break;
                    case PresentationStatus.FAILED:
                        presentationLoadStatusText.text = presentationFailedMessage;
                        presentationLoadStatusText.color = Color.red;
                        break;
                    case PresentationStatus.ENDED:
                        presentationLoadStatusText.text = presentationEndedMessage;
                        presentationLoadStatusText.color = Color.green;
                        break;
                    default:
                        break;
                }
            }
            else
            {
                Debug.LogError("presentationStatusText is not assigned");
            }
        }

        public override void Callback_OnUserLoginFromEditor(Action<string, string> userCredentials)
        {
            userCredentials(userEmailId, userPassword);
        }

        public override void Callback_OnUserLogout()
        {
            Debug.Log("Callback_OnUserLogout");
        }

        public override void Callback_OnPresentationFailed(string presentationFailedReason)
        {
            Debug.Log("Callback_OnPresentationFailed");
        }

        public override void Callback_OnAuthenticationFailed(string authenticationFailedReason)
        {
            Debug.Log("Callback_OnAuthenticationFailed");
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
        }

        public override void Callback_OnSlideStatusUpdate(SlideStatusUpdate slideStatus)
        {
            slideLoadingStatusText.text = "Slide " + slideStatus.ToString().ToLower();
        }

        public override void Callback_OnSlideChange(int newSlide)
        {
            Debug.Log("Callback_OnSlideChange");
        }
    }
}