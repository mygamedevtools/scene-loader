using System.Collections;
using System.Threading.Tasks;

namespace MyGameDevTools.SceneLoading
{
    public readonly struct WaitTask : IEnumerator
    {
        readonly Task _task;

        public object Current => null;
        public bool IsTaskCanceled => _task.IsCanceled;

        public WaitTask(Task task)
        {
            _task = task;
        }

        public bool MoveNext()
        {
            if (_task.IsFaulted)
                throw _task.Exception;

            return !_task.IsCompleted && !_task.IsCanceled;
        }

        public void Reset() { }
    }
}