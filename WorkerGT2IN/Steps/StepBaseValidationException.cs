using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkerGT2IN.Steps
{
    public class StepBaseValidationException : Exception
    {
        public StepBaseValidationException() { }

        public StepBaseValidationException(string message) : base(message) { }

        public StepBaseValidationException(string message, Exception inner) : base(message, inner) { }
    }
}
