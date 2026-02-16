using Elm.Application.Contracts;
using Elm.Application.Contracts.Abstractions.Cache;
using Elm.Application.Contracts.Abstractions.Files;
using Elm.Application.Contracts.Features.Curriculum.DTOs;
using Elm.Application.Contracts.Features.Curriculum.Queries;
using Elm.Application.Contracts.Repositories;
using MediatR;

namespace Elm.Application.Features.QuestionsBank.Handlers
{
    public sealed class GetCurriculumByDoctorIdHandler : IRequestHandler<GetCurriculumByDoctorIdQuery, Result<List<GetCurriculumDto>>>
    {
        private readonly ICurriculumRepository repository;
        private readonly IDoctorRepository doctorRepsitory;
        private readonly IGenericCacheService _cacheService;
        public GetCurriculumByDoctorIdHandler(ICurriculumRepository repository, IDoctorRepository doctorRepsitory, IGenericCacheService cacheService)
        {
            this.repository = repository;
            this.doctorRepsitory = doctorRepsitory;
            _cacheService = cacheService;
        }
        public async Task<Result<List<GetCurriculumDto>>> Handle(GetCurriculumByDoctorIdQuery request, CancellationToken cancellationToken)
        {
            var doctor = await _cacheService.GetOrSetAsync($"doctor_{request.UserId}",
                        () => doctorRepsitory.GetDoctor(request.UserId));
            if (doctor == null)
            {
                return Result<List<GetCurriculumDto>>.Failure("Doctor not found");
            }
            var curriculums = await _cacheService.GetOrSetAsync($"curriculums_{doctor.Id}",
                        () => repository.GetByDoctorIdAsync(doctor.Id), TimeSpan.FromMinutes(5));
            if (curriculums == null)
            {
                return Result<List<GetCurriculumDto>>.Failure("Curriculum not found");
            }
            return Result<List<GetCurriculumDto>>.Success(curriculums);
        }
    }
}
