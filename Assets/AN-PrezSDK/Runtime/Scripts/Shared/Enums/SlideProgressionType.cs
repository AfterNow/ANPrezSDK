using System;

namespace AfterNow.PrezSDK.Shared.Enums
{
    [Serializable]
    public enum SlideProgressionType : sbyte
    {
        PreviousSlide = -1,
        ResetSlide = 0,
        NextSlide = 1
    }
}