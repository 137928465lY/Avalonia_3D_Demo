using AnyCAD.Foundation;
using System.Reflection;

namespace AnyCAD.Demo
{
    public class TestCaseLoaderQuickSolid
    {
        public static void ForEachCase(AnyCAD.Demo.TestCase.TestCaseHandler handler)
        {
            TestCase.ForEachCase(handler, Assembly.GetExecutingAssembly());
        }
    }

}
