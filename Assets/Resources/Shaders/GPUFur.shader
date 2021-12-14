Shader "Fur/GPU Fur"
{
    Properties
    {
        [MainTexture] _BaseMap("Albedo", 2D) = "white" {}
        [MainColor] _BaseColor("Color", Color) = (1,1,1,1)

        _LayerCount("Layer Count", Float) = 8
        _FurLength("Fur Length", Range(0.0, 1.0)) = 1
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

            HLSLPROGRAM
            #pragma exclude_renderers gles gles3 glcore
            #pragma target 4.5

            #pragma multi_compile_instancing
            #pragma vertex ShellPassVertex
            #pragma fragment ShellPassFragment

            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitForwardPass.hlsl"

            struct Vertex
            {
                float3 positionOS;
                float3 normalOS;
                float4 tangentOS;
            };

            UNITY_INSTANCING_BUFFER_START(FurPerMaterial)
                UNITY_DEFINE_INSTANCED_PROP(float, _LayerCount)
                UNITY_DEFINE_INSTANCED_PROP(uint, _LayerNums)
                UNITY_DEFINE_INSTANCED_PROP(float, _FurLength)
            UNITY_INSTANCING_BUFFER_END(FurPerMaterial)

            ByteAddressBuffer _Vertics;
            StructuredBuffer<float2> _Texcoords;

            struct AttributesShell
            { 
                uint vertexID : SV_VertexID;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            float3 GetVertexData_Position(ByteAddressBuffer vBuffer, uint vid)
            {
                int vidx = vid * 40;
                float3 data = asfloat(vBuffer.Load3(vidx));
                return data;
            }

            float3 GetVertexData_Normal(ByteAddressBuffer vBuffer, uint vid)
            {
                int vidx = vid * 40;
                float3 data = asfloat(vBuffer.Load3(vidx + 12)); //offset by float3 (position) in front, so 3*4bytes = 12
                return data;
            }

            float4 GetVertexData_Tangent(ByteAddressBuffer vBuffer, uint vid)
            {
                int vidx = vid * 40;
                float4 data = asfloat(vBuffer.Load4(vidx + 24)); //offset by float3 (position) + float3 (normal) in front, so 12 + 3*4bytes = 24
                return data;
            }


            // Used in Fur shader
            Varyings ShellPassVertex(AttributesShell input)
            {
                Varyings output = (Varyings)0;

                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float4 positionOS = float4(GetVertexData_Position(_Vertics, input.vertexID), 1.0);
                float3 normalOS = GetVertexData_Normal(_Vertics, input.vertexID);
                float4 tangentOS = GetVertexData_Tangent(_Vertics, input.vertexID);
                float2 texcoord = _Texcoords[input.vertexID];


                float layerCount = UNITY_ACCESS_INSTANCED_PROP(FurPerMaterial, _LayerCount);
                uint layerNums = UNITY_ACCESS_INSTANCED_PROP(FurPerMaterial, _LayerNums);
                float furLength = UNITY_ACCESS_INSTANCED_PROP(FurPerMaterial, _FurLength);

                float layer = layerNums / layerCount;
                positionOS.xyz += normalOS * 1 * 1;

                VertexPositionInputs vertexInput = GetVertexPositionInputs(positionOS.xyz);

                // normalWS and tangentWS already normalize.
                // this is required to avoid skewing the direction during interpolation
                // also required for per-vertex lighting and SH evaluation
                VertexNormalInputs normalInput = GetVertexNormalInputs(normalOS, tangentOS);

                half3 vertexLight = VertexLighting(vertexInput.positionWS, normalInput.normalWS);

                half fogFactor = 0;
                #if !defined(_FOG_FRAGMENT)
                    fogFactor = ComputeFogFactor(vertexInput.positionCS.z);
                #endif

                output.uv = TRANSFORM_TEX(texcoord, _BaseMap);

                // already normalized from normal transform to WS.
                output.normalWS = normalInput.normalWS;
            #if defined(REQUIRES_WORLD_SPACE_TANGENT_INTERPOLATOR) || defined(REQUIRES_TANGENT_SPACE_VIEW_DIR_INTERPOLATOR)
                real sign = tangentOS.w * GetOddNegativeScale();
                half4 tangentWS = half4(normalInput.tangentWS.xyz, sign);
            #endif
            #if defined(REQUIRES_WORLD_SPACE_TANGENT_INTERPOLATOR)
                output.tangentWS = tangentWS;
            #endif

            #if defined(REQUIRES_TANGENT_SPACE_VIEW_DIR_INTERPOLATOR)
                half3 viewDirWS = GetWorldSpaceNormalizeViewDir(vertexInput.positionWS);
                half3 viewDirTS = GetViewDirectionTangentSpace(tangentWS, output.normalWS, viewDirWS);
                output.viewDirTS = viewDirTS;
            #endif

                OUTPUT_SH(output.normalWS.xyz, output.vertexSH);
            #ifdef _ADDITIONAL_LIGHTS_VERTEX
                output.fogFactorAndVertexLight = half4(fogFactor, vertexLight);
            #else
                output.fogFactor = fogFactor;
            #endif

            #if defined(REQUIRES_WORLD_SPACE_POS_INTERPOLATOR)
                output.positionWS = vertexInput.positionWS;
            #endif

            #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                output.shadowCoord = GetShadowCoord(vertexInput);
            #endif

                output.positionCS = vertexInput.positionCS;

                return output;
            }

            half4 ShellPassFragment(Varyings input) : SV_Target
            {
                return half4(1, 0, 0, 1);
            }

            ENDHLSL
        }
    }
}
