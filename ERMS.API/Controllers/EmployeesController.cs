using ERMS.Application.Features.Employees.Commands.BulkCreateEmployees;
using ERMS.Application.Features.Employees.Commands.CreateEmployee;
using ERMS.Application.Features.Employees.Commands.DeleteEmployee;
using ERMS.Application.Features.Employees.Commands.ImportEmployeesFromFile;
using ERMS.Application.Features.Employees.Commands.UpdateEmployee;
using ERMS.Application.Features.Employees.Queries.GetAllEmployees;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Threading.Tasks;

namespace ERMS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class EmployeesController : ControllerBase
    {
        private readonly IMediator _mediator;

        public EmployeesController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Lấy danh sách nhân viên (phân trang)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] GetAllEmployeesQuery query)
        {
            try
            {
                var result = await _mediator.Send(query);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Tạo nhân viên mới (+ tài khoản)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateEmployeeCommand command)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var employeeId = await _mediator.Send(command);
                return Ok(new
                {
                    message = "Tạo nhân viên thành công",
                    employeeId
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Cập nhật nhân viên
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateEmployeeCommand command)
        {
            if (id != command.Id)
                return BadRequest(new { message = "ID không khớp" });

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                await _mediator.Send(command);
                return Ok(new { message = "Cập nhật nhân viên thành công" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Xóa nhân viên (soft delete)
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id, [FromQuery] Guid enterpriseId)
        {
            try
            {
                await _mediator.Send(new DeleteEmployeeCommand { Id = id, EnterpriseId = enterpriseId });
                return Ok(new { message = "Xóa nhân viên thành công" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Import nhân viên hàng loạt (từ Excel - JSON input)
        /// </summary>
        [HttpPost("bulk")]
        public async Task<IActionResult> BulkCreate([FromBody] BulkCreateEmployeesCommand command)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var result = await _mediator.Send(command);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Import nhân viên từ file Excel/CSV
        /// </summary>
        [HttpPost("import")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> ImportFromFile([FromForm] IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "File không hợp lệ" });

            var allowedExtensions = new[] { ".xlsx", ".xls", ".csv" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(extension))
                return BadRequest(new { message = "Chỉ hỗ trợ file .xlsx, .xls, .csv" });

            try
            {
                var command = new ImportEmployeesFromFileCommand 
                { 
                    File = file,
                    Commit = Request.Form["commit"] == "true"
                };
                var result = await _mediator.Send(command);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
