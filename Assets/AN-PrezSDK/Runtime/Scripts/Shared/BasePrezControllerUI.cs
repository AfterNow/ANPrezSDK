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
        public Func<string, bool> loadPresentationFromId;
        public Action<PresentationJoinStatus, string> onPresentationJoin;
        public Action<bool> onAuthorized;
        public Action nextStep;
        public Action nextSlide;
        public Action previousSlide;

        public void LoadPresentationFromId(GameObject presentationIdInput)
        {
            string presentationId = presentationIdInput.GetComponent<TMP_InputField>().text;
            loadPresentationFromId(presentationId);
        }

        public void OnAuthorized(bool isauthorized)
        {
            onAuthorized?.Invoke(isauthorized);
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
    }
}