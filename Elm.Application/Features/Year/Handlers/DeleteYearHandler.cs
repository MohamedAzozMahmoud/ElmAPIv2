using Elm.Application.Contracts;
using Elm.Application.Contracts.Abstractions.Cache;
using Elm.Application.Contracts.Features.Year.Commands;
using Elm.Application.Contracts.Repositories;
using MediatR;

namespace Elm.Application.Features.Year.Handlers
{
    public sealed class DeleteYearHandler : IRequestHandler<DeleteYearCommand, Result<bool>>
    {
        private readonly IYearRepository yearRepository;
        private readonly IGenericCacheService cacheService;
        public DeleteYearHandler(IYearRepository yearRepository, IGenericCacheService cacheService)
        {
            this.yearRepository = yearRepository;
            this.cacheService = cacheService;
        }
        public async Task<Result<bool>> Handle(DeleteYearCommand request, CancellationToken cancellationToken)
        {
            var existingYear = await cacheService.GetOrSetAsync($"year_{request.Id}",
                () => yearRepository.GetByIdAsync(request.Id));
            if (existingYear == null)
            {
                return Result<bool>.Failure("Year not found", 404);
            }
            var result = await yearRepository.DeleteAsync(existingYear);
            if (result)
            {
                cacheService.Remove($"year_{request.Id}");
                cacheService.Remove($"GetAllYear_{existingYear.CollegeId}"); // Invalidate cache for list
                return Result<bool>.Success(true);
            }
            return Result<bool>.Failure("Not deleted");
        }
    }
}
