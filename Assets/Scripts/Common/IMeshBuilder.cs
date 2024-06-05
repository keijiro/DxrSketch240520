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

public interface IFixedTopologyMeshBuilder
{
    public int VertexCount { get; }
    public int IndexCount { get; }
    public Bounds BoundingBox { get; }
    public JobHandle ScheduleVertexJob(NativeArray<Vertex> array);
    public JobHandle ScheduleIndexJob(NativeArray<uint> array);
}

} // namespace Sketch
