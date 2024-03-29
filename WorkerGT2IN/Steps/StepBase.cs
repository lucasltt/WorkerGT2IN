﻿using Microsoft.Extensions.Logging;
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

        public Func<Task<bool>> ValidateResults { get; set; } =  delegate () { return Task.FromResult(true); };


        /// <summary>
        /// Executado no principio do metodo RunStepAsync;
        /// Utilizado para remover algum arquivo ou limpar algum registro
        /// </summary>
        public Func<Task> PreFlight { get; set; } = delegate () { return Task.CompletedTask; };



        public async Task RunStepAsync()
        {
            stopWatch = new Stopwatch();
            stopWatch.Start();

            Logger.Passo = StepNumber;
            await Logger.LogInformation($"Início do Passo {StepNumber} - {StepName}");
            try
            {
                if (IsStepEnabled())
                {
                    await Logger.LogPasso(StepName, StatusPassoEnum.Executando);
                    await PreFlight();
                    await ExecuteStep();
                }
                else
                {
                    await Logger.LogInformation($"Passo {StepNumber} Desativado");
                    await Logger.LogPasso(StepName, StatusPassoEnum.Desativado);
                }

            }
            catch(Exception ex)
            {
                await Logger.LogError($"Erro Fatal no Passo {StepNumber}: {ex.Message}");

                stopWatch.Stop();
                await Logger.LogInformation($"Término do Passo {StepNumber} - Duração: {stopWatch.Elapsed}");
                await Logger.LogPasso(StepName, StatusPassoEnum.Abortado);

                throw new StepBaseExecutionException(ex.Message);
            }

            bool stepResult = true;
            if (IsStepEnabled())
            {
                await Logger.LogInformation($"Executando Validação do passo {StepNumber}");

                try
                {
                    stepResult = await ValidateResults();
                }
                catch
                {
                    stepResult = false;
                }

                if (stepResult)
                    await Logger.LogInformation($"Passo {StepNumber} Validado");
                else
                    await Logger.LogError($"Erro na validação do passo {StepNumber}!");
            }

            stopWatch.Stop();
            await Logger.LogInformation($"Término do Passo {StepNumber} - Duração: {stopWatch.Elapsed}");
            await Logger.LogPasso(StepName, StatusPassoEnum.Finalizado);
            
            if(stepResult == false)
                throw new StepBaseValidationException();

        }


    }
}
