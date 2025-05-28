#ifndef EXPERICA_NETENV_SHADER_CORE_INCLUDED
#define EXPERICA_NETENV_SHADER_CORE_INCLUDED

#define TWOPI 6.28318530717958647692528


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

// scaled y coordinate of uv, clock-wise rotated `theta`(radius) for ShaderGraph.
void syrcw_float(float2 uv, float2 scale, float theta, out float Out)
{
	float sinv, cosv;
	sincos(theta, sinv, cosv);
	Out = cosv * uv.y * scale.y - sinv * uv.x * scale.x;
}

// sin wave, `phase` is in [0, 1]
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

// square wave, `phase` and `duty` are in [0, 1], phase == duty => 0.
float squarewave(float x, float sf, float t, float tf, float phase, float duty)
{
	return -sign(frac(sf * x - tf * t + phase) - duty);
}

// grating function for ShaderGraph
void grating_float(float type, float x, float sf, float t, float tf, float phase, float duty, out float Out)
{
	if (type == 1)
	{
		Out = sinwave(x, sf, t, tf, phase);
	}
	else if (type == 2)
	{
		Out = trianglewave(x, sf, t, tf, phase);
	}
	else
	{
		Out = squarewave(x, sf, t, tf, phase, duty);
	}
}

// disk mask centered on uv [0, 0], mask `radius` in uv coordinates
float diskmask(float2 uv, float radius)
{
	return length(uv) > radius ? 0.0 : 1.0;
}

float gaussianmask(float2 uv, float sigma)
{
	float r2 = pow(uv.x, 2) + pow(uv.y, 2);
	return exp(-0.5 * r2 / pow(sigma, 2));
}

float diskfademask(float2 uv, float radius, float scale)
{
	float d = length(uv) - radius;
	return d > 0 ? erfc(scale*d) : 1.0;
}

// mask function for ShaderGraph
void mask_float(float type, float2 uv, float radius, float sigma, float reverse, out float Out)
{
    if (type == 1)
    {
        Out = diskmask(uv, radius);
    }
    else if (type == 2)
    {
        Out = gaussianmask(uv, sigma);
    }
    else if (type == 3)
    {
        Out = diskfademask(uv, radius, sigma);
    }
    else
    {
        Out = 1.0;
    }
    if (reverse == 1)
    {
        Out = 1.0 - Out;
    }
}

// linear interpolation between `mincolor` and `maxcolor` using another `color` for ShaderGraph
void colorlerp_float(float4 mincolor, float4 maxcolor, float4 color, float channel, out float4 Out)
{
	if (channel == 0)
	{
		Out = color;
	}
	else if (channel == 1)
	{
		Out = lerp(mincolor, maxcolor, color.r);
	}
	else if (channel == 2)
	{
		Out = lerp(mincolor, maxcolor, color.g);
	}
	else if (channel == 3)
	{
		Out = lerp(mincolor, maxcolor, color.b);
	}
	else if (channel == 4)
	{
		Out = lerp(mincolor, maxcolor, color.a);
	}
	else
	{
		Out = lerp(mincolor, maxcolor, color);
	}
}

// concentric two disk
void twodisk_float(float2 uv, float inner_radius,float outer_radius,float4 inner_color,float4 outer_color, out float4 Out)
{
    float r = length(uv);
    if (r > outer_radius)
    {
        Out = float4(0, 0, 0, 0);
    }
    else if (r > inner_radius)
    {
        Out = outer_color;
    }
    else
    {
        Out = inner_color;
    }
}

#endif