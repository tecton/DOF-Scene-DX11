

struct VS_IN
{
	float3 pos : POSITION;
	float3 normal : NORMAL;
	float2 uv : TEXCOORD;
};

struct PS_IN
{
	float4 pos : SV_POSITION;
	float2 uv : TEXCOORD0;
	float3 normal : TEXCOORD1;
	float3 worldPos : TEXCOORD2;
};

cbuffer Data : register (b0)
{
	float4x4 worldViewProj;
	float4x4 world;
};

cbuffer Color : register (b1)
{
	float4 diffuseColor;
	float useTexture;
	float useLight;
	float2 padding;
	float4 lightPos;
	float4 lightAmbient;
}

Texture2D diffuseTexture;
SamplerState diffuseSampler;

PS_IN VS(VS_IN input)
{
	PS_IN output = (PS_IN)0;

	output.pos = mul(float4(input.pos, 1), worldViewProj);
	output.normal = mul(input.normal, (float3x3)world);
	output.worldPos = mul(float4(input.pos, 1), world).xyz;
	output.uv = input.uv;

	return output;
}

float4 PS(PS_IN input) : SV_Target
{
	//return float4(1, 1, 0, 1);
	float4 finalColor;
	if (useTexture > 0.5f) // true
		finalColor = diffuseTexture.Sample(diffuseSampler, input.uv);
	else
		finalColor = diffuseColor;
	if (useLight > 0.5)
	{
		float3 L = normalize(lightPos.xyz - input.worldPos);
		float3 N = normalize(input.normal);

		finalColor.xyz = finalColor.xyz * saturate(2 * dot(N, L)) + lightAmbient.xyz;
	}
	return finalColor;
}
