﻿using System.Threading.Tasks;
using UnityEngine;

namespace AfterNow.PrezSDK
{
    internal class WaitForTask : CustomYieldInstruction
    {
        public override bool keepWaiting => !_task.IsCompleted;
        private readonly Task _task;
        internal WaitForTask(Task task)
        {
            _task = task;
        }
    }
}
