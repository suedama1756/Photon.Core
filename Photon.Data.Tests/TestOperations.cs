using System;
using System.Diagnostics;

namespace Photon.Data.Tests
{
	
	public static class TestOperations
    {
        public static void TimeIt(string name, Action action)
        {
            var sw = new Stopwatch();
            sw.Start();
            action();
            sw.Stop();
            Console.WriteLine("Action {0} took: {1}ms.", name, sw.ElapsedMilliseconds);
        }

        public static T TimeIt<T>(string name, Func<T> action)
        {
            T result = default(T);
            TimeIt(name, () =>
                   {
                result = action();
            });
            return result;
        }
    }

}
