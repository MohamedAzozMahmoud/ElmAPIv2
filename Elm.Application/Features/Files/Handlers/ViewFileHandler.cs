using Elm.Application.Contracts;
using Elm.Application.Contracts.Abstractions.Cache;
using Elm.Application.Contracts.Abstractions.Files;
using Elm.Application.Contracts.Features.Files.Queries;
using Elm.Application.Contracts.Features.Images.DTOs;
using MediatR;

namespace Elm.Application.Features.Files.Handlers
{
    public sealed class ViewFileHandler : IRequestHandler<ViewFileCommand, Result<ImageDto>>
    {
        private readonly IFileStorageService fileStorage;
        private readonly IGenericCacheService cacheService;
        public ViewFileHandler(IFileStorageService _fileStorage, IGenericCacheService _cacheService)
        {
            fileStorage = _fileStorage;
            cacheService = _cacheService;
        }
        public async Task<Result<ImageDto>> Handle(ViewFileCommand request, CancellationToken cancellationToken)
        {
            return await cacheService.GetOrSetAsync($"file_{request.fileName}",
                () => fileStorage.GetFileAsync(request.fileName, "Files"), TimeSpan.FromMinutes(15));
        }
    }
}
