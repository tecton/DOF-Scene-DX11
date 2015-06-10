/** Source image in RGB, normalized CoC in A. */
Texture2D blurSourceBuffer : register (t0);
/** For the second pass, the output of the previous near-field blur pass. */
Texture2D nearSourceBuffer : register (t1);

SamplerState colorSampler;

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

struct PixelOutputType
{
	float4 blurResult: SV_TARGET0;
	float4 nearResult: SV_TARGET1;
};

cbuffer TexturePosition : register (b0)
{
	float4x4 gWorldViewProj;
};

cbuffer BlurConstants : register (b1)
{
	/** Maximum blur radius for any point in the scene, in pixels.  Used to
    reconstruct the CoC radius from the normalized CoC radius. */
	float2 textureSize;
	float2 invTextureSize;
	float maxCoCRadiusPixels;
	float nearBlurRadiusPixels;
	float invNearBlurRadiusPixels;
	float padding;
};

const float2 direction = float2(0, 1);

bool inNearField(float radiusPixels) {
    return radiusPixels > 0.25;
}

PS_IN VS(VS_IN input)
{
	PS_IN output = (PS_IN)0;

	output.pos = mul(float4(input.pos, 1), gWorldViewProj);
	output.uv = input.uv;

	return output;
}

PixelOutputType PS(PS_IN input)
{
	PixelOutputType result;
    const int GAUSSIAN_TAPS = 6;

	//result.nearResult.rgba = float4(1.0f, 0, 0, 1.0f);

	//return result;
    float gaussian[GAUSSIAN_TAPS + 1];
    
    // 11 x 11 separated Gaussian weights.  This does not dictate the 
    // blur kernel for depth of field; it is scaled to the actual
    // kernel at each pixel.
    gaussian[6] = 0.00000000000000;  // Weight applied to outside-radius values
    gaussian[5] = 0.04153263993208;
    gaussian[4] = 0.06352050813141;
    gaussian[3] = 0.08822292796029;
    gaussian[2] = 0.11143948794984;
    gaussian[1] = 0.12815541114232;
    gaussian[0] = 0.13425804976814;
    
    // Accumulate the blurry image color
    result.blurResult.rgb  = float3(0, 0, 0);
    float blurWeightSum = 0.0f;
    
    // Accumulate the near-field color and coverage
    result.nearResult = float4(0, 0, 0, 0);
    float nearWeightSum = 0.000f;
    
    // Location of the central filter tap (i.e., "this" pixel's location)
    // Account for the scaling down by 50% during blur
    float2 A = float2(input.uv) * float2(2.0f, 2.0f);

	//result.blurResult = blurSourceBuffer.Sample(colorSampler, A);
	//return result;
    
    float packedA = blurSourceBuffer.Sample(colorSampler, A).a;
    float r_A = (packedA * 2.0 - 1.0) * maxCoCRadiusPixels;
    
    // Map r_A << 0 to 0, r_A >> 0 to 1
    float nearFieldness_A = saturate(r_A * 4.0);

    for (int delta = -maxCoCRadiusPixels; delta <= maxCoCRadiusPixels; ++delta)	{
        // Tap location near A
        float2  B = A + (float2(0, delta) * invTextureSize);

        // Packed values
		B = clamp(B, invTextureSize, float2(1.0f, 1.0f));

        float4 blurInput = blurSourceBuffer.Sample(colorSampler, B);

        // Signed kernel radius at this tap, in pixels
        float r_B = (blurInput.a * 2.0 - 1.0) * float(maxCoCRadiusPixels);
        
        /////////////////////////////////////////////////////////////////////////////////////////////
        // Compute blurry buffer

        float weight = 0.0;
        
        float wNormal  = 
            // Only consider mid- or background pixels (allows inpainting of the near-field)
            float(! inNearField(r_B)) * 
            
            // Only blur B over A if B is closer to the viewer (allow 0.5 pixels of slop, and smooth the transition)
            saturate(abs(r_A) - abs(r_B) + 1.5) *
            
            // Stretch the Gaussian extent to the radius at pixel B.
            gaussian[clamp(int(float(abs(delta) * (GAUSSIAN_TAPS - 1)) / (0.001 + abs(r_B * 0.5))), 0, GAUSSIAN_TAPS)];

        weight = lerp(wNormal, 1.0, nearFieldness_A);

        // far + mid-field output 
        blurWeightSum  += weight;
        result.blurResult.rgb += blurInput.rgb * weight;
        
        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Compute near-field super blurry buffer
        
        float4 nearInput;

        // On the second pass, use the already-available alpha values
        nearInput = nearSourceBuffer.Sample(colorSampler, B);

        // We subsitute the following efficient expression for the more complex: weight = gaussian[clamp(int(float(abs(delta) * (GAUSSIAN_TAPS - 1)) * invNearBlurRadiusPixels), 0, GAUSSIAN_TAPS)];
        weight =  float(abs(delta) < nearBlurRadiusPixels);
        result.nearResult += nearInput * weight;
        nearWeightSum += weight;
    }
    
    // Retain the packed radius on the first pass.  On the second pass it is not needed.
    result.blurResult.a = 1.0;

    // Normalize the blur
    result.blurResult.rgb /= blurWeightSum;

    // The taps are already normalized, but our Gaussian filter doesn't line up 
    // with them perfectly so there is a slight error.
    result.nearResult /= nearWeightSum; 
//
	return result;
}