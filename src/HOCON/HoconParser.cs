﻿//-----------------------------------------------------------------------
// <copyright file="HoconParser.cs" company="Hocon Project">
//     Copyright (C) 2009-2015 Typesafe Inc. <http://www.typesafe.com>
//     Copyright (C) 2013-2015 Akka.NET project <https://github.com/akkadotnet/Hocon>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Hocon
{
    public delegate string HoconIncludeCallback(HoconCallbackType callbackType, string value);
    public delegate Task<string> HoconIncludeCallbackAsync(HoconCallbackType callbackType, string value);

    /// <summary>
    /// This class contains methods used to parse HOCON (Human-Optimized Config Object Notation)
    /// configuration strings.
    /// </summary>
    public class HoconParser
    {
        private readonly List<HoconSubstitution> _substitutions = new List<HoconSubstitution>();
        private HoconIncludeCallbackAsync _includeCallback = (type, value) => Task.FromResult("{}");

        private HoconTokenizerResult _tokens;
        private HoconValue _root;

        private HoconPath Path { get; } = new HoconPath();

        /// <summary>
        /// Parses the supplied HOCON configuration string into a root element.
        /// </summary>
        /// <param name="text">The string that contains a HOCON configuration string.</param>
        /// <param name="includeCallback">Callback used to resolve includes</param>
        /// <returns>The root element created from the supplied HOCON configuration string.</returns>
        /// <exception cref="HoconParserException">
        /// This exception is thrown when an unresolved substitution is encountered.
        /// It also occurs when any error is encountered while tokenizing or parsing the configuration string.
        /// </exception>
        public static HoconRoot Parse(string text, HoconIncludeCallback includeCallback = null)
        {
            return new HoconParser().ParseText(text, true, includeCallback);
        }

        /// <inheritdoc cref="Parse(string, HoconIncludeCallback)"/>
        public static async Task<HoconRoot> ParseAsync(string text, HoconIncludeCallbackAsync includeCallback = null)
        {
            return await new HoconParser().ParseTextAsync(text, true, includeCallback).ConfigureAwait(false);
        }

        private HoconRoot ParseText(string text, bool resolveSubstitutions, HoconIncludeCallback includeCallback)
        {
            HoconIncludeCallbackAsync wrappedIncludeCallback = null;
            if (includeCallback != null)
                wrappedIncludeCallback = (t, s) => Task.FromResult(includeCallback(t, s));

            return AsyncHelper.RunSync(() => ParseTextAsync(text, resolveSubstitutions, wrappedIncludeCallback));
        }

        private async Task<HoconRoot> ParseTextAsync(string text, bool resolveSubstitutions, HoconIncludeCallbackAsync includeCallback)
        {
            if (string.IsNullOrWhiteSpace(text))
                throw new HoconParserException(
                    $"Parameter {nameof(text)} is null or empty.\n" +
                    "If you want to create an empty Hocon HoconRoot, use \"{}\" or just use \"new HoconRoot();\" instead.");

            // Workaround for annoying end of line difference between windows and unix
            if (Environment.NewLine == "\r\n")
                text = text.Replace(Environment.NewLine, "\n");

            if (includeCallback != null)
                _includeCallback = includeCallback;

            try
            {
                _tokens = new HoconTokenizer(text).Tokenize();
                _root = new HoconValue(null);
                await ParseTokensAsync().ConfigureAwait(false);
                if(resolveSubstitutions)
                    ResolveSubstitutions();
            }
            catch (HoconTokenizerException e)
            {
                throw HoconParserException.Create(e, null, $"Error while tokenizing Hocon: {e.Message}", e);
            }
            catch (HoconException e)
            {
                throw HoconParserException.Create(_tokens.Current, Path, e.Message, e);
            }

            return new HoconRoot(_root, _substitutions);
        }

        private void ResolveSubstitutions()
        {
            foreach (var sub in _substitutions)
            {
                // Retrieve value
                HoconValue res;
                try
                {
                    res = ResolveSubstitution(sub);
                }
                catch(HoconException e)
                {
                    throw HoconParserException.Create(sub, sub.Path, $"Invalid substitution declaration. {e.Message}.", e);
                }

                if (res != null)
                {
                    sub.ResolvedValue = res;
                    continue;
                }

                // Try to pull value from environment
                string envValue = null;
                try
                {
                    envValue = Environment.GetEnvironmentVariable(sub.Path.Value);
                }
                catch (Exception)
                {
                    // ignored
                }

                if (envValue != null)
                {
                    // undefined value resolved to an environment variable
                    res = new HoconValue(sub.Parent);
                    if (envValue.NeedQuotes())
                        res.Add(new HoconQuotedString(sub.Parent, envValue));
                    else
                        res.Add(new HoconUnquotedString(sub.Parent, envValue));

                    sub.ResolvedValue = res;
                    continue;
                }

                // ${ throws exception if it is not resolved
                if (sub.Required)
                    throw HoconParserException.Create(sub, sub.Path, $"Unresolved substitution: {sub.Path}");

                sub.ResolvedValue = new HoconEmptyValue(sub.Parent);
            }
        }

        private HoconValue ResolveSubstitution(HoconSubstitution sub)
        {
            var subField = sub.ParentField;

            // first case, this substitution is a direct self-reference
            if (sub.Path == subField.Path)
            {
                var parent = sub.Parent;
                while (parent is HoconValue)
                    parent = parent.Parent;

                // Fail case
                if (parent is HoconArray)
                    throw new HoconException("Self-referencing substitution may not be declared within an array.");

                // try to resolve substitution by looking backward in the field assignment stack
                return subField.OlderValueThan(sub);
            }

            // second case, the substitution references a field child in the past
            if (sub.Path.IsChildPathOf(subField.Path))
            {
                var olderValue = subField.OlderValueThan(sub);
                if (olderValue.Type == HoconType.Object)
                {
                    var difLength = sub.Path.Count - subField.Path.Count;
                    var deltaPath = sub.Path.SubPath(sub.Path.Count - difLength, difLength);

                    var olderObject = olderValue.GetObject();
                    if (olderObject.TryGetValue(deltaPath, out var innerValue))
                    {
                        return innerValue.Type == HoconType.Object ? innerValue : null;
                    }
                }
            }

            // Detect invalid parent-referencing substitution
            if (subField.Path.IsChildPathOf(sub.Path))
                throw new HoconException("Substitution may not reference one of its direct parents.");

            // Detect invalid cyclic reference loop
            if (IsValueCyclic(subField.Value, new List<HoconField> { subField }, new Stack<HoconSubstitution>()))
                throw new HoconException("A cyclic substitution loop is detected in the Hocon file.");

            // third case, regular substitution
            _root.GetObject().TryGetValue(sub.Path, out var field);
            return field;
        }

        private bool IsValueCyclic(HoconValue currentValue, List<HoconField> visitedFields, Stack<HoconSubstitution> pendingSubs)
        {
            foreach (var value in currentValue)
            {
                switch (value)
                {
                    case HoconLiteral _:
                        break;
                    case HoconObject o:
                        foreach (var field in o.Values)
                        {
                            if (visitedFields.Contains(field))
                                return true;
                            visitedFields.Add(field);
                            if (IsValueCyclic(field.Value, visitedFields, pendingSubs))
                                return true;
                        }
                        break;
                    case HoconArray a:
                        foreach (var item in a.GetArray())
                        {
                            if (IsValueCyclic(item, visitedFields, pendingSubs))
                                return true;
                        }
                        break;
                    case HoconSubstitution s:
                        pendingSubs.Push(s);
                        break;
                }
            }

            while (pendingSubs.Count > 0)
            {
                var currentSub = pendingSubs.Pop();
                if (!_root.GetObject().TryGetField(currentSub.Path, out var field))
                    continue;
                if (visitedFields.Contains(field))
                {
                    return true;
                }
                if (IsValueCyclic(field.Value, visitedFields, pendingSubs))
                    return true;
            }
            return false;
        }

        private async Task ParseTokensAsync()
        {
            if (_tokens.Current.IsNonSignificant())
                ConsumeWhitelines();

            while (_tokens.Current.Type != TokenType.EndOfFile)
            {
                switch (_tokens.Current.Type)
                {
                    case TokenType.Include:
                        var parsedInclude = await ParseIncludeAsync(null).ConfigureAwait(false);
                        if (_root.Type != HoconType.Object)
                        {
                            _root.Clear();
                            _root.Add(parsedInclude.GetObject());
                        }
                        else
                            _root.Add(parsedInclude.GetObject());
                        break;

                    // Hocon config file may contain one array and one array only
                    case TokenType.StartOfArray:
                        _root.Clear();
                        _root.Add(await ParseArrayAsync(null).ConfigureAwait(false));
                        ConsumeWhitelines();
                        if (_tokens.Current.Type != TokenType.EndOfFile)
                            throw HoconParserException.Create(_tokens.Current, Path, "Hocon config can only contain one array or one object.");
                        return;

                    case TokenType.StartOfObject:
                    {
                        var parsedObject = await ParseObjectAsync(null).ConfigureAwait(false);
                        if (_root.Type != HoconType.Object)
                        {
                            _root.Clear();
                            _root.Add(parsedObject);
                        }
                        else
                            _root.Add(parsedObject.GetObject());
                        break;
                    }

                    case TokenType.LiteralValue:
                    {
                        if (_tokens.Current.IsNonSignificant())
                            ConsumeWhitelines();
                        if (_tokens.Current.Type != TokenType.LiteralValue)
                            break;

                        var parsedObject = await ParseObjectAsync(null).ConfigureAwait(false);
                        if (_root.Type != HoconType.Object)
                        {
                            _root.Clear();
                            _root.Add(parsedObject);
                        }
                        else
                            _root.Add(parsedObject.GetObject());
                        break;
                    }

                    case TokenType.Comment:
                    case TokenType.EndOfLine:
                    case TokenType.EndOfFile:
                    case TokenType.EndOfObject:
                    case TokenType.EndOfArray:
                        _tokens.Next();
                        break;

                    default:
                        throw HoconParserException.Create(_tokens.Current, null, $"Illegal token type: {_tokens.Current.Type}", null);
                }
            }
        }

        private async Task<IHoconElement> ParseIncludeAsync(IHoconElement owner)
        {
            // Sanity check
            if (_tokens.Current.Type != TokenType.Include)
                throw HoconParserException.Create(_tokens.Current, Path,
                    $"Internal parser error, ParseInclude() is called on an invalid token: `{_tokens.Current.Type}`");

            var parenthesisCount = 0;
            var required = false;
            var callbackType = HoconCallbackType.File;
            string fileName = null;
            var includeToken = _tokens.Current;

            List<TokenType> expectedTokens = new List<TokenType>(new[]
            {
                TokenType.Required,
                TokenType.Url,
                TokenType.File,
                TokenType.Classpath,
                TokenType.LiteralValue,
                TokenType.ParenthesisEnd,
                TokenType.EndOfLine
            });

            var parsing = true;
            while (parsing)
            {
                if (!_tokens.GetNextSignificant(expectedTokens.ToArray()))
                    throw HoconParserException.Create(_tokens.Current, Path,
                        $"Invalid token in include: `{_tokens.Current.Type}`", null);

                switch (_tokens.Current.Type)
                {
                    case TokenType.ParenthesisEnd:
                        if (parenthesisCount == 0)
                            throw HoconParserException.Create(_tokens.Current, Path,
                                "Unexpected closing parenthesis.", null);

                        parenthesisCount--;
                        parsing = parenthesisCount > 0;
                        break;

                    case TokenType.Required:
                        if (!_tokens.GetNextSignificant(TokenType.ParenthesisStart))
                            throw HoconParserException.Create(_tokens.Current, Path,
                                $"Expected {TokenType.ParenthesisStart}, found `{_tokens.Current.Type}` instead.");

                        parenthesisCount++;
                        required = true;
                        expectedTokens.Remove(TokenType.Required);
                        break;

                    case TokenType.Url:
                        if (!_tokens.GetNextSignificant(TokenType.ParenthesisStart))
                            throw HoconParserException.Create(_tokens.Current, Path,
                                $"Expected {TokenType.ParenthesisStart}, found `{_tokens.Current.Type}` instead.");

                        parenthesisCount++;
                        callbackType = HoconCallbackType.Url;
                        expectedTokens.Remove(TokenType.Required);
                        expectedTokens.Remove(TokenType.Url);
                        expectedTokens.Remove(TokenType.File);
                        expectedTokens.Remove(TokenType.Classpath);
                        break;

                    case TokenType.File:
                        if (!_tokens.GetNextSignificant(TokenType.ParenthesisStart))
                            throw HoconParserException.Create(_tokens.Current, Path,
                                $"Expected {TokenType.ParenthesisStart}, found `{_tokens.Current.Type}` instead.");

                        parenthesisCount++;
                        callbackType = HoconCallbackType.File;
                        expectedTokens.Remove(TokenType.Required);
                        expectedTokens.Remove(TokenType.Url);
                        expectedTokens.Remove(TokenType.File);
                        expectedTokens.Remove(TokenType.Classpath);
                        break;

                    case TokenType.Classpath:
                        if (!_tokens.GetNextSignificant(TokenType.ParenthesisStart))
                            throw HoconParserException.Create(_tokens.Current, Path,
                                $"Expected {TokenType.ParenthesisStart}, found `{_tokens.Current.Type}` instead.");

                        parenthesisCount++;
                        callbackType = HoconCallbackType.Resource;
                        expectedTokens.Remove(TokenType.Required);
                        expectedTokens.Remove(TokenType.Url);
                        expectedTokens.Remove(TokenType.File);
                        expectedTokens.Remove(TokenType.Classpath);
                        break;

                    case TokenType.LiteralValue:
                        if(_tokens.Current.IsNonSignificant())
                            ConsumeWhitespace();
                        if (_tokens.Current.Type != TokenType.LiteralValue)
                            break;

                        if (_tokens.Current.LiteralType != TokenLiteralType.QuotedLiteralValue)
                            throw HoconParserException.Create(_tokens.Current, Path,
                                $"Invalid literal type for declaring file name. Expected {TokenLiteralType.QuotedLiteralValue}, " +
                                $"found `{_tokens.Current.LiteralType}` instead.");

                        fileName = _tokens.Current.Value;
                        expectedTokens.Remove(TokenType.LiteralValue);
                        expectedTokens.Remove(TokenType.Required);
                        expectedTokens.Remove(TokenType.Url);
                        expectedTokens.Remove(TokenType.File);
                        expectedTokens.Remove(TokenType.Classpath);

                        parsing = parenthesisCount > 0;
                        break;
                    default:
                        throw HoconParserException.Create(_tokens.Current, Path,
                            $"Unexpected token `{_tokens.Current.Type}`.");
                }
            }

            if (parenthesisCount > 0)
                throw HoconParserException.Create(_tokens.Current, Path,
                    $"Expected {TokenType.ParenthesisEnd}, found `{_tokens.Current.Type}`");

            if (fileName == null)
                throw HoconParserException.Create(_tokens.Current, Path,
                    "Include does not contain any quoted file name value.");

            // Consume the last token
            _tokens.Next();

            var includeHocon = await _includeCallback(callbackType, fileName).ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(includeHocon))
            {
                if(required)
                throw HoconParserException.Create(includeToken, Path,
                    "Invalid Hocon include. Include was declared as required but include callback returned a null or empty string.");
                return new HoconEmptyValue(owner);
            }

            var includeRoot = await new HoconParser().ParseTextAsync(includeHocon, false, _includeCallback).ConfigureAwait(false);
            if (owner.Type != HoconType.Empty && owner.Type != includeRoot.Value.Type)
                throw HoconParserException.Create(includeToken, Path,
                    $"Invalid Hocon include. Hocon config substitution type must be the same as the field it's merged into. " +
                    $"Expected type: `{owner.Type}`, type returned by include callback: `{includeRoot.Value.Type}`");

            //fixup the substitution, add the current path as a prefix to the substitution path
            foreach (var substitution in includeRoot.Substitutions)
            {
                substitution.Path.InsertRange(0, Path);
            }
            _substitutions.AddRange(includeRoot.Substitutions);

            // reparent the value returned by the callback to the owner of the include declaration
            return includeRoot.Value.Clone(owner);
        }

        // The owner in this context can be either an object or an array.
        private async Task<HoconObject> ParseObjectAsync(IHoconElement owner)
        {
            var hoconObject = new HoconObject(owner);

            if (_tokens.Current.Type != TokenType.StartOfObject
                && _tokens.Current.Type != TokenType.LiteralValue
                && _tokens.Current.Type != TokenType.Include)
                throw HoconParserException.Create(_tokens.Current, Path,
                    $"Failed to parse Hocon object. Expected `{TokenType.StartOfObject}` or `{TokenType.LiteralValue}, " +
                    $"found `{_tokens.Current.Type}` instead.");

            var headless = true;
            if (_tokens.Current.Type == TokenType.StartOfObject)
            {
                headless = false;
                ConsumeWhitelines();
            }

            IHoconElement lastValue = null;
            var parsing = true;
            while (parsing)
            {
                switch (_tokens.Current.Type)
                {
                    case TokenType.Include:
                        if (lastValue != null)
                            throw HoconParserException.Create(_tokens.Current, Path,
                                $"Failed to parse Hocon object. Expected `{TokenType.Comma}` or `{TokenType.EndOfLine}`, " +
                                $"found `{_tokens.Current.Type}` instead.");

                        lastValue = await ParseIncludeAsync(hoconObject).ConfigureAwait(false);
                        break;

                    case TokenType.LiteralValue:
                        if (_tokens.Current.IsNonSignificant())
                            ConsumeWhitespace();
                        if (_tokens.Current.Type != TokenType.LiteralValue)
                            break;

                        if (lastValue != null)
                            throw HoconParserException.Create(_tokens.Current, Path,
                                $"Failed to parse Hocon object. Expected `{TokenType.Comma}` or `{TokenType.EndOfLine}`, " +
                                $"found `{_tokens.Current.Type}` instead.");

                        lastValue = await ParseFieldAsync(hoconObject).ConfigureAwait(false);
                        break;

                    // TODO: can an object be declared floating without being assigned to a field?
                    //case TokenType.StartOfObject:
                    case TokenType.Comment:
                    case TokenType.EndOfLine:
                        switch (lastValue)
                        {
                            case null:
                                ConsumeWhitelines();
                                break;
                            case HoconField _:
                                break;
                            default:
                                ((HoconValue)hoconObject.Parent).Add(lastValue.GetObject());
                                break;
                        }
                        lastValue = null;
                        ConsumeWhitelines();
                        break;

                    case TokenType.Comma:
                        switch (lastValue)
                        {
                            case null:
                                throw HoconParserException.Create(_tokens.Current, Path,
                                    $"Failed to parse Hocon object. Expected `{TokenType.Assign}` or `{TokenType.StartOfObject}`, " +
                                    $"found `{_tokens.Current.Type}` instead.");
                            case HoconField _:
                                break;
                            default:
                                ((HoconValue)hoconObject.Parent).Add(lastValue.GetObject());
                                break;
                        }
                        lastValue = null;
                        ConsumeWhitelines();
                        break;

                    case TokenType.EndOfObject:
                    case TokenType.EndOfFile:
                        if (headless && _tokens.Current.Type != TokenType.EndOfFile)
                            throw HoconParserException.Create(_tokens.Current, Path,
                                $"Failed to parse Hocon object. Expected {TokenType.EndOfFile} but found {_tokens.Current.Type} instead.");

                        if (!headless && _tokens.Current.Type != TokenType.EndOfObject)
                            throw HoconParserException.Create(_tokens.Current, Path,
                                $"Failed to parse Hocon object. Expected {TokenType.EndOfObject} but found {_tokens.Current.Type} instead.");

                        switch (lastValue)
                        {
                            case null:
                                break;
                            case HoconField _:
                                break;
                            default:
                                ((HoconValue)hoconObject.Parent).Add(lastValue.GetObject());
                                break;
                        }
                        lastValue = null;
                        parsing = false;
                        break;

                    default:
                        throw HoconParserException.Create(_tokens.Current, Path,
                            $"Failed to parse Hocon object. Unexpected token `{_tokens.Current.Type}`.");
                }
            }
            if (_tokens.Current.Type == TokenType.EndOfObject)
                _tokens.Next();
            return hoconObject;
        }

        // parse path value
        private HoconPath ParseKey()
        {
            while (_tokens.Current.LiteralType == TokenLiteralType.Whitespace)
                _tokens.Next();

            // sanity check
            if (_tokens.Current.Type != TokenType.LiteralValue)
                throw HoconParserException.Create(_tokens.Current, Path,
                    $"Internal parser error, ParseKey() is called on an invalid token. Should be `{TokenType.LiteralValue}`, found `{_tokens.Current.Type}` instead.");

            if (_tokens.Current.IsNonSignificant())
                ConsumeWhitelines();
            if (_tokens.Current.Type != TokenType.LiteralValue)
                return null;

            var keyTokens = new HoconTokenizerResult();
            while (_tokens.Current.Type == TokenType.LiteralValue)
            {
                keyTokens.Add(_tokens.Current);
                _tokens.Next();
            }

            keyTokens.Reverse();
            while (keyTokens.Count > 0 && keyTokens[0].LiteralType == TokenLiteralType.Whitespace)
            {
                keyTokens.RemoveAt(0);
            }
            keyTokens.Reverse();

            keyTokens.Add(new Token("", TokenType.EndOfFile, null));

            return HoconPath.FromTokens(keyTokens);
        }

        private async Task<HoconField> ParseFieldAsync(HoconObject owner)
        {
            // sanity check
            if(_tokens.Current.Type != TokenType.LiteralValue)
                throw HoconParserException.Create(_tokens.Current, Path,
                    $"Failed to parse Hocon field. Expected start of field {TokenType.LiteralValue}, " +
                    $"found {_tokens.Current.Type} instead.");

            var pathDelta = ParseKey();
            if (_tokens.Current.IsNonSignificant())
                ConsumeWhitelines();

            // sanity check
            if (_tokens.Current.Type != TokenType.Assign && _tokens.Current.Type != TokenType.StartOfObject)
                throw HoconParserException.Create(_tokens.Current, Path,
                    $"Failed to parse Hocon field. Expected {TokenType.Assign} or {TokenType.StartOfObject}, " +
                    $"found {_tokens.Current.Type} instead.");

            // sanity check
            if (pathDelta == null || pathDelta.Count == 0)
                throw HoconParserException.Create(_tokens.Current, Path,
                    "Failed to parse Hocon field. ParseField() was called with null or empty path");

            List<HoconField> childInPath = owner.TraversePath(pathDelta);

            Path.AddRange(pathDelta);
            HoconField currentField = childInPath[childInPath.Count - 1];

            if (_tokens.Current.Type != TokenType.Assign && _tokens.Current.Type != TokenType.StartOfObject)
                throw HoconParserException.Create(_tokens.Current, Path,
                    $"Failed to parse Hocon field. Unexpected token: `{_tokens.Current.Type}`.");

            var parsedValue = await ParseValueAsync(currentField).ConfigureAwait(false);

            foreach (var removedSub in currentField.SetValue(parsedValue))
            {
                _substitutions.Remove(removedSub);
            }

            Path.RemoveRange(Path.Count - pathDelta.Count, pathDelta.Count);
            return childInPath[0];
        }

        /// <summary>
        /// Retrieves the next value token from the tokenizer and appends it
        /// to the supplied element <paramref name="owner"/>.
        /// </summary>
        /// <param name="owner">The element to append the next token.</param>
        /// <exception cref="System.Exception">End of file reached while trying to read a value</exception>
        private async Task<HoconValue> ParseValueAsync(IHoconElement owner)
        {
            var value = new HoconValue(owner);
            var parsing = true;
            while (parsing)
            {
                switch (_tokens.Current.Type)
                {
                    case TokenType.Include:
                        value.Add(await ParseIncludeAsync(value).ConfigureAwait(false));
                        break;

                    case TokenType.LiteralValue:
                        // Consume leading whitespaces.
                        if (_tokens.Current.IsNonSignificant())
                            ConsumeWhitespace();
                        if (_tokens.Current.Type != TokenType.LiteralValue)
                            break;

                        while (_tokens.Current.Type == TokenType.LiteralValue)
                        {
                            value.Add(HoconLiteral.Create(value, _tokens.Current));
                            _tokens.Next();
                        }
                        break;

                    case TokenType.StartOfObject:
                        value.Add(await ParseObjectAsync(value).ConfigureAwait(false));
                        break;

                    case TokenType.StartOfArray:
                        value.Add(await ParseArrayAsync(value).ConfigureAwait(false));
                        break;

                    case TokenType.SubstituteOptional:
                    case TokenType.SubstituteRequired:
                        var pointerPath = HoconPath.Parse(_tokens.Current.Value);
                        HoconSubstitution sub = new HoconSubstitution(value, pointerPath, _tokens.Current,
                            _tokens.Current.Type == TokenType.SubstituteRequired);
                        _substitutions.Add(sub);
                        value.Add(sub);
                        _tokens.Next();
                        break;

                    case TokenType.EndOfObject:
                    case TokenType.EndOfArray:
                        parsing = false;
                        break;

                    // comments automatically stop value parsing.
                    case TokenType.Comment:
                        ConsumeWhitelines();
                        parsing = false;
                        break;

                    case TokenType.EndOfLine:
                        parsing = false;
                        break;

                    case TokenType.EndOfFile:
                    case TokenType.Comma:
                        parsing = false;
                        break;

                    case TokenType.Assign:
                        ConsumeWhitelines();
                        break;

                    default:
                        throw HoconParserException.Create(_tokens.Current, Path,
                            $"Failed to parse Hocon value. Unexpected token: `{_tokens.Current.Type}`");
                }
            }

            // trim trailing whitespace if result is a literal
            if (value.Type == HoconType.Literal)
            {
                if (value[value.Count - 1] is HoconWhitespace)
                    value.RemoveAt(value.Count - 1);
            }
            return value;
        }

        /// <summary>
        /// Retrieves the next array token from the tokenizer.
        /// </summary>
        /// <returns>An array of elements retrieved from the token.</returns>
        private async Task<HoconArray> ParseArrayAsync(IHoconElement owner)
        {
            var currentArray = new HoconArray(owner);

            // consume start of array token
            ConsumeWhitelines();

            IHoconElement lastValue = null;
            var parsing = true;
            while (parsing)
            {
                switch (_tokens.Current.Type)
                {
                    case TokenType.Include:
                        if (lastValue != null)
                            throw HoconParserException.Create(_tokens.Current, Path,
                                $"Failed to parse Hocon array. Expected `{TokenType.Comma}` or `{TokenType.EndOfLine}, " +
                                $"found `{_tokens.Current.Type}` instead.");

                        lastValue = await ParseIncludeAsync(currentArray).ConfigureAwait(false);
                        break;

                    case TokenType.StartOfArray:
                        if (lastValue != null)
                            throw HoconParserException.Create(_tokens.Current, Path,
                                $"Failed to parse Hocon array. Expected `{TokenType.Comma}` or `{TokenType.EndOfLine}, " +
                                $"found `{_tokens.Current.Type}` instead.");

                        // Array inside of arrays are parsed as values because it can be value concatenated with another array.
                        lastValue = await ParseValueAsync(currentArray).ConfigureAwait(false);
                        break;

                    case TokenType.StartOfObject:
                        if (lastValue != null)
                            throw HoconParserException.Create(_tokens.Current, Path,
                                $"Failed to parse Hocon array. Expected `{TokenType.Comma}` or `{TokenType.EndOfLine}, " +
                                $"found `{_tokens.Current.Type}` instead.");

                        lastValue = await ParseObjectAsync(currentArray).ConfigureAwait(false);
                        break;

                    case TokenType.LiteralValue:
                        if (_tokens.Current.IsNonSignificant())
                            ConsumeWhitelines();
                        if (_tokens.Current.Type != TokenType.LiteralValue)
                            break;

                        if (lastValue != null)
                            throw HoconParserException.Create(_tokens.Current, Path,
                                $"Failed to parse Hocon array. Expected `{TokenType.Comma}` or `{TokenType.EndOfLine}, " +
                                $"found `{_tokens.Current.Type}` instead.");

                        lastValue = await ParseValueAsync(currentArray).ConfigureAwait(false);
                        break;

                    case TokenType.SubstituteOptional:
                    case TokenType.SubstituteRequired:
                        if (lastValue != null)
                            throw HoconParserException.Create(_tokens.Current, Path,
                                $"Failed to parse Hocon array. Expected `{TokenType.Comma}` or `{TokenType.EndOfLine}, " +
                                $"found `{_tokens.Current.Type}` instead.");

                        var pointerPath = HoconPath.Parse(_tokens.Current.Value);
                        HoconSubstitution sub = new HoconSubstitution(currentArray, pointerPath, _tokens.Current,
                            _tokens.Current.Type == TokenType.SubstituteRequired);
                        _substitutions.Add(sub);
                        lastValue = sub;
                        _tokens.Next();
                        break;

                    case TokenType.Comment:
                    case TokenType.EndOfLine:
                        if (lastValue == null)
                        {
                            ConsumeWhitelines();
                            break;
                        }

                        switch (lastValue.Type)
                        {
                            case HoconType.Array:
                                currentArray.Add(lastValue);
                                break;
                            default:
                                currentArray.Add((HoconValue) lastValue);
                                break;
                        }
                        lastValue = null;
                        ConsumeWhitelines();
                        break;

                    case TokenType.Comma:
                        if (lastValue == null)
                            throw HoconParserException.Create(_tokens.Current, Path,
                                $"Failed to parse Hocon array. Expected a valid value, found `{_tokens.Current.Type}` instead.");

                        switch (lastValue.Type)
                        {
                            case HoconType.Array:
                                currentArray.Add(lastValue);
                                break;
                            default:
                                currentArray.Add(lastValue);
                                break;
                        }
                        lastValue = null;
                        ConsumeWhitelines();
                        break;

                    case TokenType.EndOfArray:
                        if (lastValue != null)
                        {
                            switch (lastValue.Type)
                            {
                                case HoconType.Array:
                                    currentArray.Add(lastValue);
                                    break;
                                default:
                                    currentArray.Add((HoconValue) lastValue);
                                    break;
                            }
                            lastValue = null;
                        }
                        parsing = false;
                        break;

                    default:
                        throw HoconParserException.Create(_tokens.Current, Path,
                            $"Failed to parse Hocon array. Expected {TokenType.EndOfArray} but found {_tokens.Current.Type} instead.");
                }
            }

            // Consume end of array token
            _tokens.Next();
            return currentArray;
        }

        // be careful when using consume methods because it also consume the current token.
        private void ConsumeWhitespace()
        {
            while (_tokens.Next().Type != TokenType.EndOfFile)
            {
                switch (_tokens.Current.Type)
                {
                    case TokenType.LiteralValue:
                        if (_tokens.Current.LiteralType == TokenLiteralType.Whitespace)
                            continue;
                        return;
                    case TokenType.Comment:
                        continue;
                    default:
                        return;
                }
            }
        }

        // be careful when using consume methods because it also consume the current token.
        private void ConsumeWhitelines()
        {
            while (_tokens.Next().Type != TokenType.EndOfFile)
            {
                switch (_tokens.Current.Type)
                {
                    case TokenType.LiteralValue:
                        if (_tokens.Current.LiteralType == TokenLiteralType.Whitespace)
                            continue;
                        return;
                    case TokenType.EndOfLine:
                    case TokenType.Comment:
                        continue;
                    default:
                        return;
                }
            }
        }
    }
}