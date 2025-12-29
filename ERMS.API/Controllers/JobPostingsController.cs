using ERMS.Application.Features.JobPostings.Commands.CreateJobPosting;
using ERMS.Application.Features.JobPostings.Commands.DeleteJobPosting;
using ERMS.Application.Features.JobPostings.Commands.UpdateJobPosting;
using ERMS.Application.Features.JobPostings.Queries.GetAllJobPostings;
using ERMS.Application.Features.JobPostings.Queries.GetJobPostingById;
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
    public class JobPostingsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public JobPostingsController(IMediator mediator)
        {
            _mediator = mediator;
        }

 
        [HttpPost]
        [Authorize(Roles = AppRoles.Manager)]
        public async Task<IActionResult> CreateJobPosting([FromBody] CreateJobPostingCommand command)
        {
            try
            {
                var jobId = await _mediator.Send(command);
                return Ok(new { message = "Tạo bài đăng thành công!", jobId });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
        [HttpGet("debug-claims")]
        [Authorize] // Chỉ cần đăng nhập là được
        public IActionResult GetClaims()
        {
            return Ok(User.Claims.Select(c => new { c.Type, c.Value }));
        }

        [HttpPut("{id}")]
        [Authorize(Roles = AppRoles.Manager)]
        public async Task<IActionResult> UpdateJobPosting(Guid id, [FromBody] UpdateJobPostingCommand command)
        {
            try
            {
                command.Id = id;
                var result = await _mediator.Send(command);
                return Ok(new { message = "Cập nhật thành công!" });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }


        [HttpDelete("{id}")]
        [Authorize(Roles = AppRoles.Manager)]
        public async Task<IActionResult> DeleteJobPosting(Guid id)
        {
            try
            {
                var result = await _mediator.Send(new DeleteJobPostingCommand { Id = id });
                return Ok(new { message = "Xóa thành công!" });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetJobPostingById(Guid id)
        {
            try
            {
                var jobPosting = await _mediator.Send(new GetJobPostingByIdQuery { Id = id });
                return Ok(jobPosting);
            }
            catch (Exception ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

    
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAllJobPostings([FromQuery] GetAllJobPostingsQuery query)
        {
            try
            {
                var jobPostings = await _mediator.Send(query);
                return Ok(jobPostings);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}