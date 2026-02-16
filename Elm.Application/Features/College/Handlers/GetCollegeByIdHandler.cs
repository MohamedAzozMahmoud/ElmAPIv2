using Elm.Application.Contracts;
using Elm.Application.Contracts.Abstractions.Cache;
using Elm.Application.Contracts.Features.College.DTOs;
using Elm.Application.Contracts.Features.College.Queries;
using Elm.Application.Contracts.Repositories;
using MediatR;

namespace Elm.Application.Features.College.Handlers
{
    public sealed class GetCollegeByIdHandler : IRequestHandler<GetCollegeByIdQuery, Result<CollegeDto>>
    {
        private readonly ICollegeRepository _collegeRepository;
        private readonly IGenericCacheService _cacheService;
        public GetCollegeByIdHandler(ICollegeRepository collegeRepository, IGenericCacheService cacheService)
        {
            _collegeRepository = collegeRepository;
            _cacheService = cacheService;
        }
        public async Task<Result<CollegeDto>> Handle(GetCollegeByIdQuery request, CancellationToken cancellationToken)
        {
            var collegeDto = await _cacheService.GetOrSetAsync($"college_{request.Id}",
                                  () => _collegeRepository.GetCollegeByIdAsync(request.Id));
            if (collegeDto == null)
            {
                return Result<CollegeDto>.Failure("College not found.");
            }
            return Result<CollegeDto>.Success(collegeDto);

        }
    }
}
