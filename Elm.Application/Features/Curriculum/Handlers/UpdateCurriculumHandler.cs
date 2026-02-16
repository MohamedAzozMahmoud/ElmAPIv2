using Elm.Application.Contracts;
using Elm.Application.Contracts.Abstractions.Cache;
using Elm.Application.Contracts.Features.Curriculum.Commands;
using Elm.Application.Contracts.Features.Curriculum.DTOs;
using Elm.Application.Contracts.Repositories;
using Elm.Application.Mapper.Elm.Application.Mappers;
using MediatR;

namespace Elm.Application.Features.QuestionsBank.Handlers
{
    public sealed class UpdateCurriculumHandler : IRequestHandler<UpdateCurriculumCommand, Result<CurriculumDto>>
    {
        private readonly ICurriculumRepository repository;
        private readonly MappingProvider _mapping;
        private readonly IGenericCacheService _cacheService;
        public UpdateCurriculumHandler(ICurriculumRepository repository, MappingProvider mapping, IGenericCacheService cacheService)
        {
            this.repository = repository;
            _mapping = mapping;
            _cacheService = cacheService;
        }
        public async Task<Result<CurriculumDto>> Handle(UpdateCurriculumCommand request, CancellationToken cancellationToken)
        {
            var existingCurriculum = await _cacheService.GetOrSetAsync($"curriculum_{request.Id}",
                        () => repository.GetByIdAsync(request.Id));
            if (existingCurriculum == null)
            {
                return Result<CurriculumDto>.Failure("Curriculum not found", 404);
            }
            _cacheService.Remove($"curriculum_{request.Id}"); // Invalidate cache
            _cacheService.Remove($"curriculums_{existingCurriculum.YearId}_{existingCurriculum.DepartmentId}"); // Invalidate related cache
            existingCurriculum.SubjectId = request.SubjectId;
            existingCurriculum.YearId = request.YearId;
            existingCurriculum.DepartmentId = request.DepartmentId;
            existingCurriculum.DoctorId = request.DoctorId;
            var updatedCurriculum = await repository.UpdateAsync(existingCurriculum);
            if (!updatedCurriculum)
            {
                return Result<CurriculumDto>.Failure("Failed to update curriculum");
            }
            var curriculumDto = _mapping.MapToDto(existingCurriculum);
            if (curriculumDto == null)
            {
                return Result<CurriculumDto>.Failure("Failed to map curriculum to DTO");
            }
            return Result<CurriculumDto>.Success(curriculumDto);
        }
    }
}
