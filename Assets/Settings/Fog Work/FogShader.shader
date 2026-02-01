Shader "Custom/FogShader"
{
    Properties
    {
        _MaxDistance("Max distance", float) = 100
        _StepSize("Step size", Range(.1, 20)) = 1
        _DensityMultiplier("Density multiplier", Range(0, 10)) = 1    
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

            float _MaxDistance;
            float _StepSize;
            float _DensityMultiplier;
            
            float get_density()
            {
                return _DensityMultiplier;
            }
            
            half4 frag(Varyings IN) : SV_Target
            {
                float depth = SampleSceneDepth(IN.texcoord);
                float3 worldPos = ComputeWorldSpacePosition(IN.texcoord, depth, UNITY_MATRIX_I_VP);
                
                float3 entryPoint = _WorldSpaceCameraPos;
                float3 viewDir = worldPos - entryPoint;
                float viewLength = length(viewDir);
                float3 rayDir = normalize(viewDir);
                
                float distLimit = min(viewLength, _MaxDistance);
                float distTraveled = 0;    // Distance travelled along ray already
                float transmittance = 0;      // Accumulated transmittance
                
                while (distTraveled < distLimit)
                {
                    float density = get_density();
                    if (density > 0)
                        transmittance += density * _StepSize;
                    distTraveled += _StepSize;
                }
                
                return transmittance;
                
                // Invert colors return 1.0 - SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, IN.texcoord);
            }
            ENDHLSL
        }
    }
}
