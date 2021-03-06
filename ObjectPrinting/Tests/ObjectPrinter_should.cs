using System.Globalization;
using System.Text.RegularExpressions;
using FluentAssertions;
using NUnit.Framework;

namespace ObjectPrinting.Tests
{
    [TestFixture]
    public class ObjectPrinter_should
    {
        private static Person person;

        [SetUp]
        public void SetUp()
        {
            person = new Person();
        }

        [Test]
        public void PrintToString_WhenPersonWithoutArgument_ShouldContainAllProperties()
        {
            var result = person.PrintToString();
            result.Should().Contain(person.GetType().Name).And.Contain(nameof(person.Id)).And
                .Contain(nameof(person.Name)).And.Contain(nameof(person.Height)).And.Contain(nameof(person.Age));
        }

        [TestCase("NAME", 180, 60)]
        public void PrintToString_WhenPersonWithArgument_ShouldContainThisArgument(string name, int height, int age)
        {
            var person = new Person {Name = name, Height = height, Age = age};
            var result = person.PrintToString();
            result.Should().Contain(name).And.Contain(height.ToString()).And.Contain(age.ToString());
        }

        [Test]
        public void PrintToString_WhenPersonWithArgument_ShouldContainValueAfterName()
        {
            var person = new Person {Name = "Value"};
            var result = person.PrintToString();
            IsValueAfterName(nameof(person.Name), person.Name, result).Should().BeTrue();
        }

        private bool IsValueAfterName(string name, string value, string str)
        {
            var regexp = new Regex($@"{name}.+{value}");
            return regexp.Match(str).Success;
        }


        [Test]
        public void ObjectPrinter_For_WhenExcludingType()
        {
            var result = ObjectPrinter.For<Person>().Excluding<int>().PrintToString(person);
            result.Should().NotContain(nameof(person.Age));
        }

        [Test]
        public void PrintToString_WhenExcludingType()
        {
            var result = person.PrintToString(config => config.Excluding<int>());
            result.Should().NotContain(nameof(person.Age));
        }

        [Test]
        public void ObjectPrinter_For_WhenExcludingProperty()
        {
            var result = ObjectPrinter.For<Person>().Excluding(p => p.Age).PrintToString(person);
            result.Should().NotContain(nameof(person.Age));
        }

        [Test]
        public void PrintToString_WhenExcludingProperty()
        {
            var result = person.PrintToString(ser => ser.Excluding(p => p.Age));
            result.Should().NotContain(nameof(person.Age));
        }

        [Test]
        public void ObjectPrinter_For_WhenAlternativePropertySerial()
        {
            var result = ObjectPrinter.For<Person>().AlternativeFor(p => p.Age).Using(prop => $"({prop})")
                .PrintToString(person);
            result.Should().Contain($"({person.Age})");
        }

        [Test]
        public void PrintToString_WhenAlternativePropertySerial()
        {
            var result = person.PrintToString(ser => ser.AlternativeFor(p => p.Age).Using(prop => $"({prop})"));
            result.Should().Contain($"({person.Age})");
        }

        [Test]
        public void ObjectPrinter_For_WhenAlternativeTypeSerial()
        {
            var result = ObjectPrinter.For<Person>().AlternativeFor<int>().Using(prop => $"({prop})")
                .PrintToString(person);
            result.Should().Contain($"({person.Age})");
        }

        [Test]
        public void PrintToString_WhenAlternativeTypeSerial()
        {
            var result = person.PrintToString(ser => ser.AlternativeFor<int>().Using(prop => $"({prop})"));
            result.Should().Contain($"({person.Age})");
        }

        [Test]
        public void ObjectPrinter_For_WhenTakeOnlySerial()
        {
            person = new Person {Name = "Ivan"};
            var result = ObjectPrinter.For<Person>().AlternativeFor<string>().TakeOnly(1).PrintToString(person);
            result.Should().NotContain(person.Name).And.Contain(person.Name[0].ToString());
        }

        [Test]
        public void PrintToString_WhenTakeOnlySerial()
        {
            person = new Person {Name = "Ivan"};
            var result = person.PrintToString(ser => ser.AlternativeFor<string>().TakeOnly(1));
            result.Should().NotContain(person.Name).And.Contain(person.Name[0].ToString());
        }

        [TestCase(null)]
        [TestCase("")]
        public void PrintToString_WhenTakeOnlySerialWithNullOrEmpty(string str)
        {
            person = new Person {Name = str};
            var result = person.PrintToString(ser => ser.AlternativeFor<string>().TakeOnly(1));
            result.Should().Contain(nameof(person.Name));
        }

        [TestCase("en-GB", 50.5, "50.5")]
        [TestCase("ru-RU", 50.5, "50,5")]
        public void ObjectPrinter_For_WhenAlternativeCultureSerial(string culture, double height, string expectHeight)
        {
            person = new Person {Height = height};
            var result = ObjectPrinter.For<Person>().AlternativeFor<double>().Using(new CultureInfo(culture))
                .PrintToString(person);
            result.Should().Contain(expectHeight);
        }

        [TestCase("en-GB", 50.5, "50.5")]
        [TestCase("ru-RU", 50.5, "50,5")]
        public void PrintToString_WhenAlternativeCultureSerial(string culture, double height, string expectHeight)
        {
            person = new Person {Height = height};
            var result = person.PrintToString(ser => ser.AlternativeFor<double>().Using(new CultureInfo(culture)));
            result.Should().Contain(expectHeight);
        }
    }
}