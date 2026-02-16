using Elm.Application.Contracts;
using Elm.Application.Contracts.Abstractions.Cache;
using Elm.Application.Contracts.Features.Department.Commands;
using Elm.Application.Contracts.Repositories;
using MediatR;

namespace Elm.Application.Features.Department.Handlers
{
    public sealed class UpdateDepartmentHandler : IRequestHandler<UpdateDepartmentCommand, Result<bool>>
    {
        private readonly IDepartmentRepository repository;
        private readonly IGenericCacheService _cacheService;
        public UpdateDepartmentHandler(IDepartmentRepository repository, IGenericCacheService cacheService)
        {
            this.repository = repository;
            this._cacheService = cacheService;
        }
        public async Task<Result<bool>> Handle(UpdateDepartmentCommand request, CancellationToken cancellationToken)
        {
            var department = await _cacheService.GetOrSetAsync($"department_{request.Id}",
                () => repository.GetByIdAsync(request.Id));
            if (department == null)
            {
                return Result<bool>.Failure("لا يوجد قسم بهذا المعرف", 404);
            }
            department.Name = request.Name;
            department.IsPaid = request.IsPaid;
            department.Type = request.Type;
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
