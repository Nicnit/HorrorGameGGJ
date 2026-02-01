Shader "Custom/FogShader"
{
    Properties
    {
        _Color("Color", Color) = (1, 1, 1, 1)
        _MaxDistance("Max distance", float) = 100
        _StepSize("Step size", Range(.1, 20)) = 1
        _DensityMultiplier("Density multiplier", Range(0, 10)) = 1
        _NoiseOffset("NoiseOffset", float) = 0.0
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            HLSLPROGRAM

            #pragma vertex Vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            float4 _Color;
            float _MaxDistance;
            float _StepSize;
            float _DensityMultiplier;
            float _NoiseOffset;
            
            float get_density()
            {
                return _DensityMultiplier;
            }
            
            half4 frag(Varyings IN) : SV_Target
            {
                float4 col = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, IN.texcoord);
                float depth = SampleSceneDepth(IN.texcoord);
                float3 worldPos = ComputeWorldSpacePosition(IN.texcoord, depth, UNITY_MATRIX_I_VP);
                
                float3 entryPoint = _WorldSpaceCameraPos;
                float3 viewDir = worldPos - entryPoint;
                float viewLength = length(viewDir);
                float3 rayDir = normalize(viewDir);
                
                float2 pixelCoords = IN.texcoord * _BlitTexture_TexelSize.zw;
                float distLimit = min(viewLength, _MaxDistance);
                // Distance travelled along ray already
                float distTraveled = InterleavedGradientNoise(pixelCoords, 
                    (int)(max(HALF_EPS, _Time.y) / unity_DeltaTime.x) * _NoiseOffset);
                float transmittance = 1;      // Accumulated transmittance
                
                while (distTraveled < distLimit)
                {
                    float density = get_density();
                    if (density > 0)
                        transmittance *= exp(-density * _StepSize); // TODO PART 1 UNDO
                    distTraveled += _StepSize;
                }
                                        // TODO PART 1 UNDO
                return lerp(col, _Color, 1.0 - saturate(transmittance));
                
                // Invert colors return 1.0 - SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, IN.texcoord);
            }
            ENDHLSL
        }
    }
}
