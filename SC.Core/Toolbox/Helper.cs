using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SC.Core.Toolbox
{
    /// <summary>
    /// Contains simple helper methods to use in various situations
    /// </summary>
    public class Helper
    {
        /// <summary>
        /// Returns the name of a field providing more type-safety - use <code>string name = Check(() => field);</code> (on the basis of <see cref="http://abdullin.com/journal/2008/12/13/how-to-find-out-variable-or-parameter-name-in-c.html"/>)
        /// </summary>
        /// <typeparam name="T">The type of the field</typeparam>
        /// <param name="expr">The expression including the field</param>
        /// <returns>Code-name of the field</returns>
        public static string Check<T>(Expression<Func<T>> expr)
        {
            if (expr.Body is MemberExpression)
            {
                var body = ((MemberExpression)expr.Body);
                return body.Member.Name;
            }
            else
            {
                if (expr.Body is ConstantExpression body)
                {
                    return body.Value.ToString();
                }
                else
                {
                    throw new ArgumentException("Cannot handle this expression: " + expr.ToString());
                }
            }

        }

        /// <summary>
        /// Flattens a sequence of strings into one string
        /// </summary>
        /// <param name="strings">The list to flatten</param>
        /// <param name="separator">The element which separates the strings</param>
        /// <returns>One string consisting of the given strings</returns>
        public static string GetOneString(IEnumerable<string> strings, string separator)
        {
            StringBuilder sb = new StringBuilder();
            int count = 1;
            int overallCount = strings.Count();
            foreach (var ele in strings)
            {
                sb.Append(ele);
                if (count < overallCount)
                {
                    sb.Append(separator);
                }
                count++;
            }
            return sb.ToString();
        }

        /// <summary>
        /// Executes a function with a given timeout (on the basis of <see cref="http://stackoverflow.com/questions/1370811/implementing-a-timeout-on-a-function-returning-a-value"/>)
        /// </summary>
        /// <typeparam name="T">The type the function returns</typeparam>
        /// <param name="func">The function to execute</param>
        /// <param name="timeout">The timeout in milliseconds</param>
        /// <returns>The value the function returns</returns>
        public static T Execute<T>(Func<T> func, int timeout)
        {
            T result;
            TryExecute(func, timeout, out result);
            return result;
        }

        /// <summary>
        /// Tries to execute the given function and aborts it after a given timeout (on the basis of <see cref="http://stackoverflow.com/questions/1370811/implementing-a-timeout-on-a-function-returning-a-value"/>)
        /// </summary>
        /// <typeparam name="T">The type the function returns</typeparam>
        /// <param name="func">The function to execute</param>
        /// <param name="timeout">The timeout in milliseconds</param>
        /// <param name="result">The result the function returns</param>
        /// <returns>Indicates whether execution was successful. <code>true</code> if successful, <code>false</code> if aborted.</returns>
        public static bool TryExecute<T>(Func<T> func, int timeout, out T result)
        {
            var t = default(T);
            var thread = new Task<T>(() => t = func());
            thread.Start();
            bool completed;
            try
            {
                completed = thread.Wait(timeout);
            }
            catch (AggregateException)
            {
                throw;
            }
            result = t;
            return completed;
        }
    }
}
