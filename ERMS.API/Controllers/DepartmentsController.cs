using ERMS.Application.Features.Departments.Commands.CreateDepartment;
using ERMS.Application.Features.Departments.Commands.DeleteDepartment;
using ERMS.Application.Features.Departments.Commands.UpdateDepartment;
using ERMS.Application.Features.Departments.Queries.GetAllDepartments;
using ERMS.Domain.Constants.Roles;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace ERMS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class DepartmentsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public DepartmentsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Lấy danh sách phòng ban (phân trang)
        /// </summary>
        [HttpGet]

        public async Task<IActionResult> GetAll([FromQuery] GetAllDepartmentsQuery query)
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
        /// Tạo phòng ban mới
        /// </summary>
        [HttpPost]
        [Authorize(Roles =AppRoles.HRManager)]
        public async Task<IActionResult> Create([FromBody] CreateDepartmentCommand command)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var departmentId = await _mediator.Send(command);
                return Ok(new
                {
                    message = "Tạo phòng ban thành công",
                    departmentId
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Cập nhật phòng ban
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = AppRoles.HRManager+","+AppRoles.DepartmentHead)]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateDepartmentCommand command)
        {
            if (id != command.Id)
                return BadRequest(new { message = "ID không khớp" });

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                await _mediator.Send(command);
                return Ok(new { message = "Cập nhật phòng ban thành công" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Xóa phòng ban (soft delete)
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id, [FromQuery] Guid enterpriseId)
        {
            try
            {
                await _mediator.Send(new DeleteDepartmentCommand { Id = id, EnterpriseId = enterpriseId });
                return Ok(new { message = "Xóa phòng ban thành công" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
