using MediatR;
using Microsoft.AspNetCore.Http;

namespace Elm.Application.Contracts.Features.Files.Commands
{
    public record UploadFileCommand(int curriculumId, string uploadedById, string Description, IFormFile FormFile) : IRequest<Result<string>>;
}
