using AfterNow.PrezSDK.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace AfterNow.PrezSDK.Shared
{
    /// <summary>
    /// Base script of the AN-Prez SDK which has the callbacks and functions which has to be derived by the user's custom script
    /// </summary>
    public abstract class BaseController : MonoBehaviour
    {
        #region public events
        public Func<string, bool> _onJoinPresentation;
        public Action _nextStep;
        public Action _nextSlide;
        public Action _previousSlide;
        public Action _quit;
        #endregion

        #region public variables
        public TMP_InputField presentationIdInput;
        public TMP_Text presentationLoadStatusText;
        public string userEmailId;
        public string userPassword;
        public string defaultPresentationID;
        public TMP_InputField userEmailIdInput;
        public TMP_InputField userPasswordInput;
        public TMP_Text presentationIDText;
        public TMP_Text currentSlideNumberText;
        public TMP_Text userLoginStatusText;
        public TMP_Text slideLoadingStatusText;

        #endregion

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
        public abstract void Callback_OnPresentationJoin(PresentationStatus joinStatus, string presentationID);

        /// <summary>
        /// This callback is invoked if the user has entered their login credentials. 
        /// </summary>
        public virtual void Callback_OnUserLoginFromEditor(Action<string, string> userCredentials)
        {
            userCredentials(userEmailId, userPassword);
        }

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
        public abstract void Callback_OnPresentationEnd();

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

        /// <summary>
        /// This function is called to initialize the events from PrezSDKManager.
        /// Should never be called by the user.
        /// </summary>
        /// <param name="onJoinPresentation"></param>
        /// <param name="nextStep"></param>
        /// <param name="nextSlide"></param>
        /// <param name="previousSlide"></param>
        internal void AssignEvents(Func<string, bool> onJoinPresentation, Action nextStep, Action nextSlide, Action previousSlide, Action quit)
        {
            _onJoinPresentation = onJoinPresentation;
            _nextSlide = nextSlide;
            _nextStep = nextStep;
            _previousSlide = previousSlide;
            _quit = quit;
        }
    }
}
