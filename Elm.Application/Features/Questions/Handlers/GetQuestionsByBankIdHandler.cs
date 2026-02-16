using Elm.Application.Contracts;
using Elm.Application.Contracts.Abstractions.Cache;
using Elm.Application.Contracts.Features.Questions.DTOs;
using Elm.Application.Contracts.Features.Questions.Queries;
using Elm.Application.Contracts.Repositories;
using MediatR;

namespace Elm.Application.Features.Questions.Handlers
{
    public sealed class GetQuestionsByBankIdHandler : IRequestHandler<GetAllQuestionsQuery, Result<List<QuestionsDto>>>
    {
        private readonly IQuestionRepository repository;
        private readonly IGenericCacheService cacheService;
        public GetQuestionsByBankIdHandler(IQuestionRepository _repository, IGenericCacheService _cacheService)
        {
            repository = _repository;
            cacheService = _cacheService;
        }
        public async Task<Result<List<QuestionsDto>>> Handle(GetAllQuestionsQuery request, CancellationToken cancellationToken)
        {
            var questionsResult = await cacheService.GetOrSetAsync(
                $"questions_{request.questionBankId}",
                () => repository.GetQuestionsByBankId(request.questionBankId),
                TimeSpan.FromMinutes(15));

            if (questionsResult.Data != null)
            {
                return Result<List<QuestionsDto>>.Success(questionsResult.Data);
            }
            return Result<List<QuestionsDto>>.Failure(questionsResult.Message, questionsResult.StatusCode);
        }
    }
}
