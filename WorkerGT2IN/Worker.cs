using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Oracle.ManagedDataAccess.Client;
using Microsoft.Extensions.Options;
using WorkerGT2IN.Entities;
using WorkerGT2IN.Services;
using WorkerGT2IN.Steps;
using Telegram.Bot;
using Telegram.Bot.Args;
using WorkerGT2IN.Controller;
using System.Diagnostics;

namespace WorkerGT2IN
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IOptions<ServiceConfig> _options;

        private Stopwatch stopWatch;

        private TelegramController telegramController;
        private LoggerController loggerController;

        private MigrationConfig migrationConfig;
        private GTechDataService gtechOracleDataService;
        private InServiceDataService inServiceOracleDataService;
        private RunExternalExecutableService runExternalExecutableService;
        private CopyFolderService copyFolderService;
        private CopyFileService copyFileService;
        private DeleteFileService deleteFileService;
        private ISMProcessService ismProcessService;
        private CheckErrorOnFileService checkErrorOnFileService;

        //private StepBase stepPublicarMetadados;
        //private StepBase stepPublicarDados;
        //private StepBase stepPublicarDGN;
        //private StepBase stepCopiarDGN;
        //private StepBase stepCongelarFila;
        //private StepBase stepExecutarProceduresInservice;
        //private StepBase stepExecutarProceduresGTech;
        //private StepBase stepMigracaoOMS;
        //private StepBase stepPararServicos;
        //private StepBase stepDeletarArquivoNET;
        //private StepBase stepUnirArquivoMAP;
        //private StepBase stepCopiarArquivoMAP;
        //private StepBase stepIniciarServicos;
        //private StepBase stepCompilarObjetos;
        //private StepBase stepIndicadores;
        //private StepBase stepDescongelarFila;


        List<StepBase> steps = new List<StepBase>();


        public Worker(ILogger<Worker> logger, IOptions<ServiceConfig> options)
        {
            _logger = logger;
            _options = options;

            gtechOracleDataService = new(_options.Value.GTechConnectionString);
            inServiceOracleDataService = new(_options.Value.InServiceConnectionString);
            telegramController = new(new(_options.Value.TelegramBotKey), _options.Value.GTechConnectionString);
            loggerController = new(_logger, telegramController);



            StepBase step;

            //stepPublicarMetadados
            step = new()
            {
                StepName = "Publicação de metadados",
                StepNumber = 1,
                Logger = loggerController,
                IsStepEnabled = delegate ()
                {
                    return migrationConfig.PublicarMetadados;
                },
                ExecuteStep = async delegate ()
                {
                    runExternalExecutableService = new(migrationConfig.CaminhoPublicadorMetadados, migrationConfig.ArgumentoPublicadorMetadados);
                    await runExternalExecutableService.RunProcessAsync();
                    //await gtechOracleDataService.UpdateSingleConfig(nameof(MigrationConfig.PublicarMetadados), "False");

                }
            };

            steps.Add(step);

            //stepPublicarDados
            step = new()
            {
                StepName = "Publicação de dados",
                StepNumber = 2,
                Logger = loggerController,
                IsStepEnabled = delegate ()
                {
                    return migrationConfig.PublicarDados;
                },
                ExecuteStep = async delegate ()
                {
                    runExternalExecutableService = new(migrationConfig.CaminhoPublicadorDados, migrationConfig.ArgumentoPublicadorDados);
                    await runExternalExecutableService.RunProcessAsync();

                }
            };

            steps.Add(step);


            //stepPublicarDGN
            step = new()
            {
                StepName = "Publicação de DGN",
                StepNumber = 3,
                Logger = loggerController,
                IsStepEnabled = delegate ()
                {
                    return migrationConfig.PublicarDGN;
                },
                ExecuteStep = async delegate ()
                {
                    runExternalExecutableService = new(migrationConfig.CaminhoPublicadorDGN, migrationConfig.ArgumentoPublicadorDGN);
                    await runExternalExecutableService.RunProcessAsync();

                }
            };

            steps.Add(step);


            //stepCopiarDGN
            step = new()
            {
                StepName = "Cópia de DGN",
                StepNumber = 4,
                Logger = loggerController,
                IsStepEnabled = delegate ()
                {
                    return migrationConfig.CopiarDGN;
                },
                ExecuteStep = async delegate ()
                {
                    copyFolderService = new(migrationConfig.PastaOrigemDGN, migrationConfig.PastaDestinoDGN, "*.dgn");
                    await copyFolderService.CopyAsync();
                }
            };

            steps.Add(step);

            //stepCongelarFila
            step = new()
            {
                StepName = "Congelar Fila de Indicadores",
                StepNumber = 5,
                Logger = loggerController,
                IsStepEnabled = delegate ()
                {
                    return migrationConfig.ExecutarCongelarFila;
                },
                ExecuteStep = async delegate ()
                {
                    while (await inServiceOracleDataService.CongelarFilaAsync(migrationConfig.ComandoCongelarFila) == false)
                    {
                        await loggerController.LogDebug($"Fila Não Congelou. Aguardando.");
                        await Task.Delay(migrationConfig.EsperaCongelarFilaSegundos * 1000);
                    }
                }
            };

            steps.Add(step);


            //stepExecutarProceduresInservice
            step = new()
            {
                StepName = "Executar Procedures InService",
                StepNumber = 6,
                Logger = loggerController,
                IsStepEnabled = delegate ()
                {
                    return migrationConfig.ProceduresInservice.Count > 0 ? true : false;
                },
                ExecuteStep = async delegate ()
                {
                    foreach (string procedure in migrationConfig.ProceduresInservice)
                    {
                        await loggerController.LogDebug($"Executando instrução: {procedure}");
                        await inServiceOracleDataService.RunCommand(procedure);
                        await Task.Delay(1000);
                    }
                }
            };

            steps.Add(step);


            //ExecutarProceduresGTech
            step = new()
            {
                StepName = "Executar Procedures GTech",
                StepNumber = 7,
                Logger = loggerController,
                IsStepEnabled = delegate ()
                {
                    return migrationConfig.ProceduresGTech.Count > 0 ? true : false;
                },
                ExecuteStep = async delegate ()
                {
                    foreach (string procedure in migrationConfig.ProceduresGTech)
                    {
                        await loggerController.LogDebug($"Executando instrução: {procedure}");
                        await gtechOracleDataService.RunCommand(procedure);
                        await Task.Delay(1000);
                    }
                }
            };

            steps.Add(step);


            //stepMigracaoOMS
            step = new()
            {
                StepName = "Migração OMS",
                StepNumber = 8,
                Logger = loggerController,
                IsStepEnabled = delegate ()
                {
                    return migrationConfig.ExecutarOMSMigration;
                },
                //PreFlight = async delegate ()
                //{
                //    deleteFileService = new DeleteFileService(migrationConfig.LogsOMSMigration, "*.log");
                //    await deleteFileService.DeleteAsync();
                //},
                //ValidateResults = async delegate()
                //{
                //    bool isValid = true;

                //    try
                //    {
                //        foreach (string file in System.IO.Directory.GetFiles(migrationConfig.LogsOMSMigration, "*.log"))
                //        {
                //            checkErrorOnFileService = new(file);
                //            if (await checkErrorOnFileService.ErrorExistsOnFileAsync())
                //                isValid = false;
                //        }

                //    }
                //    catch
                //    {
                //        return false;
                //    }
                //    return isValid;
                //},
                ExecuteStep = async delegate ()
                {
                    runExternalExecutableService = new(migrationConfig.CaminhoOMSMigration, migrationConfig.ArgumentoOMSMigration);
                    await runExternalExecutableService.RunProcessAsync();
                }
            };

            steps.Add(step);

            //stepPararServicos
            step = new()
            {
                StepName = "Parar Serviços",
                StepNumber = 9,
                Logger = loggerController,
                IsStepEnabled = delegate ()
                {
                    return migrationConfig.ServicosISM.Count > 0 && migrationConfig.PararServicos ? true : false;
                },
                ExecuteStep = async delegate ()
                {
                    ismProcessService = new(migrationConfig.CaminhoISMRequest);
                    foreach (string servico in migrationConfig.ServicosISM)
                        try
                        {
                            await ismProcessService.ControlServiceAsync(servico, ISMProcessService.ISMServiceAction.Stop);
                        }
                        catch (Exception ex)
                        {
                            await loggerController.LogError($"[{DateTime.Now}] Erro: {ex.Message}");
                        }
                }
            };

            steps.Add(step);


            //stepUnirArquivoMAP
            step = new()
            {
                StepName = "Unir Arquivos .MAP",
                StepNumber = 10,
                Logger = loggerController,
                IsStepEnabled = delegate ()
                {
                    return migrationConfig.ExecutarMergeMaps;
                },
                ExecuteStep = async delegate ()
                {
                    runExternalExecutableService = new(migrationConfig.CaminhoMergeMaps, migrationConfig.ArgumentoMergeMaps);
                    await runExternalExecutableService.RunProcessAsync();
                }
            };


            steps.Add(step);

            //stepCopiarArquivoMAP
            step = new()
            {
                StepName = "Copiar Arquivo .MAP",
                StepNumber = 11,
                Logger = loggerController,
                IsStepEnabled = delegate ()
                {
                    return migrationConfig.CopiarArquivoMAP;
                },
                ExecuteStep = async delegate ()
                {
                   
                    string map = await inServiceOracleDataService.GetCurrentMapAsync();

                    string targetPath = map == "A" ? migrationConfig.CaminhoMapBDestino : migrationConfig.CaminhoMapADestino;
                    await loggerController.LogDebug($"Mapa Atual: {map}\nDestino: {targetPath}");
                    copyFileService = new(migrationConfig.CaminhoMapOrigem, targetPath);
                    await copyFileService.CopyAsync();

                    await inServiceOracleDataService.UpdateMapAsync();
                }
            };

            steps.Add(step);


            //stepDeletarArquivoNET
            step = new()
            {
                StepName = "Deletar Arquivos .NET",
                StepNumber = 12,
                Logger = loggerController,
                IsStepEnabled = delegate ()
                {
                    return migrationConfig.DeletarArquivoNET;
                },
                ExecuteStep = async delegate ()
                {
                    deleteFileService = new(migrationConfig.CaminhoArquivoNET, "*.net");
                    await deleteFileService.DeleteAsync();
                }
            };

            steps.Add(step);

            //stepIniciarServicos
            step = new()
            {
                StepName = "Iniciar Serviços",
                StepNumber = 13,
                Logger = loggerController,
                IsStepEnabled = delegate ()
                {
                    return migrationConfig.ServicosISM.Count > 0 && migrationConfig.IniciarServicos ? true : false;
                },
                ExecuteStep = async delegate ()
                {
                    foreach (string servico in migrationConfig.ServicosISM)
                        try
                        {
                            await ismProcessService.ControlServiceAsync(servico, ISMProcessService.ISMServiceAction.Start);
                        }
                        catch (Exception ex)
                        {
                            await loggerController.LogError($"[{DateTime.Now}] Erro: {ex.Message}");
                        }
                }
            };


            steps.Add(step);

            //stepCompilarObjetos
            step = new()
            {
                StepName = "Compilar Objetos",
                StepNumber = 14,
                Logger = loggerController,
                IsStepEnabled = delegate ()
                {
                    return migrationConfig.Compilar.Count > 0 ? true : false;
                },
                ExecuteStep = async delegate ()
                {
                    foreach (string procedure in migrationConfig.Compilar)
                    {
                        await loggerController.LogDebug($"Executando instrução: {procedure}");
                        await inServiceOracleDataService.RunCommand(procedure);
                        await Task.Delay(1000);
                    }
                }
            };


            steps.Add(step);

            //stepIndicadores
            step = new()
            {
                StepName = "Indicadores",
                StepNumber = 15,
                Logger = loggerController,
                IsStepEnabled = delegate ()
                {
                    return migrationConfig.Indicadores.Count > 0 ? true : false;
                },
                ExecuteStep = async delegate ()
                {
                    foreach (string procedure in migrationConfig.Indicadores)
                    {
                        await loggerController.LogDebug($"Executando instrução: {procedure}");
                        await inServiceOracleDataService.RunCommand(procedure);
                        await Task.Delay(1000);
                    }
                }
            };


            steps.Add(step);


            //stepDescongelarFila
            step = new()
            {
                StepName = "Descongelar Fila de Indicadores",
                StepNumber = 16,
                Logger = loggerController,
                IsStepEnabled = delegate ()
                {
                    return migrationConfig.ExecutarDescongelarFila;
                },
                ExecuteStep = async delegate ()
                {
                    await inServiceOracleDataService.RunCommand(migrationConfig.ComandoDescongelarFila);
                    await Task.Delay(migrationConfig.EsperaDescongelarFilaSegundos * 100);
                }
            };

            steps.Add(step);

        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Executando o serviço de migração G/Technology para Inservice");
            _logger.LogInformation($"Conexão GTech: {_options.Value.GTechConnectionString}");
            _logger.LogInformation($"Conexão Inservice: {_options.Value.InServiceConnectionString}");
            _logger.LogInformation($"Ambiente: {_options.Value.MachineDescription}");
            _logger.LogInformation($"Versão: 1.2.1");



            telegramController.StartReceiving();


            while (!stoppingToken.IsCancellationRequested)
            {
               // _logger.LogInformation("Executando ciclo às: {time}", DateTimeOffset.Now);

                telegramController.TelegramSubscriptions = await telegramController.ReadTelegramConfigAsync();

                try
                {
                    migrationConfig = await gtechOracleDataService.ReadMigrationConfig();
                }
                catch (Exception ex)
                {
                    await loggerController.LogError(ex.Message);
                }

                
                TimeSpan horaAgendada = TimeSpan.Parse(migrationConfig.HoraAgendamentoDiario);
                if (DateTime.Now.TimeOfDay < horaAgendada)
                    await gtechOracleDataService.UpdateSingleMigrationConfig(nameof(MigrationConfig.PublicouHoje), "False");
               

                if ((migrationConfig.PublicouHoje == false && DateTime.Now.TimeOfDay >= horaAgendada && migrationConfig.AtivarAgendamento) || migrationConfig.ForcarPublicacao)
                {
                    stopWatch = new Stopwatch();
                    stopWatch.Start();

                    await loggerController.LogInformation($"▶️Executando ciclo às: {DateTime.Now} no ambiente: {_options.Value.MachineDescription}");

                    int nextStep = 1;
                    foreach (StepBase step in steps)
                    {
                        if (step.StepNumber == nextStep)
                        {
                            try
                            {
                                await step.RunStepAsync();
                                nextStep++;
                                await Task.Delay(4000);
                            }
                            catch
                            {
                                switch(step.StepNumber)
                                {
                                    case <= 5:
                                        await loggerController.LogAlert($"Como o ocorreu um erro no passo {step.StepNumber} o processo será abortado!");
                                        nextStep = 20;
                                        break;
                                    case >= 6 and <= 13:
                                        await loggerController.LogAlert($"Como o ocorreu um erro no passo {step.StepNumber} o processo passará para o passo 16!");
                                        nextStep = 16;
                                        break;
                                    case >= 14 and <= 16:
                                        await loggerController.LogAlert($"Como o ocorreu um erro no passo {step.StepNumber} o processo será abortado e exije intervençao manual!");
                                        nextStep = 20;
                                        break;

                                }


                            }

                           
                        }
                    }

                    await gtechOracleDataService.UpdateSingleMigrationConfig(nameof(MigrationConfig.ForcarPublicacao), "False");
                    await gtechOracleDataService.UpdateSingleMigrationConfig(nameof(MigrationConfig.PublicouHoje), "True");

                    stopWatch.Stop();

                    await loggerController.LogInformation($"🏁Término do ciclo às: {DateTime.Now} no ambiente: {_options.Value.MachineDescription}\nDuração total: {stopWatch.Elapsed}");
                }

                await Task.Delay(_options.Value.TempoEspera, stoppingToken);
            }

            telegramController.StopReceiving();
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Iniciando o serviço de migração G/Technology para Inservice no ambiente: {_options.Value.MachineDescription}");

            return base.StartAsync(cancellationToken);
        }


        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Parando o serviço de migração G/Technology para Inservice no ambiente: {_options.Value.MachineDescription}");


            return base.StopAsync(cancellationToken);
        }
    }
}
