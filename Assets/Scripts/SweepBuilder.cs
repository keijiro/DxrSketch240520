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

    public static SweepConfig Default()
      => new SweepConfig()
           { InstanceCount = 512, Subdivision = 64, Radius = 0.5f, Seed = 0xdeadbeefu };
}

[ExecuteInEditMode]
public sealed class SweepBuilder : MonoBehaviour, IMeshBuilder
{
    [field:SerializeField] public SweepConfig Config = SweepConfig.Default();

    public int InstanceCount => Config.InstanceCount;
    public int VertexPerInstance => (Config.Subdivision + 1) * 2;
    public int IndexPerInstance => Config.Subdivision * 6;
    public Bounds BoundingBox => new Bounds(Vector3.zero, Vector3.one * 100);

    public JobHandle ScheduleWriteVertexArrayJob
      (int instanceIndex, NativeSlice<Vertex> slice)
    {
        var job = new SweepBuilderVertexJob()
          { InstanceIndex = instanceIndex, Slice = slice, Config = Config };
        return job.Schedule();
    }

    public JobHandle ScheduleWriteIndexArrayJob
      (int instanceIndex, NativeSlice<uint> slice, uint indexOffset)
    {
        var job = new SweepBuilderIndexJob()
          { InstanceIndex = instanceIndex, IndexOffset = indexOffset, Slice = slice, Config = Config };
        return job.Schedule();
    }
}

[BurstCompile]
public struct SweepBuilderVertexJob : IJob
{
    public int InstanceIndex;
    [NativeDisableContainerSafetyRestriction]
    public NativeSlice<Vertex> Slice;
    public SweepConfig Config;

    public void Execute()
    {
        var rand = Random.CreateFromIndex((uint)InstanceIndex + Config.Seed);
        rand.NextUInt();

        var r1 = Config.Radius * rand.NextFloat();
        var r2 = Config.Radius * rand.NextFloat(0.1f) + r1;

        var theta = rand.NextFloat(math.PI * 2);
        var width = rand.NextFloat(0.4f, 1.0f);
        var z = rand.NextFloat(-0.2f, 0.2f);

        var normal = math.float3(0, 0, -1);

        var ptr = 0;
        for (var i = 0; i <= Config.Subdivision; i++)
        {
            var t = theta + i * width / Config.Subdivision;
            var x = math.cos(t);
            var y = math.sin(t);
            var p1 = math.float3(x * r1, y * r1, z);
            var p2 = math.float3(x * r2, y * r2, z);
            Slice[ptr++] = new Vertex(p1, normal);
            Slice[ptr++] = new Vertex(p2, normal);
        }
    }
}

[BurstCompile]
public struct SweepBuilderIndexJob : IJob
{
    public int InstanceIndex;
    public uint IndexOffset;
    [NativeDisableContainerSafetyRestriction]
    public NativeSlice<uint> Slice;
    public SweepConfig Config;

    public void Execute()
    {
        var ptr = 0;
        for (var i = 0u; i < Config.Subdivision; i++)
        {
            var offs = IndexOffset + i * 2;
            Slice[ptr++] = offs + 0;
            Slice[ptr++] = offs + 2;
            Slice[ptr++] = offs + 1;
            Slice[ptr++] = offs + 2;
            Slice[ptr++] = offs + 3;
            Slice[ptr++] = offs + 1;
        }
    }
}

} // namespace Sketch
