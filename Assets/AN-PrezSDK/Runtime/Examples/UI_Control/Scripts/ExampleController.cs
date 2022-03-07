using AfterNow.PrezSDK.Shared;
using AfterNow.PrezSDK.Shared.Enums;
using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ExampleController : BasePrezController
{
    [SerializeField] PrezSDKManager prezSDKManager;
    [SerializeField] string userEmailId;
    [SerializeField] string userPassword;
    [SerializeField] string defaultPresentationID;

    [SerializeField] TMP_InputField userEmailIdInput;
    [SerializeField] TMP_InputField userPasswordInput;

    [SerializeField] TMP_Text PresentationIDText;
    [SerializeField] TMP_Text CurrentSlideText;
    [SerializeField] TMP_Text userLoginStatusText;
    [SerializeField] TMP_Text presentationLoadStatusText;
    [SerializeField] TMP_Text SlideLoadingStatusText;

    [SerializeField] Button nextSlide;
    [SerializeField] Button previousSlide;
    [SerializeField] Button nextStep;
    [SerializeField] Button Quit;
    [SerializeField] Button LoadPresentation;
    [SerializeField] Button userLogin;

    [SerializeField] GameObject LoadPresentationUI;
    [SerializeField] GameObject PlayPresentationUI;
    [SerializeField] GameObject UserAccountLoginUI;
    [SerializeField] TMP_InputField PresentationID;
    
    string presentationID = null;
    bool hasSlideLoaded = false;

    public override void Callback_OnPresentationEnd()
    {
        ReturnToLoadPresentationScreen();
    }

    public override void Callback_OnSlideChange(int newSlide)
    {
        CurrentSlideText.text = newSlide.ToString();
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
                Invoke("EnableLoadPresentationUI", 2f);
            }

            UserAccountLoginUI.SetActive(false);
        }
        else
        {
            Callback_OnAuthenticationFailed("Failed to authorize to AnPrez web server");
        }
    }

    public override void Callback_OnPresentationJoin(PresentationJoinStatus joinStatus, string presentationID)
    {
        this.presentationID = presentationID;

        if (joinStatus == PresentationJoinStatus.SUCCESS)
        {
            presentationLoadStatusText.text = "Loading Presentation...";
            presentationLoadStatusText.color = Color.green;
            Invoke("EnablePlayPresentationUI", 2f);
        }
        else
        {
            Callback_OnPresentationFailed("Presentation ID invalid");
        }
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

    public void ReturnToLoadPresentationScreen()
    {
        QuitSession();
        presentationLoadStatusText.text = string.Empty;
        presentationLoadStatusText.color = Color.white;
        LoadPresentationUI.SetActive(true);
        PlayPresentationUI.SetActive(false);
        LoadPresentation.interactable = true;
    }

    public void UserLogin()
    {
        userLoginStatusText.text = "Authenticating...";
        userLoginStatusText.color = Color.black;
        prezSDKManager.Login(userEmailIdInput.text, userPasswordInput.text);
    }

    private void Start()
    {
        //Clear Status Texts
        userLoginStatusText.text = string.Empty;
        presentationLoadStatusText.text = string.Empty;
        SlideLoadingStatusText.text = string.Empty;

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
        });

        LoadPresentation.onClick.AddListener(() =>
        {
            string presentationID = PresentationID.text;
            if (int.TryParse(presentationID.Trim(), out int integerPresentID))
            {
                JoinPresentation(presentationID);
            }
            else
            {
                Callback_OnPresentationFailed("Presentation ID should only contain numbers");
            }
        });

        userLogin.onClick.AddListener(() =>
        {
            UserLogin();
        });
    }

    public override void Callback_OnPresentationFailed(string presentationFailedReason)
    {
        presentationLoadStatusText.text = presentationFailedReason;
        presentationLoadStatusText.color = Color.red;
        LoadPresentationUI.SetActive(true);
    }

    public override void Callback_OnUserLoginFromEditor(Action<string, string> userCredentials)
    {
        userCredentials(userEmailId, userPassword);
    }

    public override void Callback_OnAuthenticationFailed(string authenticationFailedReason)
    {
        userLoginStatusText.text = authenticationFailedReason;
        userLoginStatusText.color = Color.red;
    }

    void EnableLoadPresentationUI()
    {
        UserAccountLoginUI.SetActive(false);
        LoadPresentationUI.SetActive(true);
        PlayPresentationUI.SetActive(false);
    }

    void EnablePlayPresentationUI()
    {
        LoadPresentationUI.SetActive(false);
        PlayPresentationUI.SetActive(true);
        PresentationIDText.text = presentationID;
    }

    IEnumerator ShowSlideLoadingStatus(string slideLoadingStatus, Color slideLoadingStatusColor)
    {
        SlideLoadingStatusText.text = slideLoadingStatus;
        SlideLoadingStatusText.color = slideLoadingStatusColor;

        if (hasSlideLoaded)
        {
            yield return new WaitForSeconds(2f);
            SlideLoadingStatusText.text = string.Empty;
            SlideLoadingStatusText.color = Color.white;
        }
    }
}
