using System;
using Xunit;

namespace Tests
{
    public class CalcTests
    {
        [Fact]
        public void Add_When2Integers_ShouldReturnCorrectInteger()
        {
            // TODO - call the Calc.Add method with 2 integers
            var result = Calc.Add(1, 1);
            // TODO - check the result is as expected
            Assert.Equal(2, result);
        }
    }
}
