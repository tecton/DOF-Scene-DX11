struct PS_IN
{
	float4 pos : SV_POSITION;
	float2 uv : TEXCOORD0;
};

struct PixelOutputType
{
	float4 output0: SV_TARGET0;
	float4 output1: SV_TARGET1;
	float4 output2: SV_TARGET2;
};

Texture2D inputTexture0 : register (t0);
Texture2D inputTexture1 : register (t1);
Texture2D inputTexture2 : register (t2);

SamplerState textureSampler;

static const float LW1[144] = {
	-3.408,0.76077,2.0571,0.92541,1.6697,-0.66094,1.3268,-2.2038,0.13833,1.1465,-1.1657,-1.6737,
	0.15648,0.19864,-3.1271,-1.395,-2.1736,0.96259,-0.85032,3.7487,0.36571,-2.8343,3.4564,2.0218,
	1.5838,0.084927,-2.9244,-1.4575,-1.454,0.37179,-4.9547,7.7391,-7.4715,-6.5828,7.0576,-8.7744,
	5.4441,-1.4923,-2.5039,-1.0665,0.22827,0.36076,-0.83865,2.461,-0.37549,-2.5831,4.2196,2.5863,
	-0.56918,-0.89166,2.9573,1.2179,-0.20298,-0.31,0.69943,-2.7377,0.18155,3.311,-3.071,-0.76,
	-3.058,-0.37879,7.4014,3.094,1.6791,-0.36285,2.6058,-7.9074,3.0432,6.0923,-9.73,-3.3587,
	-5.4876,1.5236,2.5592,1.0881,0.43267,-0.35793,0.79266,-2.4239,0.9131,2.523,-4.2392,-2.8452,
	1.1783,0.074227,-1.5983,-0.70123,-0.3842,0.49694,-0.7436,1.5381,0.359,-1.1635,0.51305,0.57823,
	1.991,-0.063782,-1.99,-0.83753,1.56,0.41731,-0.87584,2.2277,1.3254,-2.5268,2.3594,0.77806,
	-9.2674,2.1561,4.0378,1.7048,2.3294,-0.99585,0.88928,-5.4592,-0.19112,5.666,-6.658,-7.2461,
	9.8367,-2.7486,-7.5959,-3.1251,5.7535,0.77485,-6.6251,12.373,2.0082,-8.5672,13.885,2.0677,
	-1.3161,0.95606,10.476,3.602,1.8512,0.22193,5.2075,-6.0912,2.713,10.59,6.9174,2.286
};

static const float B2[12] = {
	-1.8912,
	-2.4856,
	-3.8043,
	3.6619,
	-1.116,
	-0.40039,
	-3.7189,
	1.0849,
	1.8751,
	-7.9082,
	3.0823,
	-18.577
};

float tansig1(float x)
{
	return 2 / (1 + exp(-2 * x)) - 1;
}

PixelOutputType PS(PS_IN input) : SV_Target
{
	float4 lineData0 = inputTexture0.Sample(textureSampler, input.uv);
	float4 lineData1 = inputTexture1.Sample(textureSampler, input.uv);
	float4 lineData2 = inputTexture2.Sample(textureSampler, input.uv);

	PixelOutputType result;

	// matrix multiply and save result to different textures
	for (int i = 0; i < 4; i++)
	{
		result.output0[i] = 0;
		result.output0[i] += LW1[12 * i] * lineData0.r + LW1[12 * i + 1] * lineData0.g + LW1[12 * i + 2] * lineData0.b + LW1[12 * i + 3] * lineData0.a;
		result.output0[i] += LW1[12 * i + 4] * lineData1.r + LW1[12 * i + 5] * lineData1.g + LW1[12 * i + 6] * lineData1.b + LW1[12 * i + 7] * lineData1.a;
		result.output0[i] += LW1[12 * i + 8] * lineData2.r + LW1[12 * i + 9] * lineData2.g + LW1[12 * i + 10] * lineData2.b + LW1[12 * i + 11] * lineData2.a;
		result.output0[i] = tansig1(result.output0[i] + B2[i]);
	}
	for (int i = 0; i < 4; i++)
	{
		result.output1[i] = 0;
		result.output1[i] += LW1[12 * i + 48] * lineData0.r + LW1[12 * i + 49] * lineData0.g + LW1[12 * i + 50] * lineData0.b + LW1[12 * i + 51] * lineData0.a;
		result.output1[i] += LW1[12 * i + 52] * lineData1.r + LW1[12 * i + 53] * lineData1.g + LW1[12 * i + 54] * lineData1.b + LW1[12 * i + 55] * lineData1.a;
		result.output1[i] += LW1[12 * i + 56] * lineData2.r + LW1[12 * i + 57] * lineData2.g + LW1[12 * i + 58] * lineData2.b + LW1[12 * i + 59] * lineData2.a;
		result.output1[i] = tansig1(result.output1[i] + B2[i + 4]);
	}
	for (int i = 0; i < 4; i++)
	{
		result.output2[i] = 0;
		result.output2[i] += LW1[12 * i + 96] * lineData0.r + LW1[12 * i + 97] * lineData0.g + LW1[12 * i + 98] * lineData0.b + LW1[12 * i + 99] * lineData0.a;
		result.output2[i] += LW1[12 * i + 100] * lineData1.r + LW1[12 * i + 101] * lineData1.g + LW1[12 * i + 102] * lineData1.b + LW1[12 * i + 103] * lineData1.a;
		result.output2[i] += LW1[12 * i + 104] * lineData2.r + LW1[12 * i + 105] * lineData2.g + LW1[12 * i + 106] * lineData2.b + LW1[12 * i + 107] * lineData2.a;
		result.output2[i] = tansig1(result.output2[i] + B2[i + 8]);
	}
	return result;
}
