using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace SC.GUI
{
    /// <summary>
    /// Enables writing to a given editbox
    /// </summary>
    internal class TextBoxWriter : TextWriter
    {
        /// <summary>
        /// Returns the used encoding
        /// </summary>
        public override Encoding Encoding
        {
            get { return System.Text.Encoding.UTF8; }
        }

        /// <summary>
        /// The used format provider for <code>ToString()</code>
        /// </summary>
        private IFormatProvider _format = CultureInfo.InvariantCulture;

        /// <summary>
        /// The sequence indicating a new line
        /// </summary>
        private const string NEW_LINE = "\r\n";

        /// <summary>
        /// The textbox to write to
        /// </summary>
        private TextBox _tb;

        /// <summary>
        /// This delegate enables asynchronous calls
        /// </summary>
        /// <param name="text">string to write</param>
        delegate void TextOutputCallBack(string text);

        /// <summary>
        /// Creates a new writer
        /// </summary>
        /// <param name="tb">The textbox to write to</param>
        /// <param name="activeForm">The operating form</param>
        public TextBoxWriter(TextBox tb)
            : base()
        {
            _tb = tb;
        }

        /// <summary>
        /// Prints the given string to the output
        /// </summary>
        /// <param name="s">The string to print</param>
        private void Print(string s)
        {
            _tb.AppendText(s);
        }

        /// <summary>
        /// Prints the given string to the output
        /// </summary>
        /// <param name="s">The string to print</param>
        private void PrintLine(string s)
        {
            _tb.AppendText(s);
            _tb.AppendText(NEW_LINE);
        }

        /// <summary>
        /// Builds a string from the given array
        /// </summary>
        /// <param name="buffer">The array to print</param>
        /// <param name="index">Index</param>
        /// <param name="count">Count</param>
        /// <returns>The desired string</returns>
        private string BuildString(char[] buffer, int index, int count)
        {
            string s = "";
            for (int i = index; (i < index + count) && (i < buffer.Length); i++)
            {
                s += buffer[i].ToString(_format);
            }
            return s;
        }

        /// <summary>
        /// Builds a string from the given array
        /// </summary>
        /// <param name="buffer">The array to print</param>
        /// <returns>The desired string</returns>
        private string BuildString(char[] buffer)
        {
            string s = "";
            for (int i = 0; i < buffer.Length; i++)
            {
                s += buffer[i].ToString(_format);
            }
            return s;
        }

        /// <summary>
        /// Prints the given string
        /// </summary>
        /// <param name="s">The string to print</param>
        private void Invoke(string s)
        {
            _tb.Dispatcher.Invoke(new Action<string>(Print), s);
        }

        /// <summary>
        /// Prints the given string with a carriage return at the end
        /// </summary>
        /// <param name="s">The string to print</param>
        private void InvokeLine(string s)
        {
            _tb.Dispatcher.Invoke(new Action<string>(PrintLine), s);
        }

        public override void Write(bool value)
        {
            Invoke(value.ToString(_format));
            Debug.Write(value);
        }

        public override void Write(char value)
        {
            Invoke(value.ToString(_format));
            Debug.Write(value);
        }

        public override void Write(char[] buffer)
        {
            Invoke(BuildString(buffer));
            Debug.Write(buffer);
        }

        public override void Write(char[] buffer, int index, int count)
        {
            Invoke(BuildString(buffer, index, count));
            Debug.Write(BuildString(buffer, index, count));
        }

        public override void Write(decimal value)
        {
            Invoke(value.ToString(_format));
            Debug.Write(value);
        }
        public override void Write(double value)
        {
            Invoke(value.ToString(_format));
            Debug.Write(value);
        }

        public override void Write(float value)
        {
            Invoke(value.ToString(_format));
            Debug.Write(value);
        }

        public override void Write(int value)
        {
            Invoke(value.ToString(_format));
            Debug.Write(value);
        }

        public override void Write(long value)
        {
            Invoke(value.ToString(_format));
            Debug.Write(value);
        }

        public override void Write(object value)
        {
            Invoke(value.ToString());
            Debug.Write(value);
        }

        public override void Write(string format, object arg0)
        {
            Invoke(string.Format(format, arg0));
            Debug.Write(string.Format(format, arg0));
        }

        public override void Write(string format, object arg0, object arg1)
        {
            Invoke(string.Format(format, arg0, arg1));
            Debug.Write(string.Format(format, arg0, arg1));
        }

        public override void Write(string format, object arg0, object arg1, object arg2)
        {
            Invoke(string.Format(format, arg0, arg1, arg2));
            Debug.Write(string.Format(format, arg0, arg1, arg2));
        }

        public override void Write(string format, params object[] arg)
        {
            Invoke(string.Format(format, arg));
            Debug.Write(string.Format(format, arg));
        }

        public override void Write(string value)
        {
            Invoke(value);
            Debug.Write(value);
        }

        public override void Write(uint value)
        {
            Invoke(value.ToString(_format));
            Debug.Write(value);
        }

        public override void Write(ulong value)
        {
            Invoke(value.ToString(_format));
            Debug.Write(value);
        }

        public override void WriteLine()
        {
            InvokeLine("");
            Debug.WriteLine("");
        }

        public override void WriteLine(bool value)
        {
            InvokeLine(value.ToString(_format));
            Debug.WriteLine(value);
        }

        public override void WriteLine(char value)
        {
            InvokeLine(value.ToString(_format));
            Debug.WriteLine(value);
        }

        public override void WriteLine(char[] buffer)
        {
            InvokeLine(BuildString(buffer));
            Debug.WriteLine(buffer);
        }

        public override void WriteLine(char[] buffer, int index, int count)
        {
            InvokeLine(BuildString(buffer, index, count));
            Debug.WriteLine(BuildString(buffer, index, count));
        }

        public override void WriteLine(decimal value)
        {
            InvokeLine(value.ToString(_format));
            Debug.WriteLine(value);
        }

        public override void WriteLine(double value)
        {
            InvokeLine(value.ToString(_format));
            Debug.WriteLine(value);
        }

        public override void WriteLine(float value)
        {
            InvokeLine(value.ToString(_format));
            Debug.WriteLine(value);
        }

        public override void WriteLine(int value)
        {
            InvokeLine(value.ToString(_format));
            Debug.WriteLine(value);
        }

        public override void WriteLine(long value)
        {
            InvokeLine(value.ToString(_format));
            Debug.WriteLine(value);
        }

        public override void WriteLine(object value)
        {
            InvokeLine(value.ToString());
            Debug.WriteLine(value);
        }

        public override void WriteLine(string format, object arg0)
        {
            InvokeLine(string.Format(format, arg0));
            Debug.WriteLine(string.Format(format, arg0));
        }

        public override void WriteLine(string format, object arg0, object arg1)
        {
            InvokeLine(string.Format(format, arg0, arg1));
            Debug.WriteLine(string.Format(format, arg0, arg1));
        }

        public override void WriteLine(string format, object arg0, object arg1, object arg2)
        {
            InvokeLine(string.Format(format, arg0, arg1, arg2));
            Debug.WriteLine(string.Format(format, arg0, arg1, arg2));
        }

        public override void WriteLine(string format, params object[] arg)
        {
            InvokeLine(string.Format(format, arg));
            Debug.WriteLine(string.Format(format, arg));
        }

        public override void WriteLine(string value)
        {
            InvokeLine(value);
            Debug.WriteLine(value);
        }

        public override void WriteLine(uint value)
        {
            InvokeLine(value.ToString(_format));
            Debug.WriteLine(value);
        }

        public override void WriteLine(ulong value)
        {
            InvokeLine(value.ToString(_format));
            Debug.WriteLine(value);
        }
    }
}
