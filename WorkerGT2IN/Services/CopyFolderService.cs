using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkerGT2IN.Services
{
    public class CopyFolderService
    {
        private readonly string _sourceFolder;
        private readonly string _targetFolder;
        private readonly string _fileType;

        private CopyFileService copyFileService;

        public CopyFolderService(string sourceFolder, string targetFolder, string fileType)
        {
            _sourceFolder = sourceFolder;
            _targetFolder = targetFolder;
            _fileType = fileType;
        }

        public async Task CopyAsync()
        {
            string[] files = Directory.GetFiles(_sourceFolder, _fileType);

            foreach (string sourceFile in files)
            {
                try
                {
                    string fileName = Path.GetFileName(sourceFile);
                    string targetFile = Path.Combine(_targetFolder, fileName);
                    copyFileService = new(sourceFile, targetFile);
                    await copyFileService.CopyAsync();
                }
                catch
                {
                    throw;
                }               
            }
        }



      
    }
}
