using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkerGT2IN.Services
{
    public class ISMProcessService
    {

        private readonly string _ismRequestPath;
        private readonly string _fileName;


        public enum ISMServiceAction
        {
            Start,
            Stop
        }


        public ISMProcessService(string ismRequestPath)
        {
            _ismRequestPath = ismRequestPath;
            _fileName = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "ISMAction.txt");


        }

        public async Task ControlServiceAsync(string serviceName, ISMServiceAction action)
        {
            using System.IO.StreamWriter streamWriter = new(_fileName, false);
            await streamWriter.WriteLineAsync("Connect, localhost, 10000");
            if (action == ISMServiceAction.Start)
               await streamWriter.WriteAsync("Start, ");
            else
                await streamWriter.WriteAsync("Stop, ");

            await streamWriter.WriteLineAsync(serviceName);
            streamWriter.Close();
            streamWriter.Dispose();

            await RunProcessAsync("/inputfile:" + _fileName);
       
        }

        private async Task RunProcessAsync(string arguments)
        {

            try
            {
                using Process process = new();
                process.StartInfo.FileName = _ismRequestPath;
                process.StartInfo.Arguments = arguments;
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.RedirectStandardOutput = true;
                process.Start();
                await process.WaitForExitAsync();
                process.Dispose();
            }
            catch
            {
                throw;
            }


        }
    }
}
