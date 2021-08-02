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

        private StepBase stepPublicarMetadados;
        private StepBase stepPublicarDados;
        private StepBase stepPublicarDGN;
        private StepBase stepCopiarDGN;
        private StepBase stepCongelarFila;
        private StepBase stepExecutarProceduresInservice;
        private StepBase stepExecutarProceduresGTech;
        private StepBase stepMigracaoOMS;
        private StepBase stepPararServicos;
        private StepBase stepDeletarArquivoNET;
        private StepBase stepUnirArquivoMAP;
        private StepBase stepCopiarArquivoMAP;
        private StepBase stepIniciarServicos;
        private StepBase stepCompilarObjetos;
        private StepBase stepIndicadores;
        private StepBase stepDescongelarFila;


        public Worker(ILogger<Worker> logger, IOptions<ServiceConfig> options)
        {
            _logger = logger;
            _options = options;

            gtechOracleDataService = new(_options.Value.GTechConnectionString);
            inServiceOracleDataService = new(_options.Value.InServiceConnectionString);
            telegramController = new(new(_options.Value.TelegramBotKey), _options.Value.GTechConnectionString);
            loggerController = new (_logger, telegramController);


            stepPublicarMetadados = new()
            {
                StepName = "Publicação de metadados",
                StepNumber = 1,
                Logger = loggerController,
                IsStepEnabled = delegate()
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


            stepPublicarDados = new()
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


            stepPublicarDGN = new()
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


            stepCopiarDGN = new()
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



            stepCongelarFila = new()
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
                        _logger.LogDebug($"[{ DateTime.Now}] Fila Não Congelou. Aguardando.");
                        await Task.Delay(migrationConfig.EsperaCongelarFilaSegundos * 1000);
                    }
                }
            };

           

            stepExecutarProceduresInservice = new()
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
                        try
                        {
                            await inServiceOracleDataService.RunCommand(procedure);
                        }
                        catch(Exception ex)
                        {
                            await loggerController.LogError($"[{DateTime.Now}] Erro: {ex.Message}");
                        }
                }
            };


            stepExecutarProceduresGTech = new()
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
                        try
                        {
                            await gtechOracleDataService.RunCommand(procedure);
                        }
                        catch (Exception ex)
                        {
                            await loggerController.LogError($"[{DateTime.Now}] Erro: {ex.Message}");
                        }
                }
            };




            stepMigracaoOMS = new()
            {
                StepName = "Migração OMS",
                StepNumber = 8,
                Logger = loggerController,
                IsStepEnabled = delegate ()
                {
                    return migrationConfig.ExecutarOMSMigration;
                },
                ExecuteStep = async delegate ()
                {
                    runExternalExecutableService = new(migrationConfig.CaminhoOMSMigration, migrationConfig.ArgumentoOMSMigration);
                    await runExternalExecutableService.RunProcessAsync();
                }
            };


            stepPararServicos = new()
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
                    ismProcessService = new (migrationConfig.CaminhoISMRequest);
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




            stepUnirArquivoMAP = new()
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




            stepCopiarArquivoMAP = new()
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
                    copyFileService = new(migrationConfig.CaminhoMapOrigem, migrationConfig.CaminhoMapDestino);
                    await copyFileService.CopyAsync();
                }
            };





            stepDeletarArquivoNET = new()
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




            stepIniciarServicos = new()
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


            stepCompilarObjetos = new()
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
                        try
                        {
                            await inServiceOracleDataService.RunCommand(procedure);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"Erro:\n{ex.Message}");
                        }
                }
            };

            stepIndicadores = new()
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
                        try
                        {
                            await inServiceOracleDataService.RunCommand(procedure);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"Erro:\n{ex.Message}");
                        }
                }
            };


            stepDescongelarFila = new()
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

        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Executando o serviço de migração G/Technology para Inservice");
            _logger.LogInformation($"Conexão GTech: {_options.Value.GTechConnectionString}");
            _logger.LogInformation($"Conexão Inservice: {_options.Value.InServiceConnectionString}");
            _logger.LogInformation($"Ambiente: {_options.Value.MachineDescription}");


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

                    await loggerController.LogInformation($"Executando ciclo às: {DateTime.Now} no ambiente: {_options.Value.MachineDescription}");

                    await stepPublicarMetadados.RunStepAsync();
                    await Task.Delay(5000);

                    await stepPublicarDados.RunStepAsync();
                    await Task.Delay(5000);

                    await stepPublicarDGN.RunStepAsync();
                    await Task.Delay(5000);

                    await stepCopiarDGN.RunStepAsync();
                    await Task.Delay(5000);

                    await stepCongelarFila.RunStepAsync();
                    await Task.Delay(5000);

                    await stepExecutarProceduresInservice.RunStepAsync();
                    await Task.Delay(5000);

                    await stepMigracaoOMS.RunStepAsync();
                    await Task.Delay(5000);

                    await stepPararServicos.RunStepAsync();
                    await Task.Delay(5000);


                    await stepUnirArquivoMAP.RunStepAsync();
                    await Task.Delay(5000);

                    await stepCopiarArquivoMAP.RunStepAsync();
                    await Task.Delay(5000);



                    await stepDeletarArquivoNET.RunStepAsync();
                    await Task.Delay(5000);

                    await stepIniciarServicos.RunStepAsync();
                    await Task.Delay(5000);

                    await stepCompilarObjetos.RunStepAsync();
                    await Task.Delay(5000);


                    await stepIndicadores.RunStepAsync();
                    await Task.Delay(5000);

                    await stepDescongelarFila.RunStepAsync();
                    await Task.Delay(5000);

                    await gtechOracleDataService.UpdateSingleMigrationConfig(nameof(MigrationConfig.ForcarPublicacao), "False");
                    await gtechOracleDataService.UpdateSingleMigrationConfig(nameof(MigrationConfig.PublicouHoje), "True");

                    stopWatch.Stop();

                    await loggerController.LogInformation($"Término do ciclo às: {DateTime.Now} no ambiente: {_options.Value.MachineDescription}\nDuração total: {stopWatch.Elapsed}");
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
