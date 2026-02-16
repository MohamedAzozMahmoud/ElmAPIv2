using Elm.Application.Contracts;
using Elm.Application.Contracts.Abstractions.Cache;
using Elm.Application.Contracts.Features.College.Commands;
using Elm.Application.Contracts.Features.College.DTOs;
using Elm.Application.Contracts.Repositories;
using Elm.Application.Mapper.Elm.Application.Mappers;
using MediatR;

namespace Elm.Application.Features.College.Handlers
{
    public sealed class AddCollegeHandler : IRequestHandler<AddCollegeCommand, Result<CollegeDto>>
    {
        private readonly ICollegeRepository repository;
        private readonly MappingProvider _mapping;
        private readonly IGenericCacheService _cacheService;
        public AddCollegeHandler(ICollegeRepository _repository, MappingProvider mapping, IGenericCacheService cacheService)
        {
            repository = _repository;
            _mapping = mapping;
            _cacheService = cacheService;
        }
        public async Task<Result<CollegeDto>> Handle(AddCollegeCommand request, CancellationToken cancellationToken)
        {
            var college = new Domain.Entities.College
            {
                Name = request.name,
                UniversityId = request.UniversityId
            };
            var addedCollege = await repository.AddAsync(college);
            if (addedCollege != null)
            {
                var collegeDto = _mapping.MapToDto(addedCollege);
                _cacheService.Remove($"college_{addedCollege.Id}");
                _cacheService.Remove("all_colleges");
                return Result<CollegeDto>.Success(collegeDto);
            }
            return Result<CollegeDto>.Failure("Failed to add college");
        }
    }
}
