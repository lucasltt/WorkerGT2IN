using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkerGT2IN.Entities
{
    public class TelegramConfig
    {
        public string Username { get; set; }
        public long ChatId { get; set; }
        public int NotificationLevel { get; set; }
    }
}
