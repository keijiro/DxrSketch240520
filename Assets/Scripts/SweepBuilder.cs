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
    public int InstanceCount;
    public int Subdivision;
    public float Radius;
    public uint Seed;

    public int VertexPerInstance => (Subdivision + 1) * 2;
    public int IndexPerInstance => Subdivision * 6;

    public static SweepConfig Default()
      => new SweepConfig()
           { InstanceCount = 512, Subdivision = 64, Radius = 0.5f, Seed = 0xdeadbeefu };
}

[ExecuteInEditMode]
public sealed class SweepBuilder : MonoBehaviour, IMeshBuilder
{
    [field:SerializeField] public SweepConfig Config = SweepConfig.Default();

    public int InstanceCount => Config.InstanceCount;
    public int VertexPerInstance => Config.VertexPerInstance;
    public int IndexPerInstance => Config.IndexPerInstance;
    public Bounds BoundingBox => new Bounds(Vector3.zero, Vector3.one * Config.Radius);

    public JobHandle ScheduleWriteVertexArrayJob(NativeArray<Vertex> array)
    {
        var job = new SweepBuilderVertexJob(){ Config = Config, Array = array };
        return job.Schedule(InstanceCount, 1);
    }

    public JobHandle ScheduleWriteIndexArrayJob(NativeArray<uint> array)
    {
        var job = new SweepBuilderIndexJob(){ Config = Config, Array = array };
        return job.Schedule(InstanceCount, 1);
    }
}

[BurstCompile]
public struct SweepBuilderVertexJob : IJobParallelFor
{
    public SweepConfig Config;

    [NativeDisableContainerSafetyRestriction]
    public NativeArray<Vertex> Array;

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

        var ptr = index * Config.VertexPerInstance;
        for (var i = 0; i <= Config.Subdivision; i++)
        {
            var t = theta + i * width / Config.Subdivision;
            var x = math.cos(t);
            var y = math.sin(t);
            var p1 = math.float3(x * r1, y * r1, z);
            var p2 = math.float3(x * r2, y * r2, z);
            Array[ptr++] = new Vertex(p1, normal);
            Array[ptr++] = new Vertex(p2, normal);
        }
    }
}

[BurstCompile]
public struct SweepBuilderIndexJob : IJobParallelFor
{
    public SweepConfig Config;

    [NativeDisableContainerSafetyRestriction]
    public NativeArray<uint> Array;

    public void Execute(int index)
    {
        var ptr = index * Config.IndexPerInstance;
        var indexOffset = index * Config.VertexPerInstance;
        for (var i = 0u; i < Config.Subdivision; i++)
        {
            var offs = (uint)indexOffset + i * 2;
            Array[ptr++] = offs + 0;
            Array[ptr++] = offs + 2;
            Array[ptr++] = offs + 1;
            Array[ptr++] = offs + 2;
            Array[ptr++] = offs + 3;
            Array[ptr++] = offs + 1;
        }
    }
}

} // namespace Sketch
