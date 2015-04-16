struct PS_IN
{
	float4 pos : SV_POSITION;
	float2 uv : TEXCOORD0;
};
//
//struct PixelOutputType
//{
//	float4 output0: SV_TARGET0;
//	float4 output1: SV_TARGET1;
//	float4 output2: SV_TARGET2;
//};

Texture2D inputTexture0 : register (t0);
Texture2D inputTexture1 : register (t1);
Texture2D inputTexture2 : register (t2);
Texture2D paramTexture : register (t3);

SamplerState textureSampler;

static const float LW2[24] = {
	4.5022,-2.0152,0.054521,8.43,0.50047,-0.023602,4.8797,8.6939,-4.2548,-0.82097,0.35144,0.083645,
	4.6393,-2.1478,0.12689,-5.9249,2.7525,-0.67602,-8.4677,7.8472,-0.1633,-0.77427,0.023308,0.06734
};

static const float B3[2] = {
	1.2033,
	1.2089
};

float tansig1(float x)
{
	return 2 / (1 + exp(-2 * x)) - 1;
}

float4 PS(PS_IN input) : SV_Target
{
	float4 lineData0 = inputTexture0.Sample(textureSampler, input.uv);
	float4 lineData1 = inputTexture1.Sample(textureSampler, input.uv);
	float4 lineData2 = inputTexture2.Sample(textureSampler, input.uv);

	float4 result = float4(0, 0, 0, 0);

	for (int i = 0; i < 4; i++)
	{
		result.x += LW2[i] * lineData0[i];
		result.y += LW2[12 + i] * lineData0[i];
	}
	for (int i = 0; i < 4; i++)
	{
		result.x += LW2[i + 4] * lineData1[i];
		result.y += LW2[16 + i] * lineData1[i];
	}
	for (int i = 0; i < 4; i++)
	{
		result.x += LW2[i + 8] * lineData2[i];
		result.y += LW2[20 + i] * lineData2[i];
	}

	result.x += B3[0];
	result.y += B3[1];
	// result is simulated coc now

	float far = paramTexture.Sample(textureSampler, input.uv).a;

	result.x = clamp(abs(result.x) / 35.0f * 1000.0f / 12.0f, 0, 1.0f);
	result.y = clamp(abs(result.y) / 35.0f * 1000.0f / 12.0f, 0, 1.0f);

	if (far < 0) {
		// far
		result.x = -result.x;
		result.y = -result.y;
	}

	result.x = result.x * 0.5 + 0.5;
	result.y = result.y * 0.5 + 0.5;

	return result;

}
