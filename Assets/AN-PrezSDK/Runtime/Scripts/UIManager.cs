using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace AfterNow.PrezSDK.UI
{
    public class UIManager : MonoBehaviour
    {
        //UIs
        [Header("UIs")]
        public GameObject userLoginUI;
        public GameObject loadPresentationUI;
        public GameObject playPresentationUI;

        //User Login UI
        [Header("User Login UI")]
        public TMP_InputField userEmailId;
        public TMP_InputField userPassword;
        public Button login;
        public TMP_Text loginStatus;

        //Load Presentation UI
        [Header("Load Presentation UI")]
        public TMP_InputField presentationId;
        public string _presentationId;
        public Button loadPresentation;
        public Button logout;
        public TMP_Text presentationLoadStatus;

        //Play Presentation UI
        [Header("Play Presentation UI")]
        public Button nextSlide;
        public Button previousSlide;
        public Button nextStep;
        public Button quit;
        public TMP_Text slideNumber;
        public TMP_Text presentationIdText;
        public TMP_Text slideLoadStatus;

        public void EnableUserLoginUI()
        {
            userLoginUI.SetActive(true);
            loadPresentationUI.SetActive(false);
            playPresentationUI.SetActive(false);
        }

        public void EnableLoadPresentationUI()
        {
            userLoginUI.SetActive(false);
            loadPresentationUI.SetActive(true);
            playPresentationUI.SetActive(false);
        }

        public void EnablePlayPresentationUI()
        {
            loadPresentationUI.SetActive(false);
            playPresentationUI.SetActive(true);
            presentationIdText.text = _presentationId;
        }

        public void ReturnToLoadPresentationScreen()
        {
            QuitSession();
            presentationLoadStatus.text = string.Empty;
            presentationLoadStatus.color = Color.white;
            loadPresentationUI.SetActive(true);
            playPresentationUI.SetActive(false);
        }

        public void QuitSession()
        {
            
        }

    }
}
