using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

[assembly: InternalsVisibleTo("GuidPhantom.Tests")]

namespace GuidPhantom
{
	public static class GuidKit
	{
		/// <summary>
		/// Gets a <see cref="Guid" /> where all bits are set.
		/// https://github.com/dotnet/runtime/blob/59c2ea578bd615a63d56e8ff4b1de0a6b824691f/src/libraries/System.Private.CoreLib/src/System/Guid.cs#L42
		/// </summary>
		/// <remarks>This returns the value: FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF</remarks>
		public static Guid AllBitsSet => new Guid(uint.MaxValue, ushort.MaxValue, ushort.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue);


		[DllImport("libuuid.so.1")]
		private static extern int uuid_generate_time_safe(out byte[] bytes);

		[DllImport("rpcrt4.dll")]
		private static extern int UuidCreateSequential(out Guid guid);

#if NET8_0_OR_GREATER
		// net8+ has it
#else
		public static byte[] ToByteArray(this Guid g, bool bigEndian)
        {
  //          if (_toByteArray.Value != null)
//                return _toByteArray.Value(g, bigEndian);

            var bytes = g.ToByteArray();
            if (BitConverter.IsLittleEndian == bigEndian)
                SwapEndian(bytes);

            return bytes;
        }
#endif

		public static Guid CreateVersion1() => CreateVersion1(out var _);

		//static Delegate CreateInstanceMethodDelegate(MethodInfo method)
		//{
		//    var instExp = Expression.Parameter(method.DeclaringType);
		//    var instExpArr = new[] { instExp };
		//    var paramsExpArr = method.GetParameters()
		//        .Select(p => Expression.Parameter(p.ParameterType, p.Name))
		//        .ToArray();
		//    var call = Expression.Call(instExp, method, paramsExpArr);
		//    return Expression.Lambda(call, instExpArr.Concat(paramsExpArr)).Compile();
		//}

		//static readonly Lazy<Func<Guid, bool, byte[]>?> _toByteArray = new(() =>
		//{
		//    var meth = typeof(Guid).GetMethod("ToByteArray", new Type[] { typeof(bool) });
		//    if (meth != null)
		//    {
		//        return (Func<Guid, bool, byte[]>)CreateInstanceMethodDelegate(meth);
		//    }
		//    return null;
		//});


		//static readonly Lazy<Func<Guid>?> _createVersion7 = new(() =>
		//{
		//    var meth = typeof(Guid).GetMethod("CreateVersion7", new Type[] { });
		//    if (meth != null)
		//    {
		//        return (Func<Guid>)meth.CreateDelegate(typeof(Func<Guid>));
		//    }
		//    return null;
		//});

		//static readonly Lazy<Func<DateTimeOffset, Guid>?> _createVersion7_dto = new(() =>
		//{
		//    var meth = typeof(Guid).GetMethod("CreateVersion7", new Type[] { typeof(DateTimeOffset) });
		//    if (meth != null)
		//    {
		//        return (Func<DateTimeOffset, Guid>)meth.CreateDelegate(typeof(Func<DateTimeOffset, Guid>));
		//    }
		//    return null;
		//});

		/// <summary>
		/// Create version1 Guid: time + sequence + node/mac
		/// </summary>
		/// <param name="safe">true if generated safely (will be globally unique). false is generated unsafely (only locally unique)</param>
		/// <returns>Version 1 Guid</returns>
		public static Guid CreateVersion1(out bool safe)
		{
			// <param name="check_safe">If true, will throw is Guid is not generated safely (eg. not with a valid mac address, not with a reliable sequence etc.)</param>

			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				const int RPC_S_UUID_LOCAL_ONLY = 1824;
				const int RPC_S_UUID_NO_ADDRESS = 1739;

				int res = UuidCreateSequential(out var g);

				if (res == 0)
				{
					safe = true;
					return g;
				}

				if (res == RPC_S_UUID_LOCAL_ONLY || res == RPC_S_UUID_NO_ADDRESS)
				{
					safe = false;
					return g;
				}

				throw new Exception("Error: " + res);
			}
			else
			{
				var bytes = new byte[16];
				var res = uuid_generate_time_safe(out bytes);
				if (res == 0)
				{
					safe = true;
					return FromByteArray(bytes, bigEndian: true);
				}

				if (res == -1)
				{
					safe = false;
					return FromByteArray(bytes, bigEndian: true);
				}

				throw new Exception("Error: " + res);
			}
		}

		/// <summary>
		///  Create version 6 (same as v1, but bits rearranged to make it ordered)
		/// </summary>
		/// <returns></returns>
		public static Guid CreateVersion6() => CreateVersion6(out var _);

		/// <summary>
		/// Create version 6 (same as v1, but bits rearranged to make it ordered)
		/// </summary>
		/// <param name="safe">true if generated safely (will be globally unique). false is generated unsafely (only locally unique)</param>
		/// <returns></returns>
		public static Guid CreateVersion6(out bool safe)
		{
			var v1 = CreateVersion1(out safe);
			return v1.ConvertVersion1To6();
		}

		/// <summary>
		/// MS SQL SERVER: NEWSEQUENTIALID
		/// NEWSEQUENTIALID is Version1 with swapped endianess. This means this Guid is non-standard and the variant/version will be random/invalid.
		/// Non-standard.
		/// </summary>
		/// <returns>NEWSEQUENTIALID</returns>
		public static Guid CreateNEWSEQUENTIALID() => CreateNEWSEQUENTIALID(out var _);


		/// <summary>
		/// MS SQL SERVER: NEWSEQUENTIALID
		/// NEWSEQUENTIALID is Version1 with swapped endianess. This means this Guid is non-standard and the variant/version will be random/invalid.
		/// Non-standard.
		/// </summary>
		/// <returns>NEWSEQUENTIALID</returns>
		public static Guid CreateNEWSEQUENTIALID(out bool safe)
		{
			var v1 = CreateVersion1(out safe);
			var bytes = v1.ToByteArray(bigEndian: true);
			//SwapEndian(bytes);
			return FromByteArray(bytes, bigEndian: false);
		}

		//public static Guid ConvertNEWSEQUENTIALIDToVersion1(this Guid newseq)
		//{
		//	var bytes = newseq.ToByteArray(bigEndian: true);
		//	SwapEndian(bytes);
		//	var vv = GetVariantAndVersion(bytes);
		//	if (vv.Variant != GuidVariant.IETF)
		//		throw new Exception("Not variant " + GuidVariant.IETF); // apply to both src and target, because this byte is not swapped
		//	if (vv.Version != 1)
		//		throw new Exception("Target is not version 1");
		//	return FromByteArray(bytes, bigEndian: true);
		//}

		//public static Guid ConvertVersion1ToNEWSEQUENTIALID(this Guid v1)
		//{
		//	var bytes = v1.ToByteArray(bigEndian: true);
		//	var vv = GetVariantAndVersion(bytes);
		//	if (vv.Variant != GuidVariant.IETF)
		//		throw new Exception("Not variant " + GuidVariant.IETF);
		//	if (vv.Version != 1)
		//		throw new Exception("Not version 1");
		//	SwapEndian(bytes);
		//	return FromByteArray(bytes, bigEndian: true);
		//}

		/// <summary>
		/// Version is only defined for variant 1, so for other variants, Version is returned as NULL.
		/// This to prevent using Version for logic when there is none.
		/// </summary>
		/// <param name="g"></param>
		/// <returns>Variant and version</returns>
		public static (GuidVariant Variant, byte? Version) GetVariantAndVersion(this Guid g)
		{
			var b = g.ToByteArray(bigEndian: true);
			return GetVariantAndVersion(b);
		}

		private static (GuidVariant Variant, byte? Version) GetVariantAndVersion(byte[] b)
		{
			GuidVariant? variant;

			//byte[8]:
			//0xx Apollo
			//10x IETF
			//110 MS
			//111 reserved

			// order is important!
			if ((b[8] & 0b1000_0000) == 0)
				variant = GuidVariant.ApolloNCS;
			else if ((b[8] & 0b0100_0000) == 0)
			{
				// because the above check we know highest bit is 1
				variant = GuidVariant.IETF;
			}
			else if ((b[8] & 0b0010_0000) == 0)
			{
				// because the above checks we know higher 2 bits are 1
				variant = GuidVariant.Microsoft;
			}
			else
			{
				// because the above checks we know higher 3 bits are 1
				variant = GuidVariant.Reserved;
			}

			byte? version = null;

			if (variant == GuidVariant.IETF)
				version = (byte)((b[6] & 0b1111_0000) >> 4);

			return (variant.Value, version);
		}

		private static void SwapEndian(byte[] guid)
		{
			SwapBytes(guid, 0, 3);
			SwapBytes(guid, 1, 2);
			SwapBytes(guid, 4, 5);
			SwapBytes(guid, 6, 7);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void SwapBytes(byte[] guid, int a, int b)
		{
			var temp = guid[a];
			guid[a] = guid[b];
			guid[b] = temp;
		}




#if NET8_0_OR_GREATER
		// already has it (one that takes ReadOnlySpan<byte>)

		/// <summary>
		/// Create Guid from byte array
		/// </summary>
		/// <param name="bytes"></param>
		/// <param name="bigEndian"></param>
		/// <returns>Guid</returns>
		public static Guid FromByteArray(byte[] bytes, bool bigEndian) => new Guid(bytes, bigEndian);
#else
		/// <summary>
		/// Create Guid from byte array
		/// </summary>
		/// <param name="bytes"></param>
		/// <param name="bigEndian"></param>
		/// <returns>Guid</returns>
        public static Guid FromByteArray(byte[] bytes, bool bigEndian)
        {
            if (bytes.Length != 16)
            {
                throw new ArgumentException("Must be 16 bytes");
            }

            if (BitConverter.IsLittleEndian == bigEndian)
                SwapEndian(bytes);

            return new Guid(bytes);
        }
#endif

		/// <summary>
		/// Guid.NewGuid()
		/// </summary>
		/// <returns>Guid.NewGuid()</returns>
		public static Guid CreateVersion4() => Guid.NewGuid();

		/// <summary>
		/// Create monotonic (always increasing) version 7 Guid. NOTE: monotony is only per process.
		/// 6 first bytes are unix timestamp in milliseconds.
		/// 6 last bytes are random data.
		/// 4 middle bytes (initially 26bits of random data) are used as counter (randomly increased between 1-255), in case the time does not advance between calls.
		/// If the counter rollover (> 67_108_864) the timestamp is increased +1 ms.
		/// 
		/// This implementation DOES NOT match CreateVersion7 in .NET 9.
		/// https://github.com/dotnet/runtime/blob/59c2ea578bd615a63d56e8ff4b1de0a6b824691f/src/libraries/System.Private.CoreLib/src/System/Guid.cs#L304
		/// </summary>
		/// <returns>Version 7 Guid</returns>
		public static Guid CreateVersion7() => CreateVersion7Or8MsSql(DateTimeOffset.UtcNow, 7);

		/// <summary>
		/// Create monotonic (always increasing) version 7 Guid. NOTE: monotony is only per process.
		/// 6 first bytes is unix timestamp in milliseconds.
		/// 6 last bytes are random data.
		/// 4 middle bytes (initially 26bits of random data) are used as counter (randomly increased between 1-255), in case the time does not advance between calls.
		/// If the counter rollover (> 67_108_864) the timestamp is increased +1 ms.
		/// 
		/// This implementation DOES NOT match CreateVersion7 in .NET 9.
		/// https://github.com/dotnet/runtime/blob/59c2ea578bd615a63d56e8ff4b1de0a6b824691f/src/libraries/System.Private.CoreLib/src/System/Guid.cs#L304
		/// </summary>
		/// <param name="timestamp"></param>
		/// <returns>Version 7 Guid</returns>
		public static Guid CreateVersion7(DateTimeOffset timestamp) => CreateVersion7Or8MsSql(timestamp, 7);

		static long? _prev_ts = null;
		static int _sequence = 0;
		static object _lock = new();

		private static Guid CreateVersion7Or8MsSql(DateTimeOffset timestamp, byte version)
		{
			var bytes = Guid.NewGuid().ToByteArray(bigEndian: true);

			long now_ts = timestamp.ToUnixTimeMilliseconds();

			lock (_lock)
			{
				bool setSequence = false;
				if (now_ts <= _prev_ts)
				{
					now_ts = _prev_ts.Value;

					var rand_inc = bytes[version == 7 ? 0 : 10]; // use the first byte overwritten by timestamp
					_sequence += (rand_inc == 0 ? 42 : rand_inc);

					if (_sequence > 67_108_864)
					{
						now_ts++;
					}
					else
					{
						setSequence = true;
					}
				}

				_prev_ts = now_ts;

				if (version == 7)
					CreateVersion7(bytes, now_ts, ref _sequence, setSequence);
				else if (version == 8)
					CreateVersion8MsSql(bytes, now_ts, ref _sequence, setSequence);
				else
					throw new InvalidOperationException("Not version 7 or 8");
			}

			return FromByteArray(bytes, bigEndian: true);
		}

		/// <summary>
		/// SHA256 hash of namespace and name
		/// </summary>
		/// <param name="namespaceId"></param>
		/// <param name="name"></param>
		/// <returns>Version8SHA256 Guid</returns>
		public static Guid CreateVersion8SHA256(this Guid namespaceId, string name)
		{
			return CreateNamespaceGuid(namespaceId, () => SHA256.Create(), Encoding.UTF8.GetBytes(name), 8);
		}

		/// <summary>
		/// SHA512 hash of namespace and name
		/// </summary>
		/// <param name="namespaceId"></param>
		/// <param name="name"></param>
		/// <returns>Version8SHA512 Guid</returns>
		public static Guid CreateVersion8SHA512(this Guid namespaceId, string name)
		{
			return CreateNamespaceGuid(namespaceId, () => SHA512.Create(), Encoding.UTF8.GetBytes(name), 8);
		}

		/// <summary>
		/// Same as CreateVersion7 but bits rearranged to make it ordered in MsSql (and then set as Version8)
		/// </summary>
		/// <returns>Version8MsSql Guid</returns>
		public static Guid CreateVersion8MsSql() => CreateVersion7Or8MsSql(DateTimeOffset.UtcNow, 8);

		/// <summary>
		/// Same as CreateVersion7 but bits rearranged to make it ordered in MsSql (and then set as Version8)
		/// </summary>
		/// <param name="timestamp"></param>
		/// <returns>Version8MsSql Guid</returns>
		public static Guid CreateVersion8MsSql(DateTimeOffset timestamp) => CreateVersion7Or8MsSql(timestamp, 8);

		internal static void CreateVersion7(byte[] bytes, long unix_ts_ms, ref int sequence, bool setSequence)
		{
			if (unix_ts_ms < 0)
				throw new ArgumentOutOfRangeException(nameof(unix_ts_ms));

			if ((bytes[8] & 0b1100_0000) != 0b1000_0000)
				throw new InvalidOperationException("Not variant " + GuidVariant.IETF);

			// time
			bytes[0] = (byte)(unix_ts_ms >> (5 * 8));
			bytes[1] = (byte)(unix_ts_ms >> (4 * 8));
			bytes[2] = (byte)(unix_ts_ms >> (3 * 8));
			bytes[3] = (byte)(unix_ts_ms >> (2 * 8));
			bytes[4] = (byte)(unix_ts_ms >> (1 * 8));
			bytes[5] = (byte)unix_ts_ms;

			// set ver 7
			const byte newVer = 7;
			bytes[6] = (byte)((newVer << 4) | (bytes[6] & 0b0000_1111));

			// sequence
			if (setSequence)
			{
				if (sequence < 0 || sequence > 67_108_864) // 2^26  // 4095)
					throw new ArgumentException("Sequence must be between 0 and 67_108_864");

				bytes[6] = (byte)((bytes[6] & 0b1111_0000) | (sequence >> (8 + 6 + 8)) & 0b0000_1111);
				bytes[7] = (byte)(sequence >> (8 + 6));
				bytes[8] = (byte)((bytes[8] & 0b1100_0000) | (sequence >> 8) & 0b0011_1111);
				bytes[9] = (byte)sequence;
			}
			else
			{
				// rollover guard: make top bit of counter initially 0
				bytes[6] = (byte)(bytes[6] & 0b1111_0111);

				sequence = (int)(
					(bytes[6] & 0b0000_1111) << (8 + 6 + 8) |
					bytes[7] << (8 + 6) |
					(bytes[8] & 0b0011_1111) << 8 |
					bytes[9]);
			}
		}

		/// <summary>
		/// Create a Guid where the 6 last bytes is the unix time in milliseconds (v7 has time in the 6 first bytes)
		/// 
		/// This will make them sort correctly in ms sql server, that has a weird/opposite way of sorting:
		/// https://stackoverflow.com/questions/7810602/sql-server-guid-sort-algorithm-why
		/// "More technically, we look at bytes {10 to 15} first, then {8-9}, then {6-7}, then {4-5}, and lastly {0 to 3}."
		/// </summary>
		/// <param name="unix_ts_ms"></param>
		/// <returns>Version8MsSql Guid</returns>
		internal static void CreateVersion8MsSql(byte[] bytes, long unix_ts_ms, ref int sequence, bool setSequence)
		{
			if (unix_ts_ms < 0)
				throw new ArgumentOutOfRangeException(nameof(unix_ts_ms));

			if ((bytes[8] & 0b1100_0000) != 0b1000_0000)
				throw new InvalidOperationException("Not variant " + GuidVariant.IETF);

			// time
			bytes[10] = (byte)(unix_ts_ms >> (5 * 8));
			bytes[11] = (byte)(unix_ts_ms >> (4 * 8));
			bytes[12] = (byte)(unix_ts_ms >> (3 * 8));
			bytes[13] = (byte)(unix_ts_ms >> (2 * 8));
			bytes[14] = (byte)(unix_ts_ms >> (1 * 8));
			bytes[15] = (byte)unix_ts_ms;

			// set ver 8
			const byte newVer = 8;
			bytes[6] = (byte)((newVer << 4) | (bytes[6] & 0b0000_1111));

			// sequence
			if (setSequence)
			{
				if (sequence < 0 || sequence > 67_108_864)
					throw new ArgumentException("Sequence must be between 0 and 67_108_864");

				bytes[8] = (byte)((bytes[8] & 0b1100_0000) | (sequence >> (4 + 8 + 8)) & 0b0011_1111);
				bytes[9] = (byte)(sequence >> (4 + 8));
				bytes[7] = (byte)(sequence >> 4);
				bytes[6] = (byte)((bytes[6] & 0b1111_0000) | sequence & 0b0000_1111);
			}
			else
			{
				// rollover guard: make top bit of counter initially 0
				bytes[8] = (byte)(bytes[8] & 0b1101_1111);

				sequence = (int)(
					(bytes[8] & 0b0011_1111) << (4 + 8 + 8) |
					bytes[9] << (4 + 8) |
					bytes[7] << 4 |
					(bytes[6] & 0b0000_1111)
					);
			}
		}

		/// <summary>
		/// MD5 hash of namepace and name
		/// </summary>
		/// <param name="namespaceId"></param>
		/// <param name="name"></param>
		/// <returns>Version 3 Guid</returns>
		[SuppressMessage("Security", "CA5350:Do Not Use Weak Cryptographic Algorithms", Justification = "Per spec.")]
		[SuppressMessage("Security", "CA5351:Do Not Use Broken Cryptographic Algorithms", Justification = "Per spec.")]
		public static Guid CreateVersion3(this Guid namespaceId, string name)
		{
			return CreateNamespaceGuid(namespaceId, () => MD5.Create(), Encoding.UTF8.GetBytes(name), 3);
		}

		/// <summary>
		/// SHA1 hash of namespace and name
		/// </summary>
		/// <param name="namespaceId"></param>
		/// <param name="name"></param>
		/// <returns>Version 5 Guid</returns>
		[SuppressMessage("Security", "CA5350:Do Not Use Weak Cryptographic Algorithms", Justification = "Per spec.")]
		[SuppressMessage("Security", "CA5351:Do Not Use Broken Cryptographic Algorithms", Justification = "Per spec.")]
		public static Guid CreateVersion5(this Guid namespaceId, string name)
		{
			return CreateNamespaceGuid(namespaceId, () => SHA1.Create(), Encoding.UTF8.GetBytes(name), 5);
		}

		/// <summary>
		/// Creates a name-based UUID using the algorithm from RFC 4122 §4.3.
		/// Based on: https://github.com/Faithlife/FaithlifeUtility/blob/master/src/Faithlife.Utility/GuidUtility.cs
		/// </summary>
		/// <param name="namespaceId">The ID of the namespace.</param>
		/// <param name="nameBytes">The name (within that namespace).</param>
		/// <param name="version">The version number of the UUID to create; this value must be either
		/// 3 (for MD5 hashing) or 5 (for SHA-1 hashing).</param>
		/// <returns>A UUID derived from the namespace and name.</returns>
		[SuppressMessage("Security", "CA5350:Do Not Use Weak Cryptographic Algorithms", Justification = "Per spec.")]
		[SuppressMessage("Security", "CA5351:Do Not Use Broken Cryptographic Algorithms", Justification = "Per spec.")]
		private static Guid CreateNamespaceGuid(Guid namespaceId, Func<HashAlgorithm> hashAlgo, byte[] nameBytes, byte version)
		{
			if (!(version == 3 || version == 5 || version == 8))
				throw new ArgumentOutOfRangeException(nameof(version), "Version must be either 3 or 5 (or 8).");

			var namespaceBytes = namespaceId.ToByteArray(bigEndian: true);

			byte[] data = new byte[namespaceBytes.Length + nameBytes.Length];
			namespaceBytes.CopyTo(data, 0);
			nameBytes.CopyTo(data, namespaceBytes.Length);

			// compute the hash of the namespace ID concatenated with the name data
			//var data = namespaceBytes.Concat(nameBytes).ToArray();

			byte[] hash;
			using (var algorithm = hashAlgo())
				hash = algorithm.ComputeHash(data);

			// Copy 16 first bytes from hash straight into the guid (ignoring the rest of the bytes in the hash)
			var newGuid = new byte[16];
			Array.Copy(hash, 0, newGuid, 0, 16);

			// set version
			newGuid[6] = (byte)((version << 4) | (newGuid[6] & 0b0000_1111));

			// set variant IETF
			newGuid[8] = (byte)((newGuid[8] & 0b0011_1111) | 0b1000_0000);

			return FromByteArray(newGuid, bigEndian: true);
		}

		/// <summary>
		/// Create xor Guid from 2 regular Guid's.
		/// Since a and b must be variant IETF, the result will always be a Guid of variant ApolloNCS.
		/// Can only be used safely once, so will fail is a or b is already xor'ed.
		/// Non-standard.
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns>Xor Guid</returns>
		public static Guid CreateXorGuid(this Guid a, Guid b)
		{
			var a_bytes = a.ToByteArray(bigEndian: true);
			var b_bytes = b.ToByteArray(bigEndian: true);

			if ((a_bytes[8] & 0b1100_0000) != 0b1000_0000)
				throw new InvalidOperationException("a is not variant " + GuidVariant.IETF);
			if ((b_bytes[8] & 0b1100_0000) != 0b1000_0000)
				throw new InvalidOperationException("b is not variant " + GuidVariant.IETF);

			byte[] bytes = new byte[16];
			for (int i = 0; i <= 15; i++)
				bytes[i] = (byte)(a_bytes[i] ^ b_bytes[i]);

			// Sanity
			if ((bytes[8] & 0b1100_0000) != 0b0000_0000)
				throw new InvalidOperationException("Result is not variant " + GuidVariant.ApolloNCS);

			return FromByteArray(bytes, bigEndian: true);
		}

		/// <summary>
		/// From the xor-Guid and a or b, will return the opposite (xorGuid + a = b or xorGuid + b = a).
		/// </summary>
		/// <param name="xorGuid"></param>
		/// <param name="a_or_b"></param>
		/// <returns>Opposite of a_or_b. If a_or_b is a, b is returned. If a_or_b is b, a is returned.</returns>
		public static Guid ReverseXorGuid(this Guid xorGuid, Guid a_or_b)
		{
			var xor_bytes = xorGuid.ToByteArray(bigEndian: true);
			var a_or_b_bytes = a_or_b.ToByteArray(bigEndian: true);

			if ((xor_bytes[8] & 0b1100_0000) != 0b0000_0000)
				throw new InvalidOperationException("xorGuid is not variant " + GuidVariant.ApolloNCS);
			if ((a_or_b_bytes[8] & 0b1100_0000) != 0b1000_0000)
				throw new InvalidOperationException("a_or_b is not variant " + GuidVariant.IETF);

			byte[] bytes = new byte[16];
			for (int i = 0; i <= 15; i++)
				bytes[i] = (byte)(xor_bytes[i] ^ a_or_b_bytes[i]);

			// Sanity
			if ((bytes[8] & 0b1100_0000) != 0b1000_0000)
				throw new InvalidOperationException("Result is not variant " + GuidVariant.IETF);

			return FromByteArray(bytes, bigEndian: true);
		}


		public static Guid ConvertVersion7To8MsSql(this Guid g_v7)
		{
			var b = g_v7.ToByteArray(bigEndian: true);
			ConvertVersion7To8MsSql(b);
			return FromByteArray(b, bigEndian: true);
		}

		/// <summary>
		/// Convert from v8MsSql to v7
		/// </summary>
		/// <param name="g_v8mssql"></param>
		/// <returns>A version 7 Guid</returns>
		public static Guid ConvertVersion8MsSqlTo7(this Guid g_v8mssql)
		{
			var b = g_v8mssql.ToByteArray(bigEndian: true);
			ConvertVersion8MsSqlTo7(b);
			return FromByteArray(b, bigEndian: true);
		}

		/// <summary>
		/// Convert from v1 to v6
		/// </summary>
		/// <param name="g_v1"></param>
		/// <returns>A version 6 Guid</returns>
		public static Guid ConvertVersion1To6(this Guid g_v1)
		{
			var b = g_v1.ToByteArray(bigEndian: true);
			ConvertVersion1To6(b);
			return FromByteArray(b, bigEndian: true);
		}

		/// <summary>
		/// Convert from v6 to v1
		/// </summary>
		/// <param name="g_v6"></param>
		/// <returns>A version 1 Guid</returns>
		public static Guid ConvertVersion6To1(this Guid g_v6)
		{
			var b = g_v6.ToByteArray(bigEndian: true);
			ConvertVersion6To1(b);
			return FromByteArray(b, bigEndian: true);
		}


		/*
		  https://github.com/microsoft/referencesource/blob/master/System.Data/System/Data/SQLTypes/SQLGuid.cs
		     public struct SqlGuid : INullable, IComparable, IXmlSerializable {
		// Comparison orders.
		private static readonly int[] x_rgiGuidOrder = new int[16]
		{10, 11, 12, 13, 14, 15, 8, 9, 6, 7, 4, 5, 0, 1, 2, 3};
        // But this is with LE. With BE it becomes (because 0-3 is int and 4-5 and 6-7 is short):
        {10, 11, 12, 13, 14, 15, 8, 9, 7!, 6!, 5!, 4!, 3!, 2!, 1!, 0!};
	So 0-5 can be swapped with 10-15.
     But the middle is worse:   
    v7->v8    
    6 -> 8   variant | 6.5-8 -> 8.3-6 | 7.1-2 -> 8.7-8
    7 -> 9   7.3-8 -> 9.1-6 | 8.3-4 -> 9.7-8
    8 -> 7   8.5-8 -> 7.1-4 | 9.1-4 -> 7.5-8
    9 -> 6   version | 9.5-8 -> 6.5-8 
*/
		private static void ConvertVersion7To8MsSql(byte[] bytes)
		{
			//10x IETF
			if ((bytes[8] & 0b1100_0000) != 0b1000_0000)
				throw new InvalidOperationException("Not variant " + GuidVariant.IETF);

			byte oldVer = (byte)((bytes[6] & 0b1111_0000) >> 4);
			if (oldVer != 7)
				throw new InvalidOperationException("Not version 7");

			// time
			SwapBytes(bytes, 0, 10);
			SwapBytes(bytes, 1, 11);
			SwapBytes(bytes, 2, 12);
			SwapBytes(bytes, 3, 13);
			SwapBytes(bytes, 4, 14);
			SwapBytes(bytes, 5, 15);

			var bytes_6 = bytes[6];
			var bytes_7 = bytes[7];
			var bytes_8 = bytes[8];
			var bytes_9 = bytes[9];

			const byte newVer = 8;
			bytes[8] = (byte)(bytes_8 & 0b1100_0000 | (bytes_6 << 2) & 0b0011_1100 | (bytes_7 >> 6) & 0b0000_0011);
			bytes[9] = (byte)((bytes_7 << 2) & 0b1111_1100 | (bytes_8 >> 4) & 0b0000_0011);
			bytes[7] = (byte)((bytes_8 << 4) & 0b1111_0000 | (bytes_9 >> 4) & 0b0000_1111);
			bytes[6] = (byte)(newVer << 4 | bytes_9 & 0b0000_1111);
		}

		private static void ConvertVersion8MsSqlTo7(byte[] bytes)
		{
			//10x IETF
			if ((bytes[8] & 0b1100_0000) != 0b1000_0000)
				throw new InvalidOperationException("Not variant " + GuidVariant.IETF);

			byte oldVer = (byte)((bytes[6] & 0b1111_0000) >> 4);
			if (oldVer != 8)
				throw new InvalidOperationException("Not version 8");

			// time
			SwapBytes(bytes, 0, 10);
			SwapBytes(bytes, 1, 11);
			SwapBytes(bytes, 2, 12);
			SwapBytes(bytes, 3, 13);
			SwapBytes(bytes, 4, 14);
			SwapBytes(bytes, 5, 15);

			var bytes_6 = bytes[6];
			var bytes_7 = bytes[7];
			var bytes_8 = bytes[8];
			var bytes_9 = bytes[9];

			const byte newVer = 7;
			bytes[6] = (byte)(newVer << 4 | (bytes_8 >> 2) & 0b0000_1111);
			bytes[7] = (byte)((bytes_8 << 6) & 0b1100_0000 | (bytes_9 >> 2) & 0b0011_1111);
			bytes[8] = (byte)(bytes_8 & 0b1100_0000 | (bytes_9 << 4) & 0b0011_0000 | (bytes_7 >> 4) & 0b0000_1111);
			bytes[9] = (byte)((bytes_7 << 4) & 0b1111_0000 | bytes_6 & 0b0000_1111);
		}

		/// <summary>
		/// Fake a Guid with digits 0-9 (no hex).
		/// Range:<br/>
		/// 0 -> 00000000-0000-0000-0000-00000000000<br/>
		/// 10 -> 00000000-0000-0000-0000-00000000010<br/>
		/// 42 -> 00000000-0000-0000-0000-00000000042<br/>
		/// ...<br/>
		/// 2147483647 -> 00000000-0000-0000-0000-02147483647 (int.MaxValue)<br/>
		/// Non-standard.
		/// </summary>
		/// <param name="i"></param>
		/// <returns>A numeric Guid</returns>
		public static Guid CreateNumericGuid(int i)
		{
			if (i < 0)
				throw new ArgumentException("Must be positive");
			return new Guid(i.ToString().PadLeft(32, '0'));
		}

		/// <summary>
		/// 00000000-0000-0000-0000-00000000000 -> 0<br/>
		/// 00000000-0000-0000-0000-00000000010 -> 10<br/> 
		/// 00000000-0000-0000-0000-00000000042 -> 42<br/>
		/// ...<br/>
		/// 00000000-0000-0000-0000-02147483647 (int.MaxValue) -> 2147483647<br/>
		/// 
		/// Everything else will give error
		/// </summary>
		/// <param name="g"></param>
		/// <returns>The number used when creating the numeric Guid</returns>
		public static int ReverseNumericGuid(this Guid g)
		{
			return int.Parse(g.ToString("N"));
		}

		private static void ConvertVersion1To6(byte[] bytes)
		{
			//10x IETF
			if ((bytes[8] & 0b1100_0000) != 0b1000_0000)
				throw new InvalidOperationException("Not variant " + GuidVariant.IETF);

			byte oldVer = (byte)((bytes[6] & 0b1111_0000) >> 4);
			if (oldVer != 1)
				throw new InvalidOperationException("Not version 1");

			var bytes_0 = bytes[0];
			var bytes_1 = bytes[1];
			var bytes_2 = bytes[2];
			var bytes_3 = bytes[3];
			var bytes_4 = bytes[4];
			var bytes_5 = bytes[5];
			var bytes_6 = bytes[6];
			var bytes_7 = bytes[7];

			const byte newVer = 6;

			bytes[0] = (byte)((bytes_6 & 0b0000_1111) << 4 | (bytes_7 & 0b1111_0000) >> 4);
			bytes[1] = (byte)((bytes_7 & 0b0000_1111) << 4 | (bytes_4 & 0b1111_0000) >> 4);
			bytes[2] = (byte)((bytes_4 & 0b0000_1111) << 4 | (bytes_5 & 0b1111_0000) >> 4);
			bytes[3] = (byte)((bytes_5 & 0b0000_1111) << 4 | (bytes_0 & 0b1111_0000) >> 4);

			bytes[4] = (byte)((bytes_0 & 0b0000_1111) << 4 | (bytes_1 & 0b1111_0000) >> 4);
			bytes[5] = (byte)((bytes_1 & 0b0000_1111) << 4 | (bytes_2 & 0b1111_0000) >> 4);

			bytes[6] = (byte)(newVer << 4 | bytes_2 & 0b0000_1111);
			bytes[7] = bytes_3;
		}

		private static void ConvertVersion6To1(byte[] bytes)
		{
			//10x IETF
			if ((bytes[8] & 0b1100_0000) != 0b1000_0000)
				throw new InvalidOperationException("Not variant " + GuidVariant.IETF);

			byte oldVer = (byte)((bytes[6] & 0b1111_0000) >> 4);
			if (oldVer != 6)
				throw new InvalidOperationException("Not version 6");

			var bytes_0 = bytes[0];
			var bytes_1 = bytes[1];
			var bytes_2 = bytes[2];
			var bytes_3 = bytes[3];
			var bytes_4 = bytes[4];
			var bytes_5 = bytes[5];
			var bytes_6 = bytes[6];
			var bytes_7 = bytes[7];

			const byte newVer = 1;

			bytes[6] = (byte)(newVer << 4 | (bytes_0 & 0b1111_0000) >> 4);
			bytes[7] = (byte)((bytes_0 & 0b0000_1111) << 4 | (bytes_1 & 0b1111_0000) >> 4);

			bytes[4] = (byte)((bytes_1 & 0b0000_1111) << 4 | (bytes_2 & 0b1111_0000) >> 4);
			bytes[5] = (byte)((bytes_2 & 0b0000_1111) << 4 | (bytes_3 & 0b1111_0000) >> 4);

			bytes[0] = (byte)((bytes_3 & 0b0000_1111) << 4 | (bytes_4 & 0b1111_0000) >> 4);
			bytes[1] = (byte)((bytes_4 & 0b0000_1111) << 4 | (bytes_5 & 0b1111_0000) >> 4);
			bytes[2] = (byte)((bytes_5 & 0b0000_1111) << 4 | bytes_6 & 0b0000_1111);
			bytes[3] = bytes_7;
		}

		/// <summary>
		/// Get information about a Guid
		/// </summary>
		/// <param name="g"></param>
		/// <param name="version8type"></param>
		/// <returns>Info about the Guid</returns>
		public static GuidInfo GetGuidInfo(this Guid g, GuidVersion8Type version8type = GuidVersion8Type.Unknown)
		{
			var b = g.ToByteArray(bigEndian: true);

			var vv = GetVariantAndVersion(b);
			if (vv.Variant == GuidVariant.IETF)
			{
				if (vv.Version == 1 || vv.Version == 6)
				{
					if (vv.Version == 1)
					{
						ConvertVersion1To6(b);
					}

					// v6
					long time = (long)b[0] << 52 |
						(long)b[1] << 44 |
						(long)b[2] << 36 |
						(long)b[3] << 28 |
						(long)b[4] << 20 |
						(long)b[5] << 12 |
						(long)(b[6] & 0b0000_1111) << 8 |
						(long)b[7];

					short seq = (short)(
						(b[8] & 0b0011_1111) << 8 |
						b[9]
						);

					var mac = new byte[6];
					Array.Copy(b, 10, mac, 0, 6);

					return new GuidInfoVersion1And6(vv.Variant, vv.Version.Value, time, seq, mac);
				}
				else if (vv.Version == 7 || vv.Version == 8)
				{
					if (vv.Version == 8 && version8type == GuidVersion8Type.MsSql)
					{
						ConvertVersion8MsSqlTo7(b);
					}

					// v7
					long time = (long)b[0] << (5 * 8) |
					  (long)b[1] << (4 * 8) |
					  (long)b[2] << (3 * 8) |
					  (long)b[3] << (2 * 8) |
					  (long)b[4] << (1 * 8) |
					  (long)b[5];

					short seq = (short)(
						(b[6] & 0b0000_1111) << 8 |
						b[7]
						);

					long rand_b = (long)(b[8] & 0b0011_1111) << (7 * 8) |
						(long)b[9] << (6 * 8) |
						(long)b[10] << (5 * 8) |
						(long)b[11] << (4 * 8) |
						(long)b[12] << (3 * 8) |
						(long)b[13] << (2 * 8) |
						(long)b[14] << (1 * 8) |
						(long)b[15];

					return new GuidInfoVersion7And8(vv.Variant, vv.Version.Value, time, seq, rand_b);
				}
				else if (vv.Version is not null)
					return new GuidInfoVersion(vv.Variant, vv.Version.Value);
				else
					throw new Exception($"Impossible: {GuidVariant.IETF} always have version");
			}
			else
				return new GuidInfo(vv.Variant);
		}

		/// <summary>
		/// Increment a Guid. Depending on size of the increment, 0 to 4 last bytes will be changed/incremented.
		/// Use case: "hide" (encode) an int inside an existing Guid.
		/// Non-standard.
		/// </summary>
		/// <param name="g_base"></param>
		/// <param name="increment">Can be negative</param>
		/// <returns>Incremented Guid</returns>
		public static Guid CreateIncrementedGuid(this Guid g_base, int increment)
		{
			var bytes = g_base.ToByteArray(bigEndian: true);

			int i = 
				bytes[12] << (3 * 8) |
				bytes[13] << (2 * 8) |
				bytes[14] << (1 * 8) |
				bytes[15];

			i += increment;

			bytes[12] = (byte)(i >> (3 * 8));
			bytes[13] = (byte)(i >> (2 * 8));
			bytes[14] = (byte)(i >> (1 * 8));
			bytes[15] = (byte)i;

			return FromByteArray(bytes, bigEndian: true);
		}

		/// <summary>
		/// From the base Guid and the incremented Guid, get back the increment used to create the incremented Guid.
		/// </summary>
		/// <param name="g_incremented"></param>
		/// <param name="g_base"></param>
		/// <returns>The increment used when creating the incremented Guid</returns>
		public static int ReverseIncrementedGuid(this Guid g_incremented, Guid g_base)
		{
			var base_bytes = g_base.ToByteArray(bigEndian: true);
			var inc_bytes = g_incremented.ToByteArray(bigEndian: true);

			// bytes should be equal until the 4-0 last bytes
			for (int i = 0; i <= 15; i++)
			{
				var eq = base_bytes[i] == inc_bytes[i];
				if (i <= 11 && !eq)
					throw new ArgumentException("First 12 bytes should be equal");

				if (i >= 12 && !eq)
				{
					int base_int = 0;
					int inc_int = 0;
					for (int j = i; j <= 15; j++)
					{
						base_int |= base_bytes[j] << ((15 - j) * 8);
						inc_int |= inc_bytes[j] << ((15 - j) * 8);
					}
					return inc_int - base_int;
				}
			}

			// both guid's equal
			return 0;
		}

		/// <summary>
		/// Parse eg. 0x01918D8D60A77B77AF4A98D3DF112D66
		/// or 01918D8D60A77B77AF4A98D3DF112D66
		/// (with or without 0x prefix)
		/// Case insensitive.
		/// </summary>
		/// <param name="hex"></param>
		/// <returns></returns>
		public static Guid FromHexString(string hex, bool bigEndian = true)
		{
			if (hex.Length == 16 * 2)
				return FromByteArray(StringToByteArrayFastest(hex), bigEndian: bigEndian);

			if (hex.Length == 17 * 2 && hex[0] == '0' && (hex[1] == 'x' || hex[1] == 'X'))
				return FromByteArray(StringToByteArrayFastest(hex.Substring(2)), bigEndian: bigEndian);

			throw new ArgumentException("Must be 32 or 34 chars (0x prefixed)");
		}

		private static byte[] StringToByteArrayFastest(string hex)
		{
			if (hex.Length % 2 == 1)
				throw new ArgumentException("The binary key cannot have an odd number of digits");

			byte[] arr = new byte[hex.Length >> 1];

			for (int i = 0; i < hex.Length >> 1; ++i)
			{
				arr[i] = (byte)((GetHexVal(hex[i << 1]) << 4) + (GetHexVal(hex[(i << 1) + 1])));
			}

			return arr;
		}

		private static int GetHexVal(char hex)
		{
			int val = (int)hex;
			//For uppercase A-F letters:
			//return val - (val < 58 ? 48 : 55);
			//For lowercase a-f letters:
			//return val - (val < 58 ? 48 : 87);
			//Or the two combined, but a bit slower:
			return val - (val < 58 ? 48 : (val < 97 ? 55 : 87));
		}

	}
}
