using Elm.Application.Contracts;
using Elm.Application.Contracts.Abstractions.Cache;
using Elm.Application.Contracts.Features.Year.Commands;
using Elm.Application.Contracts.Repositories;
using MediatR;

namespace Elm.Application.Features.Year.Handlers
{
    public sealed class UpdateYearHandler : IRequestHandler<UpdateYearCommand, Result<bool>>
    {
        private readonly IYearRepository yearRepository;
        private readonly IGenericCacheService cacheService;

        public UpdateYearHandler(IYearRepository yearRepository, IGenericCacheService cacheService)
        {
            this.yearRepository = yearRepository;
            this.cacheService = cacheService;
        }
        public async Task<Result<bool>> Handle(UpdateYearCommand request, CancellationToken cancellationToken)
        {
            var year = await cacheService.GetOrSetAsync($"year_{request.Id}",
                () => yearRepository.GetByIdAsync(request.Id));
            if (year == null)
            {
                return Result<bool>.NotFound("Year not found");
            }
            year.Name = request.Name;
            var updatedYear = await yearRepository.UpdateAsync(year);
            if (updatedYear)
            {
                cacheService.Remove($"GetAllYear_{year.CollegeId}");
                cacheService.Remove($"year_{request.Id}");
                return Result<bool>.Success(true);
            }
            return Result<bool>.Failure("Failed to update year");
        }
    }
}
