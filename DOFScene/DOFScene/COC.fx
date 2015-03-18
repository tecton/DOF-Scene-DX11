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

float4 PS(PS_IN input) : SV_Target
{
	float depth = depthTexture.Sample(colorSampler, input.uv).r;
	float3 color = colorTexture.Sample(colorSampler, input.uv).rgb;
	return float4(color, depth);
}
