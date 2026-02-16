using Elm.Application.Contracts;
using Elm.Application.Contracts.Abstractions.Cache;
using Elm.Application.Contracts.Features.QuestionsBank.Commands;
using Elm.Application.Contracts.Repositories;
using MediatR;

namespace Elm.Application.Features.QuestionsBank.Handlers
{
    public sealed class UpdateQuestionsBankHandler : IRequestHandler<UpdateQuestionsBankCommand, Result<bool>>
    {
        private readonly IGenericRepository<Elm.Domain.Entities.QuestionsBank> repository;
        private readonly IGenericCacheService cacheService;
        public UpdateQuestionsBankHandler(IGenericRepository<Elm.Domain.Entities.QuestionsBank> repository, IGenericCacheService cacheService)
        {
            this.repository = repository;
            this.cacheService = cacheService;
        }
        public async Task<Result<bool>> Handle(UpdateQuestionsBankCommand request, CancellationToken cancellationToken)
        {
            var questionsBank = await cacheService.GetOrSetAsync($"questionsBank_{request.id}",
                () => repository.GetByIdAsync(request.id));
            if (questionsBank == null)
            {
                return Result<bool>.Failure("Questions Bank not found", 404);
            }
            questionsBank.Name = request.name;
            questionsBank.CurriculumId = request.curriculumId;
            var result = await repository.UpdateAsync(questionsBank);
            if (!result)
            {
                return Result<bool>.Failure("Failed to update Questions Bank");
            }
            cacheService.Remove($"QuestionsBanks_{questionsBank.CurriculumId}");
            cacheService.Remove($"questionsBank_{request.id}");
            return Result<bool>.Success(result);
        }
    }
}
