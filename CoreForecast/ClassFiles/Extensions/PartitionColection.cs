using System.Collections.Generic;
namespace CoreForecast.ClassFiles.Extensions
{
        static class PartitionColection
        {
                public static IEnumerable<IEnumerable<T>> Partition<T>(this IEnumerable<T> list, int size)
                {
                        List<T> partition = new List<T>(size);
                        foreach (var item in list)
                        {
                                partition.Add(item);
                                if (partition.Count == size)
                                {
                                        yield return partition;
                                        partition = new List<T>(size);
                                }
                        }
                        if (partition.Count > 0)
                                yield return partition;
                }
        }
}