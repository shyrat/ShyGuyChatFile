using System;

namespace ShyGuy.ChatFile
{
    public class ShyGuyFileFormatException : Exception
    {
        public ShyGuyFileFormatException() : base()
        {

        }

        public ShyGuyFileFormatException(string message) : base(message)
        {

        }

        public ShyGuyFileFormatException(string message, Exception innerException) : base(message, innerException)
        {

        }
    }
}