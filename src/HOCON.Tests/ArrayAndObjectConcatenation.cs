﻿using System.Diagnostics;
using System.Linq;
using Xunit;

namespace Hocon.Tests
{
    public class ArrayAndObjectConcatenation
    {
        /*
         * FACT:
         * Arrays can be concatenated with arrays, and objects with objects, but it is an error if they are mixed.
         */
        [Fact]
        public void CanConcatenateArray()
        {
            var hocon = @"a=[1,2] [3,4]";
            Assert.True(new[] { 1, 2, 3, 4 }.SequenceEqual(ConfigurationFactory.ParseString(hocon).GetIntList("a")));
        }

        [Fact]
        public void CanConcatenateObjectsViaValueConcatenation_1()
        {
            var hocon = "a : { b : 1 } { c : 2 }";
            var config = ConfigurationFactory.ParseString(hocon);
            Assert.Equal(1, config.GetInt("a.b"));
            Assert.Equal(2, config.GetInt("a.c"));
        }

        [Fact]
        public void CanConcatenateObjectsViaValueConcatenation_2()
        {
            //Debugger.Launch();

            var hocon = @"
data-center-generic = { cluster-size = 6 }
data-center-east = ${data-center-generic} { name = ""east"" }";

            var config = ConfigurationFactory.ParseString(hocon);

            Assert.Equal(6, config.GetInt("data-center-generic.cluster-size"));

            Assert.Equal(6, config.GetInt("data-center-east.cluster-size"));
            Assert.Equal("east", config.GetString("data-center-east.name"));
        }

        [Fact]
        public void CanConcatenateObjectsViaValueConcatenation_3()
        {
            //Debugger.Launch();

            var hocon = @"
data-center-generic = { cluster-size = 6 }
data-center-east = { name = ""east"" } ${data-center-generic}";

            var config = ConfigurationFactory.ParseString(hocon);

            Assert.Equal(6, config.GetInt("data-center-generic.cluster-size"));

            Assert.Equal(6, config.GetInt("data-center-east.cluster-size"));
            Assert.Equal("east", config.GetString("data-center-east.name"));
        }

        [Fact]
        public void CanConcatenateObjectsWhenMerged()
        {
            var hocon = @"
a : { b : 1 } 
a : { c : 2 }";

            var config = ConfigurationFactory.ParseString(hocon);
            Assert.Equal(1, config.GetInt("a.b"));
            Assert.Equal(2, config.GetInt("a.c"));
        }

#region Array and object concatenation exception spec
        [Fact]
        public void ThrowsWhenArrayAndObjectAreConcatenated_1()
        {
            var hocon = @"a : [1,2] { c : 2 }";
            Assert.Throws<HoconParserException>(() => ConfigurationFactory.ParseString(hocon));
        }

        [Fact]
        public void ThrowsWhenArrayAndObjectAreConcatenated_2()
        {
            var hocon = @"a : { c : 2 } [1,2]";
            Assert.Throws<HoconParserException>(() => ConfigurationFactory.ParseString(hocon));
        }

        [Fact]
        public void ThrowsWhenArrayAndObjectSubstitutionAreConcatenated_1()
        {
            var hocon = @"
a : { c : 2 }
b : [1,2] ${a}";
            Assert.Throws<HoconParserException>(() => ConfigurationFactory.ParseString(hocon));
        }

        [Fact]
        public void ThrowsWhenArrayAndObjectSubstitutionAreConcatenated_2()
        {
            var hocon = @"
a : { c : 2 }
b : ${a} [1,2]";
            Assert.Throws<HoconParserException>(() => ConfigurationFactory.ParseString(hocon));
        }

        [Fact]
        public void ThrowsWhenObjectAndArraySubstitutionAreConcatenated_1()
        {
            var hocon = @"
a : [1,2]
b : { c : 2 } ${a}";
            Assert.Throws<HoconParserException>(() => ConfigurationFactory.ParseString(hocon));
        }

        [Fact]
        public void ThrowsWhenObjectAndArraySubstitutionAreConcatenated_2()
        {
            var hocon = @"
a : [1,2]
b : ${a} { c : 2 }";
            Assert.Throws<HoconParserException>(() => ConfigurationFactory.ParseString(hocon));
        }
        #endregion

#region String  and object concatenation exception spec
        [Fact]
        public void ThrowsWhenStringAndObjectAreConcatenated_1()
        {
            var hocon = @"a : literal { c : 2 }";
            Assert.Throws<HoconParserException>(() => ConfigurationFactory.ParseString(hocon));
        }

        [Fact]
        public void ThrowsWhenStringAndObjectAreConcatenated_2()
        {
            var hocon = @"a : { c : 2 } literal";
            Assert.Throws<HoconParserException>(() => ConfigurationFactory.ParseString(hocon));
        }

        [Fact]
        public void ThrowsWhenStringAndObjectSubstitutionAreConcatenated_1()
        {
            var hocon = @"
a : { c : 2 }
b : literal ${a}";
            Assert.Throws<HoconParserException>(() => ConfigurationFactory.ParseString(hocon));
        }

        [Fact]
        public void ThrowsWhenStringAndObjectSubstitutionAreConcatenated_2()
        {
            var hocon = @"
a : { c : 2 }
b : ${a} literal";
            Assert.Throws<HoconParserException>(() => ConfigurationFactory.ParseString(hocon));
        }

        [Fact]
        public void ThrowsWhenObjectAndStringSubstitutionAreConcatenated_1()
        {
            var hocon = @"
a : literal
b : ${a} { c : 2 }";
            Assert.Throws<HoconParserException>(() => ConfigurationFactory.ParseString(hocon));
        }

        [Fact]
        public void ThrowsWhenObjectAndStringSubstitutionAreConcatenated_2()
        {
            var hocon = @"
a : literal
b : { c : 2 } ${a}";
            Assert.Throws<HoconParserException>(() => ConfigurationFactory.ParseString(hocon));
        }
        #endregion

        #region String and array concatenation exception spec
        [Fact]
        public void ThrowsWhenArrayAndStringAreConcatenated_1()
        {
            var hocon = @"a : [1,2] literal";
            Assert.Throws<HoconParserException>(() => ConfigurationFactory.ParseString(hocon));
        }

        [Fact]
        public void ThrowsWhenArrayAndStringAreConcatenated_2()
        {
            var hocon = @"a : literal [1,2]";
            Assert.Throws<HoconParserException>(() => ConfigurationFactory.ParseString(hocon));
        }

        [Fact]
        public void ThrowsWhenArrayAndStringSubstitutionAreConcatenated_1()
        {
            var hocon = @"
a : literal
b : ${a} [1,2]";
            Assert.Throws<HoconParserException>(() => ConfigurationFactory.ParseString(hocon));
        }

        [Fact]
        public void ThrowsWhenArrayAndStringSubstitutionAreConcatenated_2()
        {
            var hocon = @"
a : literal
b : [1,2] ${a}";
            Assert.Throws<HoconParserException>(() => ConfigurationFactory.ParseString(hocon));
        }

        [Fact]
        public void ThrowsWhenStringAndArraySubstitutionAreConcatenated_1()
        {
            var hocon = @"
a : [1,2]
b : ${a} literal";
            Assert.Throws<HoconParserException>(() => ConfigurationFactory.ParseString(hocon));
        }

        [Fact]
        public void ThrowsWhenStringAndArraySubstitutionAreConcatenated_2()
        {
            var hocon = @"
a : [1,2]
b : literal ${a}";
            Assert.Throws<HoconParserException>(() => ConfigurationFactory.ParseString(hocon));
        }
        #endregion
    }
}
