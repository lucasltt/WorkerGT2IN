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

    }
}
