using FluentResults;
using Lots.Domain.Entities;
using Lots.Domain.Interfaces;
using Lots.Domain.Validators;
using System;
using System.Text.RegularExpressions;

namespace Lots.Application.Validators
{
    public class LotValidator : IValidator<Lot>
    {
        private static readonly Regex BatchAllowed =
            new(@"^[A-Za-z0-9\-]+$", RegexOptions.Compiled);

        public Result Validate(Lot e)
        {
            var r = Result.Ok();

            // batch_number
            if (string.IsNullOrWhiteSpace(e.batch_number))
            {
                r = r.WithFieldError("batch_number", "El número de lote es obligatorio.");
            }
            else
            {
                var b = e.batch_number.Trim();

                if (b.Length < 2 || b.Length > 30)
                    r = r.WithFieldError("batch_number", "El número de lote debe tener entre 2 y 30 caracteres.");

                if (!BatchAllowed.IsMatch(b))
                    r = r.WithFieldError("batch_number", "Solo se permiten letras, números y guiones.");
            }

            // expiration_date
            if (e.expiration_date.Date < DateTime.Today)
            {
                r = r.WithFieldError("expiration_date", "La fecha de vencimiento no puede estar en el pasado.");
            }

            // quantity
            if (e.quantity < 0)
            {
                r = r.WithFieldError("quantity", "La cantidad no puede ser negativa.");
            }

            // unit_cost
            if (e.unit_cost < 0)
            {
                r = r.WithFieldError("unit_cost", "El costo unitario no puede ser negativo.");
            }

            return r;
        }
    }
}
