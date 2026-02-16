using Elm.Application.Contracts;
using Elm.Application.Contracts.Abstractions.Cache;
using Elm.Application.Contracts.Abstractions.Excel;
using Elm.Application.Contracts.Features.Questions.Commands;
using Elm.Application.Contracts.Features.Questions.DTOs;
using Elm.Application.Contracts.Repositories;
using MediatR;

namespace Elm.Application.Features.Questions.Handlers
{
    public sealed class AddByExcelQuestionsHandler : IRequestHandler<AddByExcelQuestionsCommand, Result<bool>>
    {
        private readonly IQuestionRepository repository;
        private readonly IExcelReader excelService;
        private readonly IGenericCacheService cacheService;
        public AddByExcelQuestionsHandler(IQuestionRepository _repository, IExcelReader _excelService, IGenericCacheService _cacheService)
        {
            repository = _repository;
            excelService = _excelService;
            cacheService = _cacheService;
        }
        public async Task<Result<bool>> Handle(AddByExcelQuestionsCommand request, CancellationToken cancellationToken)
        {
            var addquestions = excelService.ReadExcelFile<TemplateQuestionsDto>(request.ExcelFile);
            if (addquestions.Any(q => string.IsNullOrWhiteSpace(q.Content) || string.IsNullOrWhiteSpace(q.QuestionType)))
            {
                return Result<bool>.Failure("تم العثور على بيانات أسئلة غير صالحة.");
            }
            var result = await repository.AddRingQuestionsFromExcel(request.questionBankId, addquestions.ToList());
            if (!result.IsSuccess)
            {
                return Result<bool>.Failure(result.Message ?? "Failed to add questions from Excel.", result.StatusCode);
            }
            cacheService.Remove($"bank_count_{request.questionBankId}");
            cacheService.Remove($"questions_{request.questionBankId}");
            return Result<bool>.Success(result.Data);
        }
    }
}
