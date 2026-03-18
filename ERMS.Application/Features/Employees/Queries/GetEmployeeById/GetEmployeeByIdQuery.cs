using MediatR;
using System;

namespace ERMS.Application.Features.Employees.Queries.GetEmployeeById
{
    public sealed class GetEmployeeByIdQuery : IRequest<EmployeeDetailDto>
    {
        public Guid Id { get; set; }
    }

    public sealed class EmployeeDetailDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string EmployeeCode { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string? Phone { get; set; }
        public int DepartmentId { get; set; }
        public string DepartmentName { get; set; } = null!;
        public string? Position { get; set; }
        public Guid? JobPositionId { get; set; }
        public DateTime? HireDate { get; set; }
        public DateTime? TerminationDate { get; set; }
        public string EmploymentType { get; set; } = null!;
        public Guid? ManagerId { get; set; }
        public string? ManagerName { get; set; }
        public decimal? Salary { get; set; }
        public bool IsTrainer { get; set; }
        public List<string> Roles { get; set; } = new();
        public string Status { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
