using Experica;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Experica
{
    class HDF5Format : IFormat
    {
        /// <summary>
        /// NOT IMPLEMENTED. Will deserialize a string of text into an object.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="data"></param>
        /// <returns></returns>
        public T Deserialize<T>(string data)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// NOT IMPLEMENTED. Will Serialize an object into a serialize of bytes/text in hdf5 format
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <returns></returns>
        public string Serialize<T>(T obj)
        {
            throw new NotImplementedException();
        }
    }
}
