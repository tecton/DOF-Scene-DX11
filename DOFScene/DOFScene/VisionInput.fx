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

cbuffer ClipInfo : register (b0)
{
	float4 clipInfo;
	float focusPlaneZ;
	float3 frustum;
};

Texture2D depthTexture : register (t0);
SamplerState colorSampler;

float reconstructDepth(float d) {
    return clipInfo[0] / (clipInfo[1] * d + clipInfo[2]);
}

float4 PS(PS_IN input) : SV_Target
{
	float4 eyeDataBuffer;

	float PI = 3.14159265358979323846264;
    float depth = depthTexture.Sample(colorSampler, input.uv).r;

	float z = -reconstructDepth(depth);
	float xndc = input.uv.x * 2.0 - 1;
	float yndc = input.uv.y * 2.0 - 1;
	float x = -z * (xndc * frustum.x) / frustum.z;
	float y = z * (yndc * frustum.y) / frustum.z;

	// distance, milimeter
	float r = sqrt(x * x + y * y + z * z);
	// degree, [-15, 15]
	float theta = atan2(-y, sqrt(x * x + z * z)) / PI * 180;
	// degree, [-25, 25]
	float phi = atan2(x, z) / PI * 180;

	eyeDataBuffer.r = clamp(r * 1000, 0, 5000);
	eyeDataBuffer.g = abs(theta);
	eyeDataBuffer.b = abs(phi);
	//eyeDataBuffer.r = abs(x);
	//eyeDataBuffer.g = abs(y);
	//eyeDataBuffer.b = abs(z);

	if (r > -focusPlaneZ) // far
		eyeDataBuffer.a = -1;
	else // near
		eyeDataBuffer.a = 1;

	return eyeDataBuffer;
}
