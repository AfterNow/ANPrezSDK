using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AfterNow.AnPrez.SDK.Internal.Views;
using AfterNow.AnPrez.SDK.Unity;
using UnityEngine;

namespace Assets.AN_PrezSDK.Runtime
{
    public class ANPAnimation
    {
        public ARPAsset asset;
        public Action<ANPAnimation> callback;

        public float totalLength; // Total length (includes delay, animation, asset animation/play length)
        public float delay;

        public ARPTransition model;
        private bool isGLB;
        GameObject assetGO = null;

        public void SetAnimation(ARPTransition _transition, ARPAsset _asset, Action<ANPAnimation> _pCallback = null, float _pDelay = 0)
        {

            model = _transition;
            delay = _pDelay;
            totalLength = _pDelay + _transition.animationDuration;
            asset = _asset;

            /*switch (asset.type)
            {
                case ANPAssetType.OBJECT:
                    // Consider extra timing for assetbundle, glb, videos, and audio assets
                    if (go.name != "GLTF")
                    {
                        // AssetBundle animation clip length (expect only one clip)
                        if (go.GetComponent<Animator>())
                        {
                            Animator ac = go.GetComponent<Animator>();
                            if (ac != null)
                            {
                                RuntimeAnimatorController rac = ac.runtimeAnimatorController;
                                if (rac != null && rac.animationClips.Length > 0)
                                {
                                    totalLength += rac.animationClips[0].length;
                                }
                            }
                        }
                    }
                    else
                    {
                        // GLB animation length length (expect only one for now, but future would allow multiple)
                        isGLB = true;
                        Animation anim = go.GetComponentInChildren<Animation>();
                        if (anim != null && anim.clip != null && !string.IsNullOrEmpty(model.internalAnimation))
                        {
                            AnimationClip clip = anim.GetClip(model.internalAnimation);
                            totalLength += clip.length;
                        }
                    }
                    break;
                case ANPAssetType.AUDIO:
                    //if (go.GetComponent<AudioSource>().clip != null)
                        //totalLength += go.GetComponent<AudioSource>().clip.length;
                    break;
                case ANPAssetType.VIDEO:
                    totalLength += (float)assetController.player.length;
                    break;
            }*/

            callback = _pCallback;

        }

        public void Play(bool gotoInitial, bool skipToEnd = false, bool stopAudio = true)
        {
            //Debug.Log("ANPAnimation Play");

            ////GameObject pAssetGO = go;
            /*go = PresentationManager.loadedObjects[asset];
            go.SetActive(true);*/
            //pAssetGO = GameObject.Find(asset.FileName());

            /*foreach (var prezAsset in GameObject.FindObjectOfType<PrezSDKManager>().prezAssets)
            {
                if (asset.type == ANPAssetType.TEXT)
                {
                    //Debug.Log("asset : " + asset.text.value + " prezAsset : " + prezAsset.name);
                    if (asset.text.value.Equals(prezAsset.name))
                    {
                        assetGO = prezAsset;
                    }
                }
                else
                {
                    //Debug.Log("asset : " + asset.text.value + " prezAsset : " + prezAsset.name);
                    if (asset.FileName().Equals(prezAsset.name))
                    {
                        assetGO = prezAsset;
                    }
                }
            }*/


            //if (GameObject.FindObjectOfType<PrezSDKManager>().prezAssets.TryGetValue(asset.FileName(), out GameObject go))
            if (PrezSDKManager.uDictionaryExample.prezAssets.TryGetValue(asset.FileName(), out GameObject go))
            {
                if (asset.type == ANPAssetType.TEXT)
                {
                    //Debug.Log("asset : " + asset.text.value + " prezAsset : " + prezAsset.name);
                    if (asset.text.value.Equals(go.name))
                    {
                        assetGO = go;
                    }
                }
                else
                {
                    //Debug.Log("asset : " + asset.text.value + " prezAsset : " + prezAsset.name);
                    if (asset.FileName().Equals(go.name))
                    {
                        assetGO = go;
                    }
                }

            }


            float _delay = skipToEnd ? 0 : delay;
            float _animationDuration = skipToEnd ? 0 : model.animationDuration;

            if (skipToEnd)
            {
                LeanTween.cancel(assetGO, false);
                if (stopAudio && asset.type == ANPAssetType.AUDIO)
                {
                    assetGO.GetComponent<AudioSource>().Stop();
                }
            }



            LeanTween.delayedCall(assetGO, _delay, () =>
            {
                //Debug.Log("pAssetGO : " + assetGO.name + " delay : " + _delay + " duration : " + _animationDuration);

                //if (gotoInitial) assetController.GoToInitialTransform();

                if (model.animation == AnimationType.None)
                {
                    return;
                }

                //ResetTransform();
                assetGO.SetActive(true);

                PrezSDKManager._instance.OnAssetLoaded(asset, assetGO);

                if (skipToEnd)
                {
                    LeanTween.cancel(assetGO);
                }

                if (asset.type == ANPAssetType.IMAGE)
                {
                    float useMainColor = model.animation != AnimationType.BlurIn && model.animation != AnimationType.BlurOut ? 1 : 0;

                    if (useMainColor == 1)
                    {
                        assetGO.GetComponentInChildren<Renderer>().material.EnableKeyword("_USEMAINCOLOR_ON");

                    }
                    else
                    {
                        assetGO.GetComponentInChildren<Renderer>().material.DisableKeyword("_USEMAINCOLOR_ON");

                    }
                    //go.GetComponent<Renderer>().material.SetFloat("_UseMainColor", useMainColor);
                }

                if (!string.IsNullOrEmpty(model.internalAnimation))
                {
                    DoGlbAnim(skipToEnd, _delay);
                }
                else
                {
                    DoRegAnim(assetGO, skipToEnd, _delay, _animationDuration);
                }
            });
        }

        private void DoGlbAnim(bool skipToEnd, float _delay)
        {
            if (skipToEnd)
            {
                Complete();
            }
            else
            {
                assetGO.AddComponent<Animation>();
                Animation anim = assetGO.GetComponentInChildren<Animation>();
                anim.clip = anim.GetClip(model.internalAnimation);
                anim.wrapMode = model.GetWrapMode();
                anim.Play();

                LeanTween.value(assetGO, 0, 1, totalLength - _delay).setOnComplete(Complete);
            }
        }

        //returns whether animation happened or it got completed
        private void DoRegAnim(GameObject pAssetGO, bool skipToEnd, float _delay, float _animationDuration)
        {
            ARPAsset modelData = asset;

            PresentationManager.initialPos = PrezAssetHelper.GetVector(modelData.itemTransform.position);
            PresentationManager.initialScale = PrezAssetHelper.GetVector(modelData.itemTransform.localScale);

            Debug.Log("data " + modelData.FileName() + " " + " localScale " + modelData.itemTransform.localScale);

            /*if (!PrezSDKManager._instance.initialScales.ContainsKey(modelData))
            {
                PrezSDKManager._instance.initialScales.Add(modelData, modelData.itemTransform.localScale);
            }
            else
            {
                Debug.LogError(modelData + " key already exists");
            }*/

            if (!PrezSDKManager.uDictionaryExample.initialScales.ContainsKey(modelData))
            {
                PrezSDKManager.uDictionaryExample.initialScales.Add(modelData, modelData.itemTransform.localScale);
            }
            else
            {
                Debug.LogError(modelData + " key already exists");
            }

            switch (model.animation)
            {
                case AnimationType.Appear:
                    if (skipToEnd)
                    {
                        Complete();
                    }
                    else
                    {
                        LeanTween.value(pAssetGO, 0, 1, totalLength - _delay).setOnComplete(this.Complete);
                    }
                    break;
                case AnimationType.BlurIn:

                    //// Set go and descendants to blur layer
                    //blurMaterial.SetFloat("_blurSizeXY", 10);
                    //ChangeLayer(go, false);
                    //UpdateBlurCanvasPos();
                    //// Blur
                    //LeanTween.value(go, 10, 0, _animationDuration).setOnUpdate((float val) =>
                    //{

                    //    blurMaterial.SetFloat("_blurSizeXY", val);
                    //    UpdateBlurCanvasPos();
                    //}).setOnComplete(() =>
                    //{

                    //    // Set go and descendants to original layer
                    //    ChangeLayer(go, true);

                    //    if (skipToEnd)
                    //    {
                    //        Complete();
                    //    }
                    //});

                    //if (!skipToEnd)
                    //{
                    //    LeanTween.value(go, 0, 1, totalLength - _delay).setOnComplete(() =>
                    //    {
                    //        Complete();
                    //    });
                    //}
                    Complete();
                    break;

                case AnimationType.BlurOut:
                    //// Set go and descendants to blur layer
                    //blurMaterial.SetFloat("_blurSizeXY", 0);
                    //ChangeLayer(go, false);
                    //UpdateBlurCanvasPos();
                    //// Blur
                    //LeanTween.value(go, 0, 10, _animationDuration).setOnUpdate((float val) =>
                    //{
                    //    UpdateBlurCanvasPos();
                    //    blurMaterial.SetFloat("_blurSizeXY", val);
                    //}).setOnComplete(() =>
                    //{

                    //    // Set go and descendants to original layer
                    //    ChangeLayer(go, true);
                    //    pAssetGO.SetActive(false);
                    //});

                    //if (!skipToEnd)
                    //{
                    //    LeanTween.value(go, 0, 1, totalLength - _delay).setOnComplete(() =>
                    //    {
                    //        Complete();
                    //    });
                    //}
                    Complete();
                    break;

                case AnimationType.Disappear:
                    pAssetGO.SetActive(false);
                    Complete();
                    break;
                case AnimationType.FadeIn:
                    /*go.SetActive(false);
                    IEnumerable<GameObject> inRendererGOs = null;

                    if (isGLB)
                    {
                        inRendererGOs = go.DescendantsAndSelf().Where(x => x.GetComponent<Renderer>());

                        foreach (GameObject rendererGO in inRendererGOs)
                        {
                            Material[] mats = rendererGO.GetComponent<Renderer>().materials;

                            foreach (Material mat in mats)
                            {
                                GLTFShaderUtils.ChangeRenderMode(mat, GLTFShaderUtils.BlendMode.Blend);
                            }
                        }
                    }
                    else if (modelData.type == ANPAssetType.IMAGE || modelData.type == ANPAssetType.VIDEO)
                    {
                        StandardShaderUtils.ChangeRenderMode(go.GetComponent<Renderer>().material, StandardShaderUtils.BlendMode.Transparent);
                    }

                    LeanTween.alpha(go, 0, 0).setOnComplete(() =>
                    {
                        go.SetActive(true);
                        LeanTween.alpha(go, 1, _animationDuration).setOnComplete(() =>
                        {
                            if (isGLB)
                            {
                                foreach (GameObject rendererGO in inRendererGOs)
                                {
                                    Material[] mats = rendererGO.GetComponent<Renderer>().materials;

                                    foreach (Material mat in mats)
                                    {
                                        GLTFShaderUtils.ChangeRenderMode(mat, GLTFShaderUtils.BlendMode.Opaque);
                                    }
                                }
                            }
                            else if (modelData.type == ANPAssetType.IMAGE || modelData.type == ANPAssetType.VIDEO)
                            {
                                StandardShaderUtils.ChangeRenderMode(go.GetComponent<Renderer>().material, StandardShaderUtils.BlendMode.Opaque);
                            }

                            if (skipToEnd)
                            {
                                Complete();
                            }
                        });

                    });

                    if (!skipToEnd)
                    {
                        LeanTween.value(go, 0, 1, totalLength - _delay).setOnComplete(Complete);
                    }
                    */
                    break;
                case AnimationType.FadeOut:
                    /*IEnumerable<GameObject> outRendererGOs = null;

                    if (isGLB)
                    {
                        outRendererGOs = go.DescendantsAndSelf().Where(x => x.GetComponent<Renderer>());

                        foreach (GameObject rendererGO in outRendererGOs)
                        {
                            Material[] mats = rendererGO.GetComponent<Renderer>().materials;

                            foreach (Material mat in mats)
                            {
                                GLTFShaderUtils.ChangeRenderMode(mat, GLTFShaderUtils.BlendMode.Blend);
                            }
                        }
                    }
                    else if (modelData.type == ANPAssetType.IMAGE || modelData.type == ANPAssetType.VIDEO)
                    {
                        StandardShaderUtils.ChangeRenderMode(go.GetComponent<Renderer>().material, StandardShaderUtils.BlendMode.Fade);
                    }

                    LeanTween.alpha(go, 1, 0);
                    LeanTween.alpha(go, 0, _animationDuration).setOnComplete(() =>
                    {
                        if (isGLB)
                        {
                            foreach (GameObject rendererGO in outRendererGOs)
                            {
                                Material[] mats = rendererGO.GetComponent<Renderer>().materials;

                                foreach (Material mat in mats)
                                {
                                    GLTFShaderUtils.ChangeRenderMode(mat, GLTFShaderUtils.BlendMode.Opaque);
                                }
                            }
                        }
                        else if (modelData.type == ANPAssetType.IMAGE || modelData.type == ANPAssetType.VIDEO)
                        {
                            LeanTween.alpha(go, 1, 0);
                            StandardShaderUtils.ChangeRenderMode(go.GetComponent<Renderer>().material, StandardShaderUtils.BlendMode.Fade);
                        }
                        pAssetGO.SetActive(false);
                        Complete();
                    });
                    Complete();*/
                    break;
                case AnimationType.LeftSpinIn:
                    if (!skipToEnd)
                    {
                        LeanTween.value(pAssetGO, 0, 1, totalLength - _delay).setOnComplete(this.Complete);
                    }
                    else
                    {
                        Complete();
                    }
                    break;
                case AnimationType.LeftSpinOut:
                    pAssetGO.SetActive(false);
                    Complete();
                    break;
                case AnimationType.RightSwooshIn:
                    Vector3 leftSwooshPos = PresentationManager.initialPos;
                    leftSwooshPos.x += 1;

                    if (!skipToEnd)
                    {
                        pAssetGO.transform.localPosition = leftSwooshPos;
                    }

                    LeanTween.moveLocal(pAssetGO, PresentationManager.initialPos, _animationDuration).setOnComplete(() =>
                    {
                        if (skipToEnd)
                        {
                            Complete();
                        }
                    });

                    if (!skipToEnd)
                    {
                        LeanTween.value(pAssetGO, 0, 1, totalLength - _delay).setOnComplete(this.Complete);
                    }
                    break;
                case AnimationType.RightSwooshOut:
                    Vector3 leftSwooshOutPos = PresentationManager.initialPos;
                    leftSwooshOutPos.x += 1;

                    if (!skipToEnd)
                    {
                        pAssetGO.transform.localPosition = PresentationManager.initialPos;
                    }

                    LeanTween.moveLocal(pAssetGO, leftSwooshOutPos, _animationDuration).setOnComplete(() =>
                    {
                        pAssetGO.SetActive(false);
                        Complete();
                    });
                    break;
                case AnimationType.TopSwooshIn:
                    //Debug.Log("TopSwooshIn");

                    Vector3 topSwooshPos = PresentationManager.initialPos;
                    topSwooshPos.y += 1;

                    if (!skipToEnd)
                    {
                        pAssetGO.transform.localPosition = topSwooshPos;
                    }

                    LeanTween.moveLocal(pAssetGO, PresentationManager.initialPos, _animationDuration).setOnComplete(() =>
                    {
                        if (skipToEnd)
                        {
                            Complete();
                        }
                    });

                    if (!skipToEnd)
                    {
                        LeanTween.value(pAssetGO, 0, 1, totalLength - _delay).setOnComplete(this.Complete);
                    }
                    break;
                case AnimationType.TopSwooshOut:
                    Vector3 topSwooshOutPos = PresentationManager.initialPos;
                    topSwooshOutPos.y += 1;

                    if (!skipToEnd)
                    {
                        pAssetGO.transform.localPosition = PresentationManager.initialPos;
                    }

                    LeanTween.moveLocal(pAssetGO, topSwooshOutPos, _animationDuration).setOnComplete(() =>
                    {
                        pAssetGO.SetActive(false);
                        Complete();
                    });
                    break;
                case AnimationType.BottomSwooshIn:
                    Debug.Log("BottomSwooshIn");
                    Vector3 bottomSwooshPos = PresentationManager.initialPos;
                    bottomSwooshPos.y -= 1;

                    if (!skipToEnd)
                    {
                        pAssetGO.transform.localPosition = bottomSwooshPos;
                    }

                    LeanTween.moveLocal(pAssetGO, PresentationManager.initialPos, _animationDuration).setOnComplete(() =>
                    {
                        if (skipToEnd)
                        {
                            Complete();
                        }
                    });

                    if (!skipToEnd)
                    {
                        LeanTween.value(pAssetGO, 0, 1, totalLength - _delay).setOnComplete(this.Complete);
                    }
                    break;
                case AnimationType.BottomSwooshOut:
                    Debug.Log("BottomSwooshOut");
                    Vector3 bottomSwooshOutPos = PresentationManager.initialPos;
                    bottomSwooshOutPos.y -= 1;

                    if (!skipToEnd)
                    {
                        pAssetGO.transform.localPosition = PresentationManager.initialPos;
                    }

                    LeanTween.moveLocal(pAssetGO, bottomSwooshOutPos, _animationDuration).setOnComplete(() =>
                    {
                        pAssetGO.SetActive(false);
                        Complete();
                    });
                    break;
                case AnimationType.None:
                    // Shouldn't ever be able to reach here
                    Debug.LogError("Animation is none, this shouldn't be possible");
                    break;
                case AnimationType.PopIn:
                    if (!skipToEnd)
                    {
                        LeanTween.value(pAssetGO, 0, 1, totalLength - _delay).setOnComplete(this.Complete);
                    }
                    else
                    {
                        Complete();
                    }
                    break;
                case AnimationType.PopOut:
                    pAssetGO.SetActive(false);
                    Complete();
                    break;
                case AnimationType.LeftSwooshIn:
                    Vector3 rightSwooshPos = PresentationManager.initialPos;
                    rightSwooshPos.x -= 1;

                    if (!skipToEnd)
                    {
                        pAssetGO.transform.localPosition = rightSwooshPos;
                    }
                    LeanTween.moveLocal(pAssetGO, PresentationManager.initialPos, _animationDuration).setOnComplete(() =>
                    {
                        if (skipToEnd)
                        {
                            Complete();
                        }
                    });

                    if (!skipToEnd)
                    {
                        LeanTween.value(pAssetGO, 0, 1, totalLength - _delay).setOnComplete(this.Complete);
                    }
                    break;
                case AnimationType.LeftSwooshOut:
                    Vector3 rightSwooshOutPos = PresentationManager.initialPos;
                    rightSwooshOutPos.x -= 1;

                    if (!skipToEnd)
                    {
                        pAssetGO.transform.localPosition = PresentationManager.initialPos;
                    }

                    LeanTween.moveLocal(pAssetGO, rightSwooshOutPos, _animationDuration).setOnComplete(() =>
                    {
                        pAssetGO.SetActive(false);
                        Complete();
                    });
                    break;
                case AnimationType.RightSpinIn:
                    if (!skipToEnd)
                    {
                        LeanTween.value(pAssetGO, 0, 1, totalLength - _delay).setOnComplete(this.Complete);
                    }
                    else
                    {
                        Complete();
                    }
                    break;
                case AnimationType.RightSpinOut:
                    pAssetGO.SetActive(false);
                    Complete();
                    break;
                case AnimationType.ScaleIn:
                    Debug.Log("ScaleIn");

                    if (!skipToEnd)
                    {
                        pAssetGO.transform.localScale = Vector3.zero;
                    }

                    LeanTween.scale(pAssetGO, PresentationManager.initialScale, _animationDuration).setOnComplete(() =>
                    {
                        if (skipToEnd)
                        {
                            Complete();
                        }
                    });

                    if (!skipToEnd)
                    {
                        LeanTween.value(pAssetGO, 0, 1, totalLength - _delay).setOnComplete(this.Complete);
                    }
                    break;
                case AnimationType.ScaleOut:
                    Debug.Log("ScaleOut");

                    if (!skipToEnd)
                    {
                        pAssetGO.transform.localScale = PresentationManager.initialScale;
                    }

                    LeanTween.scale(pAssetGO, Vector3.zero, _animationDuration).setOnComplete(() =>
                    {
                        pAssetGO.SetActive(false);
                        Complete();
                    });
                    break;
                case AnimationType.StartRotationRight:
                    /*assetController.rotate.shouldUpdate = true;
                    assetController.rotate.speed = model.animationDuration;
                    Complete();*/
                    break;
                case AnimationType.StopRotation:
                    /*assetController.rotate.shouldUpdate = false;
                    Complete();*/
                    break;
                case AnimationType.StartRotationLeft:
                    /*assetController.rotate.shouldUpdate = true;
                    assetController.rotate.speed = -model.animationDuration;
                    Complete();*/
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void Complete()
        {
            if (callback == null) return;
            callback.Invoke(this);
        }

    }
}
