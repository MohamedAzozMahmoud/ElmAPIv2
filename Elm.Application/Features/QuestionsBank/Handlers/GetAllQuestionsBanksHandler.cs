using Elm.Application.Contracts;
using Elm.Application.Contracts.Abstractions.Cache;
using Elm.Application.Contracts.Features.QuestionsBank.DTOs;
using Elm.Application.Contracts.Features.QuestionsBank.Queries;
using Elm.Application.Contracts.Repositories;
using MediatR;

namespace Elm.Application.Features.QuestionsBank.Handlers
{
    public sealed class GetAllQuestionsBanksHandler : IRequestHandler<GetAllQuestionsBankQuery, Result<List<QuestionsBankDto>>>
    {
        private readonly IQuestionBankRepository repository;
        private readonly IGenericCacheService cacheService;
        public GetAllQuestionsBanksHandler(IQuestionBankRepository repository, IGenericCacheService _cacheService)
        {
            this.repository = repository;
            cacheService = _cacheService;
        }
        public async Task<Result<List<QuestionsBankDto>>> Handle(GetAllQuestionsBankQuery request, CancellationToken cancellationToken)
        {
            var questionsBanks = await cacheService.GetOrSetAsync($"QuestionsBanks_{request.curriculumId}",
                () => repository.GetQuestionsBank(request.curriculumId));
            if (!questionsBanks.IsSuccess || questionsBanks.Data == null)
            {
                return Result<List<QuestionsBankDto>>.Failure(questionsBanks.Message ?? "Failed to retrieve questions banks.", questionsBanks.StatusCode);
            }

            return Result<List<QuestionsBankDto>>.Success(questionsBanks.Data);
        }
    }
}
