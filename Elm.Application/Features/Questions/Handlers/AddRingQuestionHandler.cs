using Elm.Application.Contracts;
using Elm.Application.Contracts.Abstractions.Cache;
using Elm.Application.Contracts.Features.Questions.Commands;
using Elm.Application.Contracts.Repositories;
using MediatR;

namespace Elm.Application.Features.Questions.Handlers
{
    public sealed class AddRingQuestionHandler : IRequestHandler<AddRingQuestionsCommand, Result<bool>>
    {
        private readonly IQuestionRepository repository;
        private readonly IGenericCacheService cacheService;
        public AddRingQuestionHandler(IQuestionRepository _repository, IGenericCacheService _cacheService)
        {
            repository = _repository;
            cacheService = _cacheService;
        }
        public async Task<Result<bool>> Handle(AddRingQuestionsCommand request, CancellationToken cancellationToken)
        {
            var result = await repository.AddRingQuestions(request.questionsBankId, request.QuestionsDtos);
            if (result.IsSuccess)
            {
                cacheService.Remove($"bank_count_{request.questionsBankId}");
                cacheService.Remove($"questions_{request.questionsBankId}");
            }
            return result;
        }
    }
}
