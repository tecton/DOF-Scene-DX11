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
	9.5074e-06,-8.5799e-06,-0.16278,-0.0017415,-0.030909,
	8.7796e-05,-4.8508e-05,0.38785,0.048999,0.0011159,
	-0.00091639,0.00066209,0.03696,-8.3465e-05,3.4591e-05,
	0.0015981,-0.014642,0.051567,0.00018581,0.0012744,
	9.3368e-05,-2.9014e-06,0.27608,-0.0016148,-0.002251,
	-2.038e-05,6.7561e-06,-0.40617,-0.01645,-0.013726,
	-1.0167e-05,1.1113e-05,0.11978,0.0013002,0.00052263,
	-0.00012808,0.0020889,0.0096082,0.0063894,0.0070428,
	0.0025468,-0.0018357,-0.019674,0.00079594,0.00024365,
	7.3268e-06,-1.364e-06,-0.17973,-0.030971,-0.0053268,
	0.0057737,-2.8989e-05,0.10144,0.005558,0.0038856,
	0.00010832,-5.7859e-05,0.45223,0.0077364,0.044388,
};

static const float B1[12] = {
	2.7536,
	-5.4305,
	0.15904,
	0.34001,
	-2.299,
	4.1611,
	-1.6042,
	0.50151,
	1.2294,
	2.7373,
	-1.4601,
	-5.989
};

float tansig1(float x)
{
	return 2 / (1 + exp(-2 * x)) - 1;
}

PixelOutputType PS(PS_IN input) : SV_Target
{
	float4 lineData = paramTexture.Sample(textureSampler, input.uv);

	PixelOutputType result;
	result.output0 = float4(IW[0], IW[1], IW[2], 0);

	// matrix multiply and save result to different textures
	for (int i = 0; i < 4; i++)
		result.output0[i] = tansig1(focus * IW[i * 5] + lineData.r * IW[5 * i + 1] + pupil * [5 * i + 2] + lineData.g * [5 * i + 3] + lineData.b * IW[5 * i + 4] + B1[i]);
    for (int i = 0; i < 4; i++)
		result.output1[i] = tansig1(focus * IW[i * 5 + 20] + lineData.r * IW[5 * i + 21] + pupil * [5 * i + 22] + lineData.g * [5 * i + 23] + lineData.b * IW[5 * i + 24] + B1[4 + i]);
	for (int i = 0; i < 4; i++)
		result.output2[i] = tansig1(focus * IW[i * 5 + 40] + lineData.r * IW[5 * i + 41] + pupil * [5 * i + 42] + lineData.g * [5 * i + 43] + lineData.b * IW[5 * i + 44] + B1[8 + i]);

	return result;
}
