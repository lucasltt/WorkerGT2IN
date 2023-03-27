using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using WorkerGT2IN.Entities;
using WorkerGT2IN.Services;

namespace WorkerGT2IN.Controller
{
    public class LoggerController
    {

        private readonly ILogger<Worker> _logger;
        private readonly GTechDataService _gtechDataService;
        public int Grupo { get; set; } = default;
        public int Passo { get; set; } = default;

        public LoggerController(ILogger<Worker> logger, GTechDataService gtechDataService)
        {
            _logger = logger;
            _gtechDataService = gtechDataService;
        }




        public async Task LogInformation(string message)
        {
            _logger.LogInformation(message);
            await _gtechDataService?.InsertLogAsync(Grupo, Passo, message, Entities.TipoLogEnum.Informacao);

        }

        public async Task LogError(string message)
        {
            _logger.LogError(message);
            await _gtechDataService?.InsertLogAsync(Grupo, Passo, message, Entities.TipoLogEnum.Erro);


        }


        public async Task LogDebug(string message)
        {
            _logger.LogDebug(message);
            await _gtechDataService?.InsertLogAsync(Grupo, Passo, message, Entities.TipoLogEnum.Debug);
        }


        public async Task LogAlert(string message)
        {
            _logger.LogWarning(message);
            await _gtechDataService?.InsertLogAsync(Grupo, Passo, message, Entities.TipoLogEnum.Alerta);

        }

        public async Task LogPasso(string message, StatusPassoEnum status)
        {
            await _gtechDataService?.InsertPassoAsync(Grupo, Passo, message, status);
        }

        public async Task LogFinalMigracao(string message, StatusMigracaoEnum status)
        {
            await _gtechDataService?.InsertPassoAsync(Grupo, 95, message, status);
        }

    }
}
