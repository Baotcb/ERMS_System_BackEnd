using System;
using System.Collections.Generic;
using System.Linq;
using FluentValidation.Results;

namespace ERMS.Application.Exceptions;

public class ValidationException : Exception
{
    public IDictionary<string, string[]> Errors { get; }

    public ValidationException()
        : base("Dữ liệu không hợp lệ, vui lòng kiểm tra lại.")
    {
        Errors = new Dictionary<string, string[]>();
    }

    public ValidationException(IEnumerable<ValidationFailure> failures)
        : this()
    {
        Errors = failures
            .GroupBy(e => e.PropertyName, e => e.ErrorMessage)
            .ToDictionary(failureGroup => failureGroup.Key, failureGroup => failureGroup.ToArray());
    }

    public override string Message
    {
        get
        {
            var messages = Errors.Select(kvp => $"{kvp.Key}: {string.Join(", ", kvp.Value)}");
            return $"{base.Message} {string.Join(" | ", messages)}";
        }
    }
}
