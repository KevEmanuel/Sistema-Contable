using System;
using System.Security.Cryptography;

namespace SistemaContableDTE.Services;
//clase para guardar la contraseña cifrada, mediante un hash
public  static class ServicioContrasena
{
    
    private const int Iteraciones = 100000;
    private const int TamanoSal = 16;
    private const int TamanoHas = 32;

    //funcion para generar el hash
    public static string CrearHash(string contrasena)
    {
        if(string.IsNullOrWhiteSpace(contrasena))
        throw new ArgumentException("La contraseña no puede esta vacia. ");

        byte[] sal = RandomNumberGenerator.GetBytes(TamanoSal);
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(contrasena, sal,Iteraciones, HashAlgorithmName.SHA256, TamanoHas);

        return $"{Iteraciones}: {Convert.ToBase64String(sal)}:{Convert.ToBase64String(hash)}";

    }

    //funcion para verificar la contrasena, si guardo mediante hash
    public static bool Verificar(string contrasena, string hashGuardado)
    {
        try
        {
            string[] partes = hashGuardado.Split(':');
            if(partes.Length !=3)
            return false;
            
            int Iteraciones = int.Parse(partes[0]);
            byte[] sal = Convert.FromBase64String(partes[1]);
            byte[] hashEsperado = Convert.FromBase64String(partes[2]);

            byte[] hashActual = Rfc2898DeriveBytes.Pbkdf2(contrasena,sal,Iteraciones,HashAlgorithmName.SHA256, hashEsperado.Length);

            return CryptographicOperations.FixedTimeEquals(hashEsperado,hashActual);
        }
        catch
        {
            return false;
        }   
    }
}