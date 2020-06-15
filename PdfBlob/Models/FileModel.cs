using FluentValidation;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PDFBlobService.Models
{
    public class FileModel
    {
        public string FileName { get; set; }
        public string FileType {get; set; }
        public Uri FileLocation { get; set; }
    }

    public class FileValidator : AbstractValidator<IFormFile>
    {
        public FileValidator()
        {
            RuleFor(x => x.Length).NotNull().LessThanOrEqualTo(5000000)
                .WithMessage("File size is larger than allowed");

            RuleFor(x => x.ContentType).NotNull().Must(x => x.Equals("application/pdf"))
                .WithMessage("Only PDF are allowed");
        }
    }
    public class FilesModel
    {
        public FilesModel()
        {
            Files = new List<IFormFile>();
        }
        public IList<IFormFile> Files { get; set; }
     
    }
    public class FilesModelValidator : AbstractValidator<FilesModel>
    {
        public FilesModelValidator()
        {
            RuleForEach(x => x.Files).SetValidator(new FileValidator());
        }
    }
}
