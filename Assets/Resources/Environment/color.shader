Shader "Experica/color"
{
	Properties
	{
		// Raw color vector to prevent any Unity conversion or gamma correction.
		col("Color", Vector) = (0.5,0.5,0.5,1.0)
	}
	SubShader
	{
		Tags { "RenderType" = "Transparent" "Queue" = "Transparent" }
		Blend SrcAlpha OneMinusSrcAlpha
		LOD 300

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			float4 col;
            struct appdata
            {
                float4 vertex : POSITION;
            };
            struct v2f
            {
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                return col;
            }
            ENDCG
        }
    }
}
