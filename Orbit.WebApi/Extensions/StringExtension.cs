using System.Text;

namespace Orbit.WebApi.Extensions
{
    public static class StringExtension
    {
        //upper creado por mi
        public static string ToUpperCreadoPorMi(this string str)
        {
            //return value.ToUpper();
            //var string = value.ToUpper();

            //para probar el StringBuilder
            //sirve para construir una cadena de caracteres de manera eficiente, evitando la creación de múltiples objetos string en memoria.
            var parsed = new StringBuilder();

            foreach (var item in str.Split())
            {
                parsed.Append(item.ToUpper());
            }
            {
                return parsed.ToString();
            }
        }
    }
}
