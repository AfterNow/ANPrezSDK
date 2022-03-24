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

        public void LoadPresentationFromId()
        {
            string presentationId = presentationIdInput.text;
            _onJoinPresentation(presentationId);
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
            if (presentationLoadStatusText != null)
            {
                switch (joinStatus)
                {
                    case PresentationStatus.SUCCESS:
                        presentationLoadStatusText.text = presentationSuccessMessage;
                        break;
                    case PresentationStatus.FAILED:
                        presentationLoadStatusText.text = presentationFailedMessage;
                        break;
                    case PresentationStatus.ENDED:
                        presentationLoadStatusText.text = presentationEndedMessage;
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
            throw new NotImplementedException();
        }

        public override void Callback_OnUserLogout()
        {
            throw new NotImplementedException();
        }

        public override void Callback_OnPresentationFailed(string presentationFailedReason)
        {
            throw new NotImplementedException();
        }

        public override void Callback_OnAuthenticationFailed(string authenticationFailedReason)
        {
            throw new NotImplementedException();
        }

        public override void Callback_OnAuthorized(bool result)
        {
            throw new NotImplementedException();
        }

        public override void Callback_OnPresentationEnd()
        {
            throw new NotImplementedException();
        }

        public override void Callback_OnSlideStatusUpdate(SlideStatusUpdate slideStatus)
        {
            slideLoadingStatusText.text = "Slide " + slideStatus.ToString().ToLower();
        }

        public override void Callback_OnSlideChange(int newSlide)
        {
            throw new NotImplementedException();
        }
    }
}