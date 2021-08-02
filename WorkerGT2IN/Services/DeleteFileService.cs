using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkerGT2IN.Services
{
    public class DeleteFileService
    {

        private readonly string _targetFolder;
        private readonly string _fileType;

        public DeleteFileService(string targetFolder, string fileType)
        {
            _targetFolder = targetFolder;
            _fileType = fileType;
        }

        public async Task DeleteAsync()
        {
            string[] files = Directory.GetFiles(_targetFolder, _fileType);

            foreach (string targetFile in files)
            {
                try
                {
                    FileInfo fileInfo = new FileInfo(targetFile);
                    await Task.Factory.StartNew(() => fileInfo.Delete());
                }
                catch
                {
                    throw;
                }
            }
        }

    }
}
