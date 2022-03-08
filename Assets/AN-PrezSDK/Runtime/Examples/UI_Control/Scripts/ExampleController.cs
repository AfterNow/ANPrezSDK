using AfterNow.PrezSDK.Internal.Helpers;
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
    [SerializeField] Button Login;
    [SerializeField] Button Logout;

    [SerializeField] GameObject LoadPresentationUI;
    [SerializeField] GameObject PlayPresentationUI;
    [SerializeField] GameObject UserLoginUI;
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

            UserLoginUI.SetActive(false);
        }
        else
        {
            Callback_OnAuthenticationFailed("Incorrect Email or Password");
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

    public void UserLogout()
    {
        prezSDKManager.Logout();
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

        Login.onClick.AddListener(() =>
        {
            UserLogin();
        });

        Logout.onClick.AddListener(() =>
        {
            UserLogout();
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

    void EnableUserLoginUI()
    {
        UserLoginUI.SetActive(true);
        LoadPresentationUI.SetActive(false);
        PlayPresentationUI.SetActive(false);
    }

    void EnableLoadPresentationUI()
    {
        UserLoginUI.SetActive(false);
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

    public override void Callback_OnUserLogout()
    {
        InternalStates.Reset();

        //Delete downloaded files
        PrezSDKManager.DeleteDownloadedFiles();

        //Clear email and password fields
        userEmailIdInput.text = string.Empty;
        userPasswordInput.text = string.Empty;

        //Clear user login status
        userLoginStatusText.text= string.Empty;
        userLoginStatusText.color= Color.white;

        //Enable user login screen
        EnableUserLoginUI();
    }
}
