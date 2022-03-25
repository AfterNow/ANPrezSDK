using System.Threading.Tasks;
using UnityEngine;

namespace AfterNow.PrezSDK
{
    public class WaitForTask : CustomYieldInstruction
    {
        public override bool keepWaiting => !_task.IsCompleted;
        private readonly Task _task;
        public WaitForTask(Task task)
        {
            _task = task;
        }
    }
}