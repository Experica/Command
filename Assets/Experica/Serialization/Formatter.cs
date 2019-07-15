/*
Formatter.cs is part of the Experica.
Copyright (c) 2016 Li Alex Zhang and Contributors

Permission is hereby granted, free of charge, to any person obtaining a 
copy of this software and associated documentation files (the "Software"),
to deal in the Software without restriction, including without limitation
the rights to use, copy, modify, merge, publish, distribute, sublicense,
and/or sell copies of the Software, and to permit persons to whom the 
Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included 
in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF 
OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
using System;
using System.Collections.Generic;

namespace Experica
{
    /// <summary>
    /// The purpose of this class is to have a multion capable of formatting (serializing/deserializing) in
    /// any format class that is derived from IFormat. This class will have a list of singleton objects
    /// corresponding to each format possible. To add a type of format, simply create the class, have it
    /// end in Format, and add the type to the enum DataType in Extension.cs
    /// </summary>
    public sealed class Formatter
    {
        private Dictionary<DataFormat, IFormat> _formats = new Dictionary<DataFormat, IFormat>();

        // With lazy instantiation, it is only created once referenced.
        private static readonly Lazy<Formatter> lazy = new Lazy<Formatter>(() => new Formatter());

        // The singleton Instance of the Formatter, its thread safe.
        public static Formatter Instance { get { return lazy.Value; } }

        /// <summary>
        /// Gets the active singleton object for the corresponding data format
        /// </summary>
        /// <param name="type">The type of DataFormat to retrieve.</param>
        /// <returns>The current active DataFormat of the given type</returns>
        private IFormat GetActiveFormat(DataFormat format)
        {
            if (!_formats.ContainsKey(format))
            {
                var str = format.ToString() + "Format";
                Type type = Type.GetType("Experica." + format.ToString() + "Format");
                var obj = (IFormat)Activator.CreateInstance(type);
                _formats[format] = obj;
            }

            return _formats[format];
        }

        /// <summary>
        /// Serializes the object using a specific type of DataFormat
        /// </summary>
        /// <typeparam name="T">type of the object to serialize.</typeparam>
        /// <param name="obj">Object to serialize.</param>
        /// <param name="format">The format to serialize the Object to.</param>
        /// <returns></returns>
        public string SerialzeDataToFormat<T>(T obj, DataFormat format)
        {
            IFormat formatToUse = GetActiveFormat(format);
            return formatToUse.Serialize(obj);
        }

        /// <summary>
        /// Deserializes an object
        /// </summary>
        /// <typeparam name="T">Type to deserialize to</typeparam>
        /// <param name="data">The data in the specific format to deserialize</param>
        /// <param name="format">The format the the string is in to deserialize.</param>
        /// <returns></returns>
        public T DeserializeUsingFormat<T>(string data, DataFormat format)
        {
            IFormat formatToUse = GetActiveFormat(format);
            return formatToUse.Deserialize<T>(data);
        }
    }
}
