Shader "Unlit/grating"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
		sf("SpatialFreq", Float) = 1
		tf("TemporalFreq", Float) = 1
		t("Time", Float) = 0
		phase("SpatialPhase", Float) = 0
		sigma("Sigma", Float) = 2.5
		ys("ys", Float) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "Opaque" "Queue" = "Transparent" }
		Blend SrcAlpha OneMinusSrcAlpha
		LOD 100

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			float sf;
			float tf;
			float t;
			float phase;
			float sigma;
			float ys;

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;

			v2f vert(appdata v)
			{
				v2f o;
				o.vertex = mul(UNITY_MATRIX_MVP, v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				return o;
			}

			fixed4 frag(v2f i) : SV_Target
			{
				fixed4 col = tex2D(_MainTex, i.uv);
				float y = i.uv.y*ys + t*tf / sf;
				y = fmod(y, 1 / sf);
				if (y < 1 / (2 * sf)) {
				}
				else{
					col.rgb = col.rgb * 0;
				}
				/*if (frac(floor((i.uv.y + fmod(t*tf, tf) / tf) / sf) / 2) == 0) {
					}
				else {
					col.rgb = col.rgb * 0;
				}*/
				//float d = pow(i.uv.x - 0.5, 2) + pow(i.uv.y - 0.5, 2);
				//col.a = col.a*exp(-d / (2 * pow(sigma, 2)));
				return col;
			}
			ENDCG
		}
	}
}
