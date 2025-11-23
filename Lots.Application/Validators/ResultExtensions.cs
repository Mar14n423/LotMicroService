using FluentResults;

namespace Lots.Domain.Validators
{
    public static class ResultExtensions
    {
        public static Result WithFieldError(this Result result, string field, string message)
        {
            var error = new Error(message).WithMetadata("field", field);
            return result.WithError(error);
        }
    }
}
