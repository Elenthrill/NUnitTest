using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace MyProject
{
    public class User
    {
        public int? Id { get; private set; }
        public int? Age { get; private set; }
        public int Score { get; private set; }
        public string? Nickname { get; private set; }
        public string? Email { get; private set; }
        public string? Password { get; private set; }

        public User(int? id = null, int? age = null, int score = 0,
            string? nickname = null, string? email = null, string? password = null)
        {
            Id = id;
            Score = score;
            SetNickname(nickname);
            SetEmail(email);
            SetPassword(password);
            SetAge(age);
        }

        public void AddScore(int value)
        {
            if (value < 0 && Math.Abs(value) > Score)
                throw new InvalidOperationException("Нельзя уменьшить счёт ниже 0");
            Score += value;
        }

        public void SetNickname(string? newNick)
        {
            if (string.IsNullOrWhiteSpace(newNick))
                throw new ArgumentException("Никнейм не может быть пустым");
            if (newNick.Length < 3 || newNick.Length > 20)
                throw new ArgumentException("Никнейм должен быть от 3 до 20 символов");
            Nickname = newNick;
        }

        public void SetEmail(string? newEmail)
        {
            if (string.IsNullOrWhiteSpace(newEmail))
                throw new ArgumentException("Email не может быть пустым");
            if (!Regex.IsMatch(newEmail, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                throw new ArgumentException("Некорректный формат email");
            Email = newEmail;
        }

        public void SetPassword(string? newPassword)
        {
            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
                throw new ArgumentException("Пароль должен быть не менее 6 символов");
            Password = newPassword;
        }

        public void SetAge(int? value)
        {
            if (value.HasValue && (value < 0 || value > 150))
                throw new ArgumentOutOfRangeException(nameof(value), "Возраст должен быть от 0 до 150");
            Age = value;
        }
    }

    public class Users
    {
        public List<User> MyUsers { get; }
        public int MaxSize { get; }
        public int MinAge { get; }

        public Users(int minAge, int maxSize)
        {
            if (minAge < 0) throw new ArgumentOutOfRangeException(nameof(minAge));
            if (maxSize <= 0) throw new ArgumentOutOfRangeException(nameof(maxSize));
            MinAge = minAge;
            MaxSize = maxSize;
            MyUsers = new List<User>();
        }

        public int GetUsersCount() => MyUsers.Count;

        public bool AddUser(User user)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            if (MyUsers.Count >= MaxSize)
                return false;
            if (user.Age.HasValue && user.Age < MinAge)
                throw new InvalidOperationException($"Пользователь младше минимального возраста ({MinAge})");
            if (MyUsers.Any(u => u.Email != null && u.Email.Equals(user.Email, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException("Пользователь с таким email уже существует");
            MyUsers.Add(user);
            return true;
        }

        public bool RemoveUser(User user) => user != null && MyUsers.Remove(user);

        public bool RemoveUser(int id)
        {
            var user = MyUsers.FirstOrDefault(u => u.Id == id);
            return user != null && MyUsers.Remove(user);
        }

        public User? FindByEmail(string email) =>
            MyUsers.FirstOrDefault(u => u.Email != null && u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));

        public List<User> FindByAge(int age) =>
            MyUsers.Where(u => u.Age == age).ToList();

        public bool UpdateUserEmail(int userId, string newEmail)
        {
            var user = MyUsers.FirstOrDefault(u => u.Id == userId);
            if (user == null) return false;
            if (MyUsers.Any(u => u.Id != userId && u.Email != null && u.Email.Equals(newEmail, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException("Email уже используется другим пользователем");
            user.SetEmail(newEmail);
            return true;
        }
    }
}