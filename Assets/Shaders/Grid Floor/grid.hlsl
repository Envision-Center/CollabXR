void filtered_grid_float(in float2 p, in float n, out float o)
{
    p += 0.5 / n;
    const float2 w = max(abs(ddx(p)), abs(ddy(p)));
    const float2 a = p + 0.5*w;
    const float2 b = p - 0.5*w;
    float2 i = (floor(a)+min(frac(a)*n,1.0)-floor(b)-min(frac(b)*n,1.0))/(n*w);
    o = 1 - (1.0-i.x)*(1.0-i.y);
}
