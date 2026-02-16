using Elm.Application.Contracts;
using Elm.Application.Contracts.Abstractions.Cache;
using Elm.Application.Contracts.Features.Department.Commands;
using Elm.Application.Contracts.Repositories;
using MediatR;

namespace Elm.Application.Features.Department.Handlers
{
    public sealed class PublishedDepartmentHandler : IRequestHandler<PublishedDepartmentCommand, Result<bool>>
    {
        private readonly IDepartmentRepository repository;
        private readonly IGenericCacheService _cacheService;
        public PublishedDepartmentHandler(IDepartmentRepository repository, IGenericCacheService cacheService)
        {
            this.repository = repository;
            this._cacheService = cacheService;
        }
        public async Task<Result<bool>> Handle(PublishedDepartmentCommand request, CancellationToken cancellationToken)
        {
            var department = await _cacheService.GetOrSetAsync($"department_{request.Id}",
                () => repository.GetByIdAsync(request.Id));
            if (department == null)
            {
                return Result<bool>.Failure("لا يوجد قسم بهذا المعرف", 404);
            }
            department.IsPublished = !department.IsPublished;
            var result = await repository.UpdateAsync(department);
            if (!result)
            {
                return Result<bool>.Failure("فشل في تحديث القسم", 500);
            }
            _cacheService.Remove($"department_{request.Id}");
            _cacheService.Remove("departments_list");
            return Result<bool>.Success(true);
        }
    }
}
