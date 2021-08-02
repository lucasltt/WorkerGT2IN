using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkerGT2IN.Services
{
    public class RunExternalExecutableService
    {
        private readonly string _filePath;
        private readonly string _arguments;

        public RunExternalExecutableService(string filePath, string arguments)
        {
            _filePath = filePath;
            _arguments = arguments;
        }

        public async Task RunProcessAsync()
        {

            try
            {
                using Process process = new();
                process.StartInfo.FileName = _filePath;
                process.StartInfo.Arguments = _arguments;
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
