using System;
using UnityEngine;

namespace AfterNow.PrezSDK
{
    public class ClickableAsset : MonoBehaviour
    {
        string clickTarget;
        Action<int> onClick;
        Action onDestroy;
        static PresentationManager presentationManager;

        public BoxCollider Collider
        {
            get
            {
                if (!_collider) _collider = GetComponent<BoxCollider>();
                return _collider;
            }
        }
        private BoxCollider _collider;

        internal void Initialize(string clickTarget, Action<int> onClick, Action onDestroy)
        {
            this.clickTarget = clickTarget;
            this.onClick = onClick;
            this.onDestroy = onDestroy;

            if (!presentationManager)
            {
                presentationManager = FindObjectOfType<PresentationManager>();
            }
        }

        public void OnClick()
        {
            onClick(GetClickableSlide(clickTarget));
        }

        static int GetClickableSlide(string clickTarget)
        {
            if (!string.IsNullOrEmpty(clickTarget))
            {
                string[] clickInfo = clickTarget.Split('/');
                if (clickInfo != null)
                {
                    if (clickInfo.Length == 2)
                    {
                        int presentationID = int.Parse(clickInfo[0]);
                        string slideID = clickInfo[1];
                        // a valid slide index must be grater then zero
                        if (!string.IsNullOrEmpty(slideID))
                        {
                            return presentationManager.GetSlideIndexFromId(slideID);
                        }
                    }
                    else if (clickInfo.Length == 1) //inter presentation. not supported yet
                    {
                        Debug.LogError("Inter slide clickable transition not supported");
                    }
                }
            }
            return -1;
        }

        private void OnDestroy()
        {
            onDestroy();
        }

        private void OnMouseDown()
        {
            OnClick();
        }
    }
}
