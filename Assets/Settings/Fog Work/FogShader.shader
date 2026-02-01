Shader "Tutorial/VolumetricFog_Stark"
{
    Properties
    {
        _Color("Color", Color) = (1, 1, 1, 1)
        _MaxDistance("Max distance", float) = 100
        _StartDistance("Start distance", float) = 5
        _StepSize("Step size", Range(0.1, 20)) = 1
        _BaseDensity("Base Density (Solid)", Range(0, 1)) = 0.1 
        _DensityMultiplier("Noise Multiplier", Range(0, 10)) = 1
        
        _NoiseOffset("Noise offset", float) = 0
        _FogNoise("Fog noise", 3D) = "white" {}
        _NoiseTiling("Noise tiling", float) = 1
        _DensityThreshold("Density threshold", Range(0, 1)) = 0.1
        
        [HDR]_LightContribution("Light contribution", Color) = (1, 1, 1, 1)
        _LightScattering("Light scattering", Range(0, 1)) = 0.2
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            float4 _Color;
            float _MaxDistance;
            float _StartDistance;
            float _BaseDensity;
            float _DensityMultiplier;
            float _StepSize;
            float _NoiseOffset;
            TEXTURE3D(_FogNoise);
            float _DensityThreshold;
            float _NoiseTiling;
            float4 _LightContribution;
            float _LightScattering;

            float henyey_greenstein(float angle, float scattering)
            {
                return (1.0 - angle * angle) / (4.0 * PI * pow(1.0 + scattering * scattering - (2.0 * scattering) * angle, 1.5f));
            }

            float get_density(float3 worldPos)
            {
                float4 noise = _FogNoise.SampleLevel(sampler_TrilinearRepeat, worldPos * 0.01 * _NoiseTiling, 0);
                float noiseDensity = dot(noise, noise);
                // Clouds
                noiseDensity = saturate(noiseDensity - _DensityThreshold) * _DensityMultiplier;
                
                // Keep fog a bit dense to make hard to see through
                return saturate(noiseDensity) + _BaseDensity;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float4 col = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, IN.texcoord);
                float depth = SampleSceneDepth(IN.texcoord);
                float3 worldPos = ComputeWorldSpacePosition(IN.texcoord, depth, UNITY_MATRIX_I_VP);

                float3 entryPoint = _WorldSpaceCameraPos;
                float3 viewDir = worldPos - _WorldSpaceCameraPos;
                float viewLength = length(viewDir);
                float3 rayDir = normalize(viewDir);

                float2 pixelCoords = IN.texcoord * _BlitTexture_TexelSize.zw;
                float distLimit = min(viewLength, _MaxDistance);
                float safeStepSize = max(_StepSize, 0.1);

                float startOffset = InterleavedGradientNoise(pixelCoords, (int)(_Time.y / max(HALF_EPS, unity_DeltaTime.x))) * _NoiseOffset;
                float distTravelled = _StartDistance + startOffset;

                float accumulatedDensity = 0; 
                float4 fogCol = _Color;
                int maxSteps = 128; 

                for(int i = 0; i < maxSteps; i++)
                {
                    if (distTravelled >= distLimit) break;
                    if (accumulatedDensity >= 1.0) break;

                    float3 rayPos = entryPoint + rayDir * distTravelled;
                    float density = get_density(rayPos);
                    
                    if (density > 0)
                    {
                        Light mainLight = GetMainLight(TransformWorldToShadowCoord(rayPos));
                        fogCol.rgb += mainLight.color.rgb * _LightContribution.rgb * henyey_greenstein(dot(rayDir, mainLight.direction), _LightScattering) * density * mainLight.shadowAttenuation * safeStepSize;
                        
                        // Accumulate density
                        accumulatedDensity += density * safeStepSize; 
                    }
                    distTravelled += safeStepSize;
                }
                
                return lerp(col, fogCol, saturate(accumulatedDensity));
            }
            ENDHLSL
        }
    }
}