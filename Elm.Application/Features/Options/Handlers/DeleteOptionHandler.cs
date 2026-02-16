using Elm.Application.Contracts;
using Elm.Application.Contracts.Features.Options.Commands;
using Elm.Application.Contracts.Repositories;
using Elm.Domain.Entities;
using MediatR;

namespace Elm.Application.Features.Options.Handlers
{
    public sealed class DeleteOptionHandler : IRequestHandler<DeleteOptionCommand, Result<bool>>
    {
        private readonly IGenericRepository<Option> repository;
        public DeleteOptionHandler(IGenericRepository<Option> repository)
        {
            this.repository = repository;
        }
        public async Task<Result<bool>> Handle(DeleteOptionCommand request, CancellationToken cancellationToken)
        {
            var existingOption = await repository.GetByIdAsync(request.optionId);
            if (existingOption == null)
            {
                return Result<bool>.NotFound("Option not found.");
            }
            var result = await repository.DeleteAsync(existingOption);
            if (result)
            {
                return Result<bool>.Success(true);
            }
            return Result<bool>.Failure("Failed to delete option.");
        }
    }
}
