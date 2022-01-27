using System;
using System.Collections;
using UnityEngine;

    public interface IPrezController
    {
        event Action<string> OnPresentationJoin;
        event Action OnNextStep;
        event Action OnNextSlide;
        event Action OnPrevSlide;
        event Action OnQuit;
        event Action OnPresentationEnded;
    //Action<bool> OnAuthorized { get; }
    event Func<bool> OnAuthorized;
        event Func<int> OnSessionJoin;
    }
