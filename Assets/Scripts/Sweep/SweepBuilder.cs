using UnityEngine;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using Random = Unity.Mathematics.Random;

namespace Sketch {

[System.Serializable]
public struct SweepConfig
{
    #region Editable members

    public int InstanceCount;
    public int Subdivision;
    public float2 BaseAngle;
    public float2 SweepAngle;
    public float2 Radius;
    public float2 Width;
    public float2 Height;
    public float Displace;
    public Transition Transition;
    public uint Seed;

    #endregion

    #region Helper properties

    public int VertexPerInstance => (Subdivision + 1) * 8 + 8;
    public int IndexPerInstance => Subdivision * 6 * 4 + 12;

    public int TotalVertexCount => InstanceCount * VertexPerInstance;
    public int TotalIndexCount => InstanceCount * IndexPerInstance;

    public Bounds BoundingBox
      => new Bounds(Vector3.zero,
                    math.float3(2, 2, 0) * (Radius.y + Width.y) +
                    math.float3(0, 0, 2) * (Height.y + Displace));

    #endregion

    #region Default constructor

    public static SweepConfig Default()
      => new SweepConfig()
           { InstanceCount = 512,
             Subdivision = 64,
             BaseAngle = math.float2(-math.PI, math.PI),
             SweepAngle = math.float2(0.3f, 1.0f),
             Radius = math.float2(0.2f, 0.8f),
             Width = math.float2(0.1f, 0.2f),
             Height = math.float2(0.01f, 0.02f),
             Displace = 0.1f,
             Transition = Transition.Default(),
             Seed = 0xdeadbeef };

    #endregion
}

[BurstCompile]
public struct SweepBuilderVertexJob : IJobParallelFor
{
    public SweepConfig Config;
    public float Time;

    [NativeDisableContainerSafetyRestriction]
    public NativeArray<Vertex> Output;

    public static JobHandle Schedule(in SweepConfig config,
                                     float time,
                                     NativeArray<Vertex> output)
      => new SweepBuilderVertexJob()
           { Config = config, Time = time, Output = output }
           .Schedule(config.InstanceCount, 1);

    public void Execute(int index)
    {
        ref var C = ref Config;

        var R = Random.CreateFromIndex((uint)index + C.Seed);
        R.NextUInt();

        var (f_in, f_out) = Config.Transition.FadeInOut(Time, R.UNorm());

        var radius = R.RangeXY(C.Radius);
        var width = R.RangeXY(C.Width) / 2;
        var angle = R.RangeXY(C.BaseAngle);
        var sweep = R.RangeXY(C.SweepAngle);
        var height = R.RangeXY(C.Height) / 2;
        var disp = R.SNorm() * C.Displace;
        var z1 = disp - height;
        var z2 = disp + height;

        var zn = math.float3(0, 0, -1);
        var zp = math.float3(0, 0, +1);

        var rsub = 1.0f / C.Subdivision;

        var wp = index * C.VertexPerInstance;

        (f_in, f_out) = (1 - f_out, 1 - f_in);
        (angle, sweep) = (angle + sweep * (f_in - 1 + f_out), sweep * (f_in - f_out));

        float3 ArcPoint(float t)
          => math.float3(math.cos(t), math.sin(t), 0);

        (float3, float3, float3, float3) PointToSlice(float3 n)
          => (math.float3(n.xy * (radius - width), z1),
              math.float3(n.xy * (radius + width), z1),
              math.float3(n.xy * (radius - width), z2),
              math.float3(n.xy * (radius + width), z2));

        {
            var point = ArcPoint(angle - 0.5f * sweep);
            var slice = PointToSlice(point);
            var normal = math.cross(point, math.float3(0, 0, 1));
            Output[wp++] = new Vertex(slice.Item1, normal);
            Output[wp++] = new Vertex(slice.Item2, normal);
            Output[wp++] = new Vertex(slice.Item3, normal);
            Output[wp++] = new Vertex(slice.Item4, normal);
        }

        for (var i = 0; i <= C.Subdivision; i++)
        {
            var n = ArcPoint(angle + sweep * (i * rsub - 0.5f));
            var slice = PointToSlice(n);

            Output[wp++] = new Vertex(slice.Item1, zn);
            Output[wp++] = new Vertex(slice.Item2, zn);

            Output[wp++] = new Vertex(slice.Item1, -n);
            Output[wp++] = new Vertex(slice.Item2, +n);

            Output[wp++] = new Vertex(slice.Item3, zp);
            Output[wp++] = new Vertex(slice.Item4, zp);

            Output[wp++] = new Vertex(slice.Item3, -n);
            Output[wp++] = new Vertex(slice.Item4, +n);
        }

        {
            var point = ArcPoint(angle + 0.5f * sweep);
            var slice = PointToSlice(point);
            var normal = math.cross(point, math.float3(0, 0, -1));
            Output[wp++] = new Vertex(slice.Item1, normal);
            Output[wp++] = new Vertex(slice.Item2, normal);
            Output[wp++] = new Vertex(slice.Item3, normal);
            Output[wp++] = new Vertex(slice.Item4, normal);
        }
    }
}

[BurstCompile]
public struct SweepBuilderIndexJob : IJobParallelFor
{
    public SweepConfig Config;

    [NativeDisableContainerSafetyRestriction]
    public NativeArray<uint> Output;

    public static JobHandle Schedule
      (in SweepConfig config, NativeArray<uint> output)
      => new SweepBuilderIndexJob()
           { Config = config, Output = output }
           .Schedule(config.InstanceCount, 1);

    public void Execute(int index)
    {
        var wp = index * Config.IndexPerInstance;
        var rp = (uint)(index * Config.VertexPerInstance);

        Output[wp++] = rp + 0;
        Output[wp++] = rp + 1;
        Output[wp++] = rp + 2;
        Output[wp++] = rp + 1;
        Output[wp++] = rp + 3;
        Output[wp++] = rp + 2;
        rp += 4;

        for (var i = 0u; i < Config.Subdivision; i++, rp += 8)
        {
            Output[wp++] = rp + 0;
            Output[wp++] = rp + 8;
            Output[wp++] = rp + 1;
            Output[wp++] = rp + 1;
            Output[wp++] = rp + 8;
            Output[wp++] = rp + 9;

            Output[wp++] = rp + 4;
            Output[wp++] = rp + 5;
            Output[wp++] = rp + 12;
            Output[wp++] = rp + 5;
            Output[wp++] = rp + 13;
            Output[wp++] = rp + 12;

            Output[wp++] = rp + 2;
            Output[wp++] = rp + 6;
            Output[wp++] = rp + 10;
            Output[wp++] = rp + 6;
            Output[wp++] = rp + 14;
            Output[wp++] = rp + 10;

            Output[wp++] = rp + 3;
            Output[wp++] = rp + 11;
            Output[wp++] = rp + 7;
            Output[wp++] = rp + 7;
            Output[wp++] = rp + 11;
            Output[wp++] = rp + 15;
        }

        rp += 8;

        Output[wp++] = rp + 0;
        Output[wp++] = rp + 2;
        Output[wp++] = rp + 1;
        Output[wp++] = rp + 1;
        Output[wp++] = rp + 2;
        Output[wp++] = rp + 3;
        rp += 4;
    }
}

[ExecuteInEditMode]
public sealed class SweepBuilder : MonoBehaviour, IMeshBuilder
{
    #region Editable properties

    [field:SerializeField]
    public SweepConfig Config = SweepConfig.Default();

    #endregion

    #region IMeshBuilder implementation

    public int VertexCount => Config.TotalVertexCount;
    public int IndexCount => Config.TotalIndexCount;
    public Bounds BoundingBox => Config.BoundingBox;

    public JobHandle ScheduleVertexJob(float time, NativeArray<Vertex> output)
      => SweepBuilderVertexJob.Schedule(Config, time, output);

    public JobHandle ScheduleIndexJob(float time, NativeArray<uint> output)
      => SweepBuilderIndexJob.Schedule(Config, output);

    #endregion
}

} // namespace Sketch
