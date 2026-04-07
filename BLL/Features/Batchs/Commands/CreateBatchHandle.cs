using DAL.Entities;
using DAL.Repositories.Interfaces;
using MediatR;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BLL.Features.Batchs.Commands
{
    public class CreateBatchHandle : IRequestHandler<CreateBatchRequest, CreateBatchResponse>
    {
        private readonly IRepository<Batch> _batchRepository;
        private readonly IRepository<DAL.Entities.User> _userRepository;

        public CreateBatchHandle(
            IRepository<Batch> batchRepository,
            IRepository<DAL.Entities.User> userRepository)
        {
            _batchRepository = batchRepository;
            _userRepository = userRepository;
        }

        public async Task<CreateBatchResponse> Handle(CreateBatchRequest request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return new CreateBatchResponse
                {
                    Success = false,
                    Message = "Batch adı boş olamaz."
                };
            }

            var existing = await _batchRepository.GetWhereAsync(x => x.Name == request.Name && !x.IsDeleted);
            if (existing.Any())
            {
                return new CreateBatchResponse
                {
                    Success = false,
                    Message = "Bu isimde batch zaten mevcut."
                };
            }

            var user = await _userRepository.GetByIdAsync(request.CreatedBy);
            if (user == null)
            {
                return new CreateBatchResponse
                {
                    Success = false,
                    Message = "Geçersiz kullanıcı. Batch oluşturulamadı."
                };
            }

            var batch = new Batch
            {
                Name = request.Name,
                ReleaseId = null,
                CreatedBy = request.CreatedBy,
                CreatedAt = DateTime.UtcNow
            };

            await _batchRepository.AddAsync(batch);
            await _batchRepository.SaveAsync();

            return new CreateBatchResponse
            {
                Success = true,
                Message = "Batch başarıyla oluşturuldu.",
                BatchId = batch.Id
            };
        }
    }
}
