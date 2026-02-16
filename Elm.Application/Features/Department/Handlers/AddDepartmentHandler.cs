using Elm.Application.Contracts;
using Elm.Application.Contracts.Abstractions.Cache;
using Elm.Application.Contracts.Features.Department.Commands;
using Elm.Application.Contracts.Features.Department.DTOs;
using Elm.Application.Contracts.Repositories;
using Elm.Application.Mapper.Elm.Application.Mappers;
using MediatR;

namespace Elm.Application.Features.Department.Handlers
{
    public sealed class AddDepartmentHandler : IRequestHandler<AddDepartmentCommand, Result<DepartmentDto>>
    {
        private readonly IDepartmentRepository repository;
        private readonly MappingProvider _mapping;
        private readonly IGenericCacheService cacheService;

        public AddDepartmentHandler(IDepartmentRepository repository, MappingProvider mapping, IGenericCacheService cacheService)
        {
            this.repository = repository;
            this._mapping = mapping;
            this.cacheService = cacheService;
        }
        public async Task<Result<DepartmentDto>> Handle(AddDepartmentCommand request, CancellationToken cancellationToken)
        {
            var department = new Elm.Domain.Entities.Department
            {
                Name = request.Name,
                IsPaid = request.IsPaid,
                CollegeId = request.collegeId
            };
            var addedDepartment = await repository.AddAsync(department);
            var departmentDto = _mapping.MapToDto(addedDepartment);
            if (departmentDto is null)
            {
                return Result<DepartmentDto>.Failure("Failed to add department");
            }
            cacheService.Remove("departments_list");
            return Result<DepartmentDto>.Success(departmentDto);
        }
    }
}
