//structs shared between all effects
struct Light 
{
    float4 color;
    float3 position;
    float3 direction;
    float falloff;
    float range;
    float attenuation0;
    float attenuation1;
    float attenuation2;
    float innerConeAngle;
    float outerConeAngle;
    
};
struct VertexShaderOutput
{
     float4 Position : POSITION;
     float2 TexCoords : TEXCOORD0;
     float3 WorldNormal : TEXCOORD1;
     float3 WorldPosition : TEXCOORD2;
  
};
struct PixelShaderInput
{
     float2 TexCoords : TEXCOORD0;
     float3 WorldNormal : TEXCOORD1;
     float3 WorldPosition : TEXCOORD2;
};

Light lights[1];

//shared scene parameters
shared float4x4 viewProjection;
shared float3 cameraPosition;
shared float4 ambientLightColor;
shared int numLightsPerPass = 1;

sampler diffuseSampler;

//texture parameters can be used to set states in the 
//effect state pass code
texture2D diffuseTexture;


//the world paramter is not shared because it will
//change on every Draw() call
float4x4 world;
float4x4 worldForNormal;


//these material paramters are set per effect instance
float4 emissiveColor;
float4 diffuseColor;
float4 normalMapColor;
float4 specularColor;
float specularPower;
float specularIntensity;
bool diffuseTexEnabled = false;




//This function transforms the model to projection space and set up
//interpolators used by the pixel shader
VertexShaderOutput BasicVS(
     float3 position : POSITION,
     float3 normal : NORMAL,
     float2 texCoord : TEXCOORD0, 
     float3 binormal : BINORMAL0,
     float3 tangent   : TANGENT0)
{
     VertexShaderOutput output;

     //generate the world-view-projection matrix
     float4x4 wvp = mul(world, viewProjection);
     
     //transform the input position to the output
     output.Position = mul(float4(position, 1.0), wvp);

     output.WorldNormal =  mul(normal, worldForNormal);
     output.WorldNormal = normalize(output.WorldNormal);
     float4 worldPosition =  mul(float4(position, 1.0), world);
     output.WorldPosition = worldPosition / worldPosition.w;
     
     //copy the tex coords to the interpolator
     output.TexCoords = texCoord;
	 
     return output;
}

//The Ambient pixel shader simply adds an ambient color to the
//back buffer while outputting depth information.
float4 AmbientPS(PixelShaderInput input) : COLOR
{
	
	if(diffuseTexEnabled)
    {
        diffuseColor *= tex2D(diffuseSampler, input.TexCoords);
    }
	
	float4 color = ambientLightColor * diffuseColor + emissiveColor;
	//color = 0;
	color.a = 1;
	return color;
}


//This function calculates the diffuse and specular effect of a single light
//on a pixel given the world position, normal, and material properties
float4 CalculateSinglePointLight(Light light, float3 worldPosition, float3 worldNormal, 
                            float4 diffuseColor, float4 specularColor )
{
     float3 lightVector = light.position - worldPosition;
     float lightDist = length(lightVector);
     float3 directionToLight = normalize(lightVector);
     
     //calculate the intensity of the light with exponential falloff
     float baseIntensity = pow(saturate((light.range - lightDist) / light.range),
                                 light.falloff);
     
     float attenuation;                            
	 attenuation = 1 / (light.attenuation0 + ((light.attenuation1) * lightDist) + ((light.attenuation2) * lightDist * lightDist));
     baseIntensity *= attenuation;
     baseIntensity  = saturate(baseIntensity);
     
     float diffuseIntensity = saturate( dot(directionToLight, worldNormal));
     float4 diffuse = diffuseIntensity * light.color * diffuseColor;

     //calculate Phong components per-pixel
     float3 reflectionVector = normalize(reflect(-directionToLight, worldNormal));
     float3 directionToCamera = normalize(cameraPosition - worldPosition);
     
     //calculate specular component
     float4 specular = saturate(light.color * specularColor *
                       pow(saturate(dot(reflectionVector, directionToCamera)), 
                           specularPower));
                           
     return  baseIntensity * (diffuse + specular);
}

float4 CalculateSingleDirectionalLight(Light light, float3 worldPosition, float3 worldNormal, 
									  float4 diffuseColor, float4 specularColor )
{
     float3 lightVector = -light.direction;
     float3 directionToLight = normalize(lightVector);
     
     float diffuseIntensity = saturate( dot(directionToLight, worldNormal));
     float4 diffuse = diffuseIntensity * light.color * diffuseColor;

     //calculate Phong components per-pixel
     float3 reflectionVector = normalize(reflect(-directionToLight, worldNormal));
     float3 directionToCamera = normalize(cameraPosition - worldPosition);
     
     //calculate specular component
     float4 specular = saturate(light.color * specularColor *
                       pow(saturate(dot(reflectionVector, directionToCamera)), 
                           specularPower));
                           
     return  diffuse + specular;
}

float4 CalculateSingleSpotLight(Light light, float3 worldPosition, float3 worldNormal, 
                            float4 diffuseColor, float4 specularColor )
{
     float3 lightVector = light.position - worldPosition;
     float lightDist = length(lightVector);
     float3 directionToLight = normalize(lightVector);
     
     float distanceAttenuation;                            
	 distanceAttenuation = 1 / (light.attenuation0 + ((light.attenuation1) * lightDist) + ((light.attenuation2) * lightDist * lightDist));
     
     float innerCos = cos(light.innerConeAngle / 2);
     float outerCos = cos(light.outerConeAngle / 2);
     float lightDirCos = dot(-directionToLight, normalize(light.direction));
     
     float coneAttenuation;
     if (lightDirCos > innerCos)
     {
		coneAttenuation = 1;
     }
     else if (lightDirCos < outerCos)
     {
		coneAttenuation = 0;
     }
     else
     {
		coneAttenuation = pow((lightDirCos - outerCos) / (innerCos - outerCos), light.falloff);
     }
     
     float diffuseIntensity = saturate( dot(directionToLight, worldNormal));
     float4 diffuse = diffuseIntensity * light.color * diffuseColor;

     //calculate Phong components per-pixel
     float3 reflectionVector = normalize(reflect(-directionToLight, worldNormal));
     float3 directionToCamera = normalize(cameraPosition - worldPosition);
     
     //calculate specular component
     float4 specular = saturate(light.color * specularColor *
                       pow(saturate(dot(reflectionVector, directionToCamera)), 
                           specularPower));
                           
	//return coneAttenuation;
     return  distanceAttenuation * coneAttenuation * (diffuse + specular);
}

float4 SinglePointLightPS(PixelShaderInput input) : COLOR
{
     
    if(diffuseTexEnabled)
    {
        diffuseColor *= tex2D(diffuseSampler, input.TexCoords);
    }
    
    
	float4  color = CalculateSinglePointLight(lights[0], 
			                    input.WorldPosition, input.WorldNormal,
				                diffuseColor, specularColor);

     color.a = 1.0;
     return color;
}

float4 SingleDirectionalLightPS(PixelShaderInput input) : COLOR
{
     
    if(diffuseTexEnabled)
    {
        diffuseColor *= tex2D(diffuseSampler, input.TexCoords);
    }
   
	
	float4 color = CalculateSingleDirectionalLight(lights[0], 
				                    input.WorldPosition, input.WorldNormal,
					                diffuseColor, specularColor);
   
     
     color.a = 1.0;
     return color;
}

float4 SingleSpotLightPS(PixelShaderInput input) : COLOR
{
     
    if(diffuseTexEnabled)
    {
        diffuseColor *= tex2D(diffuseSampler, input.TexCoords);
    }
	
   
    
	float4 color = CalculateSingleSpotLight(lights[0], 
				                    input.WorldPosition, input.WorldNormal,
					                diffuseColor, specularColor);
     
     color.a = 1.0;
     
     return color;
}


technique GeneralLighting
{
    pass Ambient
    {
        //set sampler states
        MagFilter[0] = LINEAR;
        MinFilter[0] = LINEAR;
        MipFilter[0] = LINEAR;
        AddressU[0] = WRAP;
        AddressV[0] = WRAP;
        MagFilter[1] = LINEAR;
        MinFilter[1] = LINEAR;
        MipFilter[1] = LINEAR;
        AddressU[1] = WRAP;
        AddressV[1] = WRAP;
        
        //set texture states (notice the '<' , '>' brackets)
        //as the texture state assigns a reference
        Texture[0] = <diffuseTexture>;
       
        
        VertexShader = compile vs_2_0 BasicVS();
        PixelShader = compile ps_2_0 AmbientPS();
    }
    pass PointLight
    {
        VertexShader = compile vs_2_0 BasicVS();
        PixelShader = compile ps_2_0 SinglePointLightPS();
    }
    
    pass DirectionalLight
    {
        VertexShader = compile vs_2_0 BasicVS();
        PixelShader = compile ps_2_0 SingleDirectionalLightPS();
    }
    
    pass SpotLight
    {
        VertexShader = compile vs_2_0 BasicVS();
        PixelShader = compile ps_2_0 SingleSpotLightPS();
    }
    
}
