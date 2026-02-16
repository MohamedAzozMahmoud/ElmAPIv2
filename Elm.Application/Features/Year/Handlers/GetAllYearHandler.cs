using Elm.Application.Contracts;
using Elm.Application.Contracts.Abstractions.Cache;
using Elm.Application.Contracts.Features.Year.DTOs;
using Elm.Application.Contracts.Features.Year.Queries;
using Elm.Application.Contracts.Repositories;
using MediatR;

namespace Elm.Application.Features.Year.Handlers
{
    public sealed class GetAllYearHandler : IRequestHandler<GetAllYearQuery, Result<List<GetYearDto>>>
    {
        private readonly IYearRepository yearRepository;
        private readonly IGenericCacheService cacheService;
        public GetAllYearHandler(IYearRepository yearRepository, IGenericCacheService cacheService)
        {
            this.yearRepository = yearRepository;
            this.cacheService = cacheService;
        }

        public async Task<Result<List<GetYearDto>>> Handle(GetAllYearQuery request, CancellationToken cancellationToken)
        {
            var years = await cacheService.GetOrSetAsync($"GetAllYear_{request.collegeId}",
                () => yearRepository.GetAllYearInCollegeAsync(request.collegeId));
            return Result<List<GetYearDto>>.Success(years);
        }
    }
}
