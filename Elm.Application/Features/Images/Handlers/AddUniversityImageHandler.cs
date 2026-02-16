using Elm.Application.Contracts;
using Elm.Application.Contracts.Abstractions.Cache;
using Elm.Application.Contracts.Abstractions.Files;
using Elm.Application.Contracts.Features.Images.Commands;
using Elm.Application.Contracts.Repositories;
using Elm.Domain.Entities;
using MediatR;

namespace Elm.Application.Features.Images.Handlers
{
    public sealed class AddUniversityImageHandler : IRequestHandler<AddUniversityImageCommand, Result<bool>>
    {
        private readonly IGenericRepository<Elm.Domain.Entities.University> genericRepository;
        private readonly IFileStorageService fileStorageService;
        private readonly IGenericCacheService _cacheService;
        public AddUniversityImageHandler(IGenericRepository<Elm.Domain.Entities.University> genericRepository, IFileStorageService fileStorageService, IGenericCacheService cacheService)
        {
            this.genericRepository = genericRepository;
            this.fileStorageService = fileStorageService;
            _cacheService = cacheService;
        }
        public async Task<Result<bool>> Handle(AddUniversityImageCommand request, CancellationToken cancellationToken)
        {
            var imagePath = await fileStorageService.UploadImageAsync(request.File, "Images");
            if (!imagePath.IsSuccess || imagePath.Data == null)
            {
                return Result<bool>.Failure("Image upload failed", 500);
            }
            var university = await _cacheService.GetOrSetAsync("University",
                                  () => genericRepository.GetByIdAsync(request.id));
            if (university == null)
            {
                return Result<bool>.Failure("University not found", 404);
            }
            university.Img = new Image
            {
                Name = request.File.FileName,
                ContentType = request.File.ContentType,
                StorageName = Path.GetFileName(imagePath.Data)
            };
            var result = await genericRepository.UpdateAsync(university);
            if (!result)
            {
                return Result<bool>.Failure("Failed to update university image", 500);
            }
            _cacheService.Remove("University");
            return Result<bool>.Success(true);
        }
    }
}
