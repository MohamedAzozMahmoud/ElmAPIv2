using Elm.Application.Contracts;
using Elm.Application.Contracts.Abstractions.Cache;
using Elm.Application.Contracts.Abstractions.Files;
using Elm.Application.Contracts.Features.Files.Commands;
using Elm.Application.Contracts.Repositories;
using MediatR;

namespace Elm.Application.Features.Files.Handlers
{
    // ApprovedFileCommand
    public sealed class RatingFileHandler : IRequestHandler<RatingFileCommand, Result<bool>>
    {
        private readonly IFileStorageService fileStorage;
        private readonly IGenericRepository<Domain.Entities.Files> filesRepository;
        private readonly IDoctorRepository doctorsRepository;
        private readonly IGenericCacheService cacheService;
        public RatingFileHandler(IFileStorageService fileStorage, IGenericRepository<Domain.Entities.Files> filesRepository,
                IDoctorRepository doctorsRepository, IGenericCacheService cacheService)
        {
            this.fileStorage = fileStorage;
            this.filesRepository = filesRepository;
            this.doctorsRepository = doctorsRepository;
            this.cacheService = cacheService;
        }
        public async Task<Result<bool>> Handle(RatingFileCommand request, CancellationToken cancellationToken)
        {
            var file = await cacheService.GetOrSetAsync($"file_{request.fileId}",
                () => filesRepository.GetByIdAsync(request.fileId));
            if (file == null)
            {
                return Result<bool>.Failure("File not found", 404);
            }
            var doctor = await cacheService.GetOrSetAsync($"doctor_{request.userId}",
                () => doctorsRepository.GetDoctor(request.userId));
            if (doctor == null)
            {
                return Result<bool>.Failure("Doctor not found", 404);
            }
            var result = await fileStorage.RatingFileAsync(file.CurriculumId, doctor.Id, request.fileId, request.rating, request.comment);
            if (!result.IsSuccess)
            {
                return Result<bool>.Failure("Failed to rate file", 500);
            }
            cacheService.Remove($"file_{request.fileId}");
            return Result<bool>.Success(true);
        }
    }
}
