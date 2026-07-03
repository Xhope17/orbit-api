using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Logging;
using Orbit.Application.Common;
using Orbit.Application.Models.DTOs;
using Orbit.Application.Enums;
using Orbit.Application.Interfaces.Services;

namespace Orbit.Infrastructure.Services;

public class CloudinaryService : ICloudinaryService
{
    private readonly Cloudinary _cloudinary;
    private readonly ILogger<CloudinaryService> _logger;

    private static readonly Dictionary<CloudinaryFolder, string> FolderMap = new()
    {
        [CloudinaryFolder.ProfilePics] = "orbit/profile_pics",
        [CloudinaryFolder.ProfileBanners] = "orbit/profile_banners",
        [CloudinaryFolder.PostMedia] = "orbit/post_media",
    };

    private static readonly HashSet<string> VideoExtensions = [".mp4", ".webm", ".mov", ".avi", ".mkv"];

    public CloudinaryService(Cloudinary cloudinary, ILogger<CloudinaryService> logger)
    {
        _cloudinary = cloudinary;
        _logger = logger;
    }

    public async Task<Result<CloudinaryUploadResult>> UploadAsync(
        Stream fileStream,
        string fileName,
        CloudinaryFolder folder,
        string? contentType = null)
    {
        try
        {
            var folderPath = FolderMap[folder];
            var publicId = Path.GetFileNameWithoutExtension(fileName);
            var isVideo = IsVideo(contentType, fileName);

            if (isVideo)
            {
                var uploadParams = new VideoUploadParams
                {
                    File = new FileDescription(fileName, fileStream),
                    PublicId = publicId,
                    Folder = folderPath,
                };

                var result = await _cloudinary.UploadAsync(uploadParams);

                if (result.Error != null)
                {
                    _logger.LogError("Cloudinary video upload error: {Error}", result.Error.Message);
                    return Result<CloudinaryUploadResult>.Failure(result.Error.Message);
                }

                return Result<CloudinaryUploadResult>.Success(MapResult(result));
            }

            var imageParams = new ImageUploadParams
            {
                File = new FileDescription(fileName, fileStream),
                PublicId = publicId,
                Folder = folderPath,
            };

            var imageResult = await _cloudinary.UploadAsync(imageParams);

            if (imageResult.Error != null)
            {
                _logger.LogError("Cloudinary image upload error: {Error}", imageResult.Error.Message);
                return Result<CloudinaryUploadResult>.Failure(imageResult.Error.Message);
            }

            return Result<CloudinaryUploadResult>.Success(MapResult(imageResult));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cloudinary upload failed");
            return Result<CloudinaryUploadResult>.Failure(ex.Message);
        }
    }

    public async Task<Result<CloudinaryUploadResult>> UploadAsync(
        byte[] fileBytes,
        string fileName,
        CloudinaryFolder folder,
        string? contentType = null)
    {
        using var stream = new MemoryStream(fileBytes);
        return await UploadAsync(stream, fileName, folder, contentType);
    }

    public async Task<Result<bool>> DeleteAsync(string publicId)
    {
        try
        {
            var deletionParams = new DeletionParams(publicId);
            var result = await _cloudinary.DestroyAsync(deletionParams);

            if (result.Error != null)
            {
                _logger.LogError("Cloudinary delete error: {Error}", result.Error.Message);
                return Result<bool>.Failure(result.Error.Message);
            }

            return Result<bool>.Success(result.Result == "ok");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cloudinary delete failed");
            return Result<bool>.Failure(ex.Message);
        }
    }

    private static bool IsVideo(string? contentType, string fileName)
    {
        if (contentType?.StartsWith("video/", StringComparison.OrdinalIgnoreCase) == true)
            return true;

        var ext = Path.GetExtension(fileName)?.ToLowerInvariant();
        return ext != null && VideoExtensions.Contains(ext);
    }

    private static CloudinaryUploadResult MapResult(ImageUploadResult result)
    {
        return new CloudinaryUploadResult(
            result.SecureUrl?.AbsoluteUri ?? result.Url?.AbsoluteUri ?? string.Empty,
            result.PublicId,
            result.Width,
            result.Height,
            result.Bytes,
            result.Format,
            null
        );
    }

    private static CloudinaryUploadResult MapResult(VideoUploadResult result)
    {
        return new CloudinaryUploadResult(
            result.SecureUrl?.AbsoluteUri ?? result.Url?.AbsoluteUri ?? string.Empty,
            result.PublicId,
            result.Width,
            result.Height,
            result.Bytes,
            result.Format,
            result.Duration > 0 ? (int)result.Duration : null
        );
    }
}
