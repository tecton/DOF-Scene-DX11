

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

struct DirectionalLight
{
	float4 Ambient;
	float4 Diffuse;
	float4 Specular;
	float3 Direction;
	float padding;
};

struct Material
{
	float4 Ambient;
	float4 Diffuse;
	float4 Specular; // w = SpecPower
};

cbuffer cbPerObject : register (b0)
{
	float4x4 gWorld;
	float4x4 gWorldInvTranspose;
	float4x4 gWorldViewProj;
	Material gMaterial;
	float textured;
	float3 padding;
};

cbuffer cbPerFrame : register (b1)
{
	DirectionalLight gDirLight;
	float3 gEyePosW;
};

Texture2D diffuseTexture;
SamplerState diffuseSampler;

PS_IN VS(VS_IN input)
{
	PS_IN output = (PS_IN)0;

	output.pos = mul(float4(input.pos, 1), gWorldViewProj);
	output.normal = mul(input.normal, (float3x3)gWorldInvTranspose);
	output.worldPos = mul(float4(input.pos, 1), gWorld).xyz;
	output.uv = input.uv;

	return output;
}

//---------------------------------------------------------------------------------------
// Computes the ambient, diffuse, and specular terms in the lighting equation
// from a directional light.  We need to output the terms separately because
// later we will modify the individual terms.
//---------------------------------------------------------------------------------------
void ComputeDirectionalLight(Material mat, DirectionalLight L, 
                             float3 normal, float3 toEye,
					         out float4 ambient,
						     out float4 diffuse,
						     out float4 spec)
{
	// Initialize outputs.
	ambient = float4(0.0f, 0.0f, 0.0f, 0.0f);
	diffuse = float4(0.0f, 0.0f, 0.0f, 0.0f);
	spec    = float4(0.0f, 0.0f, 0.0f, 0.0f);

	// The light vector aims opposite the direction the light rays travel.
	float3 lightVec = -L.Direction;

	// Add ambient term.
	ambient = mat.Ambient * L.Ambient;	

	// Add diffuse and specular term, provided the surface is in 
	// the line of site of the light.
	
	float diffuseFactor = dot(lightVec, normal);

	// Flatten to avoid dynamic branching.
	[flatten]
	if( diffuseFactor > 0.0f )
	{
		float3 v         = reflect(-lightVec, normal);
		float specFactor = pow(max(dot(v, toEye), 0.0f), mat.Specular.w);
					
		diffuse = diffuseFactor * mat.Diffuse * L.Diffuse;
		spec    = specFactor * mat.Specular * L.Specular;
	}
}

float4 PS(PS_IN input) : SV_Target
{
	[flatten]
	if (textured > 0.5f)
		return diffuseTexture.Sample(diffuseSampler, input.uv);

	// Interpolating normal can unnormalize it, so normalize it.
    input.normal = normalize(input.normal); 

	float3 toEyeW = normalize(gEyePosW - input.worldPos);

	// Start with a sum of zero. 
	float4 ambient = float4(0.0f, 0.0f, 0.0f, 0.0f);
	float4 diffuse = float4(0.0f, 0.0f, 0.0f, 0.0f);
	float4 spec    = float4(0.0f, 0.0f, 0.0f, 0.0f);

	// Sum the light contribution from each light source.
	float4 A, D, S;

	ComputeDirectionalLight(gMaterial, gDirLight, input.normal, toEyeW, A, D, S);
	ambient += A;  
	diffuse += D;
	spec    += S;
	   
	float4 litColor = ambient + diffuse + spec;

	// Common to take alpha from diffuse material.
	litColor.a = gMaterial.Diffuse.a;

    return litColor;
}
