using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkerGT2IN.Entities
{
    public class ServiceConfig
    {
        public string GTechConnectionString { get; init; }
        public string InServiceConnectionString { get; init; }

        public string TelegramBotKey { get; init; }

        public string MachineDescription { get; init; }

        public int TempoEspera { get; init; }

        public AmbienteEnum Ambiente { get; init; }


    }
       
}
