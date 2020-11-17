using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Kugar.Core.Web.Services
{
    public interface IBackgroundTaskQueue
    {
        /// <summary>
        /// 将一个任务入队
        /// </summary>
        /// <param name="workItem"></param>
        void QueueBackgroundWorkItem(QueueHosedInvoker workItem);

        Task<QueueHosedInvoker> DequeueAsync(
            CancellationToken cancellationToken);
    }

    /// <summary>
    /// 用于后端的任务队列
    /// </summary>
    public class BackgroundTaskQueue : IBackgroundTaskQueue
    {
        private ConcurrentQueue<QueueHosedInvoker> _workItems =
            new ConcurrentQueue<QueueHosedInvoker>();
        private SemaphoreSlim _signal = new SemaphoreSlim(0);

        public void QueueBackgroundWorkItem(
            QueueHosedInvoker workItem)
        {
            if (workItem == null)
            {
                throw new ArgumentNullException(nameof(workItem));
            }

            _workItems.Enqueue(workItem);
            _signal.Release();
        }

        public async Task<QueueHosedInvoker> DequeueAsync(
            CancellationToken cancellationToken)
        {
            await _signal.WaitAsync(cancellationToken);
            _workItems.TryDequeue(out var workItem);

            return workItem;
        }
    }

    public delegate Task QueueHosedInvoker(IServiceProvider provider, CancellationToken cancellationToken);

    /// <summary>
    /// 在后台中,使用队列的方式执行任务
    /// </summary>
    public class QueuedHostedService : BackgroundService
    {
        private readonly ILogger _logger;
        private IServiceProvider _provider;
        private IBackgroundTaskQueue _taskQueue;

        public QueuedHostedService(IBackgroundTaskQueue taskQueue, IServiceProvider provider,
            ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory?.CreateLogger<QueuedHostedService>();
            _provider = provider;
            _taskQueue = taskQueue;
        }

        public IBackgroundTaskQueue TaskQueue => _taskQueue;

        protected override async Task ExecuteAsync(
            CancellationToken cancellationToken)
        {
            _logger?.LogInformation("任务队列服务开始启动");

            while (!cancellationToken.IsCancellationRequested)
            {
                var workItem = await _taskQueue.DequeueAsync(cancellationToken);

                if (workItem == null)
                {
                    continue;
                }
                using (var scope = _provider.CreateScope())
                {
                    try
                    {

                        await workItem(scope.ServiceProvider, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex,
                            $"出发任务项目错误:{workItem.GetType().FullName}");
                    }
                }
            }

            _logger?.LogInformation("队列暂停中");
        }
    }

    public static class QueuedHostedServiceGlobalExt
    {
        /// <summary>
        /// 注入一个后台队列任务管理器,在需要使用的地方,注入IBackgroundTaskQueue类即可
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection UseQueuedTaskService(this IServiceCollection services)
        {
            services.AddHostedService<QueuedHostedService>();
            services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();

            return services;
        }
    }
}
