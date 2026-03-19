using System;
using BCrypt.Net;

class Program
{
    static void Main()
    {
        string password = "Admin@123";
        string hash = BCrypt.Net.BCrypt.HashPassword(password, 11); // 11 = work factor
        Console.WriteLine(hash);
    }
}