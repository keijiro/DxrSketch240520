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
    public float Radius;
    public uint Seed;

    #endregion

    #region Helper properties

    public int VertexPerInstance => (Subdivision + 1) * 2;
    public int IndexPerInstance => Subdivision * 6;

    #endregion

    #region Default constructor

    public static SweepConfig Default()
      => new SweepConfig()
           { InstanceCount = 512,
             Subdivision = 64,
             Radius = 0.5f,
             Seed = 0xdeadbeef };

    #endregion
}

[ExecuteInEditMode]
public sealed class SweepBuilder : MonoBehaviour, IMeshBuilder
{
    #region Editable properties

    [field:SerializeField]
    public SweepConfig Config = SweepConfig.Default();

    #endregion

    #region IMeshBuilder implementation

    public int VertexCount => Config.InstanceCount * Config.VertexPerInstance;
    public int IndexCount => Config.InstanceCount * Config.IndexPerInstance;
    public Bounds BoundingBox => new Bounds(Vector3.zero, Vector3.one * Config.Radius * 2);

    public JobHandle ScheduleVertexJob(NativeArray<Vertex> output)
      => SweepBuilderVertexJob.Schedule(Config, output);

    public JobHandle ScheduleIndexJob(NativeArray<uint> output)
      => SweepBuilderIndexJob.Schedule(Config, output);

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
        var rand = Random.CreateFromIndex((uint)index + Config.Seed);
        rand.NextUInt();

        var r1 = Config.Radius * rand.NextFloat();
        var r2 = Config.Radius * rand.NextFloat(0.1f) + r1;

        var theta = rand.NextFloat(math.PI * 2);
        var width = rand.NextFloat(0.4f, 1.0f);
        var z = rand.NextFloat(-0.2f, 0.2f);

        var normal = math.float3(0, 0, -1);

        var wp = index * Config.VertexPerInstance;
        for (var i = 0; i <= Config.Subdivision; i++)
        {
            var t = theta + i * width / Config.Subdivision;
            var x = math.cos(t);
            var y = math.sin(t);
            var p1 = math.float3(x * r1, y * r1, z);
            var p2 = math.float3(x * r2, y * r2, z);
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

} // namespace Sketch
