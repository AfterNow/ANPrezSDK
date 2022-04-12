using System;

namespace AfterNow.PrezSDK.Shared.Enums
{
    /// <summary>
    /// An enum consisting of different progresses of a presentation
    /// </summary>
    [Serializable]
    public enum SlideProgressionType : sbyte
    {
        PreviousSlide = -1,
        ResetSlide = 0,
        NextSlide = 1
    }
}