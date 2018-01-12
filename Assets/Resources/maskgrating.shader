Shader "VLAB/maskgrating"
{
	Properties
	{
		ori("Orientation",Float) = 0
		orioffset("OrientationOffset",Float) = 0
		minc("MinColor",Color) = (0,0,0,1)
		maxc("MaxColor",Color) = (1,1,1,1)
		cdist("ColorDistance",Color) = (1,1,1,0)
		sf("SpatialFreq", Float) = 0.2
		tf("TemporalFreq", Float) = 2
		t("Time", Float) = 0
		phase("SpatialPhase", Float) = 0
		maskradius("MaskRadius",Float) = 0.5
		sigma("Sigma", Float) = 0.15
		sizex("SizeX",Float) = 2
		sizey("SizeY", Float) = 2
		gratingtype("GratingType",Int) = 0
		masktype("MaskType",Int) = 0
	}

		SubShader
	{
		Tags{ "RenderType" = "Transparent" "Queue" = "Transparent"}
		Blend SrcAlpha OneMinusSrcAlpha
		LOD 300

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			float ori;
	        float orioffset;
			fixed4 minc;
	        fixed4 maxc;
			fixed4 cdist;
			float sf;
			float tf;
			float t;
			float phase;
			float maskradius;
			float sigma;
			float sizex;
			float sizey;
			int gratingtype;
			int masktype;
			static const float pi = 3.141592653589793238462;
			static const float pi2 = 6.283185307179586476924;

			inline float erf(const float x)
			{
				const float sign_x = sign(x);
				const float t = 1.0 / (1.0 + 0.47047*abs(x));
				const float result = 1.0 - t*(0.3480242 + t*(-0.0958798 + t*0.7478556))*exp(-(x*x));

				return result * sign_x;
			}

			inline float erfc(const float x)
			{
				return 1.3693*exp(-0.8072*pow(x + 0.6388, 2));
			}

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
				o.uv = v.uv - 0.5;
				return o;
			}

			fixed4 frag(v2f i) : SV_Target
			{
				fixed4 c;
			    float sinv, cosv;
				sincos(radians(ori + orioffset), sinv, cosv);
				float p = cosv*i.uv.y*sizey - sinv*i.uv.x*sizex;
				float y = frac((p - t*tf / sf + phase / sf)*sf);
				if (gratingtype == 0)
				{
					if (y< 0.5)
					{
						c = maxc;
					}
					else
					{
						c = minc;
					}
				}
				else if (gratingtype == 1)
				{
					c = cdist*(sin(y*pi2) + 1) / 2 + minc;
				}
				else if (gratingtype == 2)
				{
					if (y < 0.25)
					{
						c=cdist*(y * 2 + 0.5) + minc;
					}
					else if (y < 0.75)
					{
						c=cdist*(1 - (y - 0.25) * 2) + minc;
					}
					else
					{
						c=cdist*((y - 0.75) * 2) + minc;
					}
				}

				if(masktype==0)
				{ }
				else if (masktype == 1)
				{
					if (sqrt(pow(i.uv.x, 2) + pow(i.uv.y, 2)) > maskradius)
					{
						c.a = 0;
					}
				}
				else if (masktype == 2)
				{
					float d = pow(i.uv.x, 2) + pow(i.uv.y, 2);
					c.a = c.a*exp(-d / (2 * pow(sigma, 2)));
				}
				else if (masktype == 3)
				{
					float d = sqrt(pow(i.uv.x, 2) + pow(i.uv.y, 2)) - maskradius;
					if (d > 0)
					{
						c.a = c.a*erfc(sigma*d);
					}
				}

				return c;
			}
			
			ENDCG
		}
	}
}
