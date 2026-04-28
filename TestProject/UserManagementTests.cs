using System.Collections.Generic;
using System.Threading;
using MyProject;
using MyTestFramework;

namespace TestProject
{
    [Category("User Management")]
    [Author("qa-team")]
    public class UserManagementTests
    {
        private Users _users;
        private User _sampleUser;

        [SetUp]
        public void Init()
        {
            _users = new Users(minAge: 0, maxSize: 10);
            _sampleUser = new User(id: 1, age: 25, score: 0,
                nickname: "user1", email: "user1@example.com", password: "123456");
        }

        [TearDown]
        public void Cleanup()
        {
            _users = null;
            _sampleUser = null;
        }

        // === Параметризованный тест с TestCase ===
        [TestCase(10, 20, 30)]
        [TestCase(0, -5, -5)]
        [TestCase(100, 50, 150)]
        [Category("Score")]
        [Priority(1)]
        public void AddScore_ValidValues(int initialScore, int addValue, int expected)
        {
            var user = _sampleUser;
            user.AddScore(initialScore);
            user.AddScore(addValue);
            Assert.IsEqual(expected, user.Score);
        }

        [Test]
        [Category("Score")]
        [Priority(1)]
        public void AddScore_GoesBelowZero_Throws()
        {
            var user = new User(score: 10);
            try
            {
                user.AddScore(-15);
                Assert.Fail("Должно было быть выброшено исключение"); // не должен достигнуть
            }
            catch (InvalidOperationException)
            {
                Assert.IsTrue(true); // ожидаемое исключение
            }
        }

        // === Параметризованный тест с TestSource (yield return) ===
        public static IEnumerable<object[]> NicknameValidationCases()
        {
            yield return new object[] { "abc", true };      // min length 3
            yield return new object[] { "ab", false };
            yield return new object[] { "", false };
            yield return new object[] { "verylongnickname_12345", true }; // 21 символ? проверим
            yield return new object[] { "valid_nick", true };
            yield return new object[] { "x", false };
        }

        [TestSource(nameof(NicknameValidationCases))]
        [Category("Validation")]
        [Priority(2)]
        public void ValidateNickname(string nickname, bool isValidExpected)
        {
            try
            {
                _sampleUser.SetNickname(nickname);
                Assert.IsTrue(isValidExpected);
            }
            catch (ArgumentException)
            {
                Assert.IsFalse(isValidExpected);
            }
        }

        // === Тесты на добавление пользователей ===
        [Test]
        [Category("Add User")]
        public void AddUser_Valid_Success()
        {
            bool added = _users.AddUser(_sampleUser);
            Assert.IsTrue(added);
            Assert.IsEqual(1, _users.GetUsersCount());
        }

        [Test]
        [Category("Add User")]
        public void AddUser_DuplicateEmail_Throws()
        {
            _users.AddUser(_sampleUser);
            var duplicate = new User(id: 2, email: "user1@example.com", password: "123456");
            try
            {
                _users.AddUser(duplicate);
                Assert.Fail("Должно было быть выброшено исключение");
            }
            catch (InvalidOperationException) { Assert.IsTrue(true); }
        }

        [Test]
        [Category("Add User")]
        public void AddUser_UnderMinAge_Throws()
        {
            var youngUser = new User(id: 3, age: 10, email: "young@test.com", password: "123456");
            var strictUsers = new Users(minAge: 18, maxSize: 5);
            try
            {
                strictUsers.AddUser(youngUser);
                Assert.Fail("Должно было быть выброшено исключение");
            }
            catch (InvalidOperationException) { Assert.IsTrue(true); }
        }

        [Test]
        [Category("Add User")]
        public void AddUser_MaxSizeReached_ReturnsFalse()
        {
            var smallList = new Users(minAge: 0, maxSize: 2);
            smallList.AddUser(new User(id: 1, email: "a@test.com"));
            smallList.AddUser(new User(id: 2, email: "b@test.com"));
            bool result = smallList.AddUser(new User(id: 3, email: "c@test.com"));
            Assert.IsFalse(result);
            Assert.IsEqual(2, smallList.GetUsersCount());
        }

        // === Тесты на обновление ===
        [Test]
        [Category("Email Update")]
        [Priority(1)]
        public void UpdateUserEmail_Valid_Success()
        {
            _users.AddUser(_sampleUser);
            bool updated = _users.UpdateUserEmail(1, "newemail@example.com");
            Assert.IsTrue(updated);
            Assert.IsEqual("newemail@example.com", _users.FindByEmail("newemail@example.com")?.Email);
        }

        [Test]
        [Timeout(500)]
        [Category("Email Update")]
        public void UpdateUserEmail_Conflict_Throws()
        {
            _users.AddUser(new User(id: 1, email: "a@b.com"));
            _users.AddUser(new User(id: 2, email: "c@d.com"));
            try
            {
                _users.UpdateUserEmail(1, "c@d.com");
                Assert.Fail("Должно было быть выброшено исключение");
            }
            catch (InvalidOperationException) { Assert.IsTrue(true); }
        }

        // === Асинхронный тест с паузой (бизнес-логика эмуляции) ===
        [Test]
        [Category("Async Ops")]
        public async System.Threading.Tasks.Task FindUsersByAge_AfterDelay_ReturnsCorrect()
        {
            for (int i = 0; i < 5; i++)
                _users.AddUser(new User(id: i, age: 20 + i, email: $"user{i}@test.com"));
            await System.Threading.Tasks.Task.Delay(100); // эмуляция задержки
            var result = _users.FindByAge(22);
            Assert.IsEqual(1, result.Count);
            Assert.IsEqual(22, result[0].Age);
        }
    }
}