using Elm.Application.Contracts;
using Elm.Application.Contracts.Abstractions.Cache;
using Elm.Application.Contracts.Features.Curriculum.Commands;
using Elm.Application.Contracts.Features.Curriculum.DTOs;
using Elm.Application.Contracts.Repositories;
using Elm.Application.Mapper.Elm.Application.Mappers;
using MediatR;

namespace Elm.Application.Features.QuestionsBank.Handlers
{
    public sealed class AddCurriculumHandler : IRequestHandler<AddCurriculumCommand, Result<CurriculumDto>>
    {
        private readonly ICurriculumRepository repository;
        private readonly MappingProvider _mapping;
        private readonly IGenericCacheService _cacheService;

        public AddCurriculumHandler(ICurriculumRepository repository, MappingProvider mapping, IGenericCacheService cacheService)
        {
            this.repository = repository;
            _mapping = mapping;
            _cacheService = cacheService;
        }
        public async Task<Result<CurriculumDto>> Handle(AddCurriculumCommand request, CancellationToken cancellationToken)
        {
            var curriculum = new Elm.Domain.Entities.Curriculum
            {
                SubjectId = request.SubjectId,
                YearId = request.YearId,
                DepartmentId = request.DepartmentId,
                DoctorId = request.DoctorId
            };
            var addedCurriculum = await repository.AddAsync(curriculum);
            if (addedCurriculum == null)
                return Result<CurriculumDto>.Failure("Failed to add curriculum");
            var curriculumDto = _mapping.MapToDto(addedCurriculum);

            _cacheService.Remove($"curriculums_{request.YearId}_{request.DepartmentId}"); // Invalidate cache for curriculum list
            return Result<CurriculumDto>.Success(curriculumDto);
        }
    }
}
