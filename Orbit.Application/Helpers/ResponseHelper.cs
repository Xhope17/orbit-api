
using Orbit.Application.Models.Responses;

namespace Orbit.Application.Helpers
{
    public static class ResponseHelper
    {

        public static GenericResponse<T> Create<T>(T data, List<string>? errors = null, int? count = 0, string? message = null)
        {
            var response = new GenericResponse<T>
            {
                Data = data,
                Message = message ?? "Solicitud realizada correctamente",
                Erros = errors ?? [],
                Count = count ?? 0

            };

            return response;
        }
    }
}
