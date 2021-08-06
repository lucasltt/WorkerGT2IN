using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkerGT2IN.Entities
{
    public class MigrationConfig
    {
        public bool PublicouHoje { get; set; }
        public string HoraAgendamentoDiario { get; set; }
        public bool ForcarPublicacao { get; set; }

        public bool AtivarAgendamento { get; set; }


        public bool PublicarMetadados { get; set; }
        public string CaminhoPublicadorMetadados { get; set; }
        public string ArgumentoPublicadorMetadados { get; set; }


        public bool PublicarDados{ get; set; }
        public string CaminhoPublicadorDados { get; set; }
        public string ArgumentoPublicadorDados{ get; set; }


        public bool PublicarDGN { get; set; }
        public string CaminhoPublicadorDGN { get; set; }
        public string ArgumentoPublicadorDGN { get; set; }
       


        public bool CopiarDGN { get; set; }
        public string PastaOrigemDGN { get; set; }

        public string PastaDestinoDGN { get; set; }


        public bool ExecutarCongelarFila { get; set; }
        public string ComandoCongelarFila { get; set; }

        public int EsperaCongelarFilaSegundos { get; set; }

        public string CaminhoISMRequest { get; set; }


        public bool PararServicos { get; set; }
        public bool IniciarServicos { get; set; }



        public List<string> ServicosISM { get; set; } = new List<string>();

        public List<string> ProceduresInservice { get; set; } = new List<string>();

        public List<string> ProceduresGTech { get; set; } = new List<string>();


        public List<string> Compilar { get; set; } = new List<string>();

        public List<string> Indicadores { get; set; } = new List<string>();



        public bool ExecutarOMSMigration { get; set; }
        public string CaminhoOMSMigration { get; set; }
        public string ArgumentoOMSMigration { get; set; }
        public string MapaOMSMigration { get; set; }


        public bool DeletarArquivoNET { get; set; }
        public string CaminhoArquivoNET { get; set; }



        public bool ExecutarDescongelarFila { get; set; }
        public string ComandoDescongelarFila { get; set; }

        public int EsperaDescongelarFilaSegundos { get; set; }





        public bool ExecutarMergeMaps { get; set; }
        public string CaminhoMergeMaps { get; set; }
        public string ArgumentoMergeMaps { get; set; }



        public bool CopiarArquivoMAP { get; set; }
        public string CaminhoMapOrigem { get; set; }
        public string CaminhoMapADestino { get; set; }
        public string CaminhoMapBDestino { get; set; }

    }
}
