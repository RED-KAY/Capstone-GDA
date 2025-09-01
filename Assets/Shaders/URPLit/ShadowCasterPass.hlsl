#ifndef UNIVERSAL_SHADOW_CASTER_PASS_INCLUDED
#define UNIVERSAL_SHADOW_CASTER_PASS_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
#if defined(LOD_FADE_CROSSFADE)
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
#endif


#include "VATInstancing.hlsl"

// Shadow Casting Light geometric parameters. These variables are used when applying the shadow Normal Bias and are set by UnityEngine.Rendering.Universal.ShadowUtils.SetupShadowCasterConstantBuffer in com.unity.render-pipelines.universal/Runtime/ShadowUtils.cs
// For Directional lights, _LightDirection is used when applying shadow Normal Bias.
// For Spot lights and Point lights, _LightPosition is used to compute the actual light direction because it is different at each shadow caster geometry vertex.
float3 _LightDirection;
float3 _LightPosition;

struct Attributes
{
    float4 positionOS   : POSITION;
    float3 normalOS     : NORMAL;
    float2 texcoord     : TEXCOORD0;
    uint   vertexID   : SV_VertexID;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    #if defined(_ALPHATEST_ON)
        float2 uv       : TEXCOORD0;
    #endif
    float4 positionCS   : SV_POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

float4 GetShadowPositionHClip(Attributes input)
{
    float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
    float3 normalWS = TransformObjectToWorldNormal(input.normalOS);

#if _CASTING_PUNCTUAL_LIGHT_SHADOW
    float3 lightDirectionWS = normalize(_LightPosition - positionWS);
#else
    float3 lightDirectionWS = _LightDirection;
#endif

    float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));
    positionCS = ApplyShadowClamping(positionCS);
    return positionCS;
}

Varyings ShadowPassVertex(Attributes input)
{
    Varyings output;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);

    #if defined(_ALPHATEST_ON)
    output.uv = TRANSFORM_TEX(input.texcoord, _BaseMap);
    #endif

	float slice = UNITY_ACCESS_INSTANCED_PROP(PerInstance, _AnimIndex);

// read per-animation meta for this slice
float uMeta = (slice + 0.5) * _InvAnimCount;
float4 row0 = SAMPLE_TEXTURE2D_LOD(_AnimMeta, sampler_AnimMeta, float2(uMeta, 0.25), float(0)); // (Fps, TotalFrames, RowsPerFrame, TimeScale)
float4 row1 = SAMPLE_TEXTURE2D_LOD(_AnimMeta, sampler_AnimMeta, float2(uMeta, 0.75), float(0)); // (TexWidth, TexHeight, -, -)


// keep your local variable names
float F     = row0.x;          // _FrameRate
float T     = row0.y;          // _TotalFrames
float rows  = row0.z;          // _RowsPerFrame
float t     = _Time.y * row0.w; // _TimeScale (per-anim from meta)
float w     = row1.x;          // _TextureWidth
float h     = row1.y;          // _TextureHeight

float3 positionOS = input.positionOS;
float3 normalOS   = input.normalOS;

float currentFrameFloat = frac((F/T) * t) * T;
int   frameIndex        = (int)floor(currentFrameFloat);
int   nextFrameIndex    = (frameIndex + 1) % (int)T;

float y      = floor(input.vertexID / w);
float yt0    = (rows * frameIndex) + y;
float V0     = (yt0 + 0.5) / h;

float yt1    = (rows * nextFrameIndex) + y;
float V1     = (yt1 + 0.5) / h;

float x      = fmod(input.vertexID, w);
float U      = (x + 0.5) / w;

float2 uv0   = float2(U, V0);
float2 uv1   = float2(U, V1);

// sample from the correct slice
float4 pos1  = SAMPLE_TEXTURE2D_ARRAY_LOD(_PositionTexture, sampler_PositionTexture, uv0, slice, 0);
float4 pos2  = SAMPLE_TEXTURE2D_ARRAY_LOD(_PositionTexture, sampler_PositionTexture, uv1, slice, 0);

float4 norm1 = SAMPLE_TEXTURE2D_ARRAY_LOD(_NormalTexture, sampler_NormalTexture,   uv0, slice, 0);
float4 norm2 = SAMPLE_TEXTURE2D_ARRAY_LOD(_NormalTexture, sampler_NormalTexture,   uv1, slice, 0);

float a      = frac(currentFrameFloat);
positionOS   = lerp(pos1,  pos2,  a).xyz;
normalOS     = lerp(norm1, norm2, a).xyz;

// write back
input.positionOS.xyz = positionOS;
input.normalOS       = normalOS;

    output.positionCS = GetShadowPositionHClip(input);
    return output;
}

half4 ShadowPassFragment(Varyings input) : SV_TARGET
{
    UNITY_SETUP_INSTANCE_ID(input);

    #if defined(_ALPHATEST_ON)
        Alpha(SampleAlbedoAlpha(input.uv, TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap)).a, _BaseColor, _Cutoff);
    #endif

    #if defined(LOD_FADE_CROSSFADE)
        LODFadeCrossFade(input.positionCS);
    #endif

    return 0;
}

#endif
