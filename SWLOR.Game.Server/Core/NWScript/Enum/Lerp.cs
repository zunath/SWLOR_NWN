namespace SWLOR.Game.Server.Core.NWScript.Enum
{
    public enum Lerp
    {
        None = 0, // 1
        Linear = 1, // x
        Smoothstep = 2, // x * x * (3 - 2 * x)
        InverseSmoothstep = 3, // 0.5 - sin(asin(1.0 - 2.0 * x) / 3.0)
        EaseIn = 4, // (1 - cosf(x * M_PI * 0.5))
        EaseOut = 5, // sinf(x * M_PI * 0.5)
        Quadratic = 6, // x * x
        SmootherStep = 7 // (x * x * x * (x * (6.0 * x - 15.0) + 10.0))

    }
}
