# GuidPhantom
Yet another Guid library:-) Create Uuid v1, v3, v5, v6, v7, v8MsSql, v8SHA256, v8SHA512, convert between v1 and v6, convert between v7 and v8MsSql, get Guid info, create NEWSEQUENTIALID (ms sql), create/reverse Xor Guid, create/reverse numeric Guid, create/reverse incremented Guid.

Version 7 Guid's generated by GuidPhantom are currently encoded as:

	Bits	
	48	Unix timestamp milliseconds
	4	0x7 (version)
	12	Random data / counter
	2	0x2 (variant)
	6	Random data / counter
	56	Random data

Counter start as 4bit random data.
If timestamp does not change, the counter start to increment + 1.
If the counter overflows, the timestamp is incremented + 1 and the counter is extended with 1 bit (up to max 18bits).
Counter bits are reduced gradually if on the edge/reset to 4bits when time catches up.
Goal: make Guid's created with the same timestamp as random as possible (like Guid's created with different timestamps).
This does not reduce the risk of collisons (ref: Ulid) but it tries to avoid that Guid's become visibly different (less random),
just because they were made during the same timestamp.

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

Also has sql scripts for ms sql server to create Guid v3, v5, v7, v8mssql and convert between v7 and v8mssql. Currently uses fixed 12bit counter.

v8mssql: Similar to Version 7, but bits reordered to make it ordered as uniqueidentifier in Ms Sql Server.
Ms Sql Server's uniqueidentifier has strange rules for ordering, so only NEWSEQUENTIALID (non-standard) and v8mssql will be properly ordered.
It is possible to use Version 7 stored as a binary(16) for proper ordering, if stored as big endian. Can either use big endian byte array's directly from client,
or send Guid/uniqueidentifier from the client and convert to byte array in sql, then need to use eg. dbo.uuid_swap_endian(...), to make it big endian.

References:
* https://zendesk.engineering/how-probable-are-collisions-with-ulids-monotonic-option-d604d3ed2de
* https://github.com/mareek/UUIDNext
* https://github.com/LiosK/uuidv7
* https://math.stackexchange.com/questions/4697032/threshold-for-the-number-of-uuids-generated-per-millisecond-at-which-the-colli
* https://github.com/uuid6/uuid6-ietf-draft/issues/60

