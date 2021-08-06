using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkerGT2IN.Services
{
    public class CheckErrorOnFileService
    {
        private readonly string _targetFile;

        public CheckErrorOnFileService(string targetFile)
        {
            _targetFile = targetFile;
        }

        public async Task<bool> ErrorExistsOnFileAsync()
        {
            try
            {
                using StreamReader streamReader = new StreamReader(_targetFile);
                string content = await streamReader.ReadToEndAsync();
                content = content.ToUpper();
                return content.Contains("ERROR");
            }
            catch
            {
                return true;
            }
          
        }
    }
}
