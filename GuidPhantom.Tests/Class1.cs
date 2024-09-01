using System.Linq;
using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Data.SqlTypes;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading;

namespace GuidPhantom.Tests
{
	[TestClass]
	public class UnitTest1
	{
		public UnitTest1()
		{
			// warmup
			TestV3();
			var h = GuidKit.CreateNEWSEQUENTIALID();
			Testv8sha512();
		}

		[TestMethod]
		public void Ver7SequenceRising()
		{
			Guid? prev = null;
			foreach (var h in Take(10000, () => GuidKit.CreateVersion7()).ToList())
			{
				if (prev != null && h.CompareTo(prev.Value) <= 0)
				{
					throw new Exception(h + " is less or equal to " + prev.Value);
				}
				prev = h;
			}
		}

		private IEnumerable<Guid> Take(int v, Func<Guid> value)
		{
			int i = v;
			while (v-- > 0)
				yield return value();
		}

		[TestMethod]
		public void Ver7Sequence()
		{
			var s = GuidKit.CreateVersion7();
			Testv7(s);
		}
		[TestMethod]
		public void Ver8MsSqlSequence()
		{
			var s = GuidKit.CreateVersion8MsSql();
			Testv8MsSql(s);
		}

		[TestMethod]
		public void Ver8MsSqlRising()
		{
			Guid? prev = null;
			//int i = 0;
			foreach (var h in Take(10000, () => GuidKit.CreateVersion8MsSql()).ToList())
			{
				// https://github.com/microsoft/referencesource/blob/master/System.Data/System/Data/SQLTypes/SQLGuid.cs
				// if (prev != null && h.ConvertVersion8MsSqlTo7().CompareTo(prev.Value.ConvertVersion8MsSqlTo7()) <= 0)
				if (prev != null && new SqlGuid(h).CompareTo(new SqlGuid(prev.Value)) <= 0)
				{
					throw new Exception(h + " is less or equal to " + prev.Value);
				}

				prev = h;
			}
		}

		[TestMethod]
		public void NEWSEQUENTIALID_Rising()
		{
			Guid? prev = null;
			//int i = 0;
			for (int d = 0; d < 10000; d++)
			{
				var h = GuidKit.CreateNEWSEQUENTIALID();

				if (prev != null && new SqlGuid(h).CompareTo(new SqlGuid(prev.Value)) <= 0)
				{
					throw new Exception(h + " is less or equal to " + prev.Value);
				}

				prev = h;
			}
		}


		[TestMethod]
		public void TestV1()
		{
			var v1 = GuidKit.CreateVersion1();
			
			Assert.IsTrue((GuidVariant.IETF, 1) == v1.GetVariantAndVersion());
			var inf1 = (GuidInfoVersion1And6)v1.GetGuidInfo();
			Assert.AreEqual(1, inf1.Version);
			Assert.AreEqual(GuidVariant.IETF, inf1.Variant);
			// should be pretty close, but strangely it is often over 1 seconds
			var diff = (DateTimeOffset.UtcNow - inf1.Time);
			Assert.IsTrue(diff.TotalSeconds < 2);

			Assert.AreEqual(v1, v1.ConvertVersion1To6().ConvertVersion6To1());

			var v6 = v1.ConvertVersion1To6();
			Assert.IsTrue((GuidVariant.IETF, 6) == v6.GetVariantAndVersion());
			var inf6 = (GuidInfoVersion1And6)v6.GetGuidInfo();
			Assert.AreEqual(6, inf6.Version);
			Assert.AreEqual(GuidVariant.IETF, inf6.Variant);
			Assert.AreEqual(inf1.Timestamp, inf6.Timestamp);
			Assert.AreEqual(inf1.ClockSequence, inf6.ClockSequence);
			Assert.AreEqual(inf1.Node, inf6.Node);

			// from RFC
			Assert.AreEqual(new Guid("1EC9414C-232A-6B00-B3C8-9E6BDECED846"), new Guid("C232AB00-9414-11EC-B3C8-9E6BDECED846").ConvertVersion1To6());
			var v1inf = (GuidInfoVersion1And6)new Guid("C232AB00-9414-11EC-B3C8-9E6BDECED846").GetGuidInfo();
			Assert.AreEqual(0x9E6BDECED846, v1inf.Node);
		}

		[TestMethod]
		public void TestV6()
		{
			var v6 = GuidKit.CreateVersion6();
			
			Assert.IsTrue((GuidVariant.IETF, 6) == v6.GetVariantAndVersion());
			var inf6 = (GuidInfoVersion1And6)v6.GetGuidInfo();
			Assert.AreEqual(6, inf6.Version);
			Assert.AreEqual(GuidVariant.IETF, inf6.Variant);
			// should be pretty close, but strangely it is often over 1 seconds
			var diff = (DateTimeOffset.UtcNow - inf6.Time);
			Assert.IsTrue(diff.TotalSeconds < 2);

			Assert.AreEqual(v6, v6.ConvertVersion6To1().ConvertVersion1To6());

			var v1 = v6.ConvertVersion6To1();
			Assert.IsTrue((GuidVariant.IETF, 1) == v1.GetVariantAndVersion());
			var inf1 = (GuidInfoVersion1And6)v1.GetGuidInfo();
			Assert.AreEqual(1, inf1.Version);
			Assert.AreEqual(GuidVariant.IETF, inf1.Variant);
			Assert.AreEqual(inf6.Timestamp, inf1.Timestamp);
			Assert.AreEqual(inf6.ClockSequence, inf1.ClockSequence);
			Assert.AreEqual(inf6.Node, inf1.Node);
		}



		[TestMethod]
		public void TestV3()
		{
			// ref: https://www.uuidtools.com/generate/v3
			var v3 = new Guid("E11EAC0E-4D75-4567-BA60-683D357A9227").CreateVersion3("Test42");
			Assert.AreEqual(new Guid("0dd552e7-647f-3045-86f2-c006e1e17a89"), v3);
			Assert.IsTrue((GuidVariant.IETF, 3) == v3.GetVariantAndVersion());
			var inf = (GuidInfoVersion)v3.GetGuidInfo();
			Assert.AreEqual(3, inf.Version);
			Assert.AreEqual(GuidVariant.IETF, inf.Variant);
		}


		[TestMethod]
		public void TestV4()
		{
			var v4 = GuidKit.CreateVersion4();
			Assert.IsTrue((GuidVariant.IETF, 4) == v4.GetVariantAndVersion());
			var inf = (GuidInfoVersion)v4.GetGuidInfo();
			Assert.AreEqual(4, inf.Version);
			Assert.AreEqual(GuidVariant.IETF, inf.Variant);
		}



		[TestMethod]
		public void TestV5()
		{
			// ref: https://www.uuidtools.com/generate/v5
			var v5 = new Guid("E11EAC0E-4D75-4567-BA60-683D357A9227").CreateVersion5("Test42");
			Assert.AreEqual(new Guid("73cf5b24-114a-5a5b-837c-64cf22468258"), v5);
			Assert.IsTrue((GuidVariant.IETF, 5) == v5.GetVariantAndVersion());
			var inf = (GuidInfoVersion)v5.GetGuidInfo();
			Assert.AreEqual(5, inf.Version);
			Assert.AreEqual(GuidVariant.IETF, inf.Variant);
		}

		[TestMethod]
		public void Testv8sha256()
		{
			// ref: self
			var v8 = new Guid("E11EAC0E-4D75-4567-BA60-683D357A9227").CreateVersion8SHA256("Test42");
			Assert.AreEqual(new Guid("306244bd-cd9e-88d1-a559-cf5a8b926d6c"), v8);
			Assert.IsTrue((GuidVariant.IETF, 8) == v8.GetVariantAndVersion());
			var inf = (GuidInfoVersion)v8.GetGuidInfo();
			Assert.AreEqual(8, inf.Version);
			Assert.AreEqual(GuidVariant.IETF, inf.Variant);
		}

		[TestMethod]
		public void Testv8sha512()
		{
			// ref: self
			var v8 = new Guid("E11EAC0E-4D75-4567-BA60-683D357A9227").CreateVersion8SHA512("Test42");
			Assert.AreEqual(new Guid("f1ad1980-31b0-822e-8bd7-4d4c9892e5b6"), v8);
			Assert.AreEqual((GuidVariant.IETF, (byte)8), v8.GetVariantAndVersion());
			var inf = (GuidInfoVersion)v8.GetGuidInfo();
			Assert.AreEqual(8, inf.Version);
			Assert.AreEqual(GuidVariant.IETF, inf.Variant);
		}

		[TestMethod]
		public void TestNumeric()
		{
			var i0g = GuidKit.CreateNumericGuid(0);
			var i42g = GuidKit.CreateNumericGuid(42);
			var imaxg = GuidKit.CreateNumericGuid(int.MaxValue);
			var i0 = GuidKit.ReverseNumericGuid(i0g);
			var i42 = GuidKit.ReverseNumericGuid(i42g);
			int imax = GuidKit.ReverseNumericGuid(imaxg);
			Assert.AreEqual(0, i0);
			Assert.AreEqual(42, i42);
			Assert.AreEqual(int.MaxValue, imax);
		}

		[TestMethod]
		public void TestXor()
		{
			var v1 = GuidKit.CreateVersion1();
			var v7 = GuidKit.CreateVersion7();

			var xor = GuidKit.CreateXorGuid(v1, v7);
			Assert.AreEqual((GuidVariant.ApolloNCS, null), xor.GetVariantAndVersion());
			Assert.AreEqual(v7, GuidKit.ReverseXorGuid(xor, v1));
			Assert.AreEqual(v1, GuidKit.ReverseXorGuid(xor, v7));

		}

		[TestMethod]
		public void Testv7()
		{
			var v7 = GuidKit.CreateVersion7();
			Testv7(v7);

			// from RFC
			var gi = (GuidInfoVersion7And8)new Guid("017F22E2-79B0-7CC3-98C4-DC0C0C07398F").GetGuidInfo();
			Assert.AreEqual(0x17F22E279B0, gi.Timestamp);
			Assert.AreEqual(0xCC3, gi.RandA);
			Assert.AreEqual(GuidVariant.IETF, gi.Variant);
			Assert.AreEqual(7, gi.Version);
			Assert.AreEqual(0x18C4DC0C0C07398F, gi.RandB);
		}


		[TestMethod]
		public void Testv8MsSql()
		{
			var v8 = GuidKit.CreateVersion8MsSql();
			Testv8MsSql(v8);
		}


		[TestMethod]
		public void Testv8Unknown()
		{
			// from RFC
			var g = new Guid("320C3D4D-CC00-875B-8EC9-32D5F69181C0");
			var i = (GuidInfoVersion7And8) g.GetGuidInfo();
			Assert.AreEqual(GuidVariant.IETF, i.Variant);
			Assert.AreEqual(8, i.Version);
			Assert.AreEqual(0x320C3D4DCC00, i.Timestamp);
			Assert.AreEqual(0x75B, i.RandA);
			Assert.AreEqual(0xEC932D5F69181C0, i.RandB);
		}

		[TestMethod]
		public void Testv7to8MsSqlSnapshot()
		{
			var v8mssql = new Guid("017F22E2-79B0-7CC3-98C4-DC0C0C07398F").ConvertVersion7To8MsSql();
			// defacto per now
			Assert.AreEqual(new Guid("dc0c0c07-398f-848c-b30d-017f22e279b0"), v8mssql);
			Assert.AreEqual(new Guid("017F22E2-79B0-7CC3-98C4-DC0C0C07398F"), new Guid("dc0c0c07-398f-848c-b30d-017f22e279b0").ConvertVersion8MsSqlTo7());
		}

		public void Testv7(Guid v7)
		{ 
			Assert.AreEqual((GuidVariant.IETF, (byte)7), v7.GetVariantAndVersion());
			var inf7 = (GuidInfoVersion7And8)v7.GetGuidInfo();
			Assert.AreEqual(7, inf7.Version);
			Assert.AreEqual(GuidVariant.IETF, inf7.Variant);
			var diff = DateTimeOffset.UtcNow - inf7.Time;
			Assert.IsTrue(diff.TotalSeconds < 2);

			Assert.AreEqual(v7, v7.ConvertVersion7To8MsSql().ConvertVersion8MsSqlTo7());

			var v8 = v7.ConvertVersion7To8MsSql();
			Assert.AreEqual((GuidVariant.IETF, (byte)8), v8.GetVariantAndVersion());
			var inf8 = (GuidInfoVersion7And8)v8.GetGuidInfo(version8type: GuidVersion8Type.MsSql);
			Assert.AreEqual(8, inf8.Version);
			Assert.AreEqual(GuidVariant.IETF, inf8.Variant);
			Assert.AreEqual(inf7.Timestamp, inf8.Timestamp);
		}

		public void Testv8MsSql(Guid v8)
		{
			Assert.AreEqual((GuidVariant.IETF, (byte)8), v8.GetVariantAndVersion());
			var inf8 = (GuidInfoVersion7And8)v8.GetGuidInfo(version8type: GuidVersion8Type.MsSql);
			Assert.AreEqual(8, inf8.Version);
			Assert.AreEqual(GuidVariant.IETF, inf8.Variant);
			var diff = DateTimeOffset.UtcNow - inf8.Time;
			Assert.IsTrue(diff.TotalSeconds < 2);

			Assert.AreEqual(v8, v8.ConvertVersion8MsSqlTo7().ConvertVersion7To8MsSql());

			var v7 = v8.ConvertVersion8MsSqlTo7();
			Assert.AreEqual((GuidVariant.IETF, (byte)7), v7.GetVariantAndVersion());
			var inf7 = (GuidInfoVersion7And8)v7.GetGuidInfo();
			Assert.AreEqual(7, inf7.Version);
			Assert.AreEqual(GuidVariant.IETF, inf7.Variant);
			Assert.AreEqual(inf8.Timestamp, inf7.Timestamp);
		}

		[TestMethod]
		public void TestIncremented()
		{
			// take the last 4 bytes of a Guid, increment them with increment, put the 4 bytes back.
			// Depending on size of the increment, 1 to 4 bytes will be changed.
			//var newG =  Guid.CreateIncementedGuid(Guid seed, int increment)

			// Max 4 last bytes can be different. Based on how may of the last bytes are different (4-1), t
			//var i = newG.ReverseIncementedGuid(seed);

			Guid base_g = Guid.NewGuid();
			Assert.AreEqual(int.MinValue, base_g.CreateIncrementedGuid(int.MinValue).ReverseIncrementedGuid(base_g));
			Assert.AreEqual(0, base_g.CreateIncrementedGuid(0).ReverseIncrementedGuid(base_g));
			Assert.AreEqual(42, base_g.CreateIncrementedGuid(42).ReverseIncrementedGuid(base_g));
			Assert.AreEqual(-420000, base_g.CreateIncrementedGuid(-420000).ReverseIncrementedGuid(base_g));
			Assert.AreEqual(int.MaxValue, base_g.CreateIncrementedGuid(int.MaxValue).ReverseIncrementedGuid(base_g));
		}

#if NET6_0_OR_GREATER
#else
		static Random _rand = new Random();
#endif


		[TestMethod]
		public void InternalSequence()
		{
			for (int x = 0; x < 10000; x++)
			{

#if NET6_0_OR_GREATER
				long ts = Random.Shared.NextInt64();
				int seq = Random.Shared.Next(GuidKit.CounterMax + 1);
#else
				long ts = _rand.Next();
				int seq = _rand.Next(GuidKit.CounterMax + 1);
#endif

				var bytesOrg = Guid.NewGuid().ToByteArray(bigEndian: true);

				var bytesCpy1 = CopyBytes(bytesOrg);
				// get seq
				GuidKit.CreateVersion8MsSql(bytesCpy1, ts, ref seq, false, 12);
				// set seq		
				var bytesCpy2 = CopyBytes(bytesOrg);
				GuidKit.CreateVersion8MsSql(bytesCpy2, ts, ref seq, true, 12);
				// check bytes match
				Assert.IsTrue(bytesCpy1.SequenceEqual(bytesCpy2));

				

				//var bytesv7 = Guid.NewGuid().ToByteArray(bigEndian: true);

				var bytesCpy3 = CopyBytes(bytesOrg);
				GuidKit.CreateVersion7(bytesCpy3, ts, ref seq, false, 12);
				var bytesCpy4 = CopyBytes(bytesOrg);
				GuidKit.CreateVersion7(bytesCpy4, ts, ref seq, true, 12);
				Assert.IsTrue(bytesCpy3.SequenceEqual(bytesCpy4));


				var bytesCpy5 = CopyBytes(bytesOrg);
				var bytesCpy6 = CopyBytes(bytesOrg);

				GuidKit.CreateVersion7(bytesCpy5, ts, ref seq, true, 12);
				GuidKit.CreateVersion8MsSql(bytesCpy6, ts, ref seq, true, 12);

				var v7 = GuidKit.FromByteArray(bytesCpy5, bigEndian: true);
				var v8 = GuidKit.FromByteArray(bytesCpy6, bigEndian: true);

				var i8 = (GuidInfoVersion7And8)v8.GetGuidInfo(GuidVersion8Type.MsSql);
				var i7 = (GuidInfoVersion7And8)v7.GetGuidInfo();

//				Assert.AreEqual(v8, v7.ConvertVersion7To8MsSql());
	//			Assert.AreEqual(v7, v8.ConvertVersion8MsSqlTo7());

				Assert.AreEqual(i7.Timestamp, i8.Timestamp);
				Assert.AreEqual(i7.RandA, i8.RandA);
			}
		}

		private byte[] CopyBytes(byte[] bytes)
		{
			var cpy = new byte[bytes.Length];
			Array.Copy(bytes, cpy, bytes.Length);
			return cpy;
		}

		//[TestMethod]
		//public void NewSeqConvert()
		//{
		//	var ns = GuidKit.CreateNEWSEQUENTIALID();
		//	var i = ns.GetGuidInfo();
		//	var v1 = ns.ConvertNEWSEQUENTIALIDToVersion1();
		//	var nsb = v1.ConvertVersion1ToNEWSEQUENTIALID();
		//	Assert.AreEqual(ns, nsb);
		//}
	}
}

