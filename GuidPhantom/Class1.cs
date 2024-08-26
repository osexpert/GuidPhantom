using System;
//using System.Buffers;
//using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;

namespace GuidPhantom
{
    public enum GuidVariant : byte
    {
        /// <summary>
        /// variant #0 is what was defined in the 1989 HP/Apollo Network Computing Architecture (NCA)
        /// specification and implemented in NCS 1.x and DECrpc v1.
        /// https://github.com/BeyondTrust/pbis-open/blob/master/dcerpc/uuid/uuid.c
        /// NCS: (Apollo) Network Computing System.
        /// 
        /// Guid.Empty will be of this variant.
        /// https://segment.com/blog/a-brief-history-of-the-uuid/
        /// </summary>
        ApolloNCS,
        /// <summary>
        /// Variant #1 is what was defined for the joint HP/DEC specification for the OSF
        /// (in DEC's "UID Architecture Functional Specification Version X1.0.4")
        /// and implemented in NCS 2.0, DECrpc v2, and OSF 1.0 DCE RPC.
        /// https://github.com/BeyondTrust/pbis-open/blob/master/dcerpc/uuid/uuid.c
        /// https://pubs.opengroup.org/onlinepubs/9629399/apdxa.htm
        /// OSF: The Open Software Foundation (later: The Open Group).
        /// NCS: (Apollo) Network Computing System.
        /// DCE: (OSF) Distributed Computing Environment.
        /// DEC: Digital Equipment Corporation (later: Digital)
        /// 
        /// Later specified by IETF in RFC4122.
        /// </summary>
        IETF,
        /// <summary>
        /// Micsosoft used this variant at least for some OLE/RPC/COM/DCOM interface/CLSID's:
        /// IUnknown interface ID: {00000000-0000-0000-C000-000000000046}
        /// CLSID_StdOleLink {00000300-0000-0000-C000-000000000046}
        /// coclass MsiPatch uuid (000c1094-0000-0000-c000-000000000046)] coclass MsiServerX3
        /// </summary>
        Microsoft,
        /// <summary>
        /// Reserved.
        /// Guid.AllBitsSet will be of this variant.
        /// </summary>
        Reserved,
    }



  

    public enum GuidVersion8Type
    {
        Unknown,
        MsSql,
    }

    public class GuidInfo
    {
        public GuidInfo(GuidVariant variant)
        {
            Variant = variant;
        }

        public GuidVariant Variant { get; }
    }

    public class GuidInfoVersion : GuidInfo
    {
        public GuidInfoVersion(GuidVariant variant, byte version) : base(variant)
        {
            Version = version;
        }

        public byte Version { get; }
    }

    /// <summary>
    /// Version 1 and 6
    /// </summary>
    public class GuidInfoVersion1And6 : GuidInfoVersion
    {
        public GuidInfoVersion1And6(GuidVariant variant, byte version, long timestamp, short clock_sequence, byte[] node) : base(variant, version)
        {
            if (node.Length != 6)
                throw new ArgumentException("Node must be 6 bytes");

            Timestamp = timestamp;
            ClockSequence = clock_sequence;
            _node = node;
        }

        /// <summary>
        /// 60-bit timestamp, being the number of 100-nanosecond intervals since midnight 15 October 1582 Coordinated Universal Time (UTC)
        /// </summary>
        public long Timestamp { get; }

        public DateTimeOffset Time => GetTime();

        private DateTimeOffset GetTime()
        {
            var start = new DateTimeOffset(1582, 10, 15, 0, 0, 0, TimeSpan.Zero);
            // in old .net, AddMillis does not add fractional millis...
            return start.AddTicks(Convert.ToInt64(Timestamp / 10000.0 * 10000));
        }

        /// <summary>
        /// 14bits sequence
        /// It can be:
        /// - random data for every Guid generated
        /// - a randomly seeded counter (at boot) that is incremented (with 1) every time:
        ///  a) the node changes
        ///  b) time is the same as for previously generated Guid
        ///  c) time goes back in time (compared to previously generated Guid)
        ///  Must fallback to randomly generated, if the counter is "lost".
        /// </summary>
        public short ClockSequence { get; }


        byte[] _node;

        public string NodeString  => $"{_node[0]:X2}:{_node[1]:X2}:{_node[2]:X2}:{_node[3]:X2}:{_node[4]:X2}:{_node[5]:X2}";

		public long Node => (long)_node[0] << (5 * 8) |
			(long)_node[1] << (4 * 8) |
			(long)_node[2] << (3 * 8) |
			(long)_node[3] << (2 * 8) |
			(long)_node[4] << (1 * 8)|
			(long)_node[5];

		/// <summary>
		/// 6 bytes mac address
		/// </summary>
		public byte[] GetNodeBytes()
        {
            var res = new byte[6];
            Array.Copy(_node, res, 6);
            return res;
        }
    }

    /// <summary>
    /// Version 7 (and Version 8 MsSql)
    /// </summary>
    public class GuidInfoVersion7And8 : GuidInfoVersion
    {
        public GuidInfoVersion7And8(GuidVariant variant, byte version, long timestamp, short rand_a, long rand_b) : base(variant, version)
        {
            Timestamp = timestamp;
            RandA = rand_a;
			RandB = rand_b;
        }

        /// <summary>
        /// 48 bit Unix Epoch (UTC) timestamp in milliseconds
        /// </summary>
        public long Timestamp { get; }

        public DateTimeOffset Time => GetTime();

        /// <summary>
        /// rand_a can be one of:
        /// - 12bits random data
        /// - 12bits fractional milliseconds (OPTIONAL)
        /// - 12bits monotonic sequence (randomly seeded) (OPTIONAL)
        /// </summary>
        public short RandA { get; }

		public long RandB { get; }

		private DateTimeOffset GetTime(bool add_rand_a_as_sub_milliseconds = false)
        {
            var t = DateTimeOffset.FromUnixTimeMilliseconds(Timestamp);
            if (add_rand_a_as_sub_milliseconds)
            {
                // in old .net, AddMillis does not add fractional millis...
                t = t.AddTicks(Convert.ToInt64(RandA / 4096.0 * 10000));
            }
            return t;
        }
    }
}
