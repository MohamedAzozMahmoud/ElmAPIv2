using Elm.Application.Contracts;
using Elm.Application.Contracts.Abstractions.Cache;
using Elm.Application.Contracts.Abstractions.Files;
using Elm.Application.Contracts.Features.Files.DTOs;
using Elm.Application.Contracts.Features.Files.Queries;
using MediatR;

namespace Elm.Application.Features.Files.Handlers
{
    public sealed class DownloadFileHandler : IRequestHandler<DownloadFileCommand, Result<FileResponse>>
    {
        private readonly IFileStorageService fileStorage;
        private readonly IGenericCacheService cacheService;
        public DownloadFileHandler(IFileStorageService _fileStorage, IGenericCacheService _cacheService)
        {
            fileStorage = _fileStorage;
            cacheService = _cacheService;
        }
        public async Task<Result<FileResponse>> Handle(DownloadFileCommand request, CancellationToken cancellationToken)
        {
            return await cacheService.GetOrSetAsync($"file_{request.fileName}",
                () => fileStorage.DownloadFileAsync(request.fileName, "Files"), TimeSpan.FromMinutes(15));
        }
    }
}
