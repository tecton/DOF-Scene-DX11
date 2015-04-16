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

cbuffer cbPerObject : register (b0)
{
	float4x4 gWorldViewProj;
};

cbuffer RenderMode : register (b1)
{
	int renderMode;
};

Texture2D colorTexture : register (t0);
Texture2D blurTexture : register (t1);
Texture2D nearTexture : register (t2);
Texture2D paramTexture : register (t3);
Texture2D visionBlurTexture : register (t4);
Texture2D visionNearTexture : register (t5);
Texture2D visionCoCTexture : register (t6);

SamplerState textureSampler;

PS_IN VS(VS_IN input)
{
	PS_IN output = (PS_IN)0;

	output.pos = mul(float4(input.pos, 1), gWorldViewProj);
	output.uv = input.uv;

	return output;
}

// Boost the coverage of the near field by this factor.  Should always be >= 1
//
// Make this larger if near-field objects seem too transparent
//
// Make this smaller if an obvious line is visible between the near-field blur and the mid-field sharp region
// when looking at a textured ground plane.
static const float coverageBoost = 1.5f;

float grayscale(float3 c) {
    return (c.r + c.g + c.b) / 3.0;
}

float4 pinholeResult(PS_IN input)
{
	float3 result;
    float4 pack  = colorTexture.Sample(textureSampler, input.uv);
    float3 sharp   = pack.rgb;
    float3 blurred = blurTexture.Sample(textureSampler, input.uv).rgb;
    float4 near    = nearTexture.Sample(textureSampler, input.uv);

    // Normalized Radius
    float normRadius = (pack.a * 2.0 - 1.0);

    // Boost the blur factor
    //normRadius = clamp(normRadius * 2.0, -1.0, 1.0);

    if (coverageBoost != 1.0) {
        float a = saturate(coverageBoost * near.a);
		float b = (a / max(near.a, 0.001f));
        near.rgb = near.rgb * float3(b, b, b);
        near.a = a;
    }

	// Decrease sharp image's contribution rapidly in the near field
    if (normRadius > 0.1) {
        normRadius = min(normRadius * 1.5, 1.0);
    }

    result = near.rgb;
	float3 v;
	v.x = lerp(sharp.x, blurred.x, abs(normRadius));
	v.y = lerp(sharp.y, blurred.y, abs(normRadius));
	v.z = lerp(sharp.z, blurred.z, abs(normRadius));
	result += v * float3(1.0 - near.a, 1.0 - near.a, 1.0 - near.a);

	switch (renderMode) {
	case 1:
		{
			float r = pack.a * 2.0 - 1.0;
			if (r < 0) {
				result.rgb = float3(0.0, 0.14, 0.8) * abs(r);
			} else {
				result.rgb = float3(1.0, 1.0, 0.15) * abs(r);
			}
		}
		break;
	case 2:
		{
			result = near.rgb;
		}
		break;
	case 3:
		result = sharp.rgb;
		break;
	case 4:
		result = blurred;
		break;
	}
	return float4(result, 1.0f);
}

float4 visionResult(PS_IN input)
{
	float3 result;
	float4 coc = visionCoCTexture.Sample(textureSampler, input.uv);
    float4 pack  = colorTexture.Sample(textureSampler, input.uv);
    float3 sharp   = pack.rgb;
    float3 blurred = visionBlurTexture.Sample(textureSampler, input.uv).rgb;
    float4 near    = visionNearTexture.Sample(textureSampler, input.uv);

    // Normalized Radius
    float normRadius = (coc.x * 2.0 - 1.0);

    // Boost the blur factor
    //normRadius = clamp(normRadius * 2.0, -1.0, 1.0);

    if (coverageBoost != 1.0) {
        float a = saturate(coverageBoost * near.a);
		float b = (a / max(near.a, 0.001f));
        near.rgb = near.rgb * float3(b, b, b);
        near.a = a;
    }

	// Decrease sharp image's contribution rapidly in the near field
    if (normRadius > 0.1) {
        normRadius = min(normRadius * 1.5, 1.0);
    }

    result = near.rgb;
	float3 v;
	v.x = lerp(sharp.x, blurred.x, abs(normRadius));
	v.y = lerp(sharp.y, blurred.y, abs(normRadius));
	v.z = lerp(sharp.z, blurred.z, abs(normRadius));
	result += v * float3(1.0 - near.a, 1.0 - near.a, 1.0 - near.a);


	switch (renderMode) {
	case 5:
		{
			float4 s = paramTexture.Sample(textureSampler, input.uv);
			float r = s.r / 5000;
			result.rgb = float3(r, r, r);
			break;
		}
	case 7:
		{
			float r = coc.x * 2.0 - 1.0;
			if (r < 0) {
				result.rgb = float3(0.0, 0.14, 0.8) * abs(r);
			} else {
				result.rgb = float3(1.0, 1.0, 0.15) * abs(r);
			}
			break;
		}
	case 8:
		{
			float r = coc.y * 2.0 - 1.0;
			if (r < 0) {
				result.rgb = float3(0.0, 0.14, 0.8) * abs(r);
			} else {
				result.rgb = float3(1.0, 1.0, 0.15) * abs(r);
			}
			break;
		}
	}

	return float4(result, 1.0f);
}

float4 PS(PS_IN input) : SV_Target
{
	if (renderMode < 5)
		return pinholeResult(input);
	else
		return visionResult(input);
}