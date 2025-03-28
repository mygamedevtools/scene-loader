using System.Threading;
using System.Threading.Tasks;

namespace MyGameDevTools.SceneLoading
{
    public static class TaskExtensions
    {
        public static async Task<T> RunAndDisposeToken<T>(this Task<T> valueTask, CancellationTokenSource tokenSource)
        {
            try
            {
                return await valueTask;
            }
            finally
            {
                tokenSource.Dispose();
            }
        }

        public static WaitTask<T> ToWaitTask<T>(this Task<T> task)
        {
            return new WaitTask<T>(task);
        }
    }
}