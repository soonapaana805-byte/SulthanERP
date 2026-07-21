using BCrypt.Net;

string password = "admin123";

string hash = BCrypt.Net.BCrypt.HashPassword(password);

Console.WriteLine(hash);

Console.ReadLine();