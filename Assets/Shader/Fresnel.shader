Shader "Custom/FresnelShader" {
	Properties {
	 	_Shininess ("Shininess", Range (0.01, 3)) = 1

	 	_Rim ("Rim", Range (0.01, 10)) = 1

	 	_MyColor ("Shine Color", Color) = (1,1,1,1) 

	}
	SubShader {
        Tags {"Queue"="Transparent" "RenderType"="Transparent" }
        LOD 100

        ZWrite On
        Blend SrcAlpha OneMinusSrcAlpha

		CGPROGRAM
		#pragma surface surf Lambert

		sampler2D _MainTex;
		sampler2D _Bump;
		float _Shininess;
		float _Rim;
		fixed4 _MyColor; 

		struct Input {
			float2 uv_MainTex;
			float2 uv_Bump;
			float3 viewDir;
		};

		void surf (Input IN, inout SurfaceOutput o) {
			o.Normal = UnpackNormal(tex2D(_Bump, IN.uv_Bump));
			
 			half rim = 1.0 - saturate(dot (normalize(IN.viewDir), o.Normal));
            o.Emission = _MyColor.rgb * pow (rim, _Rim);
			o.Albedo = o.Emission;
			o.Alpha = _MyColor.a + rim;
		}
		ENDCG
	} 
	FallBack "Diffuse"
}