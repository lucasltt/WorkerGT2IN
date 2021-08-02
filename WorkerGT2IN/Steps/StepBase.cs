using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WorkerGT2IN.Controller;
using WorkerGT2IN.Entities;

namespace WorkerGT2IN.Steps
{
    public class StepBase
    {
        private Stopwatch stopWatch;
        public short StepNumber { get; set; }
        public string StepName { get; set; }

        public LoggerController Logger { get; set; }
        

        public MigrationConfig Configuration { get; set; }


        public Func<Task> ExecuteStep { get; set; }

        public Func<bool> IsStepEnabled { get; set; }


        public async Task RunStepAsync()
        {
            stopWatch = new Stopwatch();
            stopWatch.Start();

            await Logger.LogInformation($"Início do Passo {StepNumber}:\n{StepName}");
            try
            {
                if (IsStepEnabled())
                    await ExecuteStep();
                else
                    await Logger.LogInformation($"Passo {StepNumber}: Desativado");
            }
            catch(Exception ex)
            {
                await Logger.LogError($"Erro Fatal no Passo {StepNumber}:\n{ex.Message}");
            }

            stopWatch.Stop();
            await Logger.LogInformation($"Término do Passo {StepNumber}:\n{StepName}\nDuração do Passo: {stopWatch.Elapsed}");

        }


    }
}
