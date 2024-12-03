using CoreTrigger.ClassFiles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace CoreTrigger.Extensions
{
    public static class TaskName
    {
        private static readonly Dictionary<WeakReference<Task>, String> taskNames = new Dictionary<WeakReference<Task>, String>();

        public static void Name(this Task pTask, String name)
        {
            if (pTask == null)
            {
                return;
            }
            var weakReference = ContainsTask(pTask) ?? new WeakReference<Task>(pTask);
            taskNames[weakReference] = name;
        }

        public static object Name(this Task task)
        {
            var weakReference = ContainsTask(task);
            if (weakReference == null) return null;
            return taskNames[weakReference];
        }

        private static WeakReference<Task> ContainsTask(Task task)
        {
            foreach (var kvp in taskNames.ToList())
            {
                var weakReference = kvp.Key;

                Task taskFromReference;
                if (!weakReference.TryGetTarget(out taskFromReference))
                {
                    taskNames.Remove(weakReference); //Keep the dictionary clean.
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
