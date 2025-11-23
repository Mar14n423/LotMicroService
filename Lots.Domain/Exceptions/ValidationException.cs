using System;
using System.Collections.Generic;

namespace Lots.Domain.Exceptions
{
    public class ValidationException : DomainException
    {
        public IDictionary<string, string> Errors { get; }

        public ValidationException(IDictionary<string, string> errors)
            : base("Validación de dominio falló.")
        {
            Errors = errors;
        }

        public ValidationException(string message, IDictionary<string, string> errors)
            : base(message)
        {
            Errors = errors;
        }
    }
}
