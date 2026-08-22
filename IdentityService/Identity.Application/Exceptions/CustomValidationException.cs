using System;
using System.Collections.Generic;
using System.Text;

namespace Identity.Application.Exceptions
{
    public class CustomValidationException : Exception
    {
        public CustomValidationException(string message) : base(message) { }
    }
}
