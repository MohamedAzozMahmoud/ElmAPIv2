using Elm.Application.Contracts;
using Elm.Application.Contracts.Abstractions.Cache;
using Elm.Application.Contracts.Abstractions.Files;
using Elm.Application.Contracts.Features.Files.Commands;
using MediatR;

namespace Elm.Application.Features.Files.Handlers
{
    public sealed class UploadFileHandler : IRequestHandler<UploadFileCommand, Result<string>>
    {
        private readonly IFileStorageService fileStorage;
        private readonly IGenericCacheService cacheService;
        public UploadFileHandler(IFileStorageService fileStorage, IGenericCacheService cacheService)
        {
            this.fileStorage = fileStorage;
            this.cacheService = cacheService;
        }
        public async Task<Result<string>> Handle(UploadFileCommand request, CancellationToken cancellationToken)
        {
            var result = await fileStorage.UploadFileAsync(request.curriculumId, request.uploadedById, request.Description, request.FormFile, "Files");
            if (result.IsSuccess)
            {
                cacheService.Remove($"files_{request.curriculumId}");
            }
            return result;
        }
    }
}
