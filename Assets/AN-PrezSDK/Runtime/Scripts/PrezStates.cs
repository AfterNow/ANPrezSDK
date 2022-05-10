
using AfterNow.PrezSDK.Internal.Views;

namespace AfterNow.PrezSDK
{
    internal static class PrezStates
    {
        internal static Presentation Presentation;

        /// <summary>
        /// From 0 to SlideCount - 1
        /// </summary>
        internal static int CurrentSlide;
        internal static int PresentationID;
        internal static int PresenterID;

        internal static void Reset()
        {
            Presentation = null;
            CurrentSlide = 0;
            PresentationID = 0;
            PresenterID = 0;
        }
    }
}