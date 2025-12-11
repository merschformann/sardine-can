using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SC.Core.Toolbox
{
    /// <summary>
    /// A stream writer implementation which also writes all output to the console
    /// </summary>
    public class DualLogWriter : StreamWriter
    {
        /// <summary>
        /// Enables or disables writing to the system diagnostics debug stream
        /// </summary>
        public bool DebugStreamWriting { get; set; }

        /// <summary>
        /// Creates a new dual log writer
        /// </summary>
        /// <param name="stream">The stream to write to</param>
        public DualLogWriter(Stream stream) : base(stream) { }

        /// <summary>
        /// Creates a new dual log writer
        /// </summary>
        /// <param name="stream">The stream to write to</param>
        /// <param name="encoding">The character encoding to use</param>
        public DualLogWriter(Stream stream, Encoding encoding) : base(stream, encoding) { }

        /// <summary>
        /// Creates a new dual log writer
        /// </summary>
        /// <param name="stream">The stream to write to</param>
        /// <param name="encoding">The character encoding to use</param>
        /// <param name="bufferSize">Sets the buffer size</param>
        public DualLogWriter(Stream stream, Encoding encoding, int bufferSize) : base(stream, encoding, bufferSize) { }

        /// <summary>
        /// Creates a new dual log writer
        /// </summary>
        /// <param name="path">The complete path of the file to write to. This might also be a filename.</param>
        public DualLogWriter(string path) : base(path) { }

        /// <summary>
        /// Creates a new dual log writer
        /// </summary>
        /// <param name="path">The complete path of the file to write to. This might also be a filename.</param>
        /// <param name="append">Determines whether the data will get appended to the file or if the file gets overwritten</param>
        public DualLogWriter(string path, bool append) : base(path, append) { }

        /// <summary>
        /// Creates a new dual log writer
        /// </summary>
        /// <param name="path">The complete path of the file to write to. This might also be a filename.</param>
        /// <param name="append">Determines whether the data will get appended to the file or if the file gets overwritten</param>
        /// <param name="encoding">The character encoding to use</param>
        public DualLogWriter(string path, bool append, Encoding encoding) : base(path, append, encoding) { }

        /// <summary>
        /// Creates a new dual log writer
        /// </summary>
        /// <param name="path">The complete path of the file to write to. This might also be a filename.</param>
        /// <param name="append">Determines whether the data will get appended to the file or if the file gets overwritten</param>
        /// <param name="encoding">The character encoding to use</param>
        /// <param name="bufferSize">Sets the buffer size</param>
        public DualLogWriter(string path, bool append, Encoding encoding, int bufferSize) : base(path, append, encoding, bufferSize) { }


        public override void Write(bool value)
        {
            base.Write(value);
            //Console.Write(value);
        }

        public override void Write(char value)
        {
            base.Write(value);
            //Console.Write(value);
        }

        public override void Write(char[] buffer)
        {
            base.Write(buffer);
            Console.Write(buffer);
            if (DebugStreamWriting)
            {
                for (int i = 0; i < buffer.Length; i++)
                {
                    Debug.Write(buffer[i]);
                }
            }
        }

        public override void Write(char[] buffer, int index, int count)
        {
            base.Write(buffer, index, count);
            Console.Write(buffer, index, count);
            if (DebugStreamWriting)
            {
                for (int i = index; (i < index + count) && (i < buffer.Length); i++)
                {
                    Debug.Write(buffer[i]);
                }
            }
        }

        public override void Write(decimal value)
        {
            base.Write(value);
            //Console.Write(value);
        }
        public override void Write(double value)
        {
            base.Write(value);
            //Console.Write(value);
        }

        public override void Write(float value)
        {
            base.Write(value);
            //Console.Write(value);
        }

        public override void Write(int value)
        {
            base.Write(value);
            //Console.Write(value);
        }

        public override void Write(long value)
        {
            base.Write(value);
            //Console.Write(value);
        }

        public override void Write(object value)
        {
            base.Write(value);
            //Console.Write(value);
        }

        public override void Write(string format, object arg0)
        {
            base.Write(format, arg0);
            //Console.Write(format, arg0);
        }

        public override void Write(string format, object arg0, object arg1)
        {
            base.Write(format, arg0, arg1);
            //Console.Write(format, arg0, arg1);
        }

        public override void Write(string format, object arg0, object arg1, object arg2)
        {
            base.Write(format, arg0, arg1, arg2);
            //Console.Write(format, arg0, arg1, arg2);
        }

        public override void Write(string format, params object[] arg)
        {
            base.Write(format, arg);
            //Console.Write(format, arg);
        }

        public override void Write(string value)
        {
            base.Write(value);
            Console.Write(value);
            if (DebugStreamWriting)
            {
                Debug.Write(value);
            }
        }

        public override void Write(uint value)
        {
            base.Write(value);
            //Console.Write(value);
        }

        public override void Write(ulong value)
        {
            base.Write(value);
            //Console.Write(value);
        }

        public override void WriteLine()
        {
            base.WriteLine();
            //Console.WriteLine();
        }

        public override void WriteLine(bool value)
        {
            base.WriteLine(value);
            //Console.WriteLine(value);
        }

        public override void WriteLine(char value)
        {
            base.WriteLine(value);
            //Console.WriteLine(value);
        }

        public override void WriteLine(char[] buffer)
        {
            base.WriteLine(buffer);
            //Console.WriteLine(buffer);
        }

        public override void WriteLine(char[] buffer, int index, int count)
        {
            base.WriteLine(buffer, index, count);
            //Console.WriteLine(buffer, index, count);
        }

        public override void WriteLine(decimal value)
        {
            base.WriteLine(value);
            //Console.WriteLine(value);
        }

        public override void WriteLine(double value)
        {
            base.WriteLine(value);
            //Console.WriteLine(value);
        }

        public override void WriteLine(float value)
        {
            base.WriteLine(value);
            //Console.WriteLine(value);
        }

        public override void WriteLine(int value)
        {
            base.WriteLine(value);
            //Console.WriteLine(value);
        }

        public override void WriteLine(long value)
        {
            base.WriteLine(value);
            //Console.WriteLine(value);
        }

        public override void WriteLine(object value)
        {
            base.WriteLine(value);
            //Console.WriteLine(value);
        }

        public override void WriteLine(string format, object arg0)
        {
            base.WriteLine(format, arg0);
            //Console.WriteLine(format, arg0);
        }

        public override void WriteLine(string format, object arg0, object arg1)
        {
            base.WriteLine(format, arg0, arg1);
            //Console.WriteLine(format, arg0, arg1);
        }

        public override void WriteLine(string format, object arg0, object arg1, object arg2)
        {
            base.WriteLine(format, arg0, arg1, arg2);
            //Console.WriteLine(format, arg0, arg1, arg2);
        }

        public override void WriteLine(string format, params object[] arg)
        {
            base.WriteLine(format, arg);
            //Console.WriteLine(format, arg);
        }

        public override void WriteLine(string value)
        {
            base.WriteLine(value);
            //Console.WriteLine(value);
        }

        public override void WriteLine(uint value)
        {
            base.WriteLine(value);
            //Console.WriteLine(value);
        }

        public override void WriteLine(ulong value)
        {
            base.WriteLine(value);
            //Console.WriteLine(value);
        }
    }
}
