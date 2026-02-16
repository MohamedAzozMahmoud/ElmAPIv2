using Elm.Application.Contracts;
using Elm.Application.Contracts.Abstractions.Cache;
using Elm.Application.Contracts.Abstractions.Files;
using Elm.Application.Contracts.Features.Files.Commands;
using Elm.Application.Contracts.Repositories;
using MediatR;

namespace Elm.Application.Features.Files.Handlers
{
    public sealed class DeleteFileHandler : IRequestHandler<DeleteFileCommand, Result<bool>>
    {
        private readonly IGenericRepository<Domain.Entities.Files> filesRepository;
        private readonly IFileStorageService fileStorage;
        private readonly IGenericCacheService cacheService;

        public DeleteFileHandler(IGenericRepository<Domain.Entities.Files> _filesRepository, IFileStorageService _fileStorage, IGenericCacheService _cacheService)
        {
            filesRepository = _filesRepository;
            fileStorage = _fileStorage;
            cacheService = _cacheService;
        }
        public async Task<Result<bool>> Handle(DeleteFileCommand request, CancellationToken cancellationToken)
        {
            var file = await filesRepository.GetByIdAsync(request.Id);
            if (file == null)
            {
                return Result<bool>.Failure("File not found.");
            }
            var deleteResult = await fileStorage.DeleteFile(file.StorageName);
            if (!deleteResult.IsSuccess)
            {
                return Result<bool>.Failure("Failed to delete file from storage.");
            }
            cacheService.Remove($"file_{file.StorageName}");
            cacheService.Remove($"files_{file.CurriculumId}");
            return Result<bool>.Success(true);
        }
    }
}
