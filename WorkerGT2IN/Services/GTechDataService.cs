using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using WorkerGT2IN.Entities;

namespace WorkerGT2IN.Services
{
    public class GTechDataService
    {
        private readonly string _oracleConnectionString;
        private readonly AmbienteEnum _ambiente;

        private string Suffix { get; set; }

        public GTechDataService(string oracleConnectionString, AmbienteEnum ambiente)
        {
            _oracleConnectionString = oracleConnectionString;
            _ambiente = ambiente;

            if (ambiente == AmbienteEnum.Producao) Suffix = string.Empty;
            else Suffix = "_qa";
        }


        public async Task UpdateSingleMigrationConfig(string parameter, string value)
        {
            try
            {
                using OracleConnection oracleConnection = new(_oracleConnectionString);
                using OracleCommand oracleCommand = new($"update g2i_config{Suffix} set valor = :val where parametro = :par", oracleConnection);
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




        public async Task<MigrationConfig> ReadMigrationConfig()
        {
            MigrationConfig migrationConfig = new MigrationConfig();

            try
            {
                using OracleConnection oracleConnection = new(_oracleConnectionString);
                using OracleCommand oracleCommand = new($"select parametro, valor, info, passo, sequencia from g2i_config{Suffix} order by passo, sequencia", oracleConnection);
                await oracleConnection.OpenAsync();
                OracleDataReader oracleDataReader = oracleCommand.ExecuteReader();
                string parametro = string.Empty;

                while (await oracleDataReader.ReadAsync())
                {
                    parametro = oracleDataReader.GetString(0);

                    switch (parametro)
                    {

                        case nameof(MigrationConfig.IniciarServicos):
                            migrationConfig.IniciarServicos = oracleDataReader.GetString(1).Equals("True") ? true : false;
                            break;
                      
                        case nameof(MigrationConfig.PararServicos):
                            migrationConfig.PararServicos = oracleDataReader.GetString(1).Equals("True") ? true : false;
                            break;
                        case nameof(MigrationConfig.AtivarAgendamento):
                            migrationConfig.AtivarAgendamento = oracleDataReader.GetString(1).Equals("True") ? true : false;
                            break;
                        case nameof(MigrationConfig.PublicarMetadados):
                            migrationConfig.PublicarMetadados = oracleDataReader.GetString(1).Equals("True") ? true : false;
                            break;
                        case nameof(MigrationConfig.CaminhoPublicadorMetadados):
                            migrationConfig.CaminhoPublicadorMetadados = oracleDataReader.GetString(1);
                            break;
                        case nameof(MigrationConfig.ArgumentoPublicadorMetadados):
                            migrationConfig.ArgumentoPublicadorMetadados = oracleDataReader.GetString(1);
                            break;
                        case nameof(MigrationConfig.PublicarDados):
                            migrationConfig.PublicarDados = oracleDataReader.GetString(1).Equals("True") ? true : false;
                            break;
                        case nameof(MigrationConfig.CaminhoPublicadorDados):
                            migrationConfig.CaminhoPublicadorDados = oracleDataReader.GetString(1);
                            break;
                        case nameof(MigrationConfig.ArgumentoPublicadorDados):
                            migrationConfig.ArgumentoPublicadorDados = oracleDataReader.GetString(1);
                            break;
                        case nameof(MigrationConfig.PublicarDGN):
                            migrationConfig.PublicarDGN = oracleDataReader.GetString(1).Equals("True") ? true : false;
                            break;
                        case nameof(MigrationConfig.CaminhoPublicadorDGN):
                            migrationConfig.CaminhoPublicadorDGN = oracleDataReader.GetString(1);
                            break;
                        case nameof(MigrationConfig.ArgumentoPublicadorDGN):
                            migrationConfig.ArgumentoPublicadorDGN = oracleDataReader.GetString(1);
                            break;
                        case nameof(MigrationConfig.CopiarDGN):
                            migrationConfig.CopiarDGN = oracleDataReader.GetString(1).Equals("True") ? true : false;
                            break;
                        case nameof(MigrationConfig.PastaOrigemDGN):
                            migrationConfig.PastaOrigemDGN = oracleDataReader.GetString(1);
                            break;
                        case nameof(MigrationConfig.PastaDestinoDGN):
                            migrationConfig.PastaDestinoDGN = oracleDataReader.GetString(1);
                            break;
                        case nameof(MigrationConfig.ExecutarCongelarFila):
                            migrationConfig.ExecutarCongelarFila = oracleDataReader.GetString(1).Equals("True") ? true : false;
                            break;
                        case nameof(MigrationConfig.ComandoCongelarFila):
                            migrationConfig.ComandoCongelarFila = oracleDataReader.GetString(1);
                            break;
                        case nameof(MigrationConfig.EsperaCongelarFilaSegundos):
                            migrationConfig.EsperaCongelarFilaSegundos = Convert.ToInt32(oracleDataReader.GetString(1));
                            break;
                        case nameof(MigrationConfig.ExecutarOMSMigration):
                            migrationConfig.ExecutarOMSMigration = oracleDataReader.GetString(1).Equals("True") ? true : false;
                            break;
                        case nameof(MigrationConfig.MapaOMSMigration):
                            migrationConfig.MapaOMSMigration = oracleDataReader.GetString(1);
                            break;
                        case nameof(MigrationConfig.CaminhoOMSMigration):
                            migrationConfig.CaminhoOMSMigration = oracleDataReader.GetString(1);
                            break;
                        case nameof(MigrationConfig.ArgumentoOMSMigration):
                            migrationConfig.ArgumentoOMSMigration = oracleDataReader.GetString(1);
                            break;
                        case nameof(MigrationConfig.CaminhoISMRequest):
                            migrationConfig.CaminhoISMRequest = oracleDataReader.GetString(1);
                            break;
                        case nameof(MigrationConfig.MapChangerPath):
                            migrationConfig.MapChangerPath = oracleDataReader.GetString(1);
                            break;
                        case nameof(MigrationConfig.ExecutarDescongelarFila):
                            migrationConfig.ExecutarDescongelarFila = oracleDataReader.GetString(1).Equals("True") ? true : false;
                            break;
                        case nameof(MigrationConfig.ComandoDescongelarFila):
                            migrationConfig.ComandoDescongelarFila = oracleDataReader.GetString(1);
                            break;
                        case nameof(MigrationConfig.EsperaDescongelarFilaSegundos):
                            migrationConfig.EsperaDescongelarFilaSegundos = Convert.ToInt32(oracleDataReader.GetString(1));
                            break;

                        case nameof(MigrationConfig.PublicouHoje):
                            migrationConfig.PublicouHoje = oracleDataReader.GetString(1).Equals("True") ? true : false;
                            break;
                        case nameof(MigrationConfig.HoraAgendamentoDiario):
                            migrationConfig.HoraAgendamentoDiario = oracleDataReader.GetString(1);
                            break;
                        case nameof(MigrationConfig.ForcarPublicacao):
                            migrationConfig.ForcarPublicacao = oracleDataReader.GetString(1).Equals("True") ? true : false;
                            break;
                        case nameof(MigrationConfig.ForcarCopiaMapa):
                            migrationConfig.ForcarCopiaMapa = oracleDataReader.GetString(1).Equals("True") ? true : false;
                            break;

                        case nameof(MigrationConfig.RenomearLayers):
                            migrationConfig.RenomearLayers = oracleDataReader.GetString(1).Equals("True") ? true : false;
                            break;
                        case nameof(MigrationConfig.CaminhoRenomearLayers):
                            migrationConfig.CaminhoRenomearLayers = oracleDataReader.GetString(1);
                            break;
                        case nameof(MigrationConfig.ArgumentoRenomearLayers):
                            migrationConfig.ArgumentoRenomearLayers = oracleDataReader.GetString(1);
                            break;

                        case nameof(MigrationConfig.ExecutarMergeMaps):
                            migrationConfig.ExecutarMergeMaps = oracleDataReader.GetString(1).Equals("True") ? true : false;
                            break;
                        case nameof(MigrationConfig.CaminhoMergeMaps):
                            migrationConfig.CaminhoMergeMaps = oracleDataReader.GetString(1);
                            break;
                        case nameof(MigrationConfig.ArgumentoMergeMaps):
                            migrationConfig.ArgumentoMergeMaps = oracleDataReader.GetString(1);
                            break;

                        case nameof(MigrationConfig.CopiarArquivoMAP):
                            migrationConfig.CopiarArquivoMAP = oracleDataReader.GetString(1).Equals("True") ? true : false;
                            break;
                        case nameof(MigrationConfig.CaminhoMapOrigem):
                            migrationConfig.CaminhoMapOrigem = oracleDataReader.GetString(1);
                            break;
                        case nameof(MigrationConfig.CaminhoMapADestino):
                            migrationConfig.CaminhoMapADestino = oracleDataReader.GetString(1);
                            break;
                        case nameof(MigrationConfig.CaminhoMapBDestino):
                            migrationConfig.CaminhoMapBDestino = oracleDataReader.GetString(1);
                            break;
                        case { } when parametro.StartsWith("ServicoISM"):
                            migrationConfig.ServicosISM.Add(oracleDataReader.GetString(1));
                            break;
                        case { } when parametro.StartsWith("Compilar"):
                            migrationConfig.Compilar.Add(oracleDataReader.GetString(1));
                            break;
                        case { } when parametro.StartsWith("Indicadores"):
                            migrationConfig.Indicadores.Add(oracleDataReader.GetString(1));
                            break;
                        case { } when parametro.StartsWith("ProcedureInservice"):
                            migrationConfig.ProceduresInservice.Add(oracleDataReader.GetString(1));
                            break;
                        case { } when parametro.StartsWith("ProcedureGTech"):
                            migrationConfig.ProceduresGTech.Add(oracleDataReader.GetString(1));
                            break;
                        case { } when parametro.StartsWith("ValidacoesGTech"):
                            migrationConfig.ValidacoesGTech.Add(oracleDataReader.GetString(1));
                            break;
                        case { } when parametro.StartsWith("Rollback"):
                            migrationConfig.Rollback.Add(oracleDataReader.GetString(1));
                            break;
                        case { } when parametro.StartsWith("ValidacoesIndicadoresCritica"):
                            migrationConfig.ValidacoesIndicadoresCritica.Add((oracleDataReader.GetString(1), oracleDataReader.GetString(2)));
                            break;
                        case { } when parametro.StartsWith("ValidacoesIndicadoresAvisos"):
                            migrationConfig.ValidacoesIndicadoresAvisos.Add((oracleDataReader.GetString(1), oracleDataReader.GetString(2)));
                            break;
                        default:
                            break;



                    }
                }
                await oracleConnection.CloseAsync();
            }
            catch
            {
                throw;
            }

            return migrationConfig;
        }

        public async Task RunCommand(string command)
        {
            try
            {
                using OracleConnection oracleConnection = new(_oracleConnectionString);
                using OracleCommand oracleCommand = new(command, oracleConnection);
                await oracleConnection.OpenAsync();
                oracleCommand.ExecuteNonQuery();
                await oracleConnection.CloseAsync();
            }
            catch
            {
                throw;
            }
        }

        public async Task<int> GenerateNextGroupIdAsync()
        {
            try
            {
                using OracleConnection oracleConnection = new(_oracleConnectionString);
                using OracleCommand oracleCommand = new($"select nvl(max(idgrupo), 0) + 1 from hxgn_migra_passos{Suffix}", oracleConnection);
                await oracleConnection.OpenAsync();
                int result = Convert.ToInt32(await oracleCommand.ExecuteScalarAsync());
                await oracleConnection.CloseAsync();

                return result;
            }
            catch
            {
                throw;
            }
        }

        public async Task<int> GetActualGroupIdAsync()
        {
            try
            {
                using OracleConnection oracleConnection = new(_oracleConnectionString);
                using OracleCommand oracleCommand = new($"select nvl(max(idgrupo), 0) from hxgn_migra_passos{Suffix}", oracleConnection);
                await oracleConnection.OpenAsync();
                int result = Convert.ToInt32(await oracleCommand.ExecuteScalarAsync());
                await oracleConnection.CloseAsync();

                return result;
            }
            catch
            {
                throw;
            }
        }


        public async Task InsertLogAsync(int grupo, int passo, string mensagem, TipoLogEnum tipo)
        {
            try
            {
                using OracleConnection oracleConnection = new(_oracleConnectionString);
                using OracleCommand oracleCommand = new($"insert into hxgn_migra_log{Suffix}(idlog, idgrupo, passo, mensagem, tipo, data) " +
                    $"values(hxgn_migra_log_seq.nextval, :gru, :pas, :msg, :tip, sysdate)", oracleConnection);
                oracleCommand.BindByName = true;
                OracleParameter oracleParameter1 = new("gru", grupo);
                OracleParameter oracleParameter2 = new("pas", passo);
                OracleParameter oracleParameter3 = new("msg", mensagem);
                OracleParameter oracleParameter4 = new("tip", tipo.ToString());
                oracleCommand.Parameters.Add(oracleParameter1);
                oracleCommand.Parameters.Add(oracleParameter2);
                oracleCommand.Parameters.Add(oracleParameter3);
                oracleCommand.Parameters.Add(oracleParameter4);

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


        public async Task InsertPassoAsync(int grupo, int passo, string mensagem, Enum status)
        {
            try
            {
                using OracleConnection oracleConnection = new(_oracleConnectionString);
                using OracleCommand oracleCommand = new($"insert into hxgn_migra_passos{Suffix}(idpasso, idgrupo, passo, mensagem, status, data) " +
                    $"values(hxgn_migra_log_seq.nextval, :gru, :pas, :msg, :sta, sysdate)", oracleConnection);
                oracleCommand.BindByName = true;
                OracleParameter oracleParameter1 = new("gru", grupo);
                OracleParameter oracleParameter2 = new("pas", passo);
                OracleParameter oracleParameter3 = new("msg", mensagem);
                OracleParameter oracleParameter4 = new("sta", status.ToString());
                oracleCommand.Parameters.Add(oracleParameter1);
                oracleCommand.Parameters.Add(oracleParameter2);
                oracleCommand.Parameters.Add(oracleParameter3);
                oracleCommand.Parameters.Add(oracleParameter4);

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


        public async Task<object> RunExecuteScalarAsync(string sql)
        {
            object valor = default(object);
            try
            {
                using OracleConnection oracleConnection = new(_oracleConnectionString);
                using OracleCommand oracleCommand = new(sql, oracleConnection);
                await oracleConnection.OpenAsync();
                valor = await oracleCommand.ExecuteScalarAsync();
                await oracleConnection.CloseAsync();
                return valor;
            }
            catch
            {
                throw;
            }
        }





    }
}
