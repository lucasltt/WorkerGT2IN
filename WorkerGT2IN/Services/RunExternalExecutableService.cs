﻿using System;
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

        public async Task<int> RunProcessAsync()
        {

            try
            {
                using Process process = new();
                int exitCode = new();
                process.StartInfo.FileName = _filePath;
                process.StartInfo.Arguments = _arguments;
                process.StartInfo.Verb = "runas";

                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.UseShellExecute = true;
                process.StartInfo.RedirectStandardError = false;
                process.StartInfo.RedirectStandardOutput = false;
                process.Start();
                await process.WaitForExitAsync();
                exitCode = process.ExitCode;
                process.Dispose();

                return exitCode;
            }
            catch
            {
                throw;
            }


        }
    }
}
