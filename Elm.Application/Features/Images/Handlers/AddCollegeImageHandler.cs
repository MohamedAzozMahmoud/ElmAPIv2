using Elm.Application.Contracts;
using Elm.Application.Contracts.Abstractions.Cache;
using Elm.Application.Contracts.Abstractions.Files;
using Elm.Application.Contracts.Features.Images.Commands;
using Elm.Application.Contracts.Repositories;
using Elm.Domain.Entities;
using MediatR;


namespace Elm.Application.Features.Images.Handlers
{
    public sealed class AddCollegeImageHandler : IRequestHandler<AddCollegeImageCommand, Result<bool>>
    {
        private readonly ICollegeRepository genericRepository;
        private readonly IFileStorageService fileStorageService;
        private readonly IGenericCacheService _cacheService;
        public AddCollegeImageHandler(ICollegeRepository genericRepository, IFileStorageService fileStorageService, IGenericCacheService cacheService)
        {
            this.genericRepository = genericRepository;
            this.fileStorageService = fileStorageService;
            _cacheService = cacheService;
        }
        public async Task<Result<bool>> Handle(AddCollegeImageCommand request, CancellationToken cancellationToken)
        {
            var imagePath = await fileStorageService.UploadImageAsync(request.File, "Images");
            if (!imagePath.IsSuccess || imagePath.Data == null)
            {
                return Result<bool>.Failure("Image upload failed", 500);
            }
            var college = await _cacheService.GetOrSetAsync($"college_{request.id}",
                                  () => genericRepository.GetByIdAsync(request.id));
            if (college == null)
            {
                return Result<bool>.Failure("College not found", 404);
            }
            college.Img = new Image
            {
                Name = request.File.FileName,
                ContentType = request.File.ContentType,
                StorageName = Path.GetFileName(imagePath.Data)

            };
            var result = await genericRepository.UpdateAsync(college);
            if (!result)
            {
                return Result<bool>.Failure("Failed to update college image", 500);
            }
            _cacheService.Remove($"image_{college.Img.StorageName}");
            _cacheService.Remove("all_colleges");
            return Result<bool>.Success(true);
        }
    }
}
