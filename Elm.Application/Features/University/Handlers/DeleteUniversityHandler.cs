using Elm.Application.Contracts;
using Elm.Application.Contracts.Abstractions.Cache;
using Elm.Application.Contracts.Abstractions.Files;
using Elm.Application.Contracts.Features.University.Commands;
using Elm.Application.Contracts.Repositories;
using MediatR;

namespace Elm.Application.Features.University.Handlers
{
    public sealed class DeleteUniversityHandler : IRequestHandler<DeleteUniversityCommand, Result<bool>>
    {
        private readonly IGenericRepository<Domain.Entities.University> repository;
        private readonly IFileStorageService fileStorageService;
        private readonly IGenericCacheService cacheService;
        public DeleteUniversityHandler(IGenericRepository<Domain.Entities.University> repository,
            IFileStorageService fileStorageService, IGenericCacheService cacheService)
        {
            this.repository = repository;
            this.fileStorageService = fileStorageService;
            this.cacheService = cacheService;
        }
        public async Task<Result<bool>> Handle(DeleteUniversityCommand request, CancellationToken cancellationToken)
        {
            var university = await cacheService.GetOrSetAsync("University",
                                    () => repository.GetByIdAsync(request.Id), TimeSpan.FromMinutes(1));
            if (university == null)
            {
                return Result<bool>.Failure("الجامعة غير موجودة", 404);
            }
            var result = await fileStorageService.DeleteUniversityAsync(university.Id);

            if (!result.IsSuccess)
            {
                return Result<bool>.Failure("فشل في حذف الصورة من التخزين", 500);
            }
            cacheService.Remove("University");
            return Result<bool>.Success(true);
        }
    }
}
