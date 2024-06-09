using Unity.Mathematics;

namespace Sketch {

[System.Serializable]
public struct Transition
{
    public float In;
    public float Stay;
    public float Out;
    public float Jitter;
    public float Power;

    public static Transition Default()
      => new Transition() { In = 2, Stay = 1, Out = 2, Jitter = 0.5f, Power = 1 };

    public (float, float) FadeInOut(float time, float rand01)
    {
        time -= rand01 * Jitter;
        return (1 - math.pow(1 - math.saturate(time / In), Power),
                math.pow(math.saturate((time - In - Stay) / Out), Power));
    }
}

} // namespace Sketch
