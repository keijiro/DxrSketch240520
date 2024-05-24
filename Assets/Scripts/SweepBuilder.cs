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
    public float Depth;
    public uint Seed;

    #endregion

    #region Helper properties

    public int VertexPerInstance => (Subdivision + 1) * 2;
    public int IndexPerInstance => Subdivision * 6;

    public int TotalVertexCount => InstanceCount * VertexPerInstance;
    public int TotalIndexCount => InstanceCount * IndexPerInstance;

    public Bounds BoundingBox
      => new Bounds(Vector3.zero,
                    math.float3(1, 1, 0) * Radius.y +
                    math.float3(0, 0, 1) * Depth);

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
             Depth = 0.1f,
             Seed = 0xdeadbeef };

    #endregion
}

[BurstCompile]
public struct SweepBuilderVertexJob : IJobParallelFor
{
    public SweepConfig Config;

    [NativeDisableContainerSafetyRestriction]
    public NativeArray<Vertex> Output;

    public static JobHandle Schedule
      (in SweepConfig config, NativeArray<Vertex> output)
      => new SweepBuilderVertexJob()
           { Config = config, Output = output }
           .Schedule(config.InstanceCount, 1);

    public void Execute(int index)
    {
        ref var C = ref Config;

        var R = Random.CreateFromIndex((uint)index + C.Seed);
        R.NextUInt();

        var radius = R.RangeXY(C.Radius);
        var width = R.RangeXY(C.Width) / 2;
        var angle = R.RangeXY(C.BaseAngle);
        var sweep = R.RangeXY(C.SweepAngle);
        var z = R.SNorm() * C.Depth;

        var normal = math.float3(0, 0, -1);
        var rsub = 1.0f / C.Subdivision;

        var wp = index * C.VertexPerInstance;
        for (var i = 0; i <= C.Subdivision; i++)
        {
            var param = i * rsub;
            var theta = angle + sweep * (param - 0.5f);
            var xy = math.float2(math.cos(theta), math.sin(theta));
            var p1 = math.float3(xy * (radius - width), z);
            var p2 = math.float3(xy * (radius + width), z);
            Output[wp++] = new Vertex(p1, normal);
            Output[wp++] = new Vertex(p2, normal);
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
        for (var i = 0u; i < Config.Subdivision; i++, rp += 2)
        {
            Output[wp++] = rp + 0;
            Output[wp++] = rp + 2;
            Output[wp++] = rp + 1;
            Output[wp++] = rp + 2;
            Output[wp++] = rp + 3;
            Output[wp++] = rp + 1;
        }
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

    public JobHandle ScheduleVertexJob(NativeArray<Vertex> output)
      => SweepBuilderVertexJob.Schedule(Config, output);

    public JobHandle ScheduleIndexJob(NativeArray<uint> output)
      => SweepBuilderIndexJob.Schedule(Config, output);

    #endregion
}

} // namespace Sketch
