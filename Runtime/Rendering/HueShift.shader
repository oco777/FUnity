Shader "FUnity/Effects/HueShift"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _HueDegrees ("Hue Degrees", Range(0, 360)) = 0
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Transparent" }
        Pass
        {
            Name "HueShift"
            Cull Off
            ZWrite Off
            ZTest Always

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float _HueDegrees;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            float3 RgbToHsv(float3 rgb)
            {
                float4 k = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
                float4 p = (rgb.g < rgb.b) ? float4(rgb.bg, k.wz) : float4(rgb.gb, k.xy);
                float4 q = (rgb.r < p.x) ? float4(p.xyw, rgb.r) : float4(rgb.r, p.yzx);
                float d = q.x - min(q.w, q.y);
                float epsilon = 1e-10;
                float hue = abs((q.w - q.y) / (6.0 * d + epsilon));
                float saturation = d / (q.x + epsilon);
                float value = q.x;
                return float3(hue, saturation, value);
            }

            float3 HsvToRgb(float3 hsv)
            {
                float3 p = abs(frac(hsv.xxx + float3(0.0, 1.0 / 3.0, 2.0 / 3.0)) * 6.0 - 3.0);
                float3 q = saturate(p - 1.0);
                return hsv.z * lerp(float3(1.0, 1.0, 1.0), q, hsv.y);
            }

            float3 ShiftHue(float3 rgb, float hueDegrees)
            {
                float3 hsv = RgbToHsv(rgb);
                hsv.x = frac(hsv.x + hueDegrees / 360.0);
                return HsvToRgb(hsv);
            }

            float4 frag(Varyings input) : SV_Target
            {
                float4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                color.rgb = ShiftHue(color.rgb, _HueDegrees);
                return color;
            }
            ENDHLSL
        }
    }
    FallBack Off
}
