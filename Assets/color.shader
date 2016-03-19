Shader "Unlit/color"
{
	Properties
	{
		col("Color", Color) = (1,1,1,1)
		length("Length",Float) = 1
		width("Width", Float) = 1
		masktype("MaskType",Int) = 0
	}
	SubShader
	{
		Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }
		Blend SrcAlpha OneMinusSrcAlpha
		LOD 300

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"
			fixed4 col;
	        float length;
			float width;
	        int masktype;

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

			v2f vert(appdata v)
			{
				v2f o;
				o.vertex = mul(UNITY_MATRIX_MVP, v.vertex);
				o.uv.x = v.uv.x - 0.5;
				o.uv.y = v.uv.y - 0.5;
				return o;
			}
			fixed4 frag(v2f i) : SV_Target
			{
				fixed4 c = col;
			    if(masktype==0)
				{ }
				else if (masktype == 1)
				{
					if (sqrt(pow(i.uv.x, 2) + pow(i.uv.y, 2))>0.5)
					{
						c.a = 0;
					}
				}
				else if (masktype == 2)
				{
					//float d = pow(i.uv.x, 2) + pow(i.uv.y, 2);
					//c.a= c.a*exp(-r2 / (2 * pow(sigma, 2)));
				}
				return c;
			}
			ENDCG
		}
	}
}
