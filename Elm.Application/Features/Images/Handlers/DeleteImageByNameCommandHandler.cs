using Elm.Application.Contracts;
using Elm.Application.Contracts.Abstractions.Cache;
using Elm.Application.Contracts.Abstractions.Files;
using Elm.Application.Contracts.Features.Images.Commands;
using MediatR;



namespace Elm.Application.Features.Images.Handlers
{
    public sealed class DeleteImageByNameCommandHandler : IRequestHandler<DeleteImageByNameCommand, Result<bool>>
    {
        private readonly IFileStorageService fileStorage;
        private readonly IGenericCacheService cacheService;
        public DeleteImageByNameCommandHandler(IFileStorageService _fileStorage, IGenericCacheService _cacheService)
        {
            fileStorage = _fileStorage;
            cacheService = _cacheService;
        }
        public async Task<Result<bool>> Handle(DeleteImageByNameCommand request, CancellationToken cancellationToken)
        {
            var result = await fileStorage.DeleteImageAsync(request.fileName);
            if (result.IsSuccess)
            {
                cacheService.Remove($"image_{request.fileName}");
                cacheService.Remove("University");
                cacheService.Remove("all_colleges");

            }
            return result;
        }
    }
}
