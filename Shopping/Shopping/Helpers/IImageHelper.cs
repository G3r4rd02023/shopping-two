namespace Shopping.Helpers
{
	public interface IImageHelper
	{
		Task<string> UploadImageAsync(IFormFile imageFile, string folder);
	}
}
