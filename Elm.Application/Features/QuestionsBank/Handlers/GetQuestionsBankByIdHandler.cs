using Elm.Application.Contracts;
using Elm.Application.Contracts.Abstractions.Cache;
using Elm.Application.Contracts.Features.QuestionsBank.DTOs;
using Elm.Application.Contracts.Features.QuestionsBank.Queries;
using Elm.Application.Contracts.Repositories;
using Elm.Application.Mapper.Elm.Application.Mappers;
using MediatR;

namespace Elm.Application.Features.QuestionsBank.Handlers
{
    public sealed class GetQuestionsBankByIdHandler : IRequestHandler<GetQuestionsBankByIdQuery, Result<QuestionsBankDto>>
    {
        private readonly IGenericRepository<Elm.Domain.Entities.QuestionsBank> repository;
        private readonly MappingProvider mapping;
        private readonly IGenericCacheService cacheService;
        public GetQuestionsBankByIdHandler(IGenericRepository<Elm.Domain.Entities.QuestionsBank> repository, MappingProvider mapping, IGenericCacheService cacheService)
        {
            this.repository = repository;
            this.mapping = mapping;
            this.cacheService = cacheService;
        }
        public async Task<Result<QuestionsBankDto>> Handle(GetQuestionsBankByIdQuery request, CancellationToken cancellationToken)
        {
            var questionsBank = await cacheService.GetOrSetAsync($"questionsBank_{request.id}",
                () => repository.GetByIdAsync(request.id));
            if (questionsBank == null)
            {
                return Result<QuestionsBankDto>.Failure("Questions Bank not found", 404);
            }
            var questionsBankDto = mapping.MapToDto(questionsBank);
            return Result<QuestionsBankDto>.Success(questionsBankDto);
        }
    }
}
