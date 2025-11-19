using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentResults;
namespace Lots.Domain.Interfaces
{
    public interface IValidator<T>
    {
        Result Validate(T entity);
    }
}
