inline float3 rgb2hsv(const float3 c)
{
    float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
    float4 p = lerp(float4(c.bg, K.wz), float4(c.gb, K.xy), step(c.b, c.g));
    float4 q = lerp(float4(p.xyw, c.r), float4(c.r, p.yzx), step(p.x, c.r));

    float d = q.x - min(q.w, q.y);
    float e = 1.0e-10;
    return float3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
}

inline float3 hsv2rgb(const float3 c)
{
    float4 K = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
    float3 p = abs(frac(c.xxx + K.xyz) * 6.0 - K.www);
    return c.z * lerp(K.xxx, clamp(p - K.xxx, 0.0, 1.0), c.y);
}

inline float invLerp(const float a, const float b, const float value)
{
    return clamp((value - a) / (b - a), 0.0, 1.0);
}

static float2 neighbours8offsets[] ={
    float2(+0, -1),
    float2(-1, +0),
    float2(+1, +0),
    float2(+0, +1),
    float2(-1, -1),
    float2(-1, +1),
    float2(+1, -1),
    float2(+1, +1),
};
