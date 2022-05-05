using AfterNow.PrezSDK.Internal.Views;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CheckClickable : MonoBehaviour
{
    public ARPAsset asset;
    
    private void OnMouseDown()
    {
        if (GetComponent<Collider>() != null)
        {
            Debug.Log(asset.clickTarget);
        }
    }
}
