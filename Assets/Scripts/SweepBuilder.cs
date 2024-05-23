using UnityEngine;
using Unity.Collections;
using Unity.Mathematics;

namespace Sketch {

[ExecuteInEditMode]
public sealed class SweepBuilder : MonoBehaviour, IMeshBuilder
{
    public int InstanceCount => 8;
    public int VertexPerInstance => 4;
    public int IndexPerInstance => 6;
    public Bounds BoundingBox => new Bounds(Vector3.zero, Vector3.one * 100);

    public void WriteVertexArray
      (int instanceIndex, NativeSlice<Vertex> array)
    {
        array[0] = new Vertex(math.float3(instanceIndex + 0.0f, 0, 0), math.float3(0, 0, -1));
        array[1] = new Vertex(math.float3(instanceIndex + 0.5f, 0, 0), math.float3(0, 0, -1));
        array[2] = new Vertex(math.float3(instanceIndex + 0.0f, 1, 0), math.float3(0, 0, -1));
        array[3] = new Vertex(math.float3(instanceIndex + 0.5f, 1, 0), math.float3(0, 0, -1));
    }

    public void WriteIndexArray
      (int instanceIndex, NativeSlice<uint> array, uint indexOffset)
    {
        array[0] = indexOffset + 0;
        array[1] = indexOffset + 2;
        array[2] = indexOffset + 1;
        array[3] = indexOffset + 2;
        array[4] = indexOffset + 3;
        array[5] = indexOffset + 1;
    }
}

} // namespace Sketch
