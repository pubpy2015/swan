﻿namespace Unosquare.Swan.Formatters
{
    using System;
    using System.Collections.Generic;

    partial class Json
    {

        /// <summary>
        /// A simple JSON Deserializer
        /// </summary>
        private class Deserializer
        {
            /// <summary>
            /// Defines the different JSOn read states
            /// </summary>
            private enum ReadState
            {
                WaitingForRootOpen,
                WaitingForField,
                WaitingForColon,
                WaitingForValue,
                WaitingForNextOrRootClose,
            }

            #region State Variables

            private object Result = null;
            private Dictionary<string, object> ResultObject = null;
            private List<object> ResultArray = null;

            private ReadState State = ReadState.WaitingForRootOpen;
            private string CurrentFieldName = null;
            private int EndIndex = 0;

            #endregion

            private Deserializer(string json, int startIndex)
            {
                for (var i = startIndex; i < json.Length; i++)
                {
                    // Terminal.Trace($"Index {i} CurrentChar: '{json[i]}' CurrentState: {state}");
                    #region Wait for { or [
                    if (State == ReadState.WaitingForRootOpen)
                    {
                        if (char.IsWhiteSpace(json, i)) continue;

                        if (json[i] == OpenObjectChar)
                        {
                            ResultObject = new Dictionary<string, object>();
                            State = ReadState.WaitingForField;
                            continue;
                        }

                        if (json[i] == OpenArrayChar)
                        {
                            ResultArray = new List<object>();
                            State = ReadState.WaitingForValue;
                            continue;
                        }

                        throw new FormatException($"Parser error (char {i}, state {State}): Expected '{OpenObjectChar}' or '{OpenArrayChar}' but got '{json[i]}'.");
                    }

                    #endregion

                    #region Wait for opening field " (only applies for object results)

                    if (State == ReadState.WaitingForField)
                    {
                        if (char.IsWhiteSpace(json, i)) continue;

                        if (json[i] == StringQuotedChar)
                        {

                            var charCount = 0;
                            for (var j = i + 1; j < json.Length; j++)
                            {
                                if (json[j] == StringQuotedChar && json[j - 1] != StringEscapeChar)
                                    break;

                                charCount++;
                            }

                            CurrentFieldName = Unescape(json.SafeSubstring(i + 1, charCount));
                            i += charCount + 1;
                            State = ReadState.WaitingForColon;
                            continue;
                        }

                        throw new FormatException($"Parser error (char {i}, state {State}): Expected '{StringQuotedChar}' but got '{json[i]}'.");
                    }

                    #endregion

                    #region Wait for field-value separator : (only applies for object results

                    if (State == ReadState.WaitingForColon)
                    {
                        if (char.IsWhiteSpace(json, i)) continue;

                        if (json[i] == ValueSeparatorChar)
                        {
                            State = ReadState.WaitingForValue;
                            continue;
                        }

                        throw new FormatException($"Parser error (char {i}, state {State}): Expected '{ValueSeparatorChar}' but got '{json[i]}'.");
                    }

                    #endregion

                    #region Wait for and Parse the value

                    if (State == ReadState.WaitingForValue)
                    {
                        if (char.IsWhiteSpace(json, i)) continue;

                        // determine the value based on what it starts with
                        switch (json[i])
                        {
                            case StringQuotedChar: // expect a string
                                {
                                    var charCount = 0;
                                    for (var j = i + 1; j < json.Length; j++)
                                    {
                                        if (json[j] == StringQuotedChar && json[j - 1] != StringEscapeChar)
                                            break;

                                        charCount++;
                                    }

                                    // Extract and set the value
                                    var value = Unescape(json.SafeSubstring(i + 1, charCount));
                                    if (CurrentFieldName != null)
                                        ResultObject[CurrentFieldName] = value;
                                    else
                                        ResultArray.Add(value);

                                    // Update state variables
                                    i += charCount + 1;
                                    CurrentFieldName = null;
                                    State = ReadState.WaitingForNextOrRootClose;
                                    continue;
                                }
                            case OpenObjectChar: // expect object
                            case OpenArrayChar: // expect array
                                {
                                    // Extract and set the value
                                    var deserializer = new Deserializer(json, i);
                                    if (CurrentFieldName != null)
                                        ResultObject[CurrentFieldName] = deserializer.Result;
                                    else
                                        ResultArray.Add(deserializer.Result);

                                    // Update state variables
                                    i = deserializer.EndIndex;
                                    CurrentFieldName = null;
                                    State = ReadState.WaitingForNextOrRootClose;
                                    continue;
                                }
                            case 't': // expect true
                                {
                                    if (json.SafeSubstring(i, TrueLiteral.Length).Equals(TrueLiteral))
                                    {
                                        // Extract and set the value
                                        if (CurrentFieldName != null)
                                            ResultObject[CurrentFieldName] = true;
                                        else
                                            ResultArray.Add(true);

                                        // Update state variables
                                        i += TrueLiteral.Length - 1;
                                        CurrentFieldName = null;
                                        State = ReadState.WaitingForNextOrRootClose;
                                        continue;
                                    }

                                    throw new FormatException($"Parser error (char {i}, state {State}): Expected '{ValueSeparatorChar}' but got '{json.SafeSubstring(i, TrueLiteral.Length)}'.");
                                }
                            case 'f': // expect false
                                {
                                    if (json.SafeSubstring(i, FalseLiteral.Length).Equals(FalseLiteral))
                                    {
                                        // Extract and set the value
                                        if (CurrentFieldName != null)
                                            ResultObject[CurrentFieldName] = false;
                                        else
                                            ResultArray.Add(false);

                                        // Update state variables
                                        i += FalseLiteral.Length - 1;
                                        CurrentFieldName = null;
                                        State = ReadState.WaitingForNextOrRootClose;
                                        continue;
                                    }

                                    throw new FormatException($"Parser error (char {i}, state {State}): Expected '{ValueSeparatorChar}' but got '{json.SafeSubstring(i, FalseLiteral.Length)}'.");
                                }
                            case 'n': // expect null
                                {
                                    if (json.SafeSubstring(i, NullLiteral.Length).Equals(NullLiteral))
                                    {
                                        // Extract and set the value
                                        if (CurrentFieldName != null)
                                            ResultObject[CurrentFieldName] = null;
                                        else
                                            ResultArray.Add(null);

                                        // Update state variables
                                        i += NullLiteral.Length - 1;
                                        CurrentFieldName = null;
                                        State = ReadState.WaitingForNextOrRootClose;
                                        continue;
                                    }

                                    throw new FormatException($"Parser error (char {i}, state {State}): Expected '{ValueSeparatorChar}' but got '{json.SafeSubstring(i, NullLiteral.Length)}'.");
                                }
                            default: // expect number
                                {
                                    var charCount = 0;
                                    for (var j = i; j < json.Length; j++)
                                    {
                                        if (char.IsWhiteSpace(json[j]) || json[j] == FieldSeparatorChar)
                                            break;

                                        charCount++;
                                    }

                                    // Extract and set the value
                                    var stringValue = json.SafeSubstring(i, charCount);
                                    decimal value = 0M;

                                    if (decimal.TryParse(stringValue, out value) == false)
                                        throw new FormatException($"Parser error (char {i}, state {State}): Expected [number] but got '{stringValue}'.");

                                    if (CurrentFieldName != null)
                                        ResultObject[CurrentFieldName] = value;
                                    else
                                        ResultArray.Add(value);

                                    // Update state variables
                                    i += charCount - 1;
                                    CurrentFieldName = null;
                                    State = ReadState.WaitingForNextOrRootClose;
                                    continue;
                                }
                        }

                    }

                    #endregion

                    #region Wait for closing ], } or an additional field or value ,

                    if (State == ReadState.WaitingForNextOrRootClose)
                    {
                        if (char.IsWhiteSpace(json, i)) continue;

                        if (json[i] == FieldSeparatorChar)
                        {
                            if (ResultObject != null)
                            {
                                State = ReadState.WaitingForField;
                                CurrentFieldName = null;
                                continue;
                            }
                            else
                            {
                                State = ReadState.WaitingForValue;
                                continue;
                            }
                        }

                        if ((ResultObject != null && json[i] == CloseObjectChar) || (ResultArray != null && json[i] == CloseArrayChar))
                        {
                            EndIndex = i;
                            Result = (ResultObject == null) ? ResultArray as object : ResultObject;
                            return;
                        }

                        throw new FormatException($"Parser error (char {i}, state {State}): Expected '{FieldSeparatorChar}' '{CloseObjectChar}' or '{CloseArrayChar}' but got '{json[i]}'.");

                    }

                    #endregion

                }



            }

            static private string Unescape(string str)
            {
                // check if we need to unescape at all
                if (str.IndexOf(StringEscapeChar) < 0)
                    return str;

                // TODO: Unescape string here
                return str;
            }

            static public object Deserialize(string json)
            {
                var deserializer = new Deserializer(json, 0);
                return deserializer.Result;
            }

        }

    }
}
