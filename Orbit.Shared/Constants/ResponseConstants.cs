namespace Orbit.Shared.Constants;

public static class ResponseConstants
{
    public const string AUTH_TOKEN_NOT_FOUND = "El token no es correcto, expiró o no se argumentó";

    public static string ErrorUnexpected(string traceId)
    {
        return $"Ha ocurrido un error inesperado: contacte con soporte, mencionando el siguiente código: {traceId}";
    }
}
