using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

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

        //Debug.Log("SetAnimation");
        _anim.SetAnimation(_transition, _asset, AnimationComplete, runningDelay);
        animations.Add(_anim);
        //Debug.Log("animations length : " + animations.Count);
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
        //Debug.Log("AnimationComplete");
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
            //Debug.Log("onGroupComplete not null, firing event");
            onGroupComplete(this);
        }
        else
        {
            //Debug.Log("onGroupComplete null");
        }
        isPlaying = false;
        hasFinished = true;
    }

    public void Play()
    {
        Debug.Log("AnimationGroup Play");
        if (animations.Count == 0)
        {
            //Debug.Log("animations are 0");
            return;
        }
        isPlaying = true;

        //Debug.Log("count : " + animations.Count);
        for (int i = 0; i < animations.Count; i++)
        {
            animations[i].Play(i == 0);
        }
    }

    public void Finish(bool stopAudio = true)
    {
        // Finish all animations and call Complete
        foreach (ANPAnimation _anim in animations)
        {
            _anim.Play(true, true, stopAudio);
        }
        isPlaying = false;
        hasFinished = true;
        Complete();
    }
}
