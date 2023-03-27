using Microsoft.VisualStudio.TestTools.UnitTesting;
using WorkerGT2IN.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;

namespace WorkerGT2IN.Services.Tests
{
    [TestClass()]
    public class GTechDataServiceTests
    {
        [TestMethod()]
        public async void GenerateNextGroupIdAsyncTest()
        {
            GTechDataService gTechDataService = new("User Id=gisprod;Password=gisprod;Data Source=10.80.0.94/GISPROD", Entities.AmbienteEnum.Producao);
            int id = await gTechDataService.GenerateNextGroupIdAsync();
            if (id > 0 == false)
                Assert.Fail();
        }
    }
}