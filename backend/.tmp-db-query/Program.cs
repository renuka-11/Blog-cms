using Microsoft.Data.Sqlite;
using System;

var dbPath = "..\\BlogCMS.API\\blogcms.db";
using var connection = new SqliteConnection($"Data Source={dbPath}");
connection.Open();

var cmd = connection.CreateCommand();
cmd.CommandText = @"SELECT Id, UserName, Email, FullName, NormalizedEmail, EmailConfirmed FROM AspNetUsers;";
using var reader = cmd.ExecuteReader();
while (reader.Read())
{
    Console.WriteLine($"Id: {reader.GetString(0)}");
    Console.WriteLine($"UserName: {reader.GetString(1)}");
    Console.WriteLine($"Email: {reader.GetString(2)}");
    Console.WriteLine($"FullName: {reader.GetString(3)}");
    Console.WriteLine($"NormalizedEmail: {reader.GetString(4)}");
    Console.WriteLine($"EmailConfirmed: {reader.GetBoolean(5)}");
    Console.WriteLine(new string('-', 40));
}
