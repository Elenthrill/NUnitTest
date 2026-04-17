using MyProject;
using MyTestFramework;

namespace TestProject
{

    public class MyTest
    {
        private Users myUserlist;
        private User myUser;
        [SetUp]
        public void Init()
        {
            myUserlist = new Users(0, 5);
            Console.WriteLine("  > Инициализация теста...");
        }

        [TearDown]
        public void CleanUp()
        {
            myUserlist = null;
            Console.WriteLine("  > Очистка теста...\n");
        }

        [TestCase(2, 2, 4)]
        [TestCase(1, 2, 3)]
        [TestCase(2, -7, 0)]
        public void TestSAddScore(int a, int b, int expected)
        {
            var myUser = new User(score: a);
            myUser.AddScore(b);
            Assert.IsEqual(expected, myUser.score);
        }

        [Test]
        public void TestIsUserEqual()
        {
            var user1 = new User();
            var user2 = user1;
            Assert.IsEqual(user1, user2);
        }

        [Test]
        public void IsUsersUnic()
        {
            for (int i = 0; i < 5; i++)
            {
                var user = new User(id: i);
                myUserlist.AddUser(user);
            }
            var user1 = myUserlist.myUsers[0];
            myUserlist.AddUser(user1);
            CollectionAssert.AllItemsAreUnique(myUserlist.myUsers);
        }

        [Test]
        public void IsUserHere()
        {
            for (int i = 0; i < 5; i++)
            {
                var user = new User(id: i);
                myUserlist.AddUser(user);
            }
            var user1 = myUserlist.myUsers.First();
            CollectionAssert.Contains(myUserlist.myUsers, user1);
        }

        [Test]
        public void IsSubSet()
        {
            for (int i = 0; i < 5; i++)
            {
                var user = new User(id: i);
                myUserlist.AddUser(user);
            }
            var badUsers = new Users(0, 3);
            for (int i = 0; i < 3; i++)
            {
                badUsers.AddUser(myUserlist.myUsers[i]);
            }
            CollectionAssert.IsSubsetOf(badUsers.myUsers, myUserlist.myUsers);
        }

        [Test]
        public void IsUserNotHere()
        {
            for (int i = 0; i < 5; i++)
            {
                var user = new User(id: i);
                myUserlist.AddUser(user);
            }
            var badUser = new Users(0, 3);
            CollectionAssert.DoesNotContain(myUserlist.myUsers, badUser);
        }

        [Test]
        public void IsUserAgeOk()
        {
            for (int i = 0; i < 5; i++)
            {
                var user = new User(id: i, age: i);
                myUserlist.AddUser(user);
            }
        }
    }

}