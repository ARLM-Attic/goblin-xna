//digitaltutors

struct Light 
{
    float4 color;
    float3 direction;
};

#define MaxBones 59
float4x4 Bones[MaxBones];
float4x4 View;
float4x4 Projection;
float4x4 World;

shared float4 ambientLightColor;
Light lights[3];
int numLights;

texture Texture;

sampler C_Sampler = sampler_state {
 Texture = <Texture>;
 MinFilter = Linear;
 MagFilter = Linear;
 MipFilter = Linear;
};

struct VS_INPUT 
{
	float4 Position : POSITION0;
	float2 TexCoord : TEXCOORD0;
	float3 Normal : NORMAL0;
	float4 BoneIndices : BLENDINDICES0;
	float4 BoneWeights : BLENDWEIGHT0;
};

struct VS_OUTPUT {
	float4 Position: POSITION0;
	float2 TexCoord: TEXCOORD0;
	float3 Normal : TEXCOORD1;
};

VS_OUTPUT VSBasic(VS_INPUT input)  {
	VS_OUTPUT output;
	
	float4x4 skinTransform = 0;
	skinTransform += Bones[input.BoneIndices.x] * input.BoneWeights.x;
	skinTransform += Bones[input.BoneIndices.y] * input.BoneWeights.y;
	skinTransform += Bones[input.BoneIndices.z] * input.BoneWeights.z;
	skinTransform += Bones[input.BoneIndices.w] * input.BoneWeights.w;	
	float4 pos = mul(input.Position, skinTransform);
	pos = mul(pos,World);
	pos = mul(pos,View);
	pos = mul(pos, Projection);
	
	float3 nml = mul(input.Normal, skinTransform);
	nml = normalize(nml);
		
	output.Position = pos;
	output.TexCoord = input.TexCoord;
	output.Normal = nml;		
		
	return output;	
}

//
//Diffuse lighting using the light source in header
//
float4 PSDiffuse(VS_OUTPUT input) : COLOR0 {
	float4 outColor = tex2D(C_Sampler, input.TexCoord);
	float4 diffuse = float4(0.0,0.0,0.0,1.0);
	for(int i = 0; i < numLights; i++)
	{
		float intensity = saturate(dot(input.Normal, normalize(-lights[i].direction)));
		diffuse += intensity * lights[i].color;
	}

	outColor = outColor * (ambientLightColor + diffuse);
	outColor.a = 1.0;
	return outColor;
	
}


technique SkinnedModelTechnique
{
	pass SkinnedModelPass
	{
		VertexShader = compile vs_1_1 VSBasic();
		PixelShader = compile ps_2_0 PSDiffuse();
	}
}

