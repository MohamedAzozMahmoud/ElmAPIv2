using Elm.Application.Contracts;
using Elm.Application.Contracts.Abstractions.Cache;
using Elm.Application.Contracts.Abstractions.Files;
using Elm.Application.Contracts.Features.Curriculum.DTOs;
using Elm.Application.Contracts.Features.Curriculum.Queries;
using Elm.Application.Contracts.Repositories;
using MediatR;

namespace Elm.Application.Features.QuestionsBank.Handlers
{
    public sealed class GerCurriculumByStudentIdHandler : IRequestHandler<GetCurriculumByStudentIdQuery, Result<List<GetCurriculumDto>>>
    {
        private readonly ICurriculumRepository repository;
        private readonly IStudentRepository studentRepository;
        private readonly IGenericCacheService _cacheService;
        public GerCurriculumByStudentIdHandler(ICurriculumRepository repository, IStudentRepository studentRepository, IGenericCacheService cacheService)
        {
            this.repository = repository;
            this.studentRepository = studentRepository;
            _cacheService = cacheService;
        }
        public async Task<Result<List<GetCurriculumDto>>> Handle(GetCurriculumByStudentIdQuery request, CancellationToken cancellationToken)
        {
            var student = await _cacheService.GetOrSetAsync($"student_{request.UserId}",
                        () => studentRepository.GetLeader(request.UserId));
            if (student == null)
            {
                return Result<List<GetCurriculumDto>>.Failure("Student not found");
            }
            var curriculums = await _cacheService.GetOrSetAsync($"curriculums_{student.YearId}_{student.DepartmentId}",
                        () => repository.GetAllCurriculumsByDeptIdAndYearIdAsync(student.DepartmentId, student.YearId), TimeSpan.FromMinutes(15));
            if (curriculums == null)
            {
                return Result<List<GetCurriculumDto>>.Failure("Curriculum not found");
            }
            return Result<List<GetCurriculumDto>>.Success(curriculums);
        }
    }
}
