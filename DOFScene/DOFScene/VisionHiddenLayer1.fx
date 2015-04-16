struct PS_IN
{
	float4 pos : SV_POSITION;
	float2 uv : TEXCOORD0;
};

cbuffer ParamInfo : register (b0)
{
	float focus;
	float pupil;
};

struct PixelOutputType
{
	float4 output0: SV_TARGET0;
	float4 output1: SV_TARGET1;
	float4 output2: SV_TARGET2;
};

Texture2D paramTexture : register (t0);
SamplerState textureSampler;

static const float IW[60] = {
	3.5485e-06,2.3553e-06,0.1453,0.01536,-0.0050253,
	3.7261e-06,2.7952e-06,0.14993,0.019982,-0.0011705,
	0.0001255,-5.6934e-06,-0.0059404,0.0011593,0.0010677,
	-0.00028393,-2.554e-06,-0.0076834,-0.0012298,-0.00026429,
	-4.8639e-07,-2.5166e-06,0.11274,0.0051139,0.024026,
	-1.9326e-05,-6.3919e-06,0.26602,0.0019556,0.0048348,
	-2.6707e-05,0.0016219,-0.15338,-0.0064625,-0.01326,
	-2.4205e-05,0.0016367,-0.044121,0.00084255,0.00014952,
	-2.4527e-06,3.8301e-06,-0.10272,-0.030603,-0.010134,
	0.0034533,1.0714e-05,0.087615,-0.0002916,-0.00037872,
	9.1773e-05,0.0087942,-0.043998,-0.00015592,-0.0002447,
	8.7086e-06,1.0917e-07,0.33072,-0.010073,0.018967
};

static const float B1[12] = {
	-2.5487,
	-1.9479,
	-0.049671,
	0.84653,
	-2.6649,
	-0.83136,
	2.3071,
	1.1448,
	2.7845,
	0.3,
	0.89871,
	-4.3725
};

float tansig1(float x)
{
	return 2 / (1 + exp(-2 * x)) - 1;
}

PixelOutputType PS(PS_IN input) : SV_Target
{
	float4 lineData = paramTexture.Sample(textureSampler, input.uv);

	PixelOutputType result;

	// matrix multiply and save result to different textures
	for (int i = 0; i < 4; i++)
		result.output0[i] = tansig1(focus * IW[i * 5] + lineData.r * IW[5 * i + 1] + pupil * IW[5 * i + 2] + lineData.g * IW[5 * i + 3] + lineData.b * IW[5 * i + 4] + B1[i]);
    for (int i = 0; i < 4; i++)
		result.output1[i] = tansig1(focus * IW[i * 5 + 20] + lineData.r * IW[5 * i + 21] + pupil * IW[5 * i + 22] + lineData.g * IW[5 * i + 23] + lineData.b * IW[5 * i + 24] + B1[4 + i]);
	for (int i = 0; i < 4; i++)
		result.output2[i] = tansig1(focus * IW[i * 5 + 40] + lineData.r * IW[5 * i + 41] + pupil * IW[5 * i + 42] + lineData.g * IW[5 * i + 43] + lineData.b * IW[5 * i + 44] + B1[8 + i]);

	return result;
}
