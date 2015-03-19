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

Texture2D colorTexture : register (t0);
SamplerState colorSampler;

PS_IN VS(VS_IN input)
{
	PS_IN output = (PS_IN)0;

	output.pos = mul(float4(input.pos, 1), gWorldViewProj);
	output.uv = input.uv;

	return output;
}

float4 PS(PS_IN input) : SV_Target
{
	float4 result;
	float4 color = colorTexture.Sample(colorSampler, input.uv);
	result.rgb = color.aaa;
	float r = color.a * 2.0 - 1.0;
	if (r < 0) {
		result.rgb = float3(0.0, 0.14, 0.8) * abs(r);
	} else {
		result.rgb = float3(1.0, 1.0, 0.15) * abs(r);
	}
	result.a = color.a;
	return result;
}