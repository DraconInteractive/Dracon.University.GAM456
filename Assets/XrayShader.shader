// xray mouse pos shader test v2.0 - mgear - http://unitycoder.com/blog

Shader "UnityLibrary/Effects/XRay2017"
{
	Properties 
	{
		_MainTex ("Base (RGB)", 2D) = "white" {}
		_Cutoff ("Alpha cutoff", Range(0,1)) = 0.5
		_Radius ("HoleRadius", Range(0.1,5)) = 2
	}
	SubShader 
	{
		Tags {"Queue"="AlphaTest" "IgnoreProjector"="True" "RenderType"="TransparentCutout"}
		LOD 100

		Cull Off // draw backfaces also, comment this line if no need for backfaces
		
		CGPROGRAM
		#pragma surface surf Lambert alphatest:_Cutoff

		struct Input 
		{
			float2 uv_MainTex;
			float3 worldPos;
		};
		
		sampler2D _MainTex;
		uniform float4 _MouseHit;
		uniform float _Radius;

		void surf (Input IN, inout SurfaceOutput o) 
		{
			half3 col = tex2D (_MainTex, IN.uv_MainTex).rgb;
			float dx = length(_MouseHit.x-IN.worldPos.x);
			float dy = length(_MouseHit.y-IN.worldPos.y);
			float dz = length(_MouseHit.z-IN.worldPos.z);
			float dist = (dx*dx+dy*dy+dz*dz)*_Radius;
			dist = clamp(dist,0,1);
			o.Albedo = col; // color is from texture
			o.Alpha = dist;  // alpha is from distance to the mouse
		}
		ENDCG
	} 
	FallBack "Diffuse"
}