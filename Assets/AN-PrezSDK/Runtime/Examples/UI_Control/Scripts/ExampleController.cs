using AfterNow.PrezSDK.Shared;
using AfterNow.PrezSDK.Shared.Enums;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ExampleController : BasePrezController
{
    [SerializeField] string username;
    [SerializeField] string password;
    [SerializeField] string defaultPresentationID;

    [SerializeField] TMP_Text PresentationIDText;
    [SerializeField] TMP_Text CurrentSlideText;
    [SerializeField] TMP_Text SlideLoadingStatusText;
    [SerializeField] Text StatusText;

    [SerializeField] Button nextSlide;
    [SerializeField] Button previousSlide;
    [SerializeField] Button nextStep;
    [SerializeField] Button Quit;
    [SerializeField] Button Login;

    [SerializeField] GameObject LoginUI;
    [SerializeField] GameObject PresentationUI;
    [SerializeField] InputField PresentationID;

    public override void Callback_OnPresentationEnd()
    {
        ReturnToLoginScreen();
    }

    public override void Callback_OnSlideChange(int newSlide)
    {
        CurrentSlideText.text = newSlide.ToString();
    }

    public override void Callback_OnAuthorized(bool result)
    {
        if(result)
        {
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
                LoginUI.SetActive(true);
                PresentationUI.SetActive(false);
            }
        }
        else
        {
            Callback_OnPresentationFailed("Failed to authorize to AnPrez web server");
        }
    }

    public override void Callback_OnPresentationJoin(PresentationJoinStatus joinStatus, string presentationID)
    {
        if(joinStatus == PresentationJoinStatus.SUCCESS)
        {
            LoginUI.SetActive(false);
            PresentationUI.SetActive(true);
            PresentationIDText.text = presentationID;
            StatusText.text = null;
        }
        else
        {
            Callback_OnPresentationFailed("Presentation ID invalid");
        }
    }

    public override void Callback_OnSlideStatusUpdate(SlideStatusUpdate slideStatus)
    {
        switch(slideStatus)
        {
            case SlideStatusUpdate.LOADING:
                SlideLoadingStatusText.text = "Loading slide...";
                break;
            case SlideStatusUpdate.LOADED:
                SlideLoadingStatusText.text = null;
                break;
        }
    }

    public void ReturnToLoginScreen()
    {
        QuitSession();
        LoginUI.SetActive(true);
        PresentationUI.SetActive(false);
        Login.interactable = true;
    }

    private void Start()
    {
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

        LoginUI.SetActive(false);
        PresentationUI.SetActive(false);

        Quit.onClick.AddListener(() =>
        {
           ReturnToLoginScreen();
        });

        Login.onClick.AddListener(() =>
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
    }

    public override void Callback_OnPresentationFailed(string presentationFailedReason)
    {
        StatusText.text = presentationFailedReason;
        LoginUI.SetActive(true);
    }

    public override void Callback_OnEnteredUserCredentials(Action<string,string> userCredentials)
    {
        userCredentials(username, password);
    }
}
