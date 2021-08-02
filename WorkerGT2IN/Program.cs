using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.EventLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WorkerGT2IN.Entities;

namespace WorkerGT2IN
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseWindowsService(option =>
                {
                    option.ServiceName = "Migração G2I";
                })
                .ConfigureLogging(logging =>
                {
                    logging.AddEventLog(new EventLogSettings()
                    {
                        SourceName = "Migração G2I",
                        LogName = "Migração G2I"
                    });
                })
                .ConfigureServices((hostContext, services) =>
                {
                    IConfiguration configuration = hostContext.Configuration;
                    services.Configure<ServiceConfig>(configuration.GetSection(nameof(ServiceConfig)));

                    services.AddHostedService<Worker>();
                });
    }
}
