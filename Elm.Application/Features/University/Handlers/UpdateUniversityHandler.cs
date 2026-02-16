using Elm.Application.Contracts;
using Elm.Application.Contracts.Abstractions.Cache;
using Elm.Application.Contracts.Features.University.Commands;
using Elm.Application.Contracts.Repositories;
using MediatR;

namespace Elm.Application.Features.University.Handlers
{
    public sealed class UpdateUniversityHandler : IRequestHandler<UpdateUniversityCommand, Result<bool>>
    {
        private readonly IGenericRepository<Domain.Entities.University> repository;
        private readonly IGenericCacheService cacheService;
        public UpdateUniversityHandler(IGenericRepository<Domain.Entities.University> repository, IGenericCacheService cacheService)
        {
            this.repository = repository;
            this.cacheService = cacheService;
        }
        public async Task<Result<bool>> Handle(UpdateUniversityCommand request, CancellationToken cancellationToken)
        {
            var university = await cacheService.GetOrSetAsync("University",
                                  () => repository.GetByIdAsync(request.Id), TimeSpan.FromMinutes(20));
            if (university == null)
            {
                return Result<bool>.Failure("University not found", 404);
            }
            university.Name = request.Name;
            var result = await repository.UpdateAsync(university);
            if (!result)
            {
                return Result<bool>.Failure("Failed to update university", 500);
            }
            cacheService.Remove("University");
            return Result<bool>.Success(result);
        }
    }
}
