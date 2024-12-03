using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace CoreTrigger.Extensions
{
    public static class TaskDuration
    {
        private static readonly Dictionary<WeakReference<Task>, Stopwatch> taskDurations = new Dictionary<WeakReference<Task>, Stopwatch>();

        public static void StartTime(this Task pTask)
        {
            if (pTask == null)
            {
                return;
            }
            var weakReference = ContainsTask(pTask) ?? new WeakReference<Task>(pTask);
            taskDurations[weakReference] = new Stopwatch();
            taskDurations[weakReference].Start();
        }

        public static TimeSpan? Duration(this Task task)
        {
            var weakReference = ContainsTask(task);
            if (weakReference == null) return null;
            return taskDurations[weakReference].Elapsed;
        }

        private static WeakReference<Task> ContainsTask(Task task)
        {
            foreach (var kvp in taskDurations.ToList())
            {
                var weakReference = kvp.Key;

                Task taskFromReference;
                if (!weakReference.TryGetTarget(out taskFromReference))
                {
                    taskDurations.Remove(weakReference); //Keep the dictionary clean.
                    continue;
                }

                if (task == taskFromReference)
                {
                    return weakReference;
                }
            }
            return null;
        }
    }
}
