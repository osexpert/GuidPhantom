using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace GuidPhantom
{
	public static class GuidKit
	{
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
                GuidKit.SwapEndian(bytes);

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
		/// <exception cref="Exception"></exception>
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
			return v1.SwapVersion1And6(verify_version: 1);
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
			SwapEndian(bytes);
			return FromByteArray(bytes, bigEndian: true);
		}

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
		/// Create version7 Guid. 6 first bytes is unix timestamp in milliseconds, the rest is random data.
		/// Rand_a is not used as counter, only random data. This means that if more than 1 Guid is made on the same millisecond,
		/// their relative order is random. This implementation matches the one in .NET 9.
		/// </summary>
		/// <returns>Version 7 Guid</returns>
		public static Guid CreateVersion7()
		{
			//            if (_createVersion7.Value != null)
			//              return _createVersion7.Value();

			return CreateVersion7(DateTimeOffset.UtcNow);
		}


		//#if NET9_0_OR_GREATER

		//        public Guid CreateVersion7(DateTimeOffset timestamp) => Guid.CreateVersion7(timestamp);
		//#else

		/// <summary>
		/// Create version7 Guid. 6 first bytes is unix timestamp in milliseconds, the rest is random data.
		/// Rand_a is not used as counter, only random data. This means that if more than 1 Guid is made on the same millisecond,
		/// their relative order is random. This implementation matches the one in .NET 9.
		/// </summary>
		/// <param name="timestamp"></param>
		/// <returns>Version 7 Guid</returns>
		/// <exception cref="ArgumentOutOfRangeException"></exception>
		public static Guid CreateVersion7(DateTimeOffset timestamp)
		{
			//          if (_createVersion7_dto.Value != null)
			//                return _createVersion7_dto.Value(timestamp);

			// 2^48 is roughly 8925.5 years, which from the Unix Epoch means we won't
			// overflow until around July of 10,895. So there isn't any need to handle
			// it given that DateTimeOffset.MaxValue is December 31, 9999. However, we
			// can't represent timestamps prior to the Unix Epoch since UUIDv7 explicitly
			// stores a 48-bit unsigned value, so we do need to throw if one is passed in.

			long unix_ts_ms = timestamp.ToUnixTimeMilliseconds();
			if (unix_ts_ms < 0)
				throw new ArgumentOutOfRangeException(nameof(timestamp));

			short dummy = 0;
			return CreateVersion7(unix_ts_ms, ref dummy, false);
		}

		private static Guid CreateVersion7(long unix_ts_ms, ref short sequence, bool setSequence)
		{
			if (unix_ts_ms < 0)
				throw new ArgumentOutOfRangeException(nameof(unix_ts_ms));

			// This isn't the most optimal way, but we don't have an easy way to get
			// secure random bytes in corelib without doing this since the secure rng
			// is in a different layer.
			Guid result = Guid.NewGuid();

			var bytes = result.ToByteArray(bigEndian: true);

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
				if (sequence < 0 || sequence > 4095)
					throw new ArgumentException("Sequence must be between 0 and 4095");

				bytes[6] = (byte)((bytes[6] & 0b1111_0000) | (sequence >> 8) & 0b0000_1111);
				bytes[7] = (byte)sequence;
			}
			else
			{
				short currSeq = (short)((bytes[6] & 0b0000_1111) << 8 | bytes[7]);
				sequence = currSeq;
			}

			return FromByteArray(bytes, bigEndian: true);
		}
		//#endif

		/// <summary>
		/// Create monotonic (always increasing) sequence of v7 Guid's.
		/// When more than one Guid is created per millisecond, 12bit rand_a (initially seeded by random data) is used as counter (+1).
		/// When counter rollover (rand_a > 4095), the timestamp is incremented (+1).
		/// </summary>
		/// <param name="timeProvider"></param>
		/// <returns>Version7 sequence</returns>
		public static IEnumerable<Guid> CreateVersion7Sequence(TimeProvider timeProvider) => CreateVersion7Sequence(TimeProvider.System);

		/// <summary>
		/// Create monotonic (always increasing) sequence of v7 Guid's.
		/// When more than one Guid is created per millisecond, 12bit rand_a (initially seeded by random data) is used as counter (+1).
		/// When counter rollover (rand_a > 4095), the timestamp is incremented (+1).
		/// </summary>
		/// <returns>Version7 sequence</returns>>
		public static IEnumerable<Guid> CreateVersion7Sequence() => CreateVersion7Or8MsSqlSequence(TimeProvider.System, 7);

		/// <summary>
		/// Same as CreateVersion7Sequence, but data is rearranged to a v8 variant that is ordered in MsSql.
		/// </summary>
		/// <returns>Version8MsSql sequence</returns>
		public static IEnumerable<Guid> CreateVersion8MsSqlSequence() => CreateVersion7Or8MsSqlSequence(TimeProvider.System, 8);

		/// <summary>
		/// Same as CreateVersion7Sequence but bits rearranged to make it ordered in MsSql (and then set as Version8)
		/// </summary>
		/// <param name="timeProvider"></param>
		/// <returns>Version8MsSql sequence</returns>
		public static IEnumerable<Guid> CreateVersion8MsSqlSequence(TimeProvider timeProvider) => CreateVersion7Or8MsSqlSequence(timeProvider, 8);

		private static IEnumerable<Guid> CreateVersion7Or8MsSqlSequence(TimeProvider timeProvider, byte version)
		{
			long? prev_ts = null;
			short sequence = 0;

			while (true)
			{
				long now_ts = timeProvider.GetUtcNow().ToUnixTimeMilliseconds();

				bool setSequence = false;
				if (now_ts <= prev_ts)
				{
					now_ts = prev_ts.Value;
					sequence++;

					if (sequence > 4095)
					{
						now_ts++;
					}
					else
					{
						setSequence = true;
					}
				}

				if (version == 7)
					yield return CreateVersion7(now_ts, ref sequence, setSequence);
				else if (version == 8)
					yield return CreateVersion8MsSql(now_ts, ref sequence, setSequence);
				else
					throw new InvalidOperationException("Not ver 7 or 8");

				prev_ts = now_ts;
			}
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
		/// Same as Version7 but bits rearranged to make it ordered in MsSql (and then set as Version8)
		/// </summary>
		/// <returns>Version8MsSql Guid</returns>
		public static Guid CreateVersion8MsSql() => CreateVersion8MsSql(DateTimeOffset.Now);

		/// <summary>
		/// Same as Version7 but bits rearranged to make it ordered in MsSql (and then set as Version8)
		/// </summary>
		/// <param name="timestamp"></param>
		/// <returns>Version8MsSql Guid</returns>
		/// <exception cref="ArgumentOutOfRangeException"></exception>
		public static Guid CreateVersion8MsSql(DateTimeOffset timestamp)
		{
			//            if (_createVersion7_dto.Value != null)
			//                return _createVersion7_dto.Value(timestamp);

			// 2^48 is roughly 8925.5 years, which from the Unix Epoch means we won't
			// overflow until around July of 10,895. So there isn't any need to handle
			// it given that DateTimeOffset.MaxValue is December 31, 9999. However, we
			// can't represent timestamps prior to the Unix Epoch since UUIDv7 explicitly
			// stores a 48-bit unsigned value, so we do need to throw if one is passed in.

			long unix_ts_ms = timestamp.ToUnixTimeMilliseconds();
			if (unix_ts_ms < 0)
				throw new ArgumentOutOfRangeException(nameof(timestamp));

			short dummy = 0;
			return CreateVersion8MsSql(unix_ts_ms, ref dummy, false);
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
		/// <exception cref="ArgumentOutOfRangeException"></exception>
		/// <exception cref="InvalidOperationException"></exception>
		private static Guid CreateVersion8MsSql(long unix_ts_ms, ref short sequence, bool setSequence)
		{
			if (unix_ts_ms < 0)
				throw new ArgumentOutOfRangeException(nameof(unix_ts_ms));

			// This isn't the most optimal way, but we don't have an easy way to get
			// secure random bytes in corelib without doing this since the secure rng
			// is in a different layer.
			Guid result = Guid.NewGuid();

			var bytes = result.ToByteArray(bigEndian: true);

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
				if (sequence < 0 || sequence > 4095)
					throw new ArgumentException("Sequence must be between 0 and 4095");

				bytes[8] = (byte)((bytes[8] & 0b1100_0000) | (sequence >> 6) & 0b0011_1111);
				bytes[9] = (byte)((sequence << 2) & 0b1111_1100);
			}
			else
			{
				short currSeq = (short)((bytes[8] & 0b0011_1111) << 6 | (bytes[9] & 0b1111_1100) >> 2);
				sequence = currSeq;
			}

			return FromByteArray(bytes, bigEndian: true);
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
		/// <exception cref="NotImplementedException"></exception>
		public static Guid CreateXorGuid(this Guid a, Guid b)
		{
			var a_bytes = a.ToByteArray(bigEndian: true);
			var b_bytes = b.ToByteArray(bigEndian: true);

			if ((a_bytes[8] & 0b1100_0000) != 0b1000_0000)
				throw new InvalidOperationException("a is not variant " + GuidVariant.IETF);
			if ((b_bytes[8] & 0b1100_0000) != 0b1000_0000)
				throw new InvalidOperationException("b is not variant " + GuidVariant.IETF);

			byte[] bytes = new byte[16];
			for (int i = 0; i < 16; i++)
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
		/// <exception cref="NotImplementedException"></exception>
		public static Guid ReverseXorGuid(this Guid xorGuid, Guid a_or_b)
		{
			var xor_bytes = xorGuid.ToByteArray(bigEndian: true);
			var a_or_b_bytes = a_or_b.ToByteArray(bigEndian: true);

			if ((xor_bytes[8] & 0b1100_0000) != 0b0000_0000)
				throw new InvalidOperationException("xorGuid is not variant " + GuidVariant.ApolloNCS);
			if ((a_or_b_bytes[8] & 0b1100_0000) != 0b1000_0000)
				throw new InvalidOperationException("a_or_b is not variant " + GuidVariant.IETF);

			byte[] bytes = new byte[16];
			for (int i = 0; i < 16; i++)
				bytes[i] = (byte)(xor_bytes[i] ^ a_or_b_bytes[i]);

			// Sanity
			if ((bytes[8] & 0b1100_0000) != 0b1000_0000)
				throw new InvalidOperationException("Result is not variant " + GuidVariant.IETF);

			return FromByteArray(bytes, bigEndian: true);
		}

		/// <summary>
		/// Swap the millis time part between the 6 first bytes and 6 last bytes.
		/// </summary>
		/// <param name="g"></param>
		/// <param name="verify_version"></param>
		/// <returns></returns>
		/// <exception cref="InvalidOperationException"></exception>
		private static Guid SwapVersion7And8MsSql(this Guid g, byte verify_version)//, bool swap_rand_a = true)
		{
			// <param name="swap_rand_a">Swap the 12 bits (OPTIONAL sub milliseconds) in octets 6-7</param>

			var bytes = g.ToByteArray(bigEndian: true);
			SwapVersion7And8MsSql(bytes, verify_version);//, swap_rand_a);
			return FromByteArray(bytes, bigEndian: true);
		}

		/// <summary>
		/// Convert from v7 to v8MsSql
		/// </summary>
		/// <param name="g_v7"></param>
		/// <returns>A version 8MsSql Guid</returns>
		public static Guid ConvertVersion7To8MsSql(this Guid g_v7) => SwapVersion7And8MsSql(g_v7, verify_version: 7);

		/// <summary>
		/// Convert from v8MsSql to v7
		/// </summary>
		/// <param name="g_v8mssql"></param>
		/// <returns>A version 7 Guid</returns>
		public static Guid ConvertVersion8MsSqlTo7(this Guid g_v8mssql) => SwapVersion7And8MsSql(g_v8mssql, verify_version: 8);

		/// <summary>
		/// Convert from v1 to v6
		/// </summary>
		/// <param name="g_v1"></param>
		/// <returns>A version 6 Guid</returns>
		public static Guid ConvertVersion1To6(this Guid g_v1) => SwapVersion1And6(g_v1, verify_version: 1);

		/// <summary>
		/// Convert from v6 to v1
		/// </summary>
		/// <param name="g_v6"></param>
		/// <returns>A version 1 Guid</returns>
		public static Guid ConvertVersion6To1(this Guid g_v6) => SwapVersion1And6(g_v6, verify_version: 6);

		private static void SwapVersion7And8MsSql(byte[] bytes, byte verify_version)//, bool swap_rand_a = true)
		{
			//byte[8]:
			//0xx Apollo
			//10x IETF
			//110 MS
			//111 reserved
			if ((bytes[8] & 0b1100_0000) != 0b1000_0000)
				throw new InvalidOperationException("Not variant " + GuidVariant.IETF);

			byte oldVer = (byte)((bytes[6] & 0b1111_0000) >> 4);
			if (oldVer != verify_version)
				throw new InvalidOperationException($"Version mismatch: expected {verify_version}, was {oldVer}");
			byte newVer;
			if (oldVer == 7)
				newVer = 8;
			else if (oldVer == 8)
				newVer = 7;
			else
				throw new InvalidOperationException("Not version 7 or 8");

			// time
			SwapBytes(bytes, 0, 10);
			SwapBytes(bytes, 1, 11);
			SwapBytes(bytes, 2, 12);
			SwapBytes(bytes, 3, 13);
			SwapBytes(bytes, 4, 14);
			SwapBytes(bytes, 5, 15);

			//if (true)//swap_rand_a)
			{
				var bytes_6 = bytes[6];
				var bytes_7 = bytes[7];
				var bytes_8 = bytes[8];
				var bytes_9 = bytes[9];

				// The sub-second part:
				// swap 12 bits from byte 7 - 8 <-> byte 9 - 10.these 12 bits are at different locations.
				// bit1 - 4: version 7 / 8 | bit5 - 8: byte9.bit3 - 6(4bits)
				bytes[6] = (byte)(newVer << 4 | (bytes_8 >> 2) & 0b0000_1111);
				// bit1 - 2: byte9.bit7 - 8 | bit3 - 8: byte10.bit1 - 6
				bytes[7] = (byte)((bytes_8 << 6) & 0b1100_0000 | (bytes_9 >> 2) & 0b0011_1111);
				// bit1 - 2: preserve variant(byte9.bit1-2) | bit3 - 6: byte7.bit5 - 8 | bit7 - 8: byte8.bit1 - 2
				bytes[8] = (byte)(bytes_8 & 0b1100_0000 | (bytes_6 << 2) & 0b0011_1100 | (bytes_7 >> 6) & 0b0000_0011);
				// bit1 - 6: byte8.bit3 - 8 | bit7 - 8: byte10.bit7 - 8(preserve)
				bytes[9] = (byte)((bytes_7 << 2) & 0b1111_1100 | bytes_9 & 0b000_0011);
			}
			//else
			//{
			//    // set version
			//    bytes[6] = (byte)((newVer << 4) | (bytes[6] & 0b0000_1111));
			//}


		}

		/// <summary>
		/// Fake a Guid with digits 0-9 (no hex).
		/// Range:<br/>
		/// 0 -> 00000000-0000-0000-0000-00000000000<br/>
		/// 1 -> 00000000-0000-0000-0000-00000000001<br/>
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
		/// 00000000-0000-0000-0000-00000000001 -> 1<br/>
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

		private static Guid SwapVersion1And6(this Guid g, byte verify_version)
		{
			var bytes = g.ToByteArray(bigEndian: true);
			SwapVersion1And6(bytes, verify_version);
			return FromByteArray(bytes, bigEndian: true);
		}

		private static void SwapVersion1And6(byte[] bytes, byte verify_version)
		{
			//byte[8]:
			//0xx Apollo
			//10x IETF
			//110 MS
			//111 reserved
			if ((bytes[8] & 0b1100_0000) != 0b1000_0000)
				throw new InvalidOperationException("Not variant " + GuidVariant.IETF);

			var bytes_0 = bytes[0];
			var bytes_1 = bytes[1];
			var bytes_2 = bytes[2];
			var bytes_3 = bytes[3];
			var bytes_4 = bytes[4];
			var bytes_5 = bytes[5];
			var bytes_6 = bytes[6];
			var bytes_7 = bytes[7];

			byte oldVer = (byte)((bytes[6] & 0b1111_0000) >> 4);
			if (oldVer != verify_version)
				throw new InvalidOperationException($"Version mismatch: expected {verify_version}, was {oldVer}");
			byte newVer;
			if (oldVer == 1)
			{
				newVer = 6;

				bytes[0] = (byte)((bytes_6 & 0b0000_1111) << 4 | (bytes_7 & 0b1111_0000) >> 4);
				bytes[1] = (byte)((bytes_7 & 0b0000_1111) << 4 | (bytes_4 & 0b1111_0000) >> 4);
				bytes[2] = (byte)((bytes_4 & 0b0000_1111) << 4 | (bytes_5 & 0b1111_0000) >> 4);
				bytes[3] = (byte)((bytes_5 & 0b0000_1111) << 4 | (bytes_0 & 0b1111_0000) >> 4);

				bytes[4] = (byte)((bytes_0 & 0b0000_1111) << 4 | (bytes_1 & 0b1111_0000) >> 4);
				bytes[5] = (byte)((bytes_1 & 0b0000_1111) << 4 | (bytes_2 & 0b1111_0000) >> 4);

				bytes[6] = (byte)(newVer << 4 | bytes_2 & 0b0000_1111);
				bytes[7] = bytes_3;
			}
			else if (oldVer == 6)
			{
				newVer = 1;

				bytes[6] = (byte)(newVer << 4 | (bytes_0 & 0b1111_0000) >> 4);
				bytes[7] = (byte)((bytes_0 & 0b0000_1111) << 4 | (bytes_1 & 0b1111_0000) >> 4);

				bytes[4] = (byte)((bytes_1 & 0b0000_1111) << 4 | (bytes_2 & 0b1111_0000) >> 4);
				bytes[5] = (byte)((bytes_2 & 0b0000_1111) << 4 | (bytes_3 & 0b1111_0000) >> 4);

				bytes[0] = (byte)((bytes_3 & 0b0000_1111) << 4 | (bytes_4 & 0b1111_0000) >> 4);
				bytes[1] = (byte)((bytes_4 & 0b0000_1111) << 4 | (bytes_5 & 0b1111_0000) >> 4);
				bytes[2] = (byte)((bytes_5 & 0b0000_1111) << 4 | bytes_6 & 0b0000_1111);
				bytes[3] = bytes_7;
			}
			else
				throw new InvalidOperationException("Not version 1 or 6");
		}

		/// <summary>
		/// Get information about a Guid
		/// </summary>
		/// <param name="g"></param>
		/// <param name="version8type"></param>
		/// <returns>Info about the Guid</returns>
		/// <exception cref="Exception"></exception>
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
						SwapVersion1And6(b, verify_version: 1);
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
				else if (vv.Version == 7 || (vv.Version == 8 && version8type == GuidVersion8Type.MsSql))
				{
					if ((vv.Version == 8 && version8type == GuidVersion8Type.MsSql))
					{
						SwapVersion7And8MsSql(b, verify_version: 8);
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

					return new GuidInfoVersion7And8MsSql(vv.Variant, vv.Version.Value, time, seq);
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
		/// <exception cref="Exception"></exception>
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
	}
}
