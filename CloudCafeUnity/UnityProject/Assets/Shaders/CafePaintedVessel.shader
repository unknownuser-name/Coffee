Shader "CloudCafe/PaintedVessel" {
 Properties { _Paint("Existing game asset",2D)="white"{} _TopPaint("Original pitcher rim",2D)="white"{} _BaseColor("Palette",Color)=(.4,.5,.65,1) _Cup("Cup",Float)=0 }
 SubShader { Tags {"RenderType"="Opaque"} Cull Off Pass { CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 sampler2D _Paint,_TopPaint;float4 _BaseColor;float _Cup;
 struct v2f {float4 pos:SV_POSITION;float3 local:TEXCOORD0;float3 normal:TEXCOORD1;};
 v2f vert(appdata_base v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.local=v.vertex.xyz;o.normal=UnityObjectToWorldNormal(v.normal);return o;}
 fixed4 frag(v2f i):SV_Target {
  float3 p=i.local+lerp(float3(0,.65,-.28),float3(0,0,0),_Cup);
  float2 uv=lerp(float2(.38+p.x*.54,.1+p.y*.60),float2(.4-p.x*.54,.08+p.y*.9),_Cup);
  float4 paint=tex2D(_Paint,saturate(uv));
  float light=floor(saturate(dot(normalize(i.normal),normalize(float3(-.45,.8,.5)))*.45+.55)*5)/5;
  float3 color=lerp(_BaseColor.rgb*(.65+light*.5),paint.rgb,smoothstep(.15,.7,paint.a)*.88);
  if(_Cup<.5&&p.y>1.15){float4 rim=tex2D(_TopPaint,saturate(float2(.49+p.x*.52,.45+p.y*.12-p.z*.48)));color=lerp(color,rim.rgb,rim.a*.75);if(abs(i.normal.y)>.35)color=lerp(color,float3(.94,.87,.72),.5);}
  return float4(color,1);
 }
 ENDCG }}
}
