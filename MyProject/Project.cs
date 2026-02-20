using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace MyProject
{
    public class User
    {
        public int? id;
        public int? age;
        public int? score;
        public string? nickname;
        public string? email;
        public string? password;

        public User(int? id = null, int? age = null, int? score = null, string?
            nickname = null, string? email = null, string? password = null)
        {
            this.id = id;
            this.age = age;
            this.score = score;
            this.nickname = nickname;
            this.email = email;
            this.password = password;
        }
        public void AddScore(int value)
            {
                this.score += value;   
            }
        public void ChangeNick(string newNick) {
            this.nickname = newNick;
        }
        public void ChangeEmail(string newEmail) 
        { this.email = newEmail; }
        public void ChangePassword(string newPassword) { this.password = newPassword;}
        public void ChangeAge(int value) { this.age = value;}


    }

    public class Users
    {
        public List<User>? myUsers;
        private int maxSize;
        private int minAge;

        public Users(int minAge, int maxSize)
        {
            myUsers = new List<User>();
            this.minAge = minAge;
            this.maxSize = maxSize;
        }
        public int GetUsersCount()
        {
            if (myUsers == null) return 0;
            return myUsers.Count();
        }

        public bool AddUser(User user)
        {
            if (myUsers == null || myUsers.Count >= maxSize) return false;
            myUsers.Add(user);
            return true;

        }
        public bool RemoveUser(User user)
        {
            if (myUsers == null) return false;
            myUsers.Remove(user);
            return true;

        }
        public bool RemoveUser(int id)
        {
            if (myUsers == null) return false;
            var userToRemove = myUsers.Find(x => x.id == id);
            if (userToRemove == null) return false;
            myUsers.Remove(userToRemove);
            return true;

        }
    }
}
