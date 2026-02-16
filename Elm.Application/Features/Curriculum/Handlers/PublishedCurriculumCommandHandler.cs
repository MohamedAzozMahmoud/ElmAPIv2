using Elm.Application.Contracts;
using Elm.Application.Contracts.Abstractions.Cache;
using Elm.Application.Contracts.Features.Curriculum.Commands;
using Elm.Application.Contracts.Repositories;
using MediatR;

namespace Elm.Application.Features.QuestionsBank.Handlers
{
    public sealed class PublishedCurriculumCommandHandler : IRequestHandler<PublishedCurriculumCommand, Result<bool>>
    {
        private readonly ICurriculumRepository repository;
        private readonly IGenericCacheService _cacheService;
        public PublishedCurriculumCommandHandler(ICurriculumRepository repository, IGenericCacheService cacheService)
        {
            this.repository = repository;
            _cacheService = cacheService;
        }
        public async Task<Result<bool>> Handle(PublishedCurriculumCommand request, CancellationToken cancellationToken)
        {
            var existingCurriculum = await repository.GetByIdAsync(request.Id);
            if (existingCurriculum == null)
            {
                return Result<bool>.Failure("لا يوجد منهج دراسي", 404);
            }
            existingCurriculum.IsPublished = !existingCurriculum.IsPublished; // Toggle the published state
            var result = await repository.UpdateAsync(existingCurriculum);
            if (!result)
            {
                return Result<bool>.Failure("فشل في تحديث حالة النشر");
            }
            _cacheService.Remove($"curriculum_{existingCurriculum.Id}"); // Invalidate cache for the specific curriculum
            _cacheService.Remove($"curriculums_{existingCurriculum.YearId}_{existingCurriculum.DepartmentId}"); // Invalidate related cache
            return Result<bool>.Success(true);
        }
    }
}
