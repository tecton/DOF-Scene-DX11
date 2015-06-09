struct VS_IN
{
	float3 pos : POSITION;
	float2 uv : TEXCOORD;
};

struct PS_IN
{
	float4 pos : SV_POSITION;
	float2 uv : TEXCOORD0;
};

cbuffer TexturePosition : register (b0)
{
	float4x4 gWorldViewProj;
};

cbuffer ClipInfo : register (b1)
{
	float4 clipInfo;
	float focusPlaneZ;
	float3 frustum;
	float4 focusPoint;
};

Texture2D depthTexture : register (t0);
Texture2D colorTexture : register (t1);
SamplerState colorSampler;

PS_IN VS(VS_IN input)
{
	PS_IN output = (PS_IN)0;

	output.pos = mul(float4(input.pos, 1), gWorldViewProj);
	output.uv = input.uv;

	return output;
}

float reconstructDepth(float d) {
    return clipInfo[0] / (clipInfo[1] * d + clipInfo[2]);
}

float4 PS(PS_IN input) : SV_Target
{
	float depth = depthTexture.Sample(colorSampler, input.uv).r;
	float z = reconstructDepth(depth);
	float3 color = colorTexture.Sample(colorSampler, input.uv).rgb;

	// Fractional radius on [0, 1]
    float radius;
    
	float scale = clipInfo.a;
    // Note that the radius is negative in the far field.
	float focusDepth = depthTexture.Sample(colorSampler, focusPoint.xy).r;
	float focusZ = reconstructDepth(focusDepth);
	radius = (z - focusZ) * scale;
    //radius = (z - focusPlaneZ) * scale;

	float4 result;

	result.rgb = color;

    // Store the radius biased because the target texture format may
    // be unsigned.  It is on the scale [0, 1] in case the format
    // is normalized fixed point.
    result.a   = saturate(radius * 0.5 + 0.5);
	//result.a = -z;

	return result;
}
