using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using WorkerGT2IN.Entities;

namespace WorkerGT2IN.Controller
{
    public class TelegramController
    {
        private readonly TelegramBotClient _telegramBotClient;
        private readonly string _oracleConnectionString;

        private const string noCommandPermission = "Desculpe {0}, você não tem permissão para executar comandos";

        public List<TelegramConfig> TelegramSubscriptions { get; set; } = new List<TelegramConfig>();

        public TelegramController(TelegramBotClient telegramBotClient, string oracleConnectionString)
        {
            _telegramBotClient = telegramBotClient;
            _telegramBotClient.OnMessage += _telegramBotClient_OnMessage;
            _oracleConnectionString = oracleConnectionString;
        }

        private async void _telegramBotClient_OnMessage(object sender, Telegram.Bot.Args.MessageEventArgs e)
        {
            string inputMessage = e.Message.Text.ToUpper().Trim();


            string username = e.Message.Chat.FirstName;
            long chatid = e.Message.Chat.Id;
            string response = default(string);
            string command = default(string);


            command = inputMessage;

            response = command switch
            {
                "/FORCARINICIO" => await CommandForcarInicioAsync(username, chatid),
                "/ATIVARMETADADOS" => await CommandMetadados(username, chatid, true),
                "/DESATIVARMETADADOS" => await CommandMetadados(username, chatid, false),
                "/ATIVARDADOS" => await CommandDados(username, chatid, true),
                "/DESATIVARDADOS" => await CommandDados(username, chatid, false),
                "/ATIVARDGN" => await CommandDGN(username, chatid, true),
                "/DESATIVARDGN" => await CommandDGN(username, chatid, false),
                "/ATIVARCOPIADGN" => await CommandCopiarDGN(username, chatid, true),
                "/DESATIVARCOPIADGN" => await CommandCopiarDGN(username, chatid, false),
                "/INSCREVER" => await CommandInscreverAsync(username, chatid),
                "/DESINSCREVER" => await CommandDesinscreverAsync(username, chatid),
                _ => CommandNotFound()
            };

            await _telegramBotClient.SendTextMessageAsync(chatid, response);

        }

        private bool UserCanControl(long chatid)
        {
            foreach (TelegramConfig telegramConfig in TelegramSubscriptions)
                if (telegramConfig.ChatId == chatid)
                    if (telegramConfig.NotificationLevel == 2) return false;

            return true;
        }


        public async Task SendInformationAsync(string message)
        {
            foreach (TelegramConfig telegramConfig in TelegramSubscriptions)
                await _telegramBotClient.SendTextMessageAsync(telegramConfig.ChatId, message);
        }

        public async Task SendErrorAsync(string message)
        {
            foreach (TelegramConfig telegramConfig in TelegramSubscriptions)
                await _telegramBotClient.SendTextMessageAsync(telegramConfig.ChatId, "❗" + message);
        }

        public async Task SendDebugAsync(string message)
        {
            foreach (TelegramConfig telegramConfig in TelegramSubscriptions.Where(k => k.NotificationLevel == 0))
                await _telegramBotClient.SendTextMessageAsync(telegramConfig.ChatId, "⚙️" + message);
        }


        public async Task SendAlertAsync(string message)
        {
            foreach (TelegramConfig telegramConfig in TelegramSubscriptions)
                await _telegramBotClient.SendTextMessageAsync(telegramConfig.ChatId, "🔔" + message);
        }

        public void StartReceiving() =>  _telegramBotClient.StartReceiving();
        public void StopReceiving() => _telegramBotClient.StopReceiving();


        private async Task<string> CommandDesinscreverAsync(string username, long chatid)
        {
            try
            {
              
                    await DeleteTelegramConfigAsync(chatid);
 
            }
            catch
            {
                return $"Olá {username}\nOcorreu um erro com sua solicitação!";
            }
            return $"Tudo certo {username}\nAgora você não receberá mais notificações do processo!";

        }

        private async Task<string> CommandForcarInicioAsync(string username, long chatid)
        {
            try
            {
                if (UserCanControl(chatid))
                    await UpdateSingleMigrationConfig(nameof(MigrationConfig.ForcarPublicacao), "True");
                else
                    return string.Format(noCommandPermission, username);
            }
            catch
            {
                return $"Olá {username}\nOcorreu um erro com sua solicitação!";
            }
            return $"Tudo certo {username}\nA processo será iniciado em breve!";


        }


        private async Task<string> CommandMetadados(string username, long chatid, bool ativar)
        {
            try
            {
                if (UserCanControl(chatid))
                    await UpdateSingleMigrationConfig(nameof(MigrationConfig.PublicarMetadados), ativar ? "True" : "False");
                else
                    return string.Format(noCommandPermission, username);
            }
            catch
            {
                return $"Olá {username}\nOcorreu um erro com sua solicitação!";
            }
            return $"Tudo certo {username}\nA publicação de metadados foi " + (ativar ? "ativada" : "desativada") + "!";


        }



        private async Task<string> CommandDados(string username, long chatid, bool ativar)
        {
            try
            {
                if (UserCanControl(chatid))
                    await UpdateSingleMigrationConfig(nameof(MigrationConfig.PublicarDados), ativar ? "True" : "False");
                else
                    return string.Format(noCommandPermission, username);
            }
            catch
            {
                return $"Olá {username}\nOcorreu um erro com sua solicitação!";
            }
            return $"Tudo certo {username}\nA publicação de dados foi " + (ativar ? "ativada" : "desativada") + "!";


        }


        private async Task<string> CommandDGN(string username, long chatid, bool ativar)
        {
            try
            {
                if (UserCanControl(chatid))
                    await UpdateSingleMigrationConfig(nameof(MigrationConfig.PublicarDGN), ativar ? "True" : "False");
                else
                    return string.Format(noCommandPermission, username);
            }
            catch
            {
                return $"Olá {username}\nOcorreu um erro com sua solicitação!";
            }
            return $"Tudo certo {username}\nA publicação de DGN foi " + (ativar ? "ativada" : "desativada") + "!";

        }




        private async Task<string> CommandCopiarDGN(string username, long chatid, bool ativar)
        {
            try
            {
                if (UserCanControl(chatid))
                    await UpdateSingleMigrationConfig(nameof(MigrationConfig.CopiarDGN), ativar ? "True" : "False");
                else
                    return string.Format(noCommandPermission, username);
            }
            catch
            {
                return $"Olá {username}\nOcorreu um erro com sua solicitação!";
            }
            return $"Tudo certo {username}\nA cópia de DGN foi " + (ativar ? "ativada" : "desativada") + "!";


        }


        private async Task<string> CommandInscreverAsync(string username, long chatid)
        {
            TelegramConfig telegramConfig = new();
            telegramConfig.Username = username;
            telegramConfig.ChatId = chatid;
            telegramConfig.NotificationLevel = 1;

            try
            {
                if(await InsertTelegramConfigAsync(telegramConfig))
                    return $"Tudo certo {username}\nAgora você receberá notificações do processo!";
                else
                    return $"Olá {username}\nVocê ja esta inscrito para receber notificações!";

            }
            catch
            {
                return $"Olá {username}\nOcorreu um erro com sua inscrição!";
            }
           

        }


        private string CommandNotFound() => "Seu comando não foi reconhecido.";

        private async Task<bool> InsertTelegramConfigAsync(TelegramConfig telegramConfig)
        {
            try
            {
                using OracleConnection oracleConnection = new(_oracleConnectionString);
                using OracleCommand oracleCommandInsert = new("insert into g2i_telegram(username, chatid, notificationlevel) values(:usr, :cht, :nti)", oracleConnection);
                using OracleCommand oracleCommandSelect = new("select count(1) from g2i_telegram where chatid = :cht)", oracleConnection);

                oracleCommandInsert.BindByName = true;
                OracleParameter oracleParameter1 = new("usr", telegramConfig.Username);
                OracleParameter oracleParameter2 = new("cht", telegramConfig.ChatId);
                OracleParameter oracleParameter3 = new("nti", telegramConfig.NotificationLevel);
                oracleCommandInsert.Parameters.Add(oracleParameter1);
                oracleCommandInsert.Parameters.Add(oracleParameter2);
                oracleCommandInsert.Parameters.Add(oracleParameter3);

                oracleCommandSelect.BindByName = true;
                OracleParameter oracleParameter4 = new("cht", telegramConfig.ChatId);
                oracleCommandSelect.Parameters.Add(oracleParameter4);




                await oracleConnection.OpenAsync();

                int registros = Convert.ToInt32(await oracleCommandSelect.ExecuteScalarAsync());
                if(registros > 0)
                {
                    await oracleConnection.CloseAsync();
                    return false;
                }

                oracleCommandInsert.ExecuteNonQuery();
                oracleCommandInsert.CommandText = "commit";
                oracleCommandInsert.ExecuteNonQuery();

                await oracleConnection.CloseAsync();
                return true;
            }
            catch
            {
                throw;
            }
        }


        private async Task DeleteTelegramConfigAsync(long chatid)
        {
            try
            {
                using OracleConnection oracleConnection = new(_oracleConnectionString);
                using OracleCommand oracleCommand = new("delete from g2i_telegram where chatid = :cht", oracleConnection);
                oracleCommand.BindByName = true;
                OracleParameter oracleParameter1 = new("cht", chatid);
              
                oracleCommand.Parameters.Add(oracleParameter1);
                await oracleConnection.OpenAsync();
                oracleCommand.ExecuteNonQuery();
                oracleCommand.CommandText = "commit";
                oracleCommand.ExecuteNonQuery();

                await oracleConnection.CloseAsync();
            }
            catch
            {
                throw;
            }
        }


        public async Task UpdateSingleMigrationConfig(string parameter, string value)
        {
            try
            {
                using OracleConnection oracleConnection = new(_oracleConnectionString);
                using OracleCommand oracleCommand = new("update g2i_config set valor = :val where parametro = :par", oracleConnection);
                oracleCommand.BindByName = true;
                OracleParameter oracleParameter1 = new("par", parameter);
                OracleParameter oracleParameter2 = new("val", value);
                oracleCommand.Parameters.Add(oracleParameter1);
                oracleCommand.Parameters.Add(oracleParameter2);

                await oracleConnection.OpenAsync();
                oracleCommand.ExecuteNonQuery();
                oracleCommand.CommandText = "commit";
                oracleCommand.ExecuteNonQuery();

                await oracleConnection.CloseAsync();
            }
            catch
            {
                throw;
            }
        }

        public async Task<List<TelegramConfig>> ReadTelegramConfigAsync()
        {

            List<TelegramConfig> telegramConfigList = new List<TelegramConfig>();
            TelegramConfig telegramConfig;

            try
            {
                using OracleConnection oracleConnection = new(_oracleConnectionString);
                using OracleCommand oracleCommand = new("select * from g2i_telegram", oracleConnection);
                await oracleConnection.OpenAsync();
                OracleDataReader oracleDataReader = oracleCommand.ExecuteReader();

                while (await oracleDataReader.ReadAsync())
                {
                    telegramConfig = new TelegramConfig();
                    telegramConfig.Username = oracleDataReader.GetString(0);
                    telegramConfig.ChatId = Convert.ToInt64(oracleDataReader.GetString(1));
                    telegramConfig.NotificationLevel = Convert.ToInt32(oracleDataReader.GetString(2));

                    telegramConfigList.Add(telegramConfig);
                }

                await oracleConnection.CloseAsync();
            }
            catch
            {
                throw;
            }

            return telegramConfigList;

        }
    }
}
