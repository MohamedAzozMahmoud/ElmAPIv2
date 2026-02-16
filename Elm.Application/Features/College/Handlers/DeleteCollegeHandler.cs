using Elm.Application.Contracts;
using Elm.Application.Contracts.Abstractions.Cache;
using Elm.Application.Contracts.Abstractions.Files;
using Elm.Application.Contracts.Features.College.Commands;
using Elm.Application.Contracts.Repositories;
using MediatR;

namespace Elm.Application.Features.College.Handlers
{
    public sealed class DeleteCollegeHandler : IRequestHandler<DeleteCollegeCommand, Result<bool>>
    {
        private readonly ICollegeRepository repository;
        private readonly IFileStorageService fileStorage;
        private readonly IGenericCacheService _cacheService;

        public DeleteCollegeHandler(ICollegeRepository _repository, IFileStorageService _fileStorage, IGenericCacheService cacheService)
        {
            repository = _repository;
            fileStorage = _fileStorage;
            _cacheService = cacheService;
        }

        public async Task<Result<bool>> Handle(DeleteCollegeCommand request, CancellationToken cancellationToken)
        {
            var college = await _cacheService.GetOrSetAsync($"college_{request.Id}",
                                  () => repository.GetByIdAsync(request.Id));
            if (college == null)
            {
                return Result<bool>.Failure("الكلية غير موجودة", 404);
            }

            var deleteImageResult = await fileStorage.DeleteCollegeAsync(college.Id);

            if (!deleteImageResult.IsSuccess)
            {
                return Result<bool>.Failure(deleteImageResult.Message, deleteImageResult.StatusCode);
            }
            _cacheService.Remove($"college_{request.Id}");
            return deleteImageResult;
        }
    }
}

