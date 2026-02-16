using Elm.Application.Contracts;
using Elm.Application.Contracts.Abstractions.Cache;
using Elm.Application.Contracts.Features.Department.Commands;
using Elm.Application.Contracts.Repositories;
using MediatR;

namespace Elm.Application.Features.Department.Handlers
{
    public sealed class DeleteDepartmentHandler : IRequestHandler<DeleteDepartmentCommand, Result<bool>>
    {
        private readonly IDepartmentRepository repository;
        private readonly IGenericCacheService _cacheService;
        public DeleteDepartmentHandler(IDepartmentRepository repository, IGenericCacheService cacheService)
        {
            this.repository = repository;
            this._cacheService = cacheService;
        }
        public async Task<Result<bool>> Handle(DeleteDepartmentCommand request, CancellationToken cancellationToken)
        {
            var existingDepartment = await _cacheService.GetOrSetAsync($"department_{request.Id}",
                () => repository.GetByIdAsync(request.Id));
            if (existingDepartment == null)
            {
                return Result<bool>.NotFound($"Department with ID {request.Id} not found");
            }
            var result = await repository.DeleteAsync(existingDepartment);
            if (!result)
            {
                return Result<bool>.Failure("Failed to delete department", 500);
            }
            _cacheService.Remove($"department_{request.Id}"); // Invalidate cache
            _cacheService.Remove("departments_list"); // Invalidate cache for list
            return Result<bool>.Success(true);
        }
    }
}
