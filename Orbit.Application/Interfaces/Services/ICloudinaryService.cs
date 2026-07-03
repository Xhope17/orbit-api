using Orbit.Application.Common;
using Orbit.Application.Models.DTOs;
using Orbit.Application.Enums;

namespace Orbit.Application.Interfaces.Services;

public interface ICloudinaryService
{
    Task<Result<CloudinaryUploadResult>> UploadAsync(
        Stream fileStream,
        string fileName,
        CloudinaryFolder folder,
        string? contentType = null);

    Task<Result<CloudinaryUploadResult>> UploadAsync(
        byte[] fileBytes,
        string fileName,
        CloudinaryFolder folder,
        string? contentType = null);

    Task<Result<bool>> DeleteAsync(string publicId);
}
