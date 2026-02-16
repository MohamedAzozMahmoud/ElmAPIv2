using Elm.Application.Contracts;
using Elm.Application.Contracts.Abstractions.Cache;
using Elm.Application.Contracts.Abstractions.Files;
using Elm.Application.Contracts.Features.Curriculum.Commands;
using Elm.Application.Contracts.Repositories;
using MediatR;

namespace Elm.Application.Features.QuestionsBank.Handlers
{
    public sealed class DeleteCurriculumHandler : IRequestHandler<DeleteCurriculumCommand, Result<bool>>
    {
        private readonly ICurriculumRepository repository;
        private readonly IFileStorageService fileStorage;
        private readonly IGenericCacheService _cacheService;
        public DeleteCurriculumHandler(ICurriculumRepository repository, IFileStorageService fileStorage, IGenericCacheService cacheService)
        {
            this.repository = repository;
            this.fileStorage = fileStorage;
            _cacheService = cacheService;
        }
        public async Task<Result<bool>> Handle(DeleteCurriculumCommand request, CancellationToken cancellationToken)
        {
            var result = await _cacheService.GetOrSetAsync($"curriculums_{request.Id}",
                () => repository.GetByIdAsync(request.Id));
            if (result == null)
            {
                return Result<bool>.Failure("Curriculum not found", 404);
            }
            var resultDelete = await fileStorage.DeleteAllFilesByCurriculumId(request.Id);
            if (!resultDelete.IsSuccess && resultDelete.StatusCode != 404)
            {
                return Result<bool>.Failure("Failed to delete associated files.", 500);
            }
            await repository.DeleteAsync(result);
            _cacheService.Remove($"curriculums_{request.Id}");
            _cacheService.Remove($"curriculums_{result.YearId}_{result.DepartmentId}");
            return Result<bool>.Success(true);
        }
    }
}
