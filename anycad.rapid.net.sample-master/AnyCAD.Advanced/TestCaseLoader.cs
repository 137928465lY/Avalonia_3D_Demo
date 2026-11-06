using AnyCAD.Foundation;
using System.Reflection;

namespace AnyCAD.Demo
{
    public class TestCaseLoaderSimulate
    {
        public static void ForEachCase(AnyCAD.Demo.TestCase.TestCaseHandler handler)
        {
            ModelingEngine.EnableSnapShape();
            AnyCAD.Simulate.SimulateInstance.Instance().Initialize();
            TestCase.ForEachCase(handler, Assembly.GetExecutingAssembly());
        }
    }

}
