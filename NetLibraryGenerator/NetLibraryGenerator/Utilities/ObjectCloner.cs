namespace NetLibraryGenerator.Utilities
{
    using System;
    using System.IO;
    using System.Runtime.Serialization;
    using System.Runtime.Serialization.Formatters.Binary;

    /// <summary>
    /// Reference Article http://www.codeproject.com/KB/tips/SerializedObjectCloner.aspx
    /// Provides a method for performing a deep copy of an object.
    /// Binary Serialization is used to perform the copy.
    /// </summary>
    public static class ObjectCloner
    {
        /// <summary>
        /// Perform a deep copy of the object via serialization.
        /// </summary>
        /// <typeparam name="T">The type of object being copied.</typeparam>
        /// <param name="source">The object instance to copy.</param>
        /// <returns>A deep copy of the object.</returns>
        public static T Clone<T>(T source)
        {
            if (source is null) {
                throw new ArgumentNullException("source", "Clone source shouldn't be null.");
            }

            if (!typeof(T).IsSerializable) {
                throw new ArgumentException("The type must be serializable.", nameof(source));
            }

            // Don't serialize a null object, simply return the default for that object
            if (ReferenceEquals(source, null)) return default!;

            using Stream stream = new MemoryStream();
            IFormatter formatter = new BinaryFormatter();
#pragma warning disable SYSLIB0011 // Тип или член устарел
            formatter.Serialize(stream, source);
            stream.Seek(0, SeekOrigin.Begin);
            return (T)formatter.Deserialize(stream);
#pragma warning restore SYSLIB0011 // Тип или член устарел
        }
    }
}