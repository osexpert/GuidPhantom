using System.Data.SqlTypes;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using GuidPhantom;

namespace ConsoleApp1
{
    internal class Program
    {
        public static void Main()
        {
			while (true)
			{
				var v777 = GuidKit.CreateVersion7();
				Console.WriteLine("" + v777 + ", " + GuidKit.CounterBits + " fut:" + GuidKit.Future);
			}

			//long i2 = 0;
			//long collnum = 0;
			//HashSet<int> hs = new();
			//while (true)
			//{
			//	var r = RandomNumberGenerator.GetInt32(int.MinValue, int.MaxValue);

			//	if (!hs.Add(r))
			//	{
			//		// coll

			//		collnum++;
			//		Console.WriteLine("collnum: " + collnum + " tot:"+i2+" rat " + i2/ collnum);
			//	}

			//	i2++;

			//	for (int c = 0; c < 10000; c++)
			//	{
			//		r += 1000;

			//		i2 ++;

			//		if (!hs.Add(r))
			//		{
			//			// coll

			//			collnum++;
			//			Console.WriteLine("collnum_2: " + collnum + " tot:" + i2 + " rat " + i2 / collnum);
			//		}
			//	}

			//	if (i2 >= 100_000_000)
			//	{
			//		Console.WriteLine("wait");
			//		Console.ReadLine();
			//	}
			//}

			var s = Stopwatch.StartNew();
			var n = DateTimeOffset.UtcNow;
			Guid? last = null;
			Guid? prev = null;
			for (int i = 0;/* i <= 100_000_000*/; i++)
			{
				var v777 = GuidKit.CreateVersion7();
				if ((i % 1_000_000) == 0)
					Console.WriteLine(v777 + " fut:" + GuidKit.Future+ " cbits:"+GuidKit.CounterBits);
				//var v777 = Guid.NewGuid();
				if (i == 100_000_000)//
					last = v777;

				if (prev != null && v777.CompareTo(prev.Value) <= 0)
				{
					throw new Exception(v777 + " is less or equal to " + prev.Value);
				}

				prev = v777;

				//for (int x=0;x<1000;x++)
				//{

				//}

			}
			s.Stop();
			Console.WriteLine("" + s.ElapsedMilliseconds + "ms");
			//Console.WriteLine("incs" + GuidKit.incs);
			Console.WriteLine("curr ts" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
			Console.WriteLine("last ts" + ((GuidInfoVersion7And8)last!.Value.GetGuidInfo()).Timestamp);

			Console.WriteLine("slower");

			for (int i = 0; i <= 10_000_000; i++)
			{
				var v777 = GuidKit.CreateVersion7();
				if ((i % 100_000) == 0)
				{
					Console.WriteLine(v777 + " fut:" + GuidKit.Future + " cbits:" + GuidKit.CounterBits);
					
				}

				if ((i % 1000) == 0)
				{
					Thread.Sleep(10);
				}
					//var v777 = Guid.NewGuid();
					//if (i == 100_000_000)//
					//last = v777;

				}

			Console.WriteLine("end " + GuidKit.Future + " cbits:" + GuidKit.CounterBits);

			return;


			var gg131 = GuidKit.FromHexString("0x01918D8D60A77B77922D5F5A89EE07BF", bigEndian: true);
			// gg131 = {01918d8d-60a7-7b77-922d-5f5a89ee07bf}
			var gg138 = GuidKit.FromHexString("0x01918D8D60A77B778951ADD260B48EEE", bigEndian: true);
			// gg138 = {01918d8d-60a7-7b77-8951-add260b48eee}
			// 138 happens later, should have had a different timestamp...something funny going on here...
			// seems like ms sql make them out of order?
			var gg137 = GuidKit.FromHexString("0x01918D8D60A77B77AF4A98D3DF112D66", bigEndian: true);
			// gg137 = {01918d8d-60a7-7b77-af4a-98d3df112d66}
			

			var v1 = GuidKit.CreateVersion1();
			var v3 = GuidKit.CreateVersion3(new Guid("d2f2f0fe-cbf8-4dc8-9ecb-eedd066dc105"), "Test42");
			var v5 = GuidKit.CreateVersion5(new Guid("d2f2f0fe-cbf8-4dc8-9ecb-eedd066dc105"), "Test42");
			var v6 = GuidKit.CreateVersion6();
			var v7 = GuidKit.CreateVersion7();
			var v8mssql = GuidKit.CreateVersion8MsSql();
			var v8sha256 = GuidKit.CreateVersion8SHA256(new Guid("d2f2f0fe-cbf8-4dc8-9ecb-eedd066dc105"), "Test42");
			var v8sha512 = GuidKit.CreateVersion8SHA512(new Guid("d2f2f0fe-cbf8-4dc8-9ecb-eedd066dc105"), "Test42");
			var v6_converted = v1.ConvertVersion1To6();
			var v1_converted = v6.ConvertVersion6To1();
			var v7_converted = v8mssql.ConvertVersion8MsSqlTo7();
			var v8mssql_converted = v7.ConvertVersion7To8MsSql();
			var nsi = GuidKit.CreateNEWSEQUENTIALID();

			var vv = nsi.GetVariantAndVersion();
			var info = nsi.GetGuidInfo();

			var xor = GuidKit.CreateXorGuid(new Guid("d2f2f0fe-cbf8-4dc8-9ecb-eedd066dc105"), new Guid("7e00d52e-8496-4239-92fc-4d59a0cde28d"));
			var b = xor.ReverseXorGuid(new Guid("d2f2f0fe-cbf8-4dc8-9ecb-eedd066dc105"));

			var num = GuidKit.CreateNumericGuid(42);
			int num42 = num.ReverseNumericGuid();

			var inc = GuidKit.CreateIncrementedGuid(new Guid("d2f2f0fe-cbf8-4dc8-9ecb-eedd066dc105"), 42);
			int inc42 = inc.ReverseIncrementedGuid(new Guid("d2f2f0fe-cbf8-4dc8-9ecb-eedd066dc105"));



			foreach (var x in Take(100000, () => GuidKit.CreateVersion7()))
				Console.WriteLine(x);

			for (int x = 0; x < 1000; x++)
			{
				Console.WriteLine(GuidKit.CreateNEWSEQUENTIALID());
			}




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
