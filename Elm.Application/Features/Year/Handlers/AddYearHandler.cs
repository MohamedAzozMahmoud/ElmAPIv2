using Elm.Application.Contracts;
using Elm.Application.Contracts.Abstractions.Cache;
using Elm.Application.Contracts.Features.Year.Commands;
using Elm.Application.Contracts.Features.Year.DTOs;
using Elm.Application.Contracts.Repositories;
using Elm.Application.Mapper.Elm.Application.Mappers;
using MediatR;

namespace Elm.Application.Features.Year.Handlers
{
    public sealed class AddYearHandler : IRequestHandler<AddYearCommand, Result<YearDto>>
    {
        private readonly IYearRepository yearRepository;
        private readonly MappingProvider mapping;
        private readonly IGenericCacheService cacheService;
        public AddYearHandler(IYearRepository yearRepository, MappingProvider mapping, IGenericCacheService cacheService)
        {
            this.yearRepository = yearRepository;
            this.mapping = mapping;
            this.cacheService = cacheService;
        }
        public async Task<Result<YearDto>> Handle(AddYearCommand request, CancellationToken cancellationToken)
        {
            var year = new Domain.Entities.Year
            {
                Name = request.name,
                CollegeId = request.collegeId
            };
            var addedYear = await yearRepository.AddAsync(year);
            if (addedYear != null)
            {
                var yearDto = mapping.MapToDto(addedYear);
                cacheService.Remove($"GetAllYear_{request.collegeId}");
                return Result<YearDto>.Success(yearDto);
            }
            return Result<YearDto>.Failure("Failed to add year");
        }
    }
}
