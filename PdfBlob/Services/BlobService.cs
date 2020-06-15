using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.WindowsAzure.Storage.Blob;
using PDFBlobService.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Linq;

namespace AzureBlobLearning.Services
{
	public interface IBlobService
	{
		Task<IEnumerable<FileModel>> ListAsync();
		Task UploadAsync(IFormFileCollection files);
		Task DeleteAsync(string fileUri);
		Task DeleteAllAsync();
		Task<IEnumerable<FileModel>> OrderListAsync(string Orderby);
		IList<ValidationFailure> Validate(IFormFileCollection files);
	}

	public class BlobService : IBlobService
	{
		private readonly IBlobFactory _azureBlobConnectionFactory;

		public BlobService(IBlobFactory azureBlobConnectionFactory)
		{
			_azureBlobConnectionFactory = azureBlobConnectionFactory;
		}

		public async Task DeleteAllAsync()
		{
			var blobContainer = await _azureBlobConnectionFactory.GetBlobContainer();

			BlobContinuationToken blobContinuationToken = null;
			do
			{
				var response = await blobContainer.ListBlobsSegmentedAsync(blobContinuationToken);
				foreach (IListBlobItem blob in response.Results)
				{
					if (blob.GetType() == typeof(CloudBlockBlob))
						await((CloudBlockBlob)blob).DeleteIfExistsAsync();
				}
				blobContinuationToken = response.ContinuationToken;
			} while (blobContinuationToken != null);
		}

		public async Task DeleteAsync(string fileUri)
		{
			var blobContainer = await _azureBlobConnectionFactory.GetBlobContainer();

			Uri uri = new Uri(fileUri);
			string filename = Path.GetFileName(uri.LocalPath);

			var blob = blobContainer.GetBlockBlobReference(filename);
			await blob.DeleteIfExistsAsync();
		}

		public async Task<IEnumerable<FileModel>> ListAsync()
		{
			var blobContainer = await _azureBlobConnectionFactory.GetBlobContainer();
			var allBlobs = new List<FileModel>();
			BlobContinuationToken blobContinuationToken = null;
			do
			{
				var response = await blobContainer.ListBlobsSegmentedAsync(blobContinuationToken);
				foreach (IListBlobItem blob in response.Results)
				{
					if (blob.GetType() == typeof(CloudBlockBlob))
					{
						var cloudblob = (CloudBlockBlob)blob;
						FileModel fileModel = new FileModel();
						fileModel.FileLocation = cloudblob.Uri;
						fileModel.FileName = cloudblob.Name;
						fileModel.FileType = cloudblob.Properties.Length.ToString() ;
						allBlobs.Add(fileModel);
					}
				}
				blobContinuationToken = response.ContinuationToken;
			} while (blobContinuationToken != null);
			return allBlobs;
		}


		public async Task<IEnumerable<FileModel>> OrderListAsync(string Orderby)
		{
			var allBlobs = await ListAsync();

			switch (Orderby)
			{ case "FileName":
				return allBlobs.OrderBy(x => x.FileName);
				case "FileSize":
					return allBlobs.OrderBy(x => x.FileType);
				default:
					return allBlobs.OrderBy(x => x.FileName);
					break;
			}
			
		}



		public async Task UploadAsync(IFormFileCollection files)
		{
			var blobContainer = await _azureBlobConnectionFactory.GetBlobContainer();

			for (int i = 0; i < files.Count; i++)
			{
				var blob = blobContainer.GetBlockBlobReference(GenerateUniqueName(files[i].FileName));
				using (var stream = files[i].OpenReadStream())
				{
					await blob.UploadFromStreamAsync(stream);

				}
			}
		}

		public IList<ValidationFailure> Validate(IFormFileCollection files)
		{
			FilesModelValidator validator = new FilesModelValidator();
			FilesModel filesModel = new FilesModel();
			foreach (var file in files)
			{
				filesModel.Files.Add(file);

			}
			ValidationResult result = validator.Validate(filesModel);
			return result.Errors;
		}

		private string GenerateUniqueName(string filename)
		{
			string ext = Path.GetExtension(filename);
			return string.Format("{0:10}_{1}{2}", DateTime.Now.Ticks, Guid.NewGuid(), ext);
		}
	}
}
