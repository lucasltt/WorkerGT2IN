using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkerGT2IN.Services
{
    public class InServiceDataService
    {
        private readonly string _oracleConnectionString;
        public InServiceDataService(string oracleConnectionString)
        {
            _oracleConnectionString = oracleConnectionString;
        }


        public async Task<bool> CongelarFilaAsync(string command)
        {
            using OracleConnection oracleConnection = new(_oracleConnectionString);
            using OracleCommand oracleCommand = new();

            oracleCommand.Connection = oracleConnection;
            //oracleCommand.CommandType = CommandType.StoredProcedure;
            oracleCommand.BindByName = true;
            oracleCommand.CommandText = command;
       

            OracleParameter parametroConfirma = new ("pc_indica", OracleDbType.Varchar2, 1, Environment.MachineName.ToString(), ParameterDirection.Output);
            oracleCommand.Parameters.Add(parametroConfirma);


            //bool migracaoLiberada = false;
            //await oracleConnection.OpenAsync();
            //while(migracaoLiberada == false)
            //{
            //    await oracleCommand.PrepareAsync();
            //    await oracleCommand.ExecuteNonQueryAsync();
            //    await Task.Delay(esperaSegundos * 1000);
            //    migracaoLiberada = parametroConfirma.Value is not null && parametroConfirma.Status != OracleParameterStatus.NullFetched && parametroConfirma.Value.ToString() == "S";
            //}



            bool migracaoLiberada = default(bool);

            await oracleConnection.OpenAsync();

            await oracleCommand.PrepareAsync();
            await oracleCommand.ExecuteNonQueryAsync();

            migracaoLiberada = parametroConfirma.Value is not null && parametroConfirma.Status != OracleParameterStatus.NullFetched && parametroConfirma.Value.ToString() == "S";

            oracleCommand.Dispose();
            await oracleConnection.CloseAsync();

            return migracaoLiberada;
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
    }
}
