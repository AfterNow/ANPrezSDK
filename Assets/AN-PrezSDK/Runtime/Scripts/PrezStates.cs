
namespace AfterNow.PrezSDK.Internal.Views
{
    internal static class PrezStates
    {
        public static Presentation Presentation;

        /// <summary>
        /// From 0 to SlideCount - 1
        /// </summary>
        public static int CurrentSlide;
        public static int PresentationID;
        public static int PresenterID;

        public static void Reset()
        {
            Presentation = null;
            CurrentSlide = 0;
            PresentationID = 0;
            PresenterID = 0;
        }
    }
}