using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkerGT2IN.Steps
{
    public class StepBaseExecutionException : Exception
    {
        public StepBaseExecutionException() { }

        public StepBaseExecutionException(string message) : base(message) { }

        public StepBaseExecutionException(string message, Exception inner) : base(message, inner) { }
    }
}
