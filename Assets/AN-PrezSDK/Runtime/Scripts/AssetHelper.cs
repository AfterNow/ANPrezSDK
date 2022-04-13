using AfterNow.PrezSDK.Internal.Views;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace AfterNow.PrezSDK
{
    /// <summary>
    /// A Helper class which handles operations related to assets like their orientation, text color etc
    /// </summary>
    internal static class AssetHelper
    {
        // An array which stores TextMeshPro fonts of the text assets loaded
        private static readonly Dictionary<string, TMP_FontAsset> LoadedFontAssets = new Dictionary<string, TMP_FontAsset>();

        /// <summary>
        /// This function takes in the text asset's object <paramref name="asset"/> and returns the font that text contains
        /// </summary>
        /// <param name="asset"> The text asset </param>
        /// <returns></returns>
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

        /// <summary>
        /// This function takes a color's html coded string <paramref name="stringColor"/> and returns it's color 
        /// </summary>
        /// <param name="stringColor"> HTML code string of a color </param>
        /// <returns> Color of the provided HTML string input <paramref name="stringColor"/> </returns>
        public static Color GetColor(string stringColor)
        {
            ColorUtility.TryParseHtmlString(stringColor, out Color newColor);
            return newColor;
        }

        /// <summary>
        /// This function handles the text alignment of the TextMeshPro Text asset. It takes in ARPText <paramref name="text"/> and returns
        /// the TextAlignmentOption which could be one of Center/Left/Right. If none is mentioned in the Text asset, center is considered
        /// as default
        /// </summary>
        /// <param name="text"> The text asset </param>
        /// <returns> TextAlignmentOption : Center/Left/Right. Default is center </returns>
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

        /// <summary>
        /// This function returns the WrapMode of an animation <paramref name="transition"/>. It could be one of Once/Loop/PingPong/Clamp.
        /// Default value is "Default"
        /// </summary>
        /// <param name="transition"> The animation of an asset </param>
        /// <returns> WrapMode i.e the type of animation to be played. It could be Once/Loop/PingPong/Clamp </returns>
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

        /// <summary>
        /// Returns background texture of the <paramref name="slide"/>
        /// </summary>
        /// <param name="slide"> Slide of the loaded presentation </param>
        /// <returns> Returns the texture which is in format of Texture2D </returns>
        public static Texture2D GetBackgroundTexture(this Slide slide)
        {
            if (slide.BackgroundTexture == null) return null;
            if (slide.BackgroundTexture is Texture2D tex)
            {
                return tex;
            }
            return null;
        }

        /// <summary>
        /// Updates the transform of the <paramref name="asset"/> which is a type of "ARPAsset" with the provided <paramref name="newTransform"/>
        /// </summary>
        /// <param name="asset"> The asset whose transform need to be updated </param>
        /// <param name="newTransform"> The transform to be applied to the <paramref name="asset"/></param>
        public static void UpdateItemTransform(this ARPAsset asset, Transform newTransform)
        {
            if (asset.itemTransform == null)
            {
                asset.itemTransform = new ItemTransform();
            }
            asset.itemTransform.SetTransform(newTransform);
        }

        /// <summary>
        /// Function which returns the ItemTransform of an <paramref name="asset"/> which is of type "ARPAsset"
        /// </summary>
        /// <param name="asset"> The asset whose ItemTransform is required </param>
        /// <returns> ItemTransform reference of the provided <paramref name="asset"/></returns>
        public static ItemTransform GetItemTransform(this ARPAsset asset)
        {
            if (asset.itemTransform == null)
            {
                asset.itemTransform = new ItemTransform();
            }
            return asset.itemTransform;
        }

        /// <summary>
        /// Updates the transform of the <paramref name="asset"/> which is a type of "Location" with the provided <paramref name="newTransform"/>
        /// </summary>
        /// <param name="asset"></param>
        /// <param name="newTransform"></param>
        public static void UpdateItemTransform(this Location asset, Transform newTransform)
        {
            if (asset.itemTransform == null)
            {
                asset.itemTransform = new ItemTransform();
            }
            asset.itemTransform.SetTransform(newTransform);
        }

        /// <summary>
        /// Function which returns the ItemTransform of an <paramref name="asset"/> which is of type "Location"
        /// </summary>
        /// <param name="asset"> The asset whose ItemTransform is required </param>
        /// <returns> ItemTransform reference of the provided <paramref name="asset"/></returns>
        public static ItemTransform GetItemTransform(this Location asset)
        {
            if (asset.itemTransform == null)
            {
                asset.itemTransform = new ItemTransform();
            }
            return asset.itemTransform;
        }

        /// <summary>
        /// This function takes care of applying intial transform of an asset
        /// </summary>
        /// <param name="obj">The gameobject of the asset to apply the initial transform for</param>
        /// <param name="itemTransform"> The initial transform value to be applied to the gameobject <paramref name="obj"/></param>
        public static void SetInitialTransform(this GameObject obj, ItemTransform itemTransform)
        {
            Transform trans = obj.transform;
            trans.localPosition = itemTransform.position.GetVector();
            trans.localRotation = itemTransform.rotation.GetQuaternion();
            trans.localScale = itemTransform.localScale.GetVector();
        }

        /// <summary>
        /// This function sets the asset's transform
        /// </summary>
        /// <param name="item"> ItemTransform reference of the asset the <paramref name="transform"/> will be applied to </param>
        /// <param name="transform"> transform that has to be applied to the asset </param>
        public static void SetTransform(this ItemTransform item, Transform transform)
        {
            item.position = item.position.SetVector(transform.localPosition);
            item.rotation = item.rotation.SetQuaternion(transform.localRotation);
            item.localScale = item.localScale.SetVector(transform.localScale);
        }

        /// <summary>
        /// This function handles logic for applying the <paramref name="vector"/> of type Vector3 to the <paramref name="item"/> of type PrezVector
        /// </summary>
        /// <param name="item"> PrezVector3 reference of an asset </param>
        /// <param name="vector"> Vector3 that has to be applied to the <paramref name="item"/> of type PrezVector3 of an asset </param>
        /// <returns> PrezVector3 </returns>
        public static PrezVector3 SetVector(this PrezVector3 item, Vector3 vector)
        {
            item.x = vector.x;
            item.y = vector.y;
            item.z = vector.z;
            return item;
        }

        /// <summary>
        /// This function handles logic for applying the <paramref name="quat"/> of type Quaternion to the <paramref name="item"/> of type PrezQuaternion
        /// </summary>
        /// <param name="item"> PrezQuaternion reference of an asset </param>
        /// <param name="quat"> Quaternion that has to be applied to the <paramref name="item"/> of type PrezQuaternion of an asset </param>
        /// <returns> PrezQuaternion </returns>
        public static PrezQuaternion SetQuaternion(this PrezQuaternion item, Quaternion quat)
        {
            item.x = quat.x;
            item.y = quat.y;
            item.z = quat.z;
            item.w = quat.w;
            return item;
        }

        /// <summary>
        /// This function converts the <paramref name="vec"/> of type PrezVector3 of an asset and returns Vector3
        /// </summary>
        /// <param name="vec"> The PrezVector3 of an asset </param>
        /// <returns> Vector3 </returns>
        public static Vector3 GetVector(this PrezVector3 vec)
        {
            return new Vector3(vec.x, vec.y, vec.z);
        }

        /// <summary>
        /// This function converts the <paramref name="quat"/> of type PrezQuaternion of an asset and returns Quaternion
        /// </summary>
        /// <param name="quat"> The PrezQuaternion of an asset </param>
        /// <returns> Quaternion </returns>
        public static Quaternion GetQuaternion(this PrezQuaternion quat)
        {
            return new Quaternion(quat.x, quat.y, quat.z, quat.w);
        }

        /// <summary>
        /// This function is responsible for returning a replacement string which will be applied to the asset bundles
        /// based on the device platform the presentation is loaded
        /// </summary>
        /// <returns> Replacement string </returns>
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
}