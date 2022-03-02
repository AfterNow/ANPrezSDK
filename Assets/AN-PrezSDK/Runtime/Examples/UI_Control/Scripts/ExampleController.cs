using AfterNow.PrezSDK.Shared;
using AfterNow.PrezSDK.Shared.Enums;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class ExampleController : BasePrezController
{
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

    public UnityEvent OnPresentationEnd;
    public UnityEvent OnSlideChange;
    public UnityEvent OnAuthorized;
    public UnityEvent OnPresentationJoin;
    public UnityEvent OnSlideStatusUpdate;
    public UnityEvent OnQuit;
    public UnityEvent OnNextStep;
    public UnityEvent OnNextSlide;
    public UnityEvent OnPreviousSlide;

    public override void Callback_OnPresentationEnd()
    {
        OnPresentationEnd?.Invoke();
        ReturnToLoginScreen();
    }

    public override void Callback_OnSlideChange(int newSlide)
    {
        OnSlideChange?.Invoke();
        CurrentSlideText.text = newSlide.ToString();
    }

    public override void Callback_OnAuthorized(bool result)
    {
        OnAuthorized?.Invoke();
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
                    StatusText.text = "Presentation ID should only contain numbers";
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
            Debug.LogError("Failed to authorize to AnPrez web server. Please try again");
        }
    }

    public override void Callback_OnPresentationJoin(PresentationJoinStatus joinStatus, string presentationID)
    {
        OnPresentationJoin?.Invoke();
        if(joinStatus == PresentationJoinStatus.SUCCESS)
        {
            LoginUI.SetActive(false);
            PresentationUI.SetActive(true);
            PresentationIDText.text = presentationID;
            StatusText.text = null;
        }
        else
        {
            Debug.LogError("Invalid presentation ID");
            StatusText.text = "Presentation ID invalid";
        }
    }

    public override void Callback_OnSlideStatusUpdate(SlideStatusUpdate slideStatus)
    {
        OnSlideStatusUpdate?.Invoke();
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
        OnQuit?.Invoke();
        QuitSession();
        LoginUI.SetActive(true);
        PresentationUI.SetActive(false);
        Login.interactable = true;
    }

    private void Start()
    {
        nextSlide.onClick.AddListener(() =>
        {
            OnNextSlide?.Invoke();
            NextSlide();
        });

        previousSlide.onClick.AddListener(() =>
        {
            OnPreviousSlide?.Invoke();
            PreviousSlide();
        });

        nextStep.onClick.AddListener(() =>
        {
            OnNextStep?.Invoke();
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
                StatusText.text = "Presentation ID should bonly contain numbers";
            }
        });
    }
}
