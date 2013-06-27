    using System;
using System.Collections.Generic;
using System.Linq;
    using System.Text.RegularExpressions;
    using NUnit.Framework;

namespace StringCalculator
{
    public class Calculator
    {
        private readonly IDelimiterFinder _delimiterFinder;

        public  string Error= "";

        public Calculator(IDelimiterFinder delimiterFinder)
        {
            _delimiterFinder = delimiterFinder;
        }

        public int Add(string val)
        {
            var delsFromString = _delimiterFinder.GetDelimeter(val).ToArray();
            var standarddels = new[] {",", "\n"};
            var dels = delsFromString.Concat(standarddels).ToArray();

            var numbers = val.Split(dels, StringSplitOptions.None);
            var sum = 0;
            var negativeNums = new List<int>();
            foreach (var number in numbers)
            {
                int num;
                if (!int.TryParse(number, out num) || num > 1000) continue;
                if (num <0)
                {
                    negativeNums.Add(num);
                }
                sum += num ;
            }

            CheckForNegativeNumbers(negativeNums);
            return string.IsNullOrWhiteSpace(val)
                       ? 0
                       : (numbers.Count() == 1 ? Convert.ToInt32(val) : sum);
        }

        private void CheckForNegativeNumbers(List<int> negativeNums)
        {
            if (!negativeNums.Any()) return;

            Error = string.Empty;
            var isFirst = true;
            foreach (var negativeNum in negativeNums)
            {
                if (isFirst)
                {
                    Error += negativeNum;
                    isFirst = false;
                }
                else
                {
                    Error += string.Format(",{0}", negativeNum);
                }
                
            }

            throw new NegativeException(Error);
        }
    }

    public interface IDelimiterFinder
    {
        IEnumerable<string> GetDelimeter(string val);
    }

    public class DelimiterFinder : IDelimiterFinder
    {
        public IEnumerable<string> GetDelimeter(string val)
        {
            var indx = val.IndexOf("//", StringComparison.Ordinal);

            var result = new  List<string>();
            if (indx < 0)
            {
                return result;
            }
            var delimiter = val.Substring(indx + 2, val.IndexOf("\n", StringComparison.Ordinal) - (indx + 2));

            if (delimiter.IndexOf("[", StringComparison.Ordinal) < 0)
            {
                result.Add(delimiter);
                return result;
            }

            var ms = Regex.Matches(delimiter, @"\[(.*?)\]");

            result.AddRange(from Match m in ms select m.Groups[1].ToString());
            return result;
        }
    }

    public class  NegativeException :Exception
    {
        public NegativeException(string error)
            : base("Negatives not allowed: " + error)
        {
            
        }
    }

    [TestFixture]
    public class CalculatorTests
    {
        private Calculator _calculator;

        [TestFixtureSetUp]
        public void Setup()
        {
            var delfinder = new DelimiterFinder();
            _calculator = new Calculator(delfinder);
        }
        
        [Test]
        public void EmptyStringShouldReturn0()
        {

            var result = _calculator.Add("");
            Assert.AreEqual(result, 0);

        }

        [TestCase("1",1)]
        [TestCase("20",20)]
        public void OnenumberShuldRetunthesameNumber(string input,int expected)
        {

            var result = _calculator.Add(input);
            Assert.AreEqual(result, expected);

        }
        [TestCase("1,3", 4)]
        [TestCase("20,50", 70)]
        public void GivencommadeliminatednumbersRetunthesumofnumbers(string input, int expected)
        {

            var result = _calculator.Add(input);
            Assert.AreEqual(result, expected);

        }
        [Test]
        public void GivenNewLineDelimterMustAddAswellAsComma()
        {

            var result = _calculator.Add("1,2\n3");
            Assert.AreEqual(result, 6);
        }
        [Test]
        public void ShouldAllowDifferentDelimiter()
        {

            var result = _calculator.Add("2;//;\n2;3");
            Assert.AreEqual(result, 7);
        }
        [Test]
        public void ShouldIgnoreNumbersBiggerThan1000()
        {

            var result = _calculator.Add("1001,2");
            Assert.AreEqual(result,2);
        }

        [TestCase("-1,2", "Negatives not allowed: -1")]
        [TestCase("2,-4,3,-5", "Negatives not allowed: -4,-5")]
        public void ShouldThrowExceptionIfAddIsCalledWithNegativeNumbs(string input,string message)
        {
             Assert.Throws(Is.TypeOf <NegativeException>()
                   .And.Message.EqualTo(message), () => _calculator.Add(input));
        }



        [Test]
        public void ShouldWorkGivenMultipleCharecterDelimiter()
        {

            var result = _calculator.Add("//[***]\n1***2***3");
            Assert.AreEqual(result,6);
        }

        [Test]
        public void ShouldWorkGivenMultiplDelimiter()
        {
            var result = _calculator.Add("//[*][%]\n1*2%3");
            Assert.AreEqual(result, 6);
        } 
        [Test]
        public void ShouldWorkGivenMultiplDelimiterOfAnyLength()
        {
            var result = _calculator.Add("//[***][%%]\n1***2%%3");
            Assert.AreEqual(result, 6);
        }
    }

    [TestFixture]
    public class DelimiterFinderTests
    {
        private DelimiterFinder _delimiterFinder;

        [TestFixtureSetUp]
        public void Setup()
        {
            _delimiterFinder = new DelimiterFinder();
        }

        ////;\n1;2
        [TestCase("//;\n2;3;1",";")]
        [TestCase("//*\n2*31","*")]
        [TestCase("//[***]\n2***3***1","***")]
        public void GetDelimeterFromAString(string val, string expected)
        {
            var result = _delimiterFinder.GetDelimeter(val).ToArray();
            Assert.AreEqual(result[0], expected);
        }
    }
}
