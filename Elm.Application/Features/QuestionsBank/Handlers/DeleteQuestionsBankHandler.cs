using Elm.Application.Contracts;
using Elm.Application.Contracts.Abstractions.Cache;
using Elm.Application.Contracts.Features.QuestionsBank.Commands;
using Elm.Application.Contracts.Repositories;
using MediatR;

namespace Elm.Application.Features.QuestionsBank.Handlers
{
    public sealed class DeleteQuestionsBankHandler : IRequestHandler<DeleteQuestionsBankCommand, Result<bool>>
    {
        private readonly IGenericRepository<Elm.Domain.Entities.QuestionsBank> repository;
        private readonly IGenericCacheService cacheService;
        public DeleteQuestionsBankHandler(IGenericRepository<Elm.Domain.Entities.QuestionsBank> repository, IGenericCacheService cacheService)
        {
            this.repository = repository;
            this.cacheService = cacheService;
        }
        public async Task<Result<bool>> Handle(DeleteQuestionsBankCommand request, CancellationToken cancellationToken)
        {
            var existingQuestionsBank = await cacheService.GetOrSetAsync($"questionsBank_{request.id}",
                () => repository.GetByIdAsync(request.id));
            if (existingQuestionsBank == null)
            {
                return Result<bool>.NotFound("Questions Bank not found");
            }
            var result = await repository.DeleteAsync(existingQuestionsBank);
            if (!result)
            {
                return Result<bool>.Failure("Failed to delete Questions Bank", 500);
            }
            cacheService.Remove($"questionsBank_{request.id}");
            cacheService.Remove($"QuestionsBanks_{existingQuestionsBank.CurriculumId}");
            return Result<bool>.Success(true);
        }
    }
}
