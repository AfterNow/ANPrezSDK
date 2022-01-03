using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.AI;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
#endif

using Object = UnityEngine.Object;
using Random = UnityEngine.Random;
using AfterNow.AnPrez.SDK.Internal.Views;

public class UDictionaryExample : MonoBehaviour
{
    [InitializeOnLoadMethod]
    static void OnLoad()
    {
        AudioListener.volume = 0.2f;
    }

    [Serializable]
    public class UDictionary : UDictionary<string, GameObject> { }
    [UDictionary.Split(30, 70)]
    public UDictionary prezAssets;


    [Serializable]
    public class UDictionary1 : UDictionary<ARPAsset, PrezVector3> { }
    [UDictionary.Split(30, 70)]
    public UDictionary1 initialScales;


    [Serializable]
    public class Key
    {
        public string id;

        public string file;
    }

    [Serializable]
    public class Value
    {
        public string firstName;

        public string lastName;
    }

    void Start()
    {
        //prezAssets["See Ya Later"] = "Space Cowboy";
    }
}