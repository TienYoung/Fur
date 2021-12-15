Shader "Fur/GPU Fur"
{
    Properties
    {
        [MainTexture] _BaseMap("Albedo", 2D) = "white" {}
        [MainColor] _BaseColor("Color", Color) = (1,1,1,1)

        [IntRange] _LayerCount("Layer Count", Range(0, 128)) = 8
        _FurLength("Fur Length", Range(0.0, 1.0)) = 1
        _FurPatternMap("Fur Pattern", 2D) = "white" {}
        _Gravity("Gravity", Range(0.0, 1.0)) = 0
        _Thickness("Thickness", Range(0.0, 1.0)) = 1
        _Top("Top", Range(0.0, 1.0)) = 1
    }
        SubShader
        {
            Tags{"RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "UniversalMaterialType" = "Lit" "IgnoreProjector" = "True" "ShaderModel" = "4.5"}
            LOD 300

            Pass
            {
                // Lightmode matches the ShaderPassName set in UniversalRenderPipeline.cs. SRPDefaultUnlit and passes with
                // no LightMode tag are also rendered by Universal Render Pipeline
                Name "BaseLit"
                Tags{"LightMode" = "UniversalForward"}

                HLSLPROGRAM
                #pragma exclude_renderers gles gles3 glcore
                #pragma target 4.5

                #pragma vertex LitPassVertex
                #pragma fragment LitPassFragment

                #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
                #include "Packages/com.unity.render-pipelines.universal/Shaders/LitForwardPass.hlsl"
                ENDHLSL
            }

            Pass
            {
                // Lightmode matches the ShaderPassName set in UniversalRenderPipeline.cs. SRPDefaultUnlit and passes with
                // no LightMode tag are also rendered by Universal Render Pipeline
                Name "ShellLit"
                Tags{"LightMode" = "FurShading"}

                ZWrite Off
                Blend SrcAlpha OneMinusSrcAlpha

                HLSLPROGRAM
                #pragma exclude_renderers gles gles3 glcore
                #pragma target 4.5

            //#pragma multi_compile_instancing
            //#pragma instancing_options procedural:ShellInstancingSetup
            #pragma vertex ShellPassVertex
            #pragma fragment ShellPassFragment


            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitForwardPass.hlsl"

                float _LayerCount;
                float _FurLength;
                float _Gravity;
                float _Thickness;
                float _Top;

                float4 _FurPatternMap_ST;
                TEXTURE2D(_FurPatternMap);        SAMPLER(sampler_FurPatternMap);
                

            ByteAddressBuffer _DeformedData;
            ByteAddressBuffer _StaticData;

            struct AttributesShell
            { 
                uint vertexID : SV_VertexID;
                uint instanceID : SV_InstanceID;
            };

            struct VaryingsShell
            {
                float4 positionCS               : SV_POSITION;

                float2 uv                       : TEXCOORD0;
                half3 normalWS                  : TEXCOORD1;
                half4 tangentWS                 : TEXCOORD2;    // xyz: tangent, w: sign
                float3 viewDirWS                : TEXCOORD3;
                half layer                      : TEXCOORD4;
            };

            float3 GetDeformedData_Position(ByteAddressBuffer vBuffer, uint vid)
            {
                int vidx = vid * 40;
                float3 data = asfloat(vBuffer.Load3(vidx));
                return data;
            }

            float3 GetDeformedData_Normal(ByteAddressBuffer vBuffer, uint vid)
            {
                int vidx = vid * 40;
                float3 data = asfloat(vBuffer.Load3(vidx + 12)); //offset by float3 (position) in front, so 3*4bytes = 12
                return data;
            }

            float4 GetDeformedData_Tangent(ByteAddressBuffer vBuffer, uint vid)
            {
                int vidx = vid * 40;
                float4 data = asfloat(vBuffer.Load4(vidx + 24)); //offset by float3 (position) + float3 (normal) in front, so 12 + 3*4bytes = 24
                return data;
            }

            //float4 GetStaticData_Color(ByteAddressBuffer vBuffer, uint vid)
            //{
            //    int vidx = vid * 12;
            //    float data = asfloat(vBuffer.Load4(vidx));
            //    return data;
            //}

            float2 GetStaticData_TexCoord0(ByteAddressBuffer vBuffer, uint vid)
            {
                int vidx = vid * 12;
                float2 data = asfloat(vBuffer.Load2(vidx + 4));
                return data;
            }

            // Used in Fur shader
            VaryingsShell ShellPassVertex(AttributesShell input)
            {
                VaryingsShell output = (VaryingsShell)0;

                float3 positionOS = GetDeformedData_Position(_DeformedData, input.vertexID);
                float3 normalOS = GetDeformedData_Normal(_DeformedData, input.vertexID);
                float4 tangentOS = GetDeformedData_Tangent(_DeformedData, input.vertexID);
                float2 texcoord = GetStaticData_TexCoord0(_StaticData, input.vertexID);


                float layer = input.instanceID / _LayerCount;
                positionOS.xyz += (normalOS + _Gravity * float3(0, 0, -1)) * _FurLength * 0.1 * layer;

                VertexPositionInputs vertexInput = GetVertexPositionInputs(positionOS);
                output.positionCS = vertexInput.positionCS;

                output.uv = texcoord;

                // normalWS and tangentWS already normalize.
                // this is required to avoid skewing the direction during interpolation
                // also required for per-vertex lighting and SH evaluation
                VertexNormalInputs normalInput = GetVertexNormalInputs(normalOS, tangentOS);

                // already normalized from normal transform to WS.
                output.normalWS = normalInput.normalWS;
                real sign = tangentOS.w * GetOddNegativeScale();
                output.tangentWS = half4(normalInput.tangentWS.xyz, sign);

                output.viewDirWS = GetWorldSpaceNormalizeViewDir(vertexInput.positionWS);

                output.layer = layer;

                return output;
            }

            // Used in Fur shader
            half4 ShellPassFragment(VaryingsShell input) : SV_Target
            {
                float2 baseUV = TRANSFORM_TEX(input.uv, _BaseMap);
                SurfaceData surfaceData;
                InitializeStandardLitSurfaceData(baseUV, surfaceData);

                InputData inputData = (InputData)0;

                float sgn = input.tangentWS.w;      // should be either +1 or -1
                float3 bitangent = sgn * cross(input.normalWS.xyz, input.tangentWS.xyz);
                half3x3 tangentToWorld = half3x3(input.tangentWS.xyz, bitangent.xyz, input.normalWS.xyz);

                inputData.tangentToWorld = tangentToWorld;
                
                inputData.normalWS = TransformTangentToWorld(surfaceData.normalTS, tangentToWorld);
                inputData.normalWS = NormalizeNormalPerPixel(inputData.normalWS);
                inputData.viewDirectionWS = input.viewDirWS;

                SETUP_DEBUG_TEXTURE_DATA(inputData, baseUV, _BaseMap);


                half4 color = UniversalFragmentPBR(inputData, surfaceData);

                float2 furPatternUV = TRANSFORM_TEX(input.uv, _FurPatternMap);
                half fur = SAMPLE_TEXTURE2D(_FurPatternMap, sampler_FurPatternMap, furPatternUV).r;

                half layer = input.layer * input.layer + 0.04;
                fur -= lerp(_Thickness, _Top, layer);
                color.rgb *= layer;
                color.a = saturate(fur);

                return color;
            }

            ENDHLSL
        }
    }
}
