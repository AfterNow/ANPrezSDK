using System;
using System.Collections;
using UnityEngine;

namespace AfterNow.AnPrez.SDK.Unity.Interfaces
{
    public interface IPrezController
    {
        event Action<string> OnPresentationJoin;
        event Action OnNextStep;
        event Action OnNextSlide;
        event Action OnPrevSlide;
        event Action OnQuit;
        //Action<bool> OnAuthorized { get; }
        event Func<bool> OnAuthorized;
        event Func<int> OnSessionJoin;
    }
}