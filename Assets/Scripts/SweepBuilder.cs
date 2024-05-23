using UnityEngine;
using Unity.Collections;
using Unity.Mathematics;
using Random = Unity.Mathematics.Random;

namespace Sketch {

[ExecuteInEditMode]
public sealed class SweepBuilder : MonoBehaviour, IMeshBuilder
{
    public int InstanceCount => 512;
    public int VertexPerInstance => (Subdivision + 1) * 2;
    public int IndexPerInstance => Subdivision * 6;
    public Bounds BoundingBox => new Bounds(Vector3.zero, Vector3.one * 100);

    int Subdivision = 64;
    float Radius = 0.5f;
    uint Seed = 0xdeadbeefu;

    public void WriteVertexArray
      (int instanceIndex, NativeSlice<Vertex> array)
    {
        var rand = Random.CreateFromIndex((uint)instanceIndex + Seed);
        rand.NextUInt();

        var r1 = Radius * rand.NextFloat();
        var r2 = Radius * rand.NextFloat(0.1f) + r1;

        var theta = rand.NextFloat(math.PI * 2);
        var width = rand.NextFloat(0.4f, 1.0f);
        var z = rand.NextFloat(-0.2f, 0.2f);

        var normal = math.float3(0, 0, -1);

        var ptr = 0;
        for (var i = 0; i <= Subdivision; i++)
        {
            var t = theta + i * width / Subdivision;
            var x = math.cos(t);
            var y = math.sin(t);
            var p1 = math.float3(x * r1, y * r1, z);
            var p2 = math.float3(x * r2, y * r2, z);
            array[ptr++] = new Vertex(p1, normal);
            array[ptr++] = new Vertex(p2, normal);
        }
    }

    public void WriteIndexArray
      (int instanceIndex, NativeSlice<uint> array, uint indexOffset)
    {
        var ptr = 0;
        for (var i = 0u; i < Subdivision; i++)
        {
            var offs = indexOffset + i * 2;
            array[ptr++] = offs + 0;
            array[ptr++] = offs + 2;
            array[ptr++] = offs + 1;
            array[ptr++] = offs + 2;
            array[ptr++] = offs + 3;
            array[ptr++] = offs + 1;
        }
    }
}

} // namespace Sketch
