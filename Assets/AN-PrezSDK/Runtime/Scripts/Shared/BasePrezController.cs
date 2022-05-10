using AfterNow.PrezSDK.Shared.Enums;
using System;
using UnityEngine;

namespace AfterNow.PrezSDK.Shared
{
    public abstract class BasePrezController : MonoBehaviour
    {
        /// <summary>
        /// Call this function with username and password to login to an account.
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        public void Login(string username, string password)
        {
            sdkManager.Login(username, password);
        }

        /// <summary>
        /// Call this function to logout to login with a new account.
        /// This cleans up and frees the resources allocated by SDK in the session.
        /// </summary>
        public void Logout()
        {
            sdkManager.Logout();
        }

        /// <summary>
        /// Call this function with Presentation ID after Authorization is successful.
        /// Returns false if presentation cannot be joined at the moment. 
        /// Returns true if the request has been taken into consideration.
        /// </summary>
        /// <param name="presentationID"></param>
        /// <returns></returns>
        public bool JoinPresentationSafe(string presentationID)
        {
            return _onJoinPresentation(presentationID);
        }

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
            _nextStep();
        }

        /// <summary>
        /// Call this function to proceed to the next slide in the presentation.
        /// </summary>
        public void NextSlide()
        {
            _nextSlide();
        }

        /// <summary>
        /// Call this function to go back to the previous slide in the presentation.
        /// </summary>
        public void PreviousSlide()
        {
            _previousSlide();
        }

        /// <summary>
        /// Call this function to quit the session, destroy all assets and clean up the memory
        /// </summary>
        public void QuitSession()
        {
            _quit();
        }


        /// <summary>
        /// This callback is invoked automatically after the presentation has been joined. 
        /// </summary>
        /// <param name="result"></param>
        public abstract void Callback_OnPresentationJoin(PresentationJoinStatus joinStatus, string presentationID);

        /// <summary>
        /// This callback is invoked if the user has entered their login credentials. 
        /// </summary>
        public abstract void Callback_OnUserLoginFromEditor(Action<string, string> userCredentials);

        /// <summary>
        /// This callback is invoked if the user logsout. 
        /// </summary>
        public abstract void Callback_OnUserLogout();

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
        public virtual void Callback_OnPresentationEnd()
        {
        }

        /// <summary>
        /// This callback is invoked when the new slide status is updated
        /// </summary>
        /// <param name="slideStatus"></param>
        public virtual void Callback_OnSlideStatusUpdate(SlideStatusUpdate slideStatus)
        {
        }

        /// <summary>
        /// This callback is invoked when the slide is changed
        /// </summary>
        /// <param name="newSlide"></param>
        public virtual void Callback_OnSlideChange(int newSlide)
        {
        }

        /// <summary>
        /// This callback is invoked when any of the asset in presentation fails to load
        /// </summary>
        /// <param name="path"></param>
        public virtual void Callback_OnAssetFailedToLoad(string path)
        {
            Debug.LogError($"Asset at {path} failed to load");
        }

        /// <summary>
        /// This function is called to initialize the events from PrezSDKManager.
        /// Should never be called by the user.
        /// </summary>
        /// <param name="onJoinPresentation"></param>
        /// <param name="nextStep"></param>
        /// <param name="nextSlide"></param>
        /// <param name="previousSlide"></param>
        internal void AssignEvents(Func<string,bool> onJoinPresentation, Action nextStep, Action nextSlide, Action previousSlide, Action quit)
        {
            _onJoinPresentation = onJoinPresentation;
            _nextSlide = nextSlide;
            _nextStep = nextStep;
            _previousSlide = previousSlide;
            _quit = quit;
        }

        /// <summary>
        /// This callback is invoked after the PrezSDKManager has been initialized (after Awake).
        /// Store the PrezSDKManager object reference to access functions like Login and Logout
        /// </summary>
        /// <param name="sdkManager"></param>
        internal void OnSDKInitialize(PrezSDKManager sdkManager)
        {
            this.sdkManager = sdkManager;
        }

        private Func<string, bool> _onJoinPresentation;
        private Action _nextStep;
        private Action _nextSlide;
        private Action _previousSlide;
        private Action _quit;
        private PrezSDKManager sdkManager;
    }
}
