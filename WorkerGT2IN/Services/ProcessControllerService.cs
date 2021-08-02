using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace WorkerGT2IN.Services
{
    public class ProcessControllerService
    {

        public enum ServiceAction
        {
            Start,
            Stop
        }

        public ProcessControllerService() { }

        #pragma warning disable CA1416

        public async Task ControlServiceAsync(string serviceName, ServiceAction action, int timeoutSeconds)
        {
            // string[] services =  ServiceController.GetServices().Select(k => k.ServiceName).ToArray();
            try
            {
                using ServiceController serviceController = new(serviceName);
                ServiceControllerStatus desiredSatus = action == ServiceAction.Start ? ServiceControllerStatus.Running : ServiceControllerStatus.Stopped;

                if (serviceController.Status == desiredSatus) return;


                if (action == ServiceAction.Start)
                    serviceController.Start();
                else
                    serviceController.Stop();




                while (serviceController.Status != desiredSatus && timeoutSeconds > 0)
                {
                    await Task.Delay(1000);
                    timeoutSeconds--;
                }
            }
            catch
            {
                throw;
            }


        }
    }
}
