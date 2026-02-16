using Elm.Application.Contracts;
using Elm.Application.Contracts.Abstractions.Cache;
using Elm.Application.Contracts.Features.Year.DTOs;
using Elm.Application.Contracts.Features.Year.Queries;
using Elm.Application.Contracts.Repositories;
using MediatR;

namespace Elm.Application.Features.Year.Handlers
{
    public sealed class GetYearByIdHandler : IRequestHandler<GetYearByIdQuery, Result<GetYearDto>>
    {
        private readonly IYearRepository yearRepository;
        private readonly IGenericCacheService cacheService;
        public GetYearByIdHandler(IYearRepository yearRepository, IGenericCacheService cacheService)
        {
            this.yearRepository = yearRepository;
            this.cacheService = cacheService;
        }
        public async Task<Result<GetYearDto>> Handle(GetYearByIdQuery request, CancellationToken cancellationToken)
        {
            var year = await cacheService.GetOrSetAsync($"year_{request.Id}",
                () => yearRepository.GetYearByIdAsync(request.Id));
            if (year == null)
            {
                return Result<GetYearDto>.Failure("Year not found");
            }
            return Result<GetYearDto>.Success(year);
        }
    }
}
