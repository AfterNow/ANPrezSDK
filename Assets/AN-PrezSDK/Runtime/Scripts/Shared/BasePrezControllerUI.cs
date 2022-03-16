using AfterNow.PrezSDK.Internal.Helpers;
using AfterNow.PrezSDK.Shared.Enums;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace AfterNow.PrezSDK.Shared
{
    public class BasePrezControllerUI : MonoBehaviour
    {
        public TMP_InputField presentationIdInput;
        public TMP_Text presentationStatusText;
        public TMP_Text currentSlideNumberText;
        public TMP_Text slideStatusText;
        public Func<string, bool> loadPresentationFromId;
        public Action<PresentationJoinStatus, string> onPresentationJoin;
        public Action<bool> onAuthorized;
        public Action nextStep;
        public Action nextSlide;
        public Action previousSlide;
        public Action quit;
        public Action<string> onAuthorizationFailed;
        public Action<string> onAuthorizationSucceeded;

        string presentationSuccessMessage = string.Empty;
        string presentationFailedMessage = string.Empty;
        string presentationEndedMessage = string.Empty;

        private void OnEnable()
        {
            PrezSDKManager.OnPresentationStatus += ShowPresentationStatusMessage;
            PrezSDKManager.OnSlideStatusUpdate += ShowSlideStatusMessage;
            PrezSDKManager.OnSlideChange += ShowCurrentSlideNumber;
        }

        public void LoadPresentationFromId()
        {
            string presentationId = presentationIdInput.text;
            loadPresentationFromId(presentationId);
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

        public void OnPresentationJoin(PresentationJoinStatus presentationJoinStatus, string presentationId)
        {
            onPresentationJoin?.Invoke(presentationJoinStatus, presentationId);
        }

        public void NextStep()
        {
            nextStep?.Invoke();
        }

        public void NextSlide()
        {
            nextSlide?.Invoke();
        }

        public void PreviousSlide()
        {
            previousSlide?.Invoke();
        }

        public void Quit()
        {
            quit?.Invoke();
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

        void ShowPresentationStatusMessage(PrezSDKManager.PresentationStatus presentationStatus)
        {
            if (presentationStatusText != null)
            {
                switch (presentationStatus)
                {
                    case PrezSDKManager.PresentationStatus.SUCCESS:
                        presentationStatusText.text = presentationSuccessMessage;
                        break;
                    case PrezSDKManager.PresentationStatus.FAILURE:
                        presentationStatusText.text = presentationFailedMessage;
                        break;
                    case PrezSDKManager.PresentationStatus.ENDED:
                        presentationStatusText.text = presentationEndedMessage;
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

        void ShowSlideStatusMessage(SlideStatusUpdate slideStatusUpdate)
        {
            slideStatusText.text = slideStatusUpdate.ToString();
        }

        void ShowCurrentSlideNumber(int currentSlideNumber)
        {
            currentSlideNumberText.text = currentSlideNumber.ToString();
        }
    }
}