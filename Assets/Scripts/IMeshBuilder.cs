using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Sketch {

public struct Vertex
{
    public float3 Position;
    public float3 Normal;

    public Vertex(float3 p, float3 n)
      => (Position, Normal) = (p, n);
}

public interface IMeshBuilder
{
    public int InstanceCount { get; }
    public int VertexPerInstance { get; }
    public int IndexPerInstance { get; }
    public Bounds BoundingBox { get; }
    public JobHandle ScheduleWriteVertexArrayJob(int instanceIndex, NativeSlice<Vertex> array);
    public JobHandle ScheduleWriteIndexArrayJob(int instanceIndex, NativeSlice<uint> array, uint indexOffset);
}

public static class IMeshBuilderExtensions
{
    public static int GetTotalVertexCount(this IMeshBuilder self)
      => self.InstanceCount * self.VertexPerInstance;

    public static int GetTotalIndexCount(this IMeshBuilder self)
      => self.InstanceCount * self.IndexPerInstance;
}

} // namespace Sketch
