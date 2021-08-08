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


            OracleParameter parametroConfirma = new("pc_indica", OracleDbType.Varchar2, 1, Environment.MachineName.ToString(), ParameterDirection.Output);
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

        public async Task RunExecuteNonQueryAsync(string command)
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


        public async Task<string> GetCurrentMapAsync()
        {
            try
            {
                using OracleConnection oracleConnection = new(_oracleConnectionString);
                using OracleCommand oracleCommand = new("select source from map_switch_table where rownum = 1 order by rev_num desc", oracleConnection);
                await oracleConnection.OpenAsync();
                object mapa = await oracleCommand.ExecuteScalarAsync();
                await oracleConnection.CloseAsync();

                return Convert.ToString(mapa);


            }
            catch
            {
                throw;
            }
        }

        public async Task UpdateMapAsync()
        {
            try
            {
                using OracleConnection oracleConnection = new(_oracleConnectionString);
                using OracleCommand oracleCommandMap = new("select source from map_switch_table where rownum = 1 order by rev_num desc", oracleConnection);
                using OracleCommand oracleCommandRev = new("select rev_num from map_switch_table where rownum = 1 order by rev_num desc", oracleConnection);

                await oracleConnection.OpenAsync();

                object map = await oracleCommandMap.ExecuteScalarAsync();
                object rev = await oracleCommandRev.ExecuteScalarAsync();

          


                using OracleCommand oracleCommandInsert = new("insert into map_switch_table(source, cdts, cterm, rev_num, is_map_db) values (:src, :cdts, :term, :rev, 'F')", oracleConnection);
                oracleCommandInsert.BindByName = true;
                OracleParameter oracleParameter1 = new("src", Convert.ToString(map) == "A" ? "B" : "A"); 
                OracleParameter oracleParameter2 = new("cdts", DateTime.Now.ToString("yyyyMMddHHmmss") + "SS");
                OracleParameter oracleParameter3 = new("term", Environment.MachineName);
                OracleParameter oracleParameter4 = new("rev", Convert.ToInt32(rev) + 1);
                oracleCommandInsert.Parameters.Add(oracleParameter1);
                oracleCommandInsert.Parameters.Add(oracleParameter2);
                oracleCommandInsert.Parameters.Add(oracleParameter3);
                oracleCommandInsert.Parameters.Add(oracleParameter4);




                oracleCommandInsert.ExecuteNonQuery();
                oracleCommandInsert.Parameters.Clear();
                oracleCommandInsert.CommandText = "commit";
                oracleCommandInsert.ExecuteNonQuery();

                await oracleConnection.CloseAsync();
            }
            catch
            {
                throw;
            }
        }

    }

}
