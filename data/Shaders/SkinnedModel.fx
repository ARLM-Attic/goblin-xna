//digitaltutors

#define MaxBones 59
float4x4 Bones[MaxBones];
float4x4 View;
float4x4 Projection;
float4x4 World;

float3 lightDir1 = float3(1,1,1);
float3 lightDir2 = float3(-1,-0,-0);
float3 lightDir3 = float3(0.5,-0,-1);


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
//Vertex lit
//
float4 PSBasic(VS_OUTPUT input) : COLOR0 {
    float4 outColor = tex2D(C_Sampler, input.TexCoord);
	return outColor;
}


//
//A debug pixel shader 
//
float4 PSDebug(VS_OUTPUT input) : COLOR0 {
    float4 outColor = float4(1.0,1.0,1.0,1.0);
	return outColor;
}

//
//Diffuse lighting using the light source in header
//
float4 PSDiffuse(VS_OUTPUT input) : COLOR0 {
	float4 outColor = tex2D(C_Sampler, input.TexCoord);
	float diffuse = saturate(dot(input.Normal, normalize(lightDir1))) +
					saturate(dot(input.Normal, normalize(lightDir2))) + 
					0.5 * saturate(dot(input.Normal, normalize(lightDir3)));
	outColor = outColor * diffuse;
	outColor.a = 1.0;
	return outColor;
	
}

//
//"Invisble man"
//
float4 PSInvisible(VS_OUTPUT input) : COLOR0 {
	float4 outColor = tex2D(C_Sampler, input.TexCoord);
	float diffuse = saturate(dot(input.Normal, normalize(lightDir1))) +
					saturate(dot(input.Normal, normalize(lightDir2))) + 
					0.5 * saturate(dot(input.Normal, normalize(lightDir3)));
	outColor = outColor * diffuse;
	outColor.a = 0.1;
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

technique Diffuse
{
	pass SkinnedModelPass
	{
		VertexShader = compile vs_1_1 VSBasic();
		PixelShader = compile ps_2_0 PSDiffuse();
	}
}

technique Debug
{
	pass SkinnedModelPass
	{
		VertexShader = compile vs_1_1 VSBasic();
		PixelShader = compile ps_2_0 PSDebug();
	}
}

technique Invisible
{
	pass SkinnedModelPass
	{
		VertexShader = compile vs_1_1 VSBasic();
		PixelShader = compile ps_2_0 PSInvisible();
	}
}

