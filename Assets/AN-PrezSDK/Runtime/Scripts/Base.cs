using AfterNow.PrezSDK.Shared.Enums;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public abstract class Base : MonoBehaviour
{
    public UnityEvent _login;
    public UnityEvent _logout;
    public UnityEvent _loadPresentation;
    public Func<string, bool> _onJoinPresentation;
    public UnityEvent _nextSlide;
    public UnityEvent _previousSlide;
    public UnityEvent _nextStep;
    public UnityEvent _quit;

    #region User Login Functions

    /// <summary>
    /// This function is called when the user clicks on the Login button
    /// </summary>
    public abstract void UserLogin();

    /// <summary>
    /// This function is called when the user clicks on the Logout button
    /// </summary>
    public abstract void UserLogout();

    #endregion

    #region User Login Events

    /// <summary>
    /// This callback is invoked if the user has entered their login credentials. 
    /// </summary>
    /*public virtual void UserLoginFromEditor(Action<string, string> userCredentials)
    {
        userCredentials(userEmailId, userPassword);
    }*/

    #endregion

    #region Presentation Functions

    /// <summary>
    /// Call this function with Presentation ID after authorization is successful.
    /// Does not impact the presentation if presentation cannot be joined at the moment.
    /// This function can also be called from Unity's inspector
    /// </summary>
    /// <param name="presentationID"></param>
    public void JoinPresentation(string presentationID)
    {
        _onJoinPresentation(presentationID);
    }

    /// <summary>
    /// Call this function to proceed to the next step in the current slide.
    /// </summary>
    public void NextStep()
    {
        _nextStep.Invoke();
    }

    /// <summary>
    /// Call this function to proceed to the next slide in the presentation.
    /// </summary>
    public void NextSlide()
    {
        _nextSlide.Invoke();
    }

    /// <summary>
    /// Call this function to go back to the previous slide in the presentation.
    /// </summary>
    public void PreviousSlide()
    {
        _previousSlide.Invoke();
    }

    /// <summary>
    /// Call this function to quit the session, destroy all assets and clean up the memory
    /// </summary>
    public void QuitSession()
    {
        _quit.Invoke();
    }

    #endregion


    #region Presentation Events

    /// <summary>
    /// This callback is invoked automatically after the presentation has been joined. 
    /// </summary>
    /// <param name="result"></param>
    public abstract void Callback_OnPresentationJoin(PresentationStatus joinStatus, string presentationID);

    /// <summary>
    /// This callback is invoked if a presentation fails to load. 
    /// </summary>
    public abstract void Callback_OnPresentationFailed(string presentationFailedReason);

    /// <summary>
    /// This callback is invoked if a presentation fails to load. 
    /// </summary>
    public abstract void Callback_OnAuthenticationFailed(string authenticationFailedReason);

    /// <summary>
    /// This callback is invoked 
    /// </summary>
    /// <param name="result"></param>
    public abstract void Callback_OnAuthorized(bool result);

    /// <summary>
    /// This callback is invoked when the presentation ends
    /// </summary>
    public abstract void Callback_OnPresentationEnd();


    #endregion

    #region Slide Functions


    #endregion


    #region Slide Events

    /// <summary>
    /// This callback is invoked when the new slide status is updated
    /// </summary>
    /// <param name="slideStatus"></param>
    public abstract void Callback_OnSlideStatusUpdate(SlideStatusUpdate slideStatus);

    /// <summary>
    /// This callback is invoked when the slide is changed
    /// </summary>
    /// <param name="newSlide"></param>
    public abstract void Callback_OnSlideChange(int newSlide);

    /// <summary>
    /// This callback is invoked when any of the asset in presentation fails to load
    /// </summary>
    /// <param name="path"></param>
    public virtual void Callback_OnAssetFailedToLoad(string path)
    {
        Debug.LogError($"Asset at {path} failed to load");
    }


    #endregion

}
