using Elm.Application.Contracts;
using Elm.Application.Contracts.Abstractions.Cache;
using Elm.Application.Contracts.Features.Questions.Commands;
using Elm.Application.Contracts.Repositories;
using MediatR;

namespace Elm.Application.Features.Questions.Handlers
{
    public sealed class DeleteQuestionHandler : IRequestHandler<DeleteQuestionCommand, Result<bool>>
    {
        private readonly IQuestionRepository repository;
        private readonly IGenericCacheService cacheService;
        public DeleteQuestionHandler(IQuestionRepository _repository, IGenericCacheService _cacheService)
        {
            repository = _repository;
            cacheService = _cacheService;
        }
        public async Task<Result<bool>> Handle(DeleteQuestionCommand request, CancellationToken cancellationToken)
        {
            var cachedQuestion = await cacheService.GetOrSetAsync($"questions_{request.questionId}",
                () => repository.GetByIdAsync(request.questionId));
            if (cachedQuestion == null)
            {
                return Result<bool>.Failure("Question not found", 404);
            }
            var result = await repository.DeleteAsync(cachedQuestion);
            if (!result)
            {
                return Result<bool>.Failure("Question not found", 404);
            }
            cacheService.Remove($"bank_count_{cachedQuestion.QuestionBankId}");
            cacheService.Remove($"questions_{request.questionId}");
            return Result<bool>.Success(true);
        }
    }
}
