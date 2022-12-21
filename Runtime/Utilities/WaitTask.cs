/**
 * WaitTask.cs
 * Created by: João Borks [joao.borks@gmail.com]
 * Created on: 2022-12-21
 */

using System.Collections;
using System.Threading.Tasks;

namespace MyGameDevTools.SceneLoading
{
    public class WaitTask : IEnumerator
    {
        readonly Task _task;

        public object Current => null;

        public WaitTask(Task task)
        {
            _task = task;
        }
        public WaitTask(ValueTask task)
        {
            _task = task.AsTask();
        }

        public bool MoveNext()
        {
            if (_task.IsFaulted)
                throw _task.Exception;

            return !_task.IsCompleted;
        }

        public void Reset() { }
    }
}