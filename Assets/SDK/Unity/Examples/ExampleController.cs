using AfterNow.AnPrez.SDK.Unity.Interfaces;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace AfterNow.AnPrez.SDK.Unity.Examples
{
    public class ExampleController : MonoBehaviour, IPrezController
    {
        public event Action<string> OnPresentationJoin;
        public event Action OnNextStep;
        public event Action OnNextSlide;
        public event Action OnPrevSlide;
        public event Action OnQuit;
        public event Func<bool> OnAuthorized;
        public event Func<int> OnSessionJoin;

        [SerializeField] Button nextSlide;
        [SerializeField] Button previousSlide;
        [SerializeField] Button nextStep;
        [SerializeField] string presentationID;

        [SerializeField] GameObject LoginUI;
        [SerializeField] GameObject PresentationUI;
        [SerializeField] Button Quit;
        [SerializeField] Button Login;
        [SerializeField] InputField PresentationID;
        [SerializeField] Text StatusText;

        private void Start()
        {
            nextSlide.onClick.AddListener(() =>
            {
                OnNextSlide?.Invoke();
            });

            previousSlide.onClick.AddListener(() =>
            {
                OnPrevSlide?.Invoke();
            });

            nextStep.onClick.AddListener(() =>
            {
                OnNextStep?.Invoke();
            });


            StartCoroutine(PollLogin());

            LoginUI.SetActive(false);
            PresentationUI.SetActive(false);
            Quit.onClick.AddListener(() => 
            {
                OnQuit?.Invoke();
                LoginUI.SetActive(true);
                PresentationUI.SetActive(false);
            });

            Login.onClick.AddListener(() =>
            {
                string presentationID = PresentationID.text;
                if (int.TryParse(presentationID.Trim(), out int integerPresentID))
                {
                    OnPresentationJoin?.Invoke(presentationID);
                    StartCoroutine(OnSessionLogin());
                }
                else
                {
                    StatusText.text = "Presentation ID should bonly contain numbers";
                }
            });


        }

        IEnumerator OnSessionLogin()
        {
            while (true)
            {
                int result = OnSessionJoin();

                if (result == 1)
                {
                    LoginUI.SetActive(false);
                    PresentationUI.SetActive(true);

                    StatusText.text = null;
                    yield break;

                }
                else if (result == -1)
                {
                    StatusText.text = "Presentation ID invalid";
                    yield break;
                }
                else
                {
                    yield return null;
                }
            }
            
        }
        private IEnumerator PollLogin()
        {
            while (true)
            {
                bool isAuthorized = OnAuthorized();
                if (isAuthorized)
                {
                    if (!string.IsNullOrEmpty(presentationID))
                    {
                        if (int.TryParse(presentationID.Trim(), out int integerPresentationID))
                        {
                            OnPresentationJoin?.Invoke(presentationID);
                            StartCoroutine(OnSessionLogin());
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
                    yield break;
                }
                yield return null;
            }
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.J))
            {
            }
        }
    }
}