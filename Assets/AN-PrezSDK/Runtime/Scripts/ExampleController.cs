using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AfterNow.PrezSDK.UI;
using AfterNow.PrezSDK.Shared.Enums;

[RequireComponent(typeof(UIManager))]
public class ExampleController : Base
{
    public UIManager uIManager;
    
    // Start is called before the first frame update
    void Start()
    {
        uIManager.login.onClick.AddListener(() =>
        {
            UserLogin();
        });

        uIManager.logout.onClick.AddListener(() =>
        {
            UserLogout();
        });

        uIManager.loadPresentation.onClick.AddListener(() =>
        {

        });

        uIManager.nextSlide.onClick.AddListener(() =>
        {

        });

        uIManager.previousSlide.onClick.AddListener(() =>
        {

        });

        uIManager.nextStep.onClick.AddListener(() =>
        {

        });

        uIManager.quit.onClick.AddListener(() =>
        {

        });

    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public override void Callback_OnAuthenticationFailed(string authenticationFailedReason)
    {
    }

    public override void Callback_OnAuthorized(bool result)
    {
        Debug.Log("auth is " + result);
    }

    public override void Callback_OnPresentationEnd()
    {
    }

    public override void Callback_OnPresentationFailed(string presentationFailedReason)
    {
    }

    public override void Callback_OnPresentationJoin(PresentationStatus joinStatus, string presentationID)
    {
    }

    public override void Callback_OnSlideChange(int newSlide)
    {
    }

    public override void Callback_OnSlideStatusUpdate(SlideStatusUpdate slideStatus)
    {
    }

    public override void UserLogin()
    {
        PrezSDKManager._instance.Login(uIManager.userEmailId.text, uIManager.userPassword.text);
    }

    public override void UserLogout()
    {
        PrezSDKManager._instance.Logout();
    }
}
