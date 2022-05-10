using AfterNow.PrezSDK.Internal.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace AfterNow.PrezSDK
{
    internal class AnimationTimeline
    {
        internal List<AnimationGroup> animationGroups = new List<AnimationGroup>();

        private AnimationGroup currentGroup;
        internal event Action<AnimationTimeline> OnTimelineComplete;
        internal event Action<AnimationGroup> OnGroupCompleted;
        internal readonly bool FirstElementAutomatic;
        internal readonly bool FirstElementHasTransitionAnimation;
        int currentPlayingIndex;

        internal AnimationTimeline(List<AnimationGroup> pAnimationGroups)
        {
            // Make sure AnimationGroupComplete gets called when any animation group completes
            animationGroups = pAnimationGroups;

            if (animationGroups.Count > 0 && animationGroups[0].animations.Count > 0 && animationGroups[0].animations[0].model.startType == AnimationStartType.Automatically)
            {
                FirstElementAutomatic = true;
            }
            if (animationGroups.Count > 0 && animationGroups[0].animations.Count > 0)
            {
                var anim = animationGroups[0].animations[0].model.animation;
                if (anim == AnimationType.None || anim == AnimationType.Appear)
                    FirstElementHasTransitionAnimation = false;
                else FirstElementHasTransitionAnimation = true;
            }

            foreach (AnimationGroup ag in animationGroups)
            {
                //Debug.Log("ag.onGroupComplete += AnimationGroupCompleted");
                ag.onGroupComplete += AnimationGroupCompleted;
            }
        }

        private void AnimationGroupCompleted(AnimationGroup pAnimationGroup)
        {
            //Debug.Log("AnimationGroupCompleted");
            /*        Debug.Log("~ animation group completed" + animationGroups.IndexOf(pAnimationGroup));
            */        // Once an animation group completes, the next group either plays automatically or waits on command
                      //UnityEngine.Debug.LogError("Animation group complete:"+$"{pAnimationGroup.GroupIndex} : {animationGroups.Count}");
            OnGroupCompleted?.Invoke(pAnimationGroup);

            int justPlayedIdx = pAnimationGroup.GroupIndex;
            if (justPlayedIdx + 1 >= animationGroups.Count)
            {
                /*            Debug.Log("FINISHED");
                */
                currentGroup = null;
                if (OnTimelineComplete != null)
                {
                    OnTimelineComplete.Invoke(this);
                }
                return;
            }
            AnimationGroup nextAnimationGroup = animationGroups[justPlayedIdx + 1];

            if (nextAnimationGroup == null)
            {
                /*            Debug.Log("~ last animation group");
                */
                // Last animation group just finished
                return;
            }

            currentGroup = nextAnimationGroup;
            currentPlayingIndex = justPlayedIdx + 1;

            if (nextAnimationGroup.animations[0].model.startType == AnimationStartType.AfterPreviousAnim)
            {
                /*            Debug.Log("~ next group automatic");
                */
                currentGroup = nextAnimationGroup;
                // Play animation group automatically since first animation plays automatically
                nextAnimationGroup.Play();

            }
            else if (nextAnimationGroup.animations[0].model.startType == AnimationStartType.OnCommand)
            {
                /*            Debug.Log("~ next group on command");
                */
                // Wait for on command since first animation plays on command
            }
        }

        /// <summary>
        /// Returns false when stuck at 
        /// </summary>
        /// <param name="pointNum"></param>
        /// <returns></returns>
        internal int Play(int pointNum, bool nextStep = true)
        {
            if (pointNum < 0)
            {
                if (!FirstElementAutomatic)
                {
                    Reset();
                    return -1;
                }
                else pointNum = 0;
            }

            AnimationGroup groupToFinish = null;
            AnimationGroup groupToPlay = null;
            bool stopAudio = true;
            for (int i = 0; i < animationGroups.Count; i++)
            {
                if (pointNum == i)
                {

                    currentPlayingIndex = i;
                    currentGroup = animationGroups[i];
                    if (currentGroup.isPlaying)
                    {
                        //Debug.LogError("Finishing group"); 
                        if (!nextStep)
                        {
                            groupToPlay = currentGroup;
                            ResetGroup(currentGroup);
                        }
                        else
                        {
                            groupToFinish = currentGroup;
                            stopAudio = false; //never stop audio which is playing by air taps until slide change
                        }
                        i++;
                    }
                    else if (currentGroup.animations[0].model.startType == AnimationStartType.OnCommand || //iteration group waiting to be played
                        (pointNum == 0 && currentGroup.animations[0].model.startType == AnimationStartType.Automatically)) //if first element and automatic
                    {
                        // Current group must be waiting on command to play
                        /*            Debug.Log("PLAYING on comand group");
                        */
                        groupToPlay = currentGroup;
                        //currentGroup.Play();
                        //Debug.LogError("Play next group on command");
                    }
                    else
                    {
                        pointNum++;
                        continue;
                    }
                }
                else if (i < pointNum)
                {
                    if (!animationGroups[i].hasFinished)
                    {
                        animationGroups[i].Finish();
                        Debug.Log("Finish1");
                    }
                }
                else
                {
                    ResetGroup(animationGroups[i]);
                }
            }

            if (groupToFinish != null)
            {
                groupToFinish.Finish(stopAudio);
                //Debug.LogError("Force finishing group");
                //Debug.Log("AnimationGroupCompleted(groupToFinish)");
                AnimationGroupCompleted(groupToFinish); //when we go to previous step, we dont need to do finish callback
            }
            else if (groupToPlay != null)
            {
                groupToPlay.Play();
            }
            return pointNum;
        }

        internal void Reset()
        {
            foreach (var group in animationGroups)
            {
                ResetGroup(group);
            }
            currentGroup = null;
            currentPlayingIndex = -1;
        }

        void ResetGroup(AnimationGroup group)
        {
            /*foreach (var anp in group.animations)
            {
                anp.Play(true, true);

                GameObject go = GameObject.Find(anp.asset.FileName());
                HideAndStop(go);
                SetInitialTransform(go);
            }
            group.Reset();*/
        }
    }
}
