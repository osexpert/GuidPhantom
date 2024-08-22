using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using GuidPhantom;

namespace ConsoleApp1
{
    internal class Program
    {
        public static void Main()
        {
			foreach (var x in Take(100000, () => GuidKit.CreateVersion7()))
				Console.WriteLine(x);

			for (int x = 0; x < 1000; x++)
			{
				Console.WriteLine(GuidKit.CreateNEWSEQUENTIALID());
			}

			var v1 = GuidKit.CreateVersion1();
			var v3 = GuidKit.CreateVersion3(new Guid(""), "Test42");
			var v5 = GuidKit.CreateVersion5(new Guid(""), "Test42");
			var v6 = GuidKit.CreateVersion6();
			var v7 = GuidKit.CreateVersion7();
			var v8mssql = GuidKit.CreateVersion8MsSql();
			var v8sha256 = GuidKit.CreateVersion8SHA256(new Guid(""), "Test42");
			var v8sha512 = GuidKit.CreateVersion8SHA512(new Guid(""), "Test42");
			var v6_converted = v1.ConvertVersion1To6();
			var v1_converted = v6.ConvertVersion6To1();
			var nsi = GuidKit.CreateNEWSEQUENTIALID();

			var v7_1000 = Take(1000, () => GuidKit.CreateVersion7());
			var v8MsSql7_1000 = Take(1000, () => GuidKit.CreateVersion8MsSql());

			var vv = nsi.GetVariantAndVersion();
			var info = nsi.GetGuidInfo();

			var xor = GuidKit.CreateXorGuid(new Guid(), new Guid());
			var b = xor.ReverseXorGuid(new Guid());

			var num = GuidKit.CreateNumericGuid(42);
			int num42 = num.ReverseNumericGuid();

			var inc = GuidKit.CreateIncrementedGuid(new Guid(), 42);
			int inc42 = inc.ReverseIncrementedGuid(new Guid());



			var g = Guid.NewGuid();
			for (int i = int.MinValue; i < int.MaxValue; i++)
			{
				//GuidKit.CreateIncrementedGuid()
				if (g.CreateIncrementedGuid(i).ReverseIncrementedGuid(g) != i)
					throw new Exception();
			}

			g = Guid.Empty;
			for (int i = int.MinValue; i < int.MaxValue; i++)
			{
				//GuidKit.CreateIncrementedGuid()
				if (g.CreateIncrementedGuid(i).ReverseIncrementedGuid(g) != i)
					throw new Exception();
			}

			g = new Guid(Guid.Empty.ToString().Replace('0', 'F'));
			for (int i = int.MinValue; i < int.MaxValue; i++)
			{
				//GuidKit.CreateIncrementedGuid()
				if (g.CreateIncrementedGuid(i).ReverseIncrementedGuid(g) != i)
					throw new Exception();
			}
		}

		private static IEnumerable<Guid> Take(int v, Func<Guid> value)
		{
			int i = v;
			while (v-- > 0)
				yield return value();
		}
	}
}
