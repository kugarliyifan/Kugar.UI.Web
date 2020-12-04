using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Kugar.Core.ExtMethod;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NCrontab;

namespace Kugar.Core.Web.Services
{
    /// <summary>
    /// 一个简答模式的计划任务<br/>用于在一些已知的计划时间执行某些任务的情况下使用,Cron属性在服务启动后,变无法修改,如需配置运行时可修改,请使用Hangfire之类的其他第三方框架
    /// </summary>
    public abstract class SimpleScheduledTaskService : BackgroundService
    {
        private IServiceProvider _provider = null;
        private CrontabSchedule _crontab = null;
        private string _cron;
        private bool _enabled=true;
        private bool _isInited = false;

        protected SimpleScheduledTaskService(IServiceProvider provider)
        {
            _provider = provider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {


            try
            {
                _crontab = CrontabSchedule.Parse(_cron);
            }
            catch (Exception e)
            {
                throw;
            }

            while (Enabled && _crontab != null)
            {

                var nextDt = _crontab.GetNextOccurrence(DateTime.Now.AddSeconds(2));

                var interval = (nextDt - DateTime.Now);

                await Task.Delay(interval, stoppingToken);

                var logger = (ILogger)_provider.GetService(typeof(ILogger));

                try
                {
                    logger.Log(LogLevel.Trace, $"启动计划任务:{this.GetType().Name}");

                    await Run(_provider, stoppingToken);

                    logger.Log(LogLevel.Trace, $"完成计划任务:{this.GetType().Name}");
                }
                catch (Exception e)
                {
                    logger.Log(LogLevel.Error, e, $"计划任务执行异常:{e.Message}");
                }
            }
        }

        protected abstract Task Run(IServiceProvider provider, CancellationToken stoppingToken);

        /// <summary>
        /// 计划任务的Cron配置字符串,可使用在线生成器生成后,填入
        /// </summary>
        public virtual string Cron
        {
            get => _cron;
        }

        /// <summary>
        /// 计划任务是否启动
        /// </summary>
        public virtual bool Enabled
        {
            set => _enabled = value;
            get => _enabled;
        }
    }
}
