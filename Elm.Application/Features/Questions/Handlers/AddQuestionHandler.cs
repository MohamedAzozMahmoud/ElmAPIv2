using Elm.Application.Contracts;
using Elm.Application.Contracts.Abstractions.Cache;
using Elm.Application.Contracts.Features.Questions.Commands;
using Elm.Application.Contracts.Features.Questions.DTOs;
using Elm.Application.Contracts.Repositories;
using Elm.Application.Mapper.Elm.Application.Mappers;
using Elm.Domain.Entities;
using Elm.Domain.Enums;
using MediatR;

namespace Elm.Application.Features.Questions.Handlers
{
    public sealed class AddQuestionHandler : IRequestHandler<AddQuestionCommand, Result<QuestionsDto>>
    {
        private readonly IQuestionRepository repository;
        private readonly MappingProvider mapping;
        private readonly IGenericCacheService cacheService;
        public AddQuestionHandler(IQuestionRepository _repository, MappingProvider mapping, IGenericCacheService _cacheService)
        {
            repository = _repository;
            this.mapping = mapping;
            cacheService = _cacheService;
        }
        public async Task<Result<QuestionsDto>> Handle(AddQuestionCommand request, CancellationToken cancellationToken)
        {
            var question = new Question
            {
                QuestionBankId = request.questionBankId,
                Content = request.QuestionsDto.Content,
                Options = request.QuestionsDto.Options.Select(o => new Option
                {
                    Content = o.Content,
                    IsCorrect = o.IsCorrect
                }).ToList(),
                QuestionType = Enum.Parse<QuestionType>(request.QuestionsDto.QuestionType),
            };
            var question1 = await repository.AddAsync(question);
            var questionDto = mapping.MapToDto(question1);
            if (questionDto is null)
            {
                return Result<QuestionsDto>.Failure("Failed to add question");
            }
            cacheService.Remove($"bank_count_{question1.QuestionBankId}");
            cacheService.Remove($"questions_{request.questionBankId}");
            return Result<QuestionsDto>.Success(questionDto);
        }
    }
}
