using FluentResults;
using Lots.Domain.Entities;
using Lots.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Lots.Application.Validators
{
    public class LotValidator : IValidator<Lot>
    {
        private static readonly Regex BatchAllowed =
            new(@"^[A-Za-z0-9\-]+$", RegexOptions.Compiled);

        public Result Validate(Lot e)
        {
            var r = Result.Ok();

            if (string.IsNullOrWhiteSpace(e.batch_number))
            {
                r = r.WithError(new Error("El número de lote es obligatorio.")
                    .WithMetadata("FieldName", "batch_number"));
            }
            else
            {
                var b = e.batch_number.Trim();
                if (b.Length is < 2 or > 30)
                    r = r.WithError(new Error("Debe tener entre 2 y 30 caracteres.")
                        .WithMetadata("FieldName", "batch_number"));

                if (!BatchAllowed.IsMatch(b))
                    r = r.WithError(new Error("Solo se permiten letras, números y guiones.")
                        .WithMetadata("FieldName", "batch_number"));
            }

            if (e.expiration_date.Date < DateTime.Today)
            {
                r = r.WithError(new Error("La fecha de vencimiento no puede estar en el pasado.")
                    .WithMetadata("FieldName", "expiration_date"));
            }

            if (e.quantity < 0)
            {
                r = r.WithError(new Error("La cantidad no puede ser negativa.")
                    .WithMetadata("FieldName", "quantity"));
            }

            if (e.unit_cost < 0)
            {
                r = r.WithError(new Error("El costo unitario no puede ser negativo.")
                    .WithMetadata("FieldName", "unit_cost"));
            }

            return r;
        }
    }
}
