using Elm.Application.Contracts;
using Elm.Application.Contracts.Abstractions.Cache;
using Elm.Application.Contracts.Abstractions.Files;
using Elm.Application.Contracts.Features.Files.DTOs;
using Elm.Application.Contracts.Features.Files.Queries;
using MediatR;

namespace Elm.Application.Features.Files.Handlers
{
    public sealed class GetAllFilesByCurriculumIdHandler : IRequestHandler<GetAllFilesByCurriculumIdQuery, Result<List<FileView>>>
    {
        private readonly IFileStorageService fileStorage;
        private readonly IGenericCacheService cacheService;
        public GetAllFilesByCurriculumIdHandler(IFileStorageService _fileStorage, IGenericCacheService _cacheService)
        {
            fileStorage = _fileStorage;
            cacheService = _cacheService;
        }
        public async Task<Result<List<FileView>>> Handle(GetAllFilesByCurriculumIdQuery request, CancellationToken cancellationToken)
        {
            return await cacheService.GetOrSetAsync($"files_{request.curriculumId}",
                () => fileStorage.GetAllFilesByCurriculumIdAsync(request.curriculumId, "Files"));
        }
    }
}
