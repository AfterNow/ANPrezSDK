using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using AfterNow.AnPrez.SDK.Unity;
using AfterNow.AnPrez.SDK.Internal.Views;

namespace Assets.AN_PrezSDK.Runtime
{
    public class AnimationGroup
    {

        public List<ANPAnimation> animations = new List<ANPAnimation>();
        public event Action<AnimationGroup> onGroupComplete;
        public event Action<ANPAnimation> OnAnimationComplete;
        private float longestAnimTime = 0;
        private ANPAnimation longestAnim;

        private float runningDelay = 0;

        public bool isPlaying;
        public bool hasFinished;

        public int GroupIndex { get; private set; }

        int finishedAnimations = 0;

        public AnimationGroup(int groupNum)
        {
            GroupIndex = groupNum;
        }
        public void AddAnimation(ARPTransition _transition, ARPAsset _asset)
        {
            ANPAnimation _anim = new ANPAnimation();
            runningDelay += _transition.atTime;

            _anim.SetAnimation(_transition, _asset, AnimationComplete, runningDelay);
            animations.Add(_anim);

            FindLongest();

        }

        private void FindLongest()
        {
            foreach (ANPAnimation animation in animations)
            {
                if (longestAnimTime <= animation.totalLength)
                {
                    longestAnimTime = animation.totalLength;
                    longestAnim = animation;
                }
            }
        }

        void AnimationComplete(ANPAnimation animation)
        {
            OnAnimationComplete?.Invoke(animation);
            if (++finishedAnimations == animations.Count)
            {
                Complete();
                finishedAnimations = 0;
            }
        }

        private void Complete()
        {
            if (onGroupComplete != null)
            {
                onGroupComplete(this);
            }
            isPlaying = false;
            hasFinished = true;
        }

    }
}
