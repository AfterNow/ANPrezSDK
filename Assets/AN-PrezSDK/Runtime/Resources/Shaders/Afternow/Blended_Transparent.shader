Shader "Unlit/Blended Transparent" {
    Properties {
        _Color ("Main Color", Color) = (1,1,1,1)
        _MainTex ("Base (RGB) Trans (A)", 2D) = "white" {}
		
		[Enum(UnityEngine.Rendering.CompareFunction)]
        _ZTest("ZTest", Float) = 2
		
		[Toggle] _Invert ("Write Depth", Float) = 1
		
		//Disabled = 0
		//Never = 1
		//Less = 2
		//Equal = 3
		//LessEqual = 4
		//Greater = 5
		//NotEqual = 6
		//GreaterEqual = 7
		//Always = 8
    }

    SubShader {
        Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
        Cull Off
        ZWrite [_Invert]
		ZTest [_ZTest]
        Lighting Off
        Fog { Mode Off }

        Blend SrcAlpha OneMinusSrcAlpha 

        Pass {
            Color [_Color]
            SetTexture [_MainTex] { combine texture * primary } 
        }
    }
}
