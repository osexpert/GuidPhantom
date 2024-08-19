# GuidPhantom
Yet another Guid library:-) Create Uuid v1, v3, v5, v6, v7, v8MsSql, v8SHA256, v8SHA512, convert between v1 and v6, convert between v7 and v8MsSql, get info about a Guid (dissect), create monotonic sequence of v7 and v8MsSql Guid's, create NEWSEQUENTIALID (ms sql), create/reverse Xor Guid, create/reverse numeric Guid.

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

			var v7_1000 = GuidKit.CreateVersion7Sequence().Take(1000);
			var v8MsSql7_1000 = GuidKit.CreateVersion8MsSqlSequence().Take(1000);

			var vv = nsi.GetVariantAndVersion();
			var info = nsi.GetGuidInfo();

			var xor = GuidKit.CreateXorGuid(new Guid(), new Guid());
			var b = xor.ReverseXorGuid(new Guid());

			var num = GuidKit.CreateNumericGuid(42);
			int num42 = num.ReverseNumericGuid();

			var inc = GuidKit.CreateIncrementedGuid(new Guid(), 42);
			int inc42 = inc.ReverseIncrementedGuid(new Guid());
