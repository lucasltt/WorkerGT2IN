using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;

namespace WorkerGT2IN.Controller
{
    public class LoggerController
    {

        private readonly ILogger<Worker> _logger;
        private readonly TelegramController _telegramController;

        public LoggerController(ILogger<Worker> logger, TelegramController telegramController)
        {
            _logger = logger;
            _telegramController = telegramController;
        }


        public async Task LogInformation(string message)
        {
            _logger.LogInformation(message);
            await _telegramController.SendInformationAsync(message);
        }

        public async Task LogError(string message)
        {
            _logger.LogError(message);
            await _telegramController.SendErrorAsync(message);
        }


        public async Task LogDebug(string message)
        {
            _logger.LogDebug(message);
            await _telegramController.SendDebugAsync(message);
        }


        public async Task LogAlert(string message)
        {
            _logger.LogWarning(message);
            await _telegramController.SendAlertAsync(message);
        }

    }
}
