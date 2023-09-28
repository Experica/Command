#ifndef EXPERICA_SHADER_CORE_INCLUDED
#define EXPERICA_SHADER_CORE_INCLUDED

#define TWOPI	6.28318530717958647692528

// error function
float erf(float x)
{
	float t = 1.0 / (1.0 + 0.47047 * abs(x));
	float result = 1.0 - t * (0.3480242 + t * (-0.0958798 + t * 0.7478556)) * exp(-(x * x));
	return result * sign(x);
}

// complementary error function
float erfc(float x)
{
	return 1.3693 * exp(-0.8072 * pow(x + 0.6388, 2));
}

// scaled y of a point clock-wise rotated theta(radius).
void cwroty_float(float2 uv, float2 uvscale, float theta, out float Out)
{
	float sinv, cosv;
	sincos(theta, sinv, cosv);
	Out = cosv * uv.y * uvscale.y - sinv * uv.x * uvscale.x;
}

// sin wave, phase is in [0, 1] scale
float sinwave(float x, float sf, float t, float tf, float phase)
{
	return sin(TWOPI * (sf * x - tf * t + phase));
}

float trianglewave(float x, float sf, float t, float tf, float phase)
{
	float p = frac(sf * x - tf * t + phase);
	if (p < 0.25)
	{
		return 4.0 * p;
	}
	else if (p < 0.75)
	{
		return -4.0 * (p - 0.5);
	}
	else
	{
		return -4.0 * (1.0 - p);
	}
}

// square wave, phase and duty are in [0, 1] scale, phase == duty => 0.
float squarewave(float x, float sf, float t, float tf, float phase, float duty)
{
	return -sign(frac(sf * x - tf * t + phase) - duty);
}

void grating_float(float gratingtype, float x, float sf, float t, float tf, float phase, float duty, out float Out)
{
	if (gratingtype == 1)
	{
		Out = sinwave(x, sf, t, tf, phase);
	}
	else if (gratingtype == 2)
	{
		Out = trianglewave(x, sf, t, tf, phase);
	}
	else
	{
		Out = squarewave(x, sf, t, tf, phase, duty);
	}
}

// disk mask centered on uv [0, 0], maskradius in uv coordinates
float diskmask(float2 uv, float maskradius)
{
	return length(uv) > maskradius ? 0.0 : 1.0;
}

float gaussianmask(float2 uv, float sigma)
{
	float r2 = pow(uv.x, 2) + pow(uv.y, 2);
	return exp(-0.5 * r2 / pow(sigma, 2));
}

float diskfademask(float2 uv, float maskradius, float scale)
{
	float d = length(uv) - maskradius;
	return d > 0 ? erfc(scale*d) : 1.0;
}

void mask_float(float masktype, float2 uv, float maskradius, float sigma, out float Out)
{
	if (masktype == 1)
	{
		Out = diskmask(uv, maskradius);
	}
	else if (masktype == 2)
	{
		Out = gaussianmask(uv, sigma);
	}
	else if (masktype == 3)
	{
		Out = diskfademask(uv, maskradius, sigma);
	}
	else
	{
		Out = 1.0;
	}
}

void colormodulatecolor_float(float4 mincolor, float4 maxcolor, float channelmodulate, float4 color, out float4 Out)
{
	if (channelmodulate == 0)
	{
		Out = color;
	}
	else if (channelmodulate == 1)
	{
		Out = lerp(mincolor, maxcolor, color.r);
	}
	else if (channelmodulate == 2)
	{
		Out = lerp(mincolor, maxcolor, color.g);
	}
	else if (channelmodulate == 3)
	{
		Out = lerp(mincolor, maxcolor, color.b);
	}
	else if (channelmodulate == 4)
	{
		Out = lerp(mincolor, maxcolor, color.a);
	}
	else
	{
		Out = lerp(mincolor, maxcolor, color);
	}
}

#endif