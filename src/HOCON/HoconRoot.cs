﻿//-----------------------------------------------------------------------
// <copyright file="HoconRoot.cs" company="Hocon Project">
//     Copyright (C) 2009-2015 Typesafe Inc. <http://www.typesafe.com>
//     Copyright (C) 2013-2015 Akka.NET project <https://github.com/akkadotnet/hocon>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;

namespace Hocon
{
    /// <summary>
    /// This class represents the root element in a HOCON (Human-Optimized Config Object Notation)
    /// configuration string.
    /// </summary>
    public class HoconRoot
    {
        /// <summary>
        /// Retrieves the value associated with this element.
        /// </summary>
        public HoconValue Value { get; }

        /// <summary>
        /// Retrieves an enumeration of substitutions associated with this element.
        /// </summary>
        public IEnumerable<HoconSubstitution> Substitutions { get; }

        internal static readonly HoconRoot Empty = new HoconRoot(new HoconValue(null));

        /// <summary>
        /// Initializes a new instance of the <see cref="HoconRoot"/> class.
        /// </summary>
        /// <param name="value">The value to associate with this element.</param>
        /// <param name="substitutions">An enumeration of substitutions to associate with this element.</param>
        internal HoconRoot(HoconValue value, IEnumerable<HoconSubstitution> substitutions)
        {
            Value = value;
            Substitutions = substitutions;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HoconRoot"/> class.
        /// </summary>
        /// <param name="value">The value to associate with this element.</param>
        public HoconRoot(HoconValue value)
        {
            Value = value;
            Substitutions = Enumerable.Empty<HoconSubstitution>();
        }

        private HoconValue GetNode(HoconPath path)
        {
            HoconValue currentNode = Value;
            if (currentNode == null)
            {
                throw new InvalidOperationException("Current node should not be null");
            }
            foreach (string key in path)
            {
                currentNode = currentNode.GetChildObject(key);
            }

            if(currentNode == null)
                return HoconValue.Undefined;

            return currentNode.Type == HoconType.Empty ? HoconValue.Undefined : currentNode;
        }

        /// <summary>
        /// Determine if a HOCON configuration element exists at the specified location
        /// </summary>
        /// <param name="path">The location to check for a configuration value.</param>
        /// <returns><c>true</c> if a value was found, <c>false</c> otherwise.</returns>
        public bool HasPath(string path)
            => HasPath(HoconPath.Parse(path));

        /// <summary>
        /// Determine if a HOCON configuration element exists at the specified location
        /// </summary>
        /// <param name="path">The location to check for a configuration value.</param>
        /// <returns><c>true</c> if a value was found, <c>false</c> otherwise.</returns>
        public virtual bool HasPath(HoconPath path)
            => !ReferenceEquals(GetNode(path), HoconValue.Undefined);

        #region Value getter methods

        /// <summary>
        /// Retrieves a string value from the specified path in the configuration.
        /// </summary>
        /// <param name="path">The path that contains the value to retrieve.</param>
        /// <param name="default">The default value to return if the value doesn't exist.</param>
        /// <returns>The string value defined in the specified path.</returns>
        public string GetString(string path, string @default = null)
            => GetString(HoconPath.Parse(path), @default);

        /// <inheritdoc cref="GetString(string,string)"/>
        public virtual string GetString(HoconPath path, string @default = null)
        {
            HoconValue value = GetNode(path);
            if (ReferenceEquals(value, HoconValue.Undefined))
                return @default;

            return value.GetString();
        }

        /// <summary>
        /// Retrieves a boolean value from the specified path in the configuration.
        /// </summary>
        /// <param name="path">The path that contains the value to retrieve.</param>
        /// <param name="default">The default value to return if the value doesn't exist.</param>
        /// <returns>The boolean value defined in the specified path.</returns>
        public bool GetBoolean(string path, bool @default = false)
            => GetBoolean(HoconPath.Parse(path), @default);

        /// <inheritdoc cref="GetBoolean(string,bool)"/>
        public virtual bool GetBoolean(HoconPath path, bool @default = false)
        {
            HoconValue value = GetNode(path);
            if (ReferenceEquals(value, HoconValue.Undefined))
                return @default;

            return value.GetBoolean();
        }

        /// <summary>
        /// Retrieves a long value, optionally suffixed with a 'b', from the specified path in the configuration.
        /// </summary>
        /// <param name="path">The path that contains the value to retrieve.</param>
        /// <returns>The long value defined in the specified path.</returns>
        public long? GetByteSize(string path)
            => GetByteSize(HoconPath.Parse(path));

        /// <inheritdoc cref="GetByteSize(string)"/>
        public virtual long? GetByteSize(HoconPath path)
        {
            HoconValue value = GetNode(path);
            if (ReferenceEquals(value, HoconValue.Undefined))
                return null;
            return value.GetByteSize();
        }

        /// <summary>
        /// Retrieves an integer value from the specified path in the configuration.
        /// </summary>
        /// <param name="path">The path that contains the value to retrieve.</param>
        /// <param name="default">The default value to return if the value doesn't exist.</param>
        /// <returns>The integer value defined in the specified path.</returns>
        public int GetInt(string path, int @default = 0)
            => GetInt(HoconPath.Parse(path), @default);

        /// <inheritdoc cref="GetInt(string,int)"/>
        public virtual int GetInt(HoconPath path, int @default = 0)
        {
            HoconValue value = GetNode(path);
            if (ReferenceEquals(value, HoconValue.Undefined))
                return @default;

            return value.GetInt();
        }

        /// <summary>
        /// Retrieves a long value from the specified path in the configuration.
        /// </summary>
        /// <param name="path">The path that contains the value to retrieve.</param>
        /// <param name="default">The default value to return if the value doesn't exist.</param>
        /// <returns>The long value defined in the specified path.</returns>
        public long GetLong(string path, long @default = 0)
            => GetLong(HoconPath.Parse(path), @default);

        /// <inheritdoc cref="GetLong(string,long)"/>
        public virtual long GetLong(HoconPath path, long @default = 0)
        {
            HoconValue value = GetNode(path);
            if (ReferenceEquals(value, HoconValue.Undefined))
                return @default;

            return value.GetLong();
        }

        /// <summary>
        /// Retrieves a float value from the specified path in the configuration.
        /// </summary>
        /// <param name="path">The path that contains the value to retrieve.</param>
        /// <param name="default">The default value to return if the value doesn't exist.</param>
        /// <returns>The float value defined in the specified path.</returns>
        public float GetFloat(string path, float @default = 0)
            => GetFloat(HoconPath.Parse(path), @default);

        /// <inheritdoc cref="GetFloat(string,float)"/>
        public virtual float GetFloat(HoconPath path, float @default = 0)
        {
            HoconValue value = GetNode(path);
            if (ReferenceEquals(value, HoconValue.Undefined))
                return @default;

            return value.GetFloat();
        }

        /// <summary>
        /// Retrieves a decimal value from the specified path in the configuration.
        /// </summary>
        /// <param name="path">The path that contains the value to retrieve.</param>
        /// <param name="default">The default value to return if the value doesn't exist.</param>
        /// <returns>The decimal value defined in the specified path.</returns>
        public decimal GetDecimal(string path, decimal @default = 0)
            => GetDecimal(HoconPath.Parse(path), @default);

        /// <inheritdoc cref="GetDecimal(string,decimal)"/>
        public virtual decimal GetDecimal(HoconPath path, decimal @default = 0)
        {
            HoconValue value = GetNode(path);
            if (ReferenceEquals(value, HoconValue.Undefined))
                return @default;

            return value.GetDecimal();
        }

        /// <summary>
        /// Retrieves a double value from the specified path in the configuration.
        /// </summary>
        /// <param name="path">The path that contains the value to retrieve.</param>
        /// <param name="default">The default value to return if the value doesn't exist.</param>
        /// <returns>The double value defined in the specified path.</returns>
        public double GetDouble(string path, double @default = 0)
            => GetDouble(HoconPath.Parse(path), @default);

        /// <inheritdoc cref="GetDouble(string,double)"/>
        public virtual double GetDouble(HoconPath path, double @default = 0)
        {
            HoconValue value = GetNode(path);
            if (ReferenceEquals(value, HoconValue.Undefined))
                return @default;

            return value.GetDouble();
        }

        /// <summary>
        /// Retrieves a list of boolean values from the specified path in the configuration.
        /// </summary>
        /// <param name="path">The path that contains the values to retrieve.</param>
        /// <returns>The list of boolean values defined in the specified path.</returns>
        public IList<Boolean> GetBooleanList(string path)
            => GetBooleanList(HoconPath.Parse(path));

        /// <inheritdoc cref="GetBooleanList(string)"/>
        public virtual IList<Boolean> GetBooleanList(HoconPath path)
        {
            HoconValue value = GetNode(path);
            if (ReferenceEquals(value, HoconValue.Undefined))
                throw new HoconParserException($"Hocon path {path} was not an array.");

            return value.GetBooleanList();
        }

        /// <summary>
        /// Retrieves a list of decimal values from the specified path in the configuration.
        /// </summary>
        /// <param name="path">The path that contains the values to retrieve.</param>
        /// <returns>The list of decimal values defined in the specified path.</returns>
        public IList<decimal> GetDecimalList(string path)
            => GetDecimalList(HoconPath.Parse(path));

        /// <inheritdoc cref="GetDecimalList(string)"/>
        public virtual IList<decimal> GetDecimalList(HoconPath path)
        {
            HoconValue value = GetNode(path);
            if (ReferenceEquals(value, HoconValue.Undefined))
                throw new HoconParserException($"Hocon path {path} was not an array.");

            return value.GetDecimalList();
        }

        /// <summary>
        /// Retrieves a list of float values from the specified path in the configuration.
        /// </summary>
        /// <param name="path">The path that contains the values to retrieve.</param>
        /// <returns>The list of float values defined in the specified path.</returns>
        public IList<float> GetFloatList(string path)
            => GetFloatList(HoconPath.Parse(path));

        /// <inheritdoc cref="GetFloatList(string)"/>
        public virtual IList<float> GetFloatList(HoconPath path)
        {
            HoconValue value = GetNode(path);
            if (ReferenceEquals(value, HoconValue.Undefined))
                throw new HoconParserException($"Hocon path {path} was not an array.");

            return value.GetFloatList();
        }

        /// <summary>
        /// Retrieves a list of double values from the specified path in the configuration.
        /// </summary>
        /// <param name="path">The path that contains the values to retrieve.</param>
        /// <returns>The list of double values defined in the specified path.</returns>
        public IList<double> GetDoubleList(string path)
            => GetDoubleList(HoconPath.Parse(path));

        /// <inheritdoc cref="GetDoubleList(string)"/>
        public virtual IList<double> GetDoubleList(HoconPath path)
        {
            HoconValue value = GetNode(path);
            if (ReferenceEquals(value, HoconValue.Undefined))
                throw new HoconParserException($"Hocon path {path} was not an array.");

            return value.GetDoubleList();
        }

        /// <summary>
        /// Retrieves a list of int values from the specified path in the configuration.
        /// </summary>
        /// <param name="path">The path that contains the values to retrieve.</param>
        /// <returns>The list of int values defined in the specified path.</returns>
        public IList<int> GetIntList(string path)
            => GetIntList(HoconPath.Parse(path));

        /// <inheritdoc cref="GetIntList(string)"/>
        public virtual IList<int> GetIntList(HoconPath path)
        {
            HoconValue value = GetNode(path);
            if (ReferenceEquals(value, HoconValue.Undefined))
                throw new HoconParserException($"Hocon path {path} was not an array.");

            return value.GetIntList();
        }

        /// <summary>
        /// Retrieves a list of long values from the specified path in the configuration.
        /// </summary>
        /// <param name="path">The path that contains the values to retrieve.</param>
        /// <returns>The list of long values defined in the specified path.</returns>
        public IList<long> GetLongList(string path)
            => GetLongList(HoconPath.Parse(path));

        /// <inheritdoc cref="GetLongList(string)"/>
        public virtual IList<long> GetLongList(HoconPath path)
        {
            HoconValue value = GetNode(path);
            if (ReferenceEquals(value, HoconValue.Undefined))
                throw new HoconParserException($"Hocon path {path} was not an array.");

            return value.GetLongList();
        }

        /// <summary>
        /// Retrieves a list of byte values from the specified path in the configuration.
        /// </summary>
        /// <param name="path">The path that contains the values to retrieve.</param>
        /// <returns>The list of byte values defined in the specified path.</returns>
        public IList<byte> GetByteList(string path)
            => GetByteList(HoconPath.Parse(path));

        /// <inheritdoc cref="GetByteList(string)"/>
        public virtual IList<byte> GetByteList(HoconPath path)
        {
            HoconValue value = GetNode(path);
            if (ReferenceEquals(value, HoconValue.Undefined))
                throw new HoconParserException($"Hocon path {path} was not an array.");

            return value.GetByteList();
        }

        /// <summary>
        /// Retrieves a list of string values from the specified path in the configuration.
        /// </summary>
        /// <param name="path">The path that contains the values to retrieve.</param>
        /// <returns>The list of string values defined in the specified path.</returns>
        public IList<string> GetStringList(string path)
            => GetStringList(HoconPath.Parse(path));

        /// <inheritdoc cref="GetStringList(string)"/>
        public virtual IList<string> GetStringList(HoconPath path)
        {
            HoconValue value = GetNode(path);
            if (ReferenceEquals(value, HoconValue.Undefined))
                throw new HoconParserException($"Hocon path {path} was not an array.");

            return value.GetStringList();
        }

        /// <summary>
        /// Retrieves a <see cref="HoconValue"/> from a specific path.
        /// </summary>
        /// <param name="path">The path that contains the value to retrieve.</param>
        /// <returns>The <see cref="HoconValue"/> found at the location if one exists, otherwise <c>null</c>.</returns>
        public HoconValue GetValue(string path)
            => GetValue(HoconPath.Parse(path));

        /// <inheritdoc cref="GetValue(string)"/>
        public HoconValue GetValue(HoconPath path)
        {
            HoconValue value = GetNode(path);
            return value;
        }

        /// <summary>
        /// Retrieves a <see cref="TimeSpan"/> value from the specified path in the configuration.
        /// </summary>
        /// <param name="path">The path that contains the value to retrieve.</param>
        /// <param name="default">The default value to return if the value doesn't exist.</param>
        /// <param name="allowInfinite"><c>true</c> if infinite timespans are allowed; otherwise <c>false</c>.</param>
        /// <returns>The <see cref="TimeSpan"/> value defined in the specified path.</returns>
        public virtual TimeSpan GetTimeSpan(string path, TimeSpan? @default = null, bool allowInfinite = true)
            => GetTimeSpan(HoconPath.Parse(path), @default, allowInfinite);

        /// <inheritdoc cref="GetTimeSpan(string,System.Nullable{System.TimeSpan},bool)"/>
        public virtual TimeSpan GetTimeSpan(HoconPath path, TimeSpan? @default = null, bool allowInfinite = true)
        {
            HoconValue value = GetNode(path);
            if (value == null)
                return @default.GetValueOrDefault();

            return value.GetTimeSpan(allowInfinite);
        }
        #endregion

        /// <summary>
        /// Retrieves an enumerable key value pair representation of the current configuration.
        /// </summary>
        /// <returns>The current configuration represented as an enumerable key value pair.</returns>
        public virtual IEnumerable<KeyValuePair<string, HoconValue>> AsEnumerable()
        {
            return Value.GetObject();
        }

        /// <summary>
        /// Converts the current configuration to a string.
        /// </summary>
        /// <returns>A string containing the current configuration.</returns>
        public override string ToString()
            => Value?.ToString() ?? "";

        /// <summary>
        /// Converts the current configuration to a pretty printed string.
        /// </summary>
        /// <param name="indentSize">The number of spaces used for each indent.</param>
        /// <returns>A string containing the current configuration.</returns>
        public string PrettyPrint(int indentSize)
            => Value.ToString(1, indentSize);

    }
}
