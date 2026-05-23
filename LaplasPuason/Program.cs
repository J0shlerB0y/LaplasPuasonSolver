using System;
using System.Windows.Forms;
using LaplasPuason.MathCore;

namespace LaplasPuason
{
    internal static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            //if (args.Length > 0 && args[0] == "--selftest")
            //{
            //    RunSelfTest();
            //    return;
            //}
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }

        //private static void RunSelfTest()
        //{
        //    AllocConsole();
        //    Console.WriteLine("=== Self-test ===");
        //    var domain = new RingDomain(0, 0, 2, 3);
        //    var src = PolyParser.Parse("-6*(x^2-y^2)");
        //    var b1 = PolyParser.Parse("x^2-2*y^2+y-1");
        //    var b2 = PolyParser.Parse("2*x^2+4*y^2+x-25");
        //    var solver = new RingSolver(domain);

        //    var nResult = solver.Solve(BoundaryType.Neumann, src, new[] { b1, b2 });
        //    Console.WriteLine("Neumann solution:");
        //    Console.WriteLine(nResult.FullSolution.Format(4));
        //    Console.WriteLine("--- particular ---");
        //    Console.WriteLine(nResult.ParticularSolution.Format(4));
        //    Console.WriteLine("--- homogeneous ---");
        //    Console.WriteLine(nResult.HomogeneousSolution.Format(4));
        //    Console.WriteLine();

        //    var dResult = solver.Solve(BoundaryType.Dirichlet, src, new[] { b1, b2 });
        //    Console.WriteLine("Dirichlet solution:");
        //    Console.WriteLine(dResult.FullSolution.Format(4));

        //    Console.WriteLine();
        //    Console.WriteLine("=== Inner disk Dirichlet test ===");
        //    var inner = new InnerDiskDomain(0, 0, 2);
        //    var iSolver = new InnerDiskSolver(inner);
        //    var b = PolyParser.Parse("x^2");
        //    var iRes = iSolver.Solve(BoundaryType.Dirichlet, PolyXY.Zero, new[] { b });
        //    Console.WriteLine(iRes.FullSolution.Format(4));

        //    Console.WriteLine();
        //    Console.WriteLine("=== Outer disk Dirichlet test ===");
        //    var outer = new OuterDiskDomain(0, 0, 2);
        //    var oSolver = new OuterDiskSolver(outer);
        //    var oRes = oSolver.Solve(BoundaryType.Dirichlet, PolyXY.Zero, new[] { PolyParser.Parse("y") });
        //    Console.WriteLine(oRes.FullSolution.Format(4));
        //}

        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        private static extern bool AllocConsole();
    }
}
