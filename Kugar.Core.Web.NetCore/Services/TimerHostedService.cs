using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Kugar.Core.Web.Services
{
    /// <summary>
    /// 用于在后台执行一个定时任务,用于取代TimeEx,在asp.net core的环境下使用,继承该类后,使用 services.AddHostedService&lt;当前类类型&gt;();后,自动在后台启动当前定时任务
    /// </summary>
    public abstract class TimerHostedService : BackgroundService
    {
        private IServiceProvider _provider = null;

        protected TimerHostedService(IServiceProvider provider)
        {
            _provider = provider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (Enabled && Internal>0)
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    await Task.Delay(Internal, stoppingToken);

                    if (!stoppingToken.IsCancellationRequested)
                    {
                        using (var scope = _provider.CreateScope())
                        {
                            try
                            {
                                await Run(scope.ServiceProvider, stoppingToken);
                            }
                            catch (Exception e)
                            {

                            }
                        }
                    }

                    
                }
            }

            

            return;
        }

        /// <summary>
        /// 实际执行的定时器处理函数
        /// </summary>
        /// <param name="serviceScope">当次的Ioc容器,可获取当前程序中用于注入的容器内的类</param>
        /// <param name="stoppingToken">是否暂停</param>
        /// <returns></returns>
        protected abstract Task Run(IServiceProvider serviceProvider, CancellationToken stoppingToken);

        /// <summary>
        /// 定时器间隔触发时间,单位是ms
        /// </summary>
        protected abstract int Internal {  get; }

        /// <summary>
        /// 当前定时器是否启用,true为定时器有效,false为停用
        /// </summary>
        public virtual bool Enabled { set; get; } = true;
    }
}