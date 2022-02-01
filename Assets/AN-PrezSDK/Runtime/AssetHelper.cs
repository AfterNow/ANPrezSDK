using AfterNow.PrezSDK.Internal.Views;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

    public static class PrezAssetHelper
    {
        private static readonly Dictionary<string, TMP_FontAsset> LoadedFontAssets = new Dictionary<string, TMP_FontAsset>();
        public static TMP_FontAsset GetFontAsset(this ARPText asset)
        {
            string fontName = asset.GetFontName();
            if (LoadedFontAssets.TryGetValue(fontName, out TMP_FontAsset font))
            {
                return font;
            }
            else
            {
                string path = "Tmp_Fonts/" + fontName;
                TMP_FontAsset newFont = Resources.Load<TMP_FontAsset>(path);
                if (newFont == null)
                {
                    Debug.LogError(fontName);
                    newFont = Resources.Load<TMP_FontAsset>("Brandon-Black");
                }
                LoadedFontAssets.Add(fontName, newFont);
                return newFont;
            }
        }

        public static Color GetColor(string stringColor)
        {
            ColorUtility.TryParseHtmlString(stringColor, out Color newColor);
            return newColor;
        }

        public static TextAlignmentOptions GetTMPAlignment(this ARPText text)
        {
            switch (text.alignment)
            {
                case ARPFontAlignment.CENTER:
                    return TextAlignmentOptions.Center;
                case ARPFontAlignment.LEFT:
                    return TextAlignmentOptions.Left;
                case ARPFontAlignment.RIGHT:
                    return TextAlignmentOptions.Right;
                default:
                    return TextAlignmentOptions.Center;
            }
        }

        public static WrapMode GetWrapMode(this ARPTransition transition)
        {
            switch (transition.internalAnimationWrap)
            {
                case ARPAnimationWrap.Once:
                    return WrapMode.Once;
                case ARPAnimationWrap.Loop:
                    return WrapMode.Loop;
                case ARPAnimationWrap.Pingpong:
                    return WrapMode.PingPong;
                case ARPAnimationWrap.Clamp:
                    return WrapMode.Clamp;
                default:
                    return WrapMode.Default;
            }
        }

        public static Texture2D GetBackgroundTexture(this Slide slide)
        {
            if (slide.BackgroundTexture == null) return null;
            if(slide.BackgroundTexture is Texture2D tex)
            {
                return tex;
            }
            return null;
        }

        public static void UpdateItemTransform(this ARPAsset asset,Transform newTransform)
        {
            if (asset.itemTransform == null)
            {
                asset.itemTransform = new ItemTransform();
            }
            asset.itemTransform.SetTransform(newTransform);
        }

        public static ItemTransform GetItemTransform(this ARPAsset asset)
        {
            if (asset.itemTransform == null)
            {
                asset.itemTransform = new ItemTransform();
            }
            return asset.itemTransform;
        }

        public static void UpdateItemTransform(this Location asset, Transform newTransform)
        {
            if (asset.itemTransform == null)
            {
                asset.itemTransform = new ItemTransform();
            }
            asset.itemTransform.SetTransform(newTransform);
        }

        public static ItemTransform GetItemTransform(this Location asset)
        {
            if (asset.itemTransform == null)
            {
                asset.itemTransform = new ItemTransform();
            }
            return asset.itemTransform;
        }

        public static void SetInitialPosition(this GameObject obj, ItemTransform itemTransform)
        {
            Transform trans = obj.transform;
            trans.localPosition = itemTransform.position.GetVector();
            trans.localRotation = itemTransform.rotation.GetQuaternion();
            trans.localScale = itemTransform.localScale.GetVector();
        }


        public static void SetTransform(this ItemTransform item,Transform transform)
        {
            item.position = item.position.SetVector(transform.localPosition);
            item.rotation = item.rotation.SetQuaternion(transform.localRotation);
            item.localScale = item.localScale.SetVector(transform.localScale);
        }

        public static PrezVector3 SetVector(this PrezVector3 item, Vector3 vector)
        {
            item.x = vector.x;
            item.y = vector.y;
            item.z = vector.z;
            return item;
        }

        public static PrezQuaternion SetQuaternion(this PrezQuaternion item, Quaternion quat)
        {
            item.x = quat.x;
            item.y = quat.y;
            item.z = quat.z;
            item.w = quat.w;
            return item;
        }

        public static Vector3 GetVector(this PrezVector3 vec)
        {
            return new Vector3(vec.x, vec.y, vec.z);
        }

        public static Quaternion GetQuaternion(this PrezQuaternion quat)
        {
            return new Quaternion(quat.x, quat.y, quat.z, quat.w);
        }

        public static string ReplacementString()
        {
            string replacement = null;

            switch (Application.platform)
            {
                case RuntimePlatform.OSXEditor:
                case RuntimePlatform.OSXPlayer:
                    replacement = "-osx.assetbundle";
                    break;
                case RuntimePlatform.WindowsPlayer:
                case RuntimePlatform.WindowsEditor:
                case RuntimePlatform.WSAPlayerX86:
                case RuntimePlatform.WSAPlayerX64:
                    replacement = "-uwp.assetbundle";
                    break;
                case RuntimePlatform.IPhonePlayer:
                    replacement = "-iphoneplayer.assetbundle";
                    break;
                case RuntimePlatform.Android:
                    replacement = "-android.assetbundle";
                    break;
                case RuntimePlatform.LinuxPlayer:
                case RuntimePlatform.LinuxEditor:
                    replacement = "-linux.assetbundle";
                    break;
                default:
                    break;
            }

            return replacement;
        }
    }

