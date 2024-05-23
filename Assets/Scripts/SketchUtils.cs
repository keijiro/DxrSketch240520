using UnityEngine;
using Unity.Collections;

namespace Sketch {

public static class SketchUtils
{
    public static T GetLiveComponent<T>(this MonoBehaviour self) where T : class
    {
        var c = self.GetComponent<T>();
        return c != null ? c : null;
    }

    public static NativeArray<T> NewTempJobArray<T>(int count) where T : struct
      => new NativeArray<T>(count, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
}

} // namespace Sketch
