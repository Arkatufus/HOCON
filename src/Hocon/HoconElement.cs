﻿// -----------------------------------------------------------------------
// <copyright file="HoconImmutableElement.cs" company="Akka.NET Project">
//      Copyright (C) 2013 - 2020 .NET Foundation <https://github.com/akkadotnet/hocon>
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Numerics;

namespace Hocon
{
    public abstract class HoconElement :
        IEquatable<HoconElement>
    {
        [Obsolete("There is no need to use Value property anymore, please remove it.")]
        public HoconElement Value => this;

        public abstract HoconType Type { get; }

        public abstract string Raw { get; }

        public HoconElement this[int index]
        {
            get
            {
                switch (this)
                {
                    case HoconObject obj:
                        return obj.ToArray()[index];
                    case HoconArray arr:
                        return arr[index];
                    default:
                        throw new HoconException(
                            $"Integer indexers only works on positive integer keyed {nameof(HoconObject)} or {nameof(HoconArray)}");
                }
            }
        }

        public virtual HoconElement this[string path]
        {
            get
            {
                if (this is HoconObject obj)
                    return obj[path];

                throw new HoconException($"String path indexers only works on {nameof(HoconObject)}");
            }
        }

        public virtual HoconElement this[HoconPath path]
        {
            get
            {
                if (this is HoconObject obj)
                    return obj[path];

                throw new HoconException($"HoconPath path indexers only works on {nameof(HoconObject)}");
            }
        }

        public HoconElement AtKey(string key)
        {
            return new HoconObjectBuilder
            {
                { key, this }
            }.Build();
        }

        public virtual bool HasPath(string path)
        {
            return this.TryGetValue(path, out _);
        }

        public virtual bool HasPath(HoconPath path)
        {
            return this.TryGetValue(path, out _);
        }

        public override bool Equals(object other)
        {
            if (other == null) return false;
            if (ReferenceEquals(this, other)) return true;
            if (!(other is HoconElement element)) return false;
            return Equals(element);
        }

        public abstract bool Equals(HoconElement other);

        public abstract string ToString(int indent, int indentSize);

        public static bool operator == (HoconElement left, HoconElement right)
        {
            if (ReferenceEquals(left, right)) return true;
            return left.Equals(right);
        }

        public static bool operator !=(HoconElement left, HoconElement right)
        {
            if (ReferenceEquals(left, right)) return false;
            return !left.Equals(right);
        }

        #region Casting operators

        public static implicit operator bool(HoconElement element)
        {
            if (element is HoconLiteral lit)
                return lit;

            throw new HoconException(
                $"Can only convert {nameof(HoconLiteral)} type to bool, found {element.GetType()} instead.");
        }

        public static implicit operator sbyte(HoconElement element)
        {
            if (element is HoconLiteral lit)
                return lit;

            throw new HoconException(
                $"Can only convert {nameof(HoconLiteral)} type to sbyte, found {element.GetType()} instead.");
        }

        public static implicit operator byte(HoconElement element)
        {
            if (element is HoconLiteral lit)
                return lit;

            throw new HoconException(
                $"Can only convert {nameof(HoconLiteral)} type to byte, found {element.GetType()} instead.");
        }

        public static implicit operator short(HoconElement element)
        {
            if (element is HoconLiteral lit)
                return lit;

            throw new HoconException(
                $"Can only convert {nameof(HoconLiteral)} type to short, found {element.GetType()} instead.");
        }

        public static implicit operator ushort(HoconElement element)
        {
            if (element is HoconLiteral lit)
                return lit;

            throw new HoconException(
                $"Can only convert {nameof(HoconLiteral)} type to ushort, found {element.GetType()} instead.");
        }

        public static implicit operator int(HoconElement element)
        {
            if (element is HoconLiteral lit)
                return lit;

            throw new HoconException(
                $"Can only convert {nameof(HoconLiteral)} type to int, found {element.GetType()} instead.");
        }

        public static implicit operator uint(HoconElement element)
        {
            if (element is HoconLiteral lit)
                return lit;

            throw new HoconException(
                $"Can only convert {nameof(HoconLiteral)} type to uint, found {element.GetType()} instead.");
        }

        public static implicit operator long(HoconElement element)
        {
            if (element is HoconLiteral lit)
                return lit;

            throw new HoconException(
                $"Can only convert {nameof(HoconLiteral)} type to long, found {element.GetType()} instead.");
        }

        public static implicit operator ulong(HoconElement element)
        {
            if (element is HoconLiteral lit)
                return lit;

            throw new HoconException(
                $"Can only convert {nameof(HoconLiteral)} type to ulong, found {element.GetType()} instead.");
        }

        public static implicit operator BigInteger(HoconElement element)
        {
            if (element is HoconLiteral lit)
                return lit;

            throw new HoconException(
                $"Can only convert {nameof(HoconLiteral)} type to BigInteger, found {element.GetType()} instead.");
        }

        public static implicit operator float(HoconElement element)
        {
            if (element is HoconLiteral lit)
                return lit;

            throw new HoconException(
                $"Can only convert {nameof(HoconLiteral)} type to float, found {element.GetType()} instead.");
        }

        public static implicit operator double(HoconElement element)
        {
            if (element is HoconLiteral lit)
                return lit;

            throw new HoconException(
                $"Can only convert {nameof(HoconLiteral)} type to double, found {element.GetType()} instead.");
        }

        public static implicit operator decimal(HoconElement element)
        {
            if (element is HoconLiteral lit)
                return lit;

            throw new HoconException(
                $"Can only convert {nameof(HoconLiteral)} type to decimal, found {element.GetType()} instead.");
        }

        public static implicit operator TimeSpan(HoconElement element)
        {
            if (element is HoconLiteral lit)
                return lit;

            throw new HoconException(
                $"Can only convert {nameof(HoconLiteral)} type to TimeSpan, found {element.GetType()} instead.");
        }

        public static implicit operator string(HoconElement element)
        {
            if (element is HoconLiteral lit)
                return lit.Value;

            throw new HoconException(
                $"Can only convert {nameof(HoconLiteral)} type to string, found {element.GetType()} instead.");
        }

        public static implicit operator char(HoconElement element)
        {
            if (element is HoconLiteral lit)
                return lit;

            throw new HoconException(
                $"Can only convert {nameof(HoconLiteral)} type to string, found {element.GetType()} instead.");
        }

        public static implicit operator bool[](HoconElement element)
        {
            switch (element)
            {
                case HoconArray arr:
                    return arr;
                case HoconObject obj:
                    return obj;
                default:
                    throw new HoconException(
                        $"Can only convert positive integer keyed {nameof(HoconObject)} and {nameof(HoconArray)}" +
                        $" into bool[], found {element.GetType()} instead.");
            }
        }

        public static implicit operator sbyte[](HoconElement element)
        {
            switch (element)
            {
                case HoconArray arr:
                    return arr;
                case HoconObject obj:
                    return obj;
                default:
                    throw new HoconException(
                        $"Can only convert positive integer keyed {nameof(HoconObject)} and {nameof(HoconArray)}" +
                        $" into sbyte[], found {element.GetType()} instead.");
            }
        }

        public static implicit operator byte[](HoconElement element)
        {
            switch (element)
            {
                case HoconArray arr:
                    return arr;
                case HoconObject obj:
                    return obj;
                default:
                    throw new HoconException(
                        $"Can only convert positive integer keyed {nameof(HoconObject)} and {nameof(HoconArray)}" +
                        $" into byte[], found {element.GetType()} instead.");
            }
        }

        public static implicit operator short[](HoconElement element)
        {
            switch (element)
            {
                case HoconArray arr:
                    return arr;
                case HoconObject obj:
                    return obj;
                default:
                    throw new HoconException(
                        $"Can only convert positive integer keyed {nameof(HoconObject)} and {nameof(HoconArray)}" +
                        $" into short[], found {element.GetType()} instead.");
            }
        }

        public static implicit operator ushort[](HoconElement element)
        {
            switch (element)
            {
                case HoconArray arr:
                    return arr;
                case HoconObject obj:
                    return obj;
                default:
                    throw new HoconException(
                        $"Can only convert positive integer keyed {nameof(HoconObject)} and {nameof(HoconArray)}" +
                        $" into ushort[], found {element.GetType()} instead.");
            }
        }

        public static implicit operator int[](HoconElement element)
        {
            switch (element)
            {
                case HoconArray arr:
                    return arr;
                case HoconObject obj:
                    return obj;
                default:
                    throw new HoconException(
                        $"Can only convert positive integer keyed {nameof(HoconObject)} and {nameof(HoconArray)}" +
                        $" into int[], found {element.GetType()} instead.");
            }
        }

        public static implicit operator uint[](HoconElement element)
        {
            switch (element)
            {
                case HoconArray arr:
                    return arr;
                case HoconObject obj:
                    return obj;
                default:
                    throw new HoconException(
                        $"Can only convert positive integer keyed {nameof(HoconObject)} and {nameof(HoconArray)}" +
                        $" into uint[], found {element.GetType()} instead.");
            }
        }

        public static implicit operator long[](HoconElement element)
        {
            switch (element)
            {
                case HoconArray arr:
                    return arr;
                case HoconObject obj:
                    return obj;
                default:
                    throw new HoconException(
                        $"Can only convert positive integer keyed {nameof(HoconObject)} and {nameof(HoconArray)}" +
                        $" into long[], found {element.GetType()} instead.");
            }
        }

        public static implicit operator ulong[](HoconElement element)
        {
            switch (element)
            {
                case HoconArray arr:
                    return arr;
                case HoconObject obj:
                    return obj;
                default:
                    throw new HoconException(
                        $"Can only convert positive integer keyed {nameof(HoconObject)} and {nameof(HoconArray)}" +
                        $" into ulong[], found {element.GetType()} instead.");
            }
        }

        public static implicit operator BigInteger[](HoconElement element)
        {
            switch (element)
            {
                case HoconArray arr:
                    return arr;
                case HoconObject obj:
                    return obj;
                default:
                    throw new HoconException(
                        $"Can only convert positive integer keyed {nameof(HoconObject)} and {nameof(HoconArray)}" +
                        $" into BigInteger[], found {element.GetType()} instead.");
            }
        }

        public static implicit operator float[](HoconElement element)
        {
            switch (element)
            {
                case HoconArray arr:
                    return arr;
                case HoconObject obj:
                    return obj;
                default:
                    throw new HoconException(
                        $"Can only convert positive integer keyed {nameof(HoconObject)} and {nameof(HoconArray)}" +
                        $" into float[], found {element.GetType()} instead.");
            }
        }

        public static implicit operator double[](HoconElement element)
        {
            switch (element)
            {
                case HoconArray arr:
                    return arr;
                case HoconObject obj:
                    return obj;
                default:
                    throw new HoconException(
                        $"Can only convert positive integer keyed {nameof(HoconObject)} and {nameof(HoconArray)}" +
                        $" into double[], found {element.GetType()} instead.");
            }
        }

        public static implicit operator decimal[](HoconElement element)
        {
            switch (element)
            {
                case HoconArray arr:
                    return arr;
                case HoconObject obj:
                    return obj;
                default:
                    throw new HoconException(
                        $"Can only convert positive integer keyed {nameof(HoconObject)} and {nameof(HoconArray)}" +
                        $" into decimal[], found {element.GetType()} instead.");
            }
        }

        public static implicit operator TimeSpan[](HoconElement element)
        {
            switch (element)
            {
                case HoconArray arr:
                    return arr;
                case HoconObject obj:
                    return obj;
                default:
                    throw new HoconException(
                        $"Can only convert positive integer keyed {nameof(HoconObject)} and {nameof(HoconArray)}" +
                        $" into TimeSpan[], found {element.GetType()} instead.");
            }
        }

        public static implicit operator string[](HoconElement element)
        {
            switch (element)
            {
                case HoconArray arr:
                    return arr;
                case HoconObject obj:
                    return obj;
                default:
                    throw new HoconException(
                        $"Can only convert positive integer keyed {nameof(HoconObject)} and {nameof(HoconArray)}" +
                        $" into string[], found {element.GetType()} instead.");
            }
        }

        public static implicit operator char[](HoconElement element)
        {
            switch (element)
            {
                case HoconArray arr:
                    return arr;
                case HoconObject obj:
                    return obj;
                case HoconLiteral lit:
                    return lit;
            }

            // Should never reach this code
            throw new HoconException("Should never reach this code");
        }

        #endregion
    }
}