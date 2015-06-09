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
	//ambient = mat.Ambient * L.Ambient;	

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

void ComputeAshikhminShirley(Material mat, DirectionalLight L, 
                             float3 n, float3 v,
					         out float4 ambient,
						     out float4 diffuse,
						     out float4 spec)
{
	// Initialize outputs.
	ambient = float4(0.0f, 0.0f, 0.0f, 0.0f);
	diffuse = float4(0.0f, 0.0f, 0.0f, 0.0f);
	spec    = float4(0.0f, 0.0f, 0.0f, 0.0f);

	// The light vector aims opposite the direction the light rays travel.
	float3 l = -L.Direction;
	float3 h = normalize(l + v);

	// Define the coordinate frame
    float3 epsilon = float3( 1.0f, 0.0f, 0.0f );
    float3 tangent = normalize( cross( n, epsilon ) );
    float3 bitangent = normalize( cross( n, tangent ) );

	// Generate any useful aliases
    float VdotN = dot( v, n );
    float LdotN = dot( l, n );
    float HdotN = dot( h, n );
    float HdotL = dot( h, l );
    float HdotT = dot( h, tangent );
    float HdotB = dot( h, bitangent );

	//ambient = mat.Ambient * 0.5;
	float3 Rd = (mat.Diffuse).rgb;
	float3 Rs = (mat.Specular).rgb;// * L.Specular).rgb;
	float Nu = 10;
    float Nv = 10;

	// Compute the diffuse term
    float3 Pd = (28.0f * Rd) / ( 23.0f * 3.14159f );
    Pd *= (1.0f - Rs);
    Pd *= (1.0f - pow(1.0f - (LdotN / 2.0f), 5.0f));
    Pd *= (1.0f - pow(1.0f - (VdotN / 2.0f), 5.0f));
	diffuse.xyz = Pd * L.Diffuse;// * 12;

	// Compute the specular term
    float ps_num_exp = Nu * HdotT * HdotT + Nv * HdotB * HdotB;
    ps_num_exp /= (1.0f - HdotN * HdotN);
 
    float Ps_num = sqrt( (Nu + 1) * (Nv + 1) );
    Ps_num *= pow( HdotN, ps_num_exp );
 
    float Ps_den = 8.0f * 3.14159f * HdotL;
    Ps_den *= max( LdotN, VdotN );
 
    float3 Ps = Rs * (Ps_num / Ps_den);
    Ps *= ( Rs + (1.0f - Rs) * pow( 1.0f - HdotL, 5.0f ) );
	spec.xyz = Ps * L.Specular;
}

float rand(float2 co){
      return 0.5+(frac(sin(dot(co.xy ,float2(12.9898,78.233))) * 43758.5453))*0.5;
}

float4 PS(PS_IN input) : SV_Target
{
	//float c = rand(input.normal.xy);
	//return float4(c, c, c, 1.0f);
	//return float4(input.normal, 1.0f);
	//return float4(1, 0, 0, 1);
	[flatten]
	if (textured > 0.5f)
		return diffuseTexture.Sample(diffuseSampler, input.uv) - float4(0.2f, 0.2f, 0.2f, 0);

	// Interpolating normal can unnormalize it, so normalize it.
    float3 N = normalize(input.normal);

	float3 V = normalize(gEyePosW - input.worldPos);

	// Start with a sum of zero. 
	float4 ambient = float4(0.0f, 0.0f, 0.0f, 0.0f);
	float4 diffuse = float4(0.0f, 0.0f, 0.0f, 0.0f);
	float4 spec    = float4(0.0f, 0.0f, 0.0f, 0.0f);

	// Sum the light contribution from each light source.
	float4 A, D, S;

	//ComputeDirectionalLight(gMaterial, gDirLight, N, V, A, D, S);
	ComputeAshikhminShirley(gMaterial, gDirLight, N, V, A, D, S);
	ambient += A;  
	diffuse += D;
	spec    += S;
	   
	float4 litColor = clamp(ambient + diffuse + spec, 0.0, 1.0);

	// Common to take alpha from diffuse material.
	litColor.a = gMaterial.Diffuse.a;

    return litColor;
}
