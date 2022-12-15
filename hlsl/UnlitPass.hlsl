// for now
struct appdata
{
    float4 vertex : POSITION;
    float2 uv : TEXCOORD0;
};

struct v2f
{
    float2 uv : TEXCOORD0;
    float4 vertex : SV_POSITION;
};

v2f vert (appdata v)
{
    v2f o;
    o.vertex = UnityObjectToClipPos(v.vertex);
    //o.uv = TRANSFORM_TEX(v.uv, _MainTex);
    //UNITY_TRANSFER_FOG(o,o.vertex);
    return o;
}

half4 frag (v2f i) : SV_Target
{
    // sample the texture
//    fixed4 col = tex2D(_MainTex, i.uv);
SurfaceDescriptionInputs sdi;
SurfaceDescription sd = SurfaceDescriptionFunction(sdi);
    return half4(sd.Color, sd.Alpha);
}