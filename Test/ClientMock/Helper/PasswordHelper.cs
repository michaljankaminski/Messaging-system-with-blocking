using EdcsClient.Model;
using EdcsClient.Service;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace EdcsClient.Helper
{
    public interface IAuthenticationHelper
    {
        bool Authenticate(ref User user);
    }
    public class AuthenticationHelper : IAuthenticationHelper
    {
        private readonly IDbService _dbService;
        public AuthenticationHelper(IDbService service)
        {
            _dbService = service;
        }
        public bool Authenticate(ref User user)
        {
            string password = user.Password;
            var passwordResult = HashPassword(ref password);
            if (passwordResult == true)
            {
                if (_dbService.GetUser(user.Name, password) != null)
                {
                    user = _dbService.GetUser(user.Name, password);
                    return true;
                }
                else
                    return false;
            }
            else
                throw new Exception("I was unable to hash the password. Internal error.");
        }
        private bool HashPassword(ref string password)
        {
            try
            {
                using (SHA256 sha256Hash = SHA256.Create())
                {
                    byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(password));

                    StringBuilder builder = new StringBuilder();
                    for (int i = 0; i < bytes.Length; i++)
                    {
                        builder.Append(bytes[i].ToString("x2"));
                    }
                    password = builder.ToString();
                }
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

    }
}
