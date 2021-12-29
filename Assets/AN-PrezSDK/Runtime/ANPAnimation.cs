using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using AfterNow.AnPrez.SDK.Unity;
using AfterNow.AnPrez.SDK.Internal.Views;
namespace Assets.AN_PrezSDK.Runtime
{
    public class ANPAnimation
    {
        public ARPAsset asset;
        public Action<ANPAnimation> callback;

        public float totalLength; // Total length (includes delay, animation, asset animation/play length)
        public float delay;

        public ARPTransition model;

        public void SetAnimation(ARPTransition _transition, ARPAsset _asset, Action<ANPAnimation> _pCallback = null, float _pDelay = 0)
        {
            
            model = _transition;
            delay = _pDelay;
            totalLength = _pDelay + _transition.animationDuration;
            asset = _asset;

            switch (asset.type)
            {
                case ANPAssetType.IMAGE:
                    break;
                case ANPAssetType.VIDEO:
                    break;
                case ANPAssetType.OBJECT:
                    break;
                case ANPAssetType.AUDIO:
                    break;
                case ANPAssetType.TEXT:
                    break;
                default:
                    break;
            }

            callback = _pCallback;

        }

    }
}
