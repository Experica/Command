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
    public sealed class Formatter
    {
        // This can be static in a console/desktop application, just be wary of potential memory issues
        private Dictionary<DataFormat, IFormat> _formats = new Dictionary<DataFormat, IFormat>();

        private static readonly Lazy<Formatter> lazy = new Lazy<Formatter>(() => new Formatter());

        public static Formatter Instance { get { return lazy.Value; } }

        /// <summary>
        /// Get the current active policy of the given type.
        /// </summary>
        /// <param name="type">The type of policy to retrieve.</param>
        /// <returns>The current active policy of the given type</returns>
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

        public string SerialzeDataToFormat<T>(T obj, DataFormat format)
        {
            IFormat formatToUse = GetActiveFormat(format);
            return formatToUse.Serialize(obj);
        }

        public T DeserializeUsingFormat<T>(string data, DataFormat format)
        {
            IFormat formatToUse = GetActiveFormat(format);
            return formatToUse.Deserialize<T>(data);
        }
    }
}
