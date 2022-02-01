using System;

namespace AfterNow.PrezSDK.Internal.Views
{
    [Serializable]
    public class ARPTransition
    {
        public string id;
        public AnimationType animation;
        public float animationDuration;
        public AnimationStartType startType;
        public float atTime;
        public string assetId;
        public string internalAnimation;
        public ARPAnimationWrap internalAnimationWrap = ARPAnimationWrap.None;
    }

    [Serializable]
    public class ARPSlideTransition : ARPTransition
    {
        public new SlideAnimationType animation;
        public float delay;
    }

    public enum AnimationType
    {
        None, Appear, FadeIn, ScaleIn, BlurIn, PopIn, LeftSwooshIn, RightSwooshIn,
        RightSpinIn, LeftSpinIn, Disappear, FadeOut, ScaleOut, BlurOut, PopOut, LeftSwooshOut,
        RightSwooshOut, RightSpinOut, LeftSpinOut, TopSwooshIn, TopSwooshOut, BottomSwooshIn, BottomSwooshOut, StartRotationRight, StopRotation, StartRotationLeft
    }

    public enum SlideAnimationType { None, Disappear, ScaleOut }

    public enum AnimationStartType { None, OnCommand, Automatically, WithPreviousAnim, AfterPreviousAnim }

    public enum ARPAnimationWrap { None, Once, Clamp, Loop, Pingpong }
}
