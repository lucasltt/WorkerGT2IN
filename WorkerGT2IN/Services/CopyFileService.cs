using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkerGT2IN.Services
{
    public class CopyFileService
    {
        private readonly string _sourceFile;
        private readonly string _targetFile;

        public CopyFileService(string sourceFile, string targetFile)
        {
            _sourceFile = sourceFile;
            _targetFile = targetFile;
        }

        public async Task CopyAsync()
        {
            var fileOptions = FileOptions.Asynchronous | FileOptions.SequentialScan;
            var bufferSize = 4096;

            using (var sourceStream =
                  new FileStream(_sourceFile, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize, fileOptions))

            using (var destinationStream =
                  new FileStream(_targetFile, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize, fileOptions))

                await sourceStream.CopyToAsync(destinationStream, bufferSize)
                                           .ConfigureAwait(continueOnCapturedContext: false);
        }
    }
}
