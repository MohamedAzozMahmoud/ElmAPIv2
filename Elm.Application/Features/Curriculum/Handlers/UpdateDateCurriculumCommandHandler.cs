using Elm.Application.Contracts;
using Elm.Application.Contracts.Abstractions.Cache;
using Elm.Application.Contracts.Features.Curriculum.Commands;
using Elm.Application.Contracts.Repositories;
using MediatR;

namespace Elm.Application.Features.QuestionsBank.Handlers
{
    public sealed class UpdateDateCurriculumCommandHandler : IRequestHandler<UpdateDateCurriculumCommand, Result<bool>>
    {
        private readonly ICurriculumRepository repository;
        private readonly IGenericCacheService _cacheService;
        public UpdateDateCurriculumCommandHandler(ICurriculumRepository repository, IGenericCacheService cacheService)
        {
            this.repository = repository;
            _cacheService = cacheService;
        }
        public async Task<Result<bool>> Handle(UpdateDateCurriculumCommand request, CancellationToken cancellationToken)
        {
            var existingCurriculum = await _cacheService.GetOrSetAsync($"curriculum_{request.Id}"
                , () => repository.GetByIdAsync(request.Id));
            if (existingCurriculum == null)
            {
                return Result<bool>.Failure("لا يوجد منهج دراسي", 404);
            }
            existingCurriculum.StartMonth = request.StartMonth;
            existingCurriculum.EndMonth = request.EndMonth;
            await repository.UpdateAsync(existingCurriculum);
            _cacheService.Remove($"curriculum_{request.Id}");
            _cacheService.Remove($"curriculums_{existingCurriculum.YearId}_{existingCurriculum.DepartmentId}"); // Invalidate related cache
            await _cacheService.GetOrSetAsync($"curriculum_{request.Id}", () => Task.FromResult(existingCurriculum)); // Update cache
            return Result<bool>.Success(true);
        }
    }
}
