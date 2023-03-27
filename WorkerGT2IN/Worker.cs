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
using System.IO;

namespace WorkerGT2IN
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IOptions<ServiceConfig> _options;

        private readonly string Version = "1.5.0";

        private Stopwatch stopWatch;

       // private TelegramController telegramController;
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

        private StepBase stepRollback;
        private StepBase stepCopiarMapa;

        private List<StepBase> steps = new List<StepBase>();


        public Worker(ILogger<Worker> logger, IOptions<ServiceConfig> options)
        {
            _logger = logger;
            _options = options;

            gtechOracleDataService = new(_options.Value.GTechConnectionString, _options.Value.Ambiente);
            inServiceOracleDataService = new(_options.Value.InServiceConnectionString);
            loggerController = new(_logger, gtechOracleDataService);



            StepBase step;


            step = new()
            {
                StepName = "Validações Dados",
                StepNumber = 1,
                Logger = loggerController,
                IsStepEnabled = delegate ()
                {
                    return migrationConfig.ValidacoesGTech.Count > 0 ? true : false;
                },
                ExecuteStep = async delegate ()
                {
                    bool errorsFound = false;
                    foreach (string procedure in migrationConfig.ValidacoesGTech)
                    {
                        await loggerController.LogDebug($"Executando instrução: {procedure}");
                        await gtechOracleDataService.RunCommand(procedure);
                        await Task.Delay(1000);
                    }

                    if (errorsFound)
                        throw new Exception($"Foram identificadas validações que falharam!");

                },
                ValidateResults = async delegate ()
                {
                    bool isValid = true;

                    try
                    {
                        int registrosInvalidos = Convert.ToInt32(await gtechOracleDataService.RunExecuteScalarAsync("select count(1) from g2i_migra_rel_erros where prioridade = 1"));
                        if (registrosInvalidos > 0) isValid = false;


                    }
                    catch
                    {
                        return false;
                    }
                    return isValid;
                },
            };
            steps.Add(step);

            //stepPublicarMetadados
            step = new()
            {
                StepName = "Publicação de metadados",
                StepNumber = 2,
                Logger = loggerController,
                IsStepEnabled = delegate ()
                {
                    return migrationConfig.PublicarMetadados;
                },
                ExecuteStep = async delegate ()
                {
                    runExternalExecutableService = new(migrationConfig.CaminhoPublicadorMetadados, migrationConfig.ArgumentoPublicadorMetadados);
                    await runExternalExecutableService.RunProcessAsync();

                }
            };

            steps.Add(step);

            //stepPublicarDados
            step = new()
            {
                StepName = "Publicação de dados",
                StepNumber = 3,
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
                StepNumber = 4,
                Logger = loggerController,
                IsStepEnabled = delegate ()
                {
                    return migrationConfig.PublicarDGN;
                },
                PreFlight = async delegate ()
                {

                    try
                    {
                        deleteFileService = new(migrationConfig.PastaOrigemDGN, "*.dgn");
                        await deleteFileService.DeleteAsync();
                    }
                    catch { }

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
                StepNumber = 5,
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
                StepNumber = 6,
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

            //stepExecutarProceduresInservice
            step = new()
            {
                StepName = "Executar Procedures InService",
                StepNumber = 8,
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
                        await inServiceOracleDataService.RunExecuteNonQueryAsync(procedure);
                        await Task.Delay(1000);
                    }
                }
            };

            steps.Add(step);


            //stepMigracaoOMS
            step = new()
            {
                StepName = "Migração OMS",
                StepNumber = 9,
                Logger = loggerController,
                IsStepEnabled = delegate ()
                {
                    return migrationConfig.ExecutarOMSMigration;
                },
                PreFlight = async delegate ()
                {

                    try
                    {
                        FileInfo fileInfo = new FileInfo(migrationConfig.MapaOMSMigration);
                        await Task.Factory.StartNew(() => fileInfo.Delete());
                    }
                    catch { }

                },
                //ValidateResults = async delegate ()
                //{
                //    bool isValid = true;
                //    isValid = File.Exists(migrationConfig.MapaOMSMigration);
                //    return await Task.FromResult(isValid);;
                //},
                ExecuteStep = async delegate ()
                {
                    runExternalExecutableService = new(migrationConfig.CaminhoOMSMigration, migrationConfig.ArgumentoOMSMigration);
                    await runExternalExecutableService.RunProcessAsync();
                }
            };

            steps.Add(step);

            ////stepPararServicos
            //step = new()
            //{
            //    StepName = "Parar Serviços",
            //    StepNumber = 9,
            //    Logger = loggerController,
            //    IsStepEnabled = delegate ()
            //    {
            //        return migrationConfig.ServicosISM.Count > 0 && migrationConfig.PararServicos ? true : false;
            //    },
            //    ExecuteStep = async delegate ()
            //    {
            //        ismProcessService = new(migrationConfig.CaminhoISMRequest);
            //        foreach (string servico in migrationConfig.ServicosISM)
            //            try
            //            {
            //                await ismProcessService.ControlServiceAsync(servico, ISMProcessService.ISMServiceAction.Stop);
            //            }
            //            catch (Exception ex)
            //            {
            //                await loggerController.LogError($"[{DateTime.Now}] Erro: {ex.Message}");
            //            }
            //    }
            //};

            //steps.Add(step);

            //stepRenameLayers
            step = new()
            {
                StepName = "Renomear Layers .MAP",
                StepNumber = 10,
                Logger = loggerController,
                IsStepEnabled = delegate ()
                {
                    return migrationConfig.RenomearLayers;
                },
                ExecuteStep = async delegate ()
                {
                    runExternalExecutableService = new(migrationConfig.CaminhoRenomearLayers, migrationConfig.ArgumentoRenomearLayers);
                    await runExternalExecutableService.RunProcessAsync();
                }
            };

            steps.Add(step);

            //stepUnirArquivoMAP
            step = new()
            {
                StepName = "Unir Arquivos .MAP",
                StepNumber = 11,
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

            ////stepCopiarArquivoMAP
            //step = new()
            //{
            //    StepName = "Copiar Arquivo .MAP",
            //    StepNumber = 12,
            //    Logger = loggerController,
            //    IsStepEnabled = delegate ()
            //    {
            //        return migrationConfig.CopiarArquivoMAP;
            //    },
            //    ExecuteStep = async delegate ()
            //    {

            //        string map = await inServiceOracleDataService.GetCurrentMapAsync();

            //        string targetPath = map == "A" ? migrationConfig.CaminhoMapBDestino : migrationConfig.CaminhoMapADestino;
            //        await loggerController.LogDebug($"Mapa Atual: {map}\nDestino: {targetPath}");
            //        copyFileService = new(migrationConfig.CaminhoMapOrigem, targetPath);
            //        await copyFileService.CopyAsync();

            //        await inServiceOracleDataService.UpdateMapAsync();
            //    }
            //};

            //steps.Add(step);


            ////stepDeletarArquivoNET
            //step = new()
            //{
            //    StepName = "Deletar Arquivos .NET",
            //    StepNumber = 13,
            //    Logger = loggerController,
            //    IsStepEnabled = delegate ()
            //    {
            //        return migrationConfig.DeletarArquivoNET;
            //    },
            //    ExecuteStep = async delegate ()
            //    {
            //        deleteFileService = new(migrationConfig.CaminhoArquivoNET, "*.net");
            //        await deleteFileService.DeleteAsync();
            //    }
            //};

            //steps.Add(step);

            ////stepIniciarServicos
            //step = new()
            //{
            //    StepName = "Iniciar Serviços",
            //    StepNumber = 14,
            //    Logger = loggerController,
            //    IsStepEnabled = delegate ()
            //    {
            //        return migrationConfig.ServicosISM.Count > 0 && migrationConfig.IniciarServicos ? true : false;
            //    },
            //    ExecuteStep = async delegate ()
            //    {
            //        foreach (string servico in migrationConfig.ServicosISM)
            //            try
            //            {
            //                await ismProcessService.ControlServiceAsync(servico, ISMProcessService.ISMServiceAction.Start);
            //            }
            //            catch (Exception ex)
            //            {
            //                await loggerController.LogError($"[{DateTime.Now}] Erro: {ex.Message}");
            //            }
            //    }
            //};


            //steps.Add(step);

            //stepCompilarObjetos
            step = new()
            {
                StepName = "Compilar Objetos",
                StepNumber = 12,
                Logger = loggerController,
                IsStepEnabled = delegate ()
                {
                    return migrationConfig.Compilar.Count > 0 ? true : false;
                },
                ValidateResults = async delegate ()
                {
                    bool isValid = true;

                    try
                    {
                        int registrosInvalidos = Convert.ToInt32(await inServiceOracleDataService.RunExecuteScalarAsync("select count(1) from all_objects where status = 'INVALID' and owner = 'INDICADORES' and object_type != 'SYNONYM'"));
                        if (registrosInvalidos > 0) isValid = false;

                        isValid = true;

                    }
                    catch
                    {
                        return false;
                    }
                    return isValid;
                },
                ExecuteStep = async delegate ()
                {
                    foreach (string procedure in migrationConfig.Compilar)
                    {
                        await loggerController.LogDebug($"Executando instrução: {procedure}");
                        await inServiceOracleDataService.RunExecuteNonQueryAsync(procedure);
                        await Task.Delay(1000);
                    }
                }
            };


            steps.Add(step);

            //stepIndicadores
            step = new()
            {
                StepName = "Indicadores",
                StepNumber = 13,
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
                        await inServiceOracleDataService.RunExecuteNonQueryAsync(procedure);
                        await Task.Delay(1000);
                    }
                }
            };


            steps.Add(step);

            //stepValidacoesIndicadoresCriticas
            //step = new()
            //{
            //    StepName = "Validações Critícas Indicadores",
            //    StepNumber = 13,
            //    Logger = loggerController,
            //    IsStepEnabled = delegate ()
            //    {
            //        return migrationConfig.ValidacoesIndicadoresCritica.Count > 0 ? true : false;
            //    },
            //    ExecuteStep = async delegate ()
            //    {
            //        bool errorsFound = false;
            //        foreach ((string procedure, string info) in migrationConfig.ValidacoesIndicadoresCritica)
            //        {
            //            await loggerController.LogDebug($"Executando instrução: {info}");
            //            int valor = Convert.ToInt32(await inServiceOracleDataService.RunExecuteScalarAsync(procedure));
            //            if(valor > 0)
            //            {
            //                errorsFound = true;
            //                await loggerController.LogError($"Validação \"{info}\" falhou retornando {valor} resultados.");
            //            }
            //            await Task.Delay(1000);
            //        }

            //        if (errorsFound)
            //            throw new Exception($"Foram identificadas validações que falharam!");

            //    }
            //};
            //steps.Add(step);

            //stepDescongelarFila
            step = new()
            {
                StepName = "Descongelar Fila de Indicadores",
                StepNumber = 14,
                Logger = loggerController,
                IsStepEnabled = delegate ()
                {
                    return migrationConfig.ExecutarDescongelarFila;
                },
                ExecuteStep = async delegate ()
                {
                    await inServiceOracleDataService.RunExecuteNonQueryAsync(migrationConfig.ComandoDescongelarFila);
                    await Task.Delay(migrationConfig.EsperaDescongelarFilaSegundos * 100);
                }
            };

            steps.Add(step);


            //stepRollback
            stepRollback = new()
            {
                StepName = "Rollback",
                StepNumber = -1,
                Logger = loggerController,
                IsStepEnabled = delegate ()
                {
                    return migrationConfig.Rollback.Count > 0 ? true : false;
                },
                ExecuteStep = async delegate ()
                {
                    foreach (string procedure in migrationConfig.Rollback)
                    {
                        await loggerController.LogDebug($"Executando instrução: {procedure}");
                        await inServiceOracleDataService.RunExecuteNonQueryAsync(procedure);
                        await Task.Delay(1000);
                    }

                 

                    await Task.Delay(1000);
                }
            };


            //stepCopiarMapa
            stepCopiarMapa = new()
            {
                StepName = "Copiar Mapa Inservice",
                StepNumber = 99,
                Logger = loggerController,
                IsStepEnabled = delegate ()
                {
                    return true;
                },
                ExecuteStep = async delegate ()
                {
                    runExternalExecutableService = new(migrationConfig.MapChangerPath, string.Empty);
                    int exitCode = await runExternalExecutableService.RunProcessAsync();
                    await loggerController.LogDebug($"Resultado do MapChanger: {exitCode}");
                    if(exitCode != 0 && exitCode != -31)
                        throw new Exception($"Falha no copia do mapa!");


                }
            };


        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            await loggerController.LogInformation("Executando o serviço de migração G/Technology para Inservice");
            await loggerController.LogInformation($"Conexão GTech: {_options.Value.GTechConnectionString}");
            await loggerController.LogInformation($"Conexão Inservice: {_options.Value.InServiceConnectionString}");
            await loggerController.LogInformation($"Ambiente: {_options.Value.MachineDescription}");
            await loggerController.LogInformation($"Versão: {Version}");
            


            while (!stoppingToken.IsCancellationRequested)
            {


                try
                {
                    migrationConfig = await gtechOracleDataService.ReadMigrationConfig();
                }
                catch (Exception ex)
                {
                    await loggerController.LogError(ex.Message);
                }

                await gtechOracleDataService.UpdateSingleMigrationConfig(nameof(MigrationConfig.LastEpoch), (DateTime.Now - epoch).TotalSeconds.ToString());
                await gtechOracleDataService.UpdateSingleMigrationConfig(nameof(MigrationConfig.VersaoServidor), Version);

                if (migrationConfig.ForcarCopiaMapa)
                {
                    stopWatch = new Stopwatch();
                    stopWatch.Start();

                    loggerController.Grupo = await gtechOracleDataService.GetActualGroupIdAsync();

                    await loggerController.LogInformation($"Copiando mapa as {DateTime.Now} no ambiente: {_options.Value.MachineDescription}");

                    try
                    {
                        await stepCopiarMapa.RunStepAsync();
                    }
                    catch (Exception ex) 
                    {
                    }
                    await gtechOracleDataService.UpdateSingleMigrationConfig(nameof(MigrationConfig.ForcarCopiaMapa), "False");

                    stopWatch.Stop();

                    await loggerController.LogInformation($"Término da copia do mapa às: {DateTime.Now} com ruração total: {stopWatch.Elapsed}");

                }



                if (migrationConfig.ForcarPublicacao)
                { 
                    stopWatch = new Stopwatch();
                    stopWatch.Start();

                    await loggerController.LogInformation($"Executando ciclo às: {DateTime.Now} no ambiente: {_options.Value.MachineDescription}");

                    int nextStep = 1;

                    int grupo = await gtechOracleDataService.GenerateNextGroupIdAsync();
                    loggerController.Grupo = grupo;

                    StatusMigracaoEnum statusMigracao = StatusMigracaoEnum.Finalizada;
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
                                switch (step.StepNumber)
                                {
                                    case <= 5:
                                        await loggerController.LogAlert($"Como o ocorreu um erro no passo {step.StepNumber} o processo será Cancelado!");
                                        statusMigracao = StatusMigracaoEnum.Cancelada;
                                        nextStep = 20;
                                        break;
                                    case >= 6 and <= 8:
                                        await loggerController.LogAlert($"Como o ocorreu um erro no passo {step.StepNumber} o processo passará para o passo 14!");
                                        statusMigracao = StatusMigracaoEnum.Cancelada;
                                        //descongela
                                        nextStep = 14;
                                        break;
                                    case >= 9 and <= 12:
                                        await loggerController.LogAlert($"Como o ocorreu um erro no passo {step.StepNumber} será realizado o RollBack");
                                        try
                                        {
                                            await stepRollback.RunStepAsync();
                                            await Task.Delay(4000);
                                        }
                                        catch { }
                                        statusMigracao = StatusMigracaoEnum.Anulada;

                                        //descongela
                                        nextStep = 14;
                                        break;
                                    case >= 13 and <= 14:
                                        await loggerController.LogAlert($"Como o ocorreu um erro no passo {step.StepNumber} o processo será abortado e exije intervençao manual!");
                                        statusMigracao = StatusMigracaoEnum.Abortada;
                                        nextStep = 20;
                                        break;

                                }


                            }


                        }
                    }

                    await loggerController.LogFinalMigracao("Migração encerrada", statusMigracao);
                    await gtechOracleDataService.UpdateSingleMigrationConfig(nameof(MigrationConfig.ForcarPublicacao), "False");
                    await gtechOracleDataService.UpdateSingleMigrationConfig(nameof(MigrationConfig.PublicouHoje), "True");

                    stopWatch.Stop();

                    await loggerController.LogInformation($"Término do ciclo às: {DateTime.Now} no ambiente: {_options.Value.MachineDescription} com Duração total: {stopWatch.Elapsed}");
                }

                await Task.Delay(_options.Value.TempoEspera, stoppingToken);
            }

            //telegramController.StopReceiving();
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
