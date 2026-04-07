using ERMS.Application.Interface;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ERMS.Application.Features.Training.Commands.DeleteTrainingRequest
{
    public sealed class DeleteTrainingRequestHandler
        : IRequestHandler<DeleteTrainingRequestCommand, bool>
    {
        private readonly IERMSDbContext _context;
       

        public DeleteTrainingRequestHandler(IERMSDbContext context)
        {
            _context = context;
         
        }

        public async Task<bool> Handle(
            DeleteTrainingRequestCommand request,
            CancellationToken cancellationToken)
        {
            var entity = await _context.TrainingRequests
                .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken);

            if (entity == null || entity.IsDeleted)
            {
                return false;
            }

           

            if (entity.Status == "Completed" && entity.Status == "AddedToPlan")
            {
                throw new Exception("Không thể xóa yêu cầu đào tạo này do đang nằm trong kế hoạch đào tạo");
            }

            // Soft delete
            entity.IsDeleted = true;
            entity.DeletedAt = DateTime.UtcNow;

            _context.TrainingRequests.Update(entity);
            await _context.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
}