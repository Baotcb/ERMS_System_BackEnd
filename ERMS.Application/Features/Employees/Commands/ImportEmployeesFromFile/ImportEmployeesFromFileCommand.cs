using MediatR;
using Microsoft.AspNetCore.Http;

namespace ERMS.Application.Features.Employees.Commands.ImportEmployeesFromFile
{
    /// <summary>
    /// Command để import nhân viên từ file Excel/CSV
    /// </summary>
    public sealed class ImportEmployeesFromFileCommand : IRequest<ImportEmployeesFromFileResult>
    {
        /// <summary>
        /// File Excel/CSV upload
        /// </summary>
        public IFormFile File { get; set; } = null!;

        /// <summary>
        /// If true, execute import. If false, dry-run/validate only.
        /// </summary>
        public bool Commit { get; set; }
    }
}
