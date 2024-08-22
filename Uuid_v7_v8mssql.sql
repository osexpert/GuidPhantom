IF OBJECT_ID('uuid_swap_endian') IS NOT NULL drop function uuid_swap_endian
IF OBJECT_ID('uuid_v7_data') IS NOT NULL DROP VIEW uuid_v7_data
IF OBJECT_ID('uuid_v7') IS NOT NULL drop function uuid_v7
IF OBJECT_ID('uuid_v8mssql') IS NOT NULL drop function uuid_v8mssql
IF OBJECT_ID('uuid_v8mssql_from_v7') IS NOT NULL drop function uuid_v8mssql_from_v7
IF OBJECT_ID('uuid_v7_from_v8mssql') IS NOT NULL drop function uuid_v7_from_v8mssql
GO

CREATE FUNCTION uuid_swap_endian (@uuid binary(16))
RETURNS binary(16)
WITH EXECUTE AS CALLER
AS
BEGIN
	return SUBSTRING(@uuid, 4, 1) +
		SUBSTRING(@uuid, 3, 1) +
		SUBSTRING(@uuid, 2, 1) +
		SUBSTRING(@uuid, 1, 1) +
		SUBSTRING(@uuid, 6, 1) +
		SUBSTRING(@uuid, 5, 1) +
		SUBSTRING(@uuid, 8, 1) +
		SUBSTRING(@uuid, 7, 1) +
		SUBSTRING(@uuid, 9, 10)
END
GO

CREATE VIEW uuid_v7_data
AS
SELECT 
	SYSUTCDATETIME() AS utc_now,
    CRYPT_GEN_RANDOM (10) AS rand_10
GO

-- uuid v7: time + random
-- Not ordered in ms sql server if stored as uniqueidentifier.
CREATE FUNCTION uuid_v7()
RETURNS uniqueidentifier
WITH EXECUTE AS CALLER
AS
BEGIN
	declare @rand binary(10)
	declare @utc_now datetime2
	select @utc_now = utc_now, @rand = rand_10 from uuid_v7_data
	declare @epoc_ms bigint = DATEDIFF_BIG(ms, '1970-01-01', @utc_now)
	declare @time binary(6) = cast(@epoc_ms as binary(6))
	declare @uuid binary(16) = @time + @rand

	-- set version 7
	declare @byte7 binary = (SUBSTRING(@uuid, 7, 1) & 15) | (7 * 16)
	set @uuid = SUBSTRING(@uuid, 1, 6) + @byte7 + SUBSTRING(@uuid, 8, 9)

	-- set variant 1
	declare @byte9 binary = (SUBSTRING(@uuid, 9, 1) & 63) | 128
	set @uuid = SUBSTRING(@uuid, 1, 8) + @byte9 + SUBSTRING(@uuid, 10, 7)

	-- swap result
	return dbo.uuid_swap_endian(@uuid)
END
GO

-- Like uuid v7, but swap rand and time to make it ordered in ms sql server when stored as uniqueidentifier.
-- Based on https://github.com/mareek/UUIDNext/blob/main/Src/UUIDNext/Generator/UuidV8SqlServerGenerator.cs
CREATE FUNCTION uuid_v8mssql()
RETURNS uniqueidentifier
WITH EXECUTE AS CALLER
AS
BEGIN
	declare @rand binary(10)
	declare @utc_now datetime2
	select @utc_now = utc_now, @rand = rand_10 from uuid_v7_data
	declare @epoc_ms bigint = DATEDIFF_BIG(ms, '1970-01-01', @utc_now)
	declare @time binary(6) = cast(@epoc_ms as binary(6))
	declare @uuid binary(16) = @rand + @time

	-- set version 8
	declare @byte7 binary = (SUBSTRING(@uuid, 7, 1) & 15) | (8 * 16)
	set @uuid = SUBSTRING(@uuid, 1, 6) + @byte7 + SUBSTRING(@uuid, 8, 9)

	-- set variant 1
	declare @byte9 binary = (SUBSTRING(@uuid, 9, 1) & 63) | 128
	set @uuid = SUBSTRING(@uuid, 1, 8) + @byte9 + SUBSTRING(@uuid, 10, 7)

	-- swap result
	return dbo.uuid_swap_endian(@uuid)
END
GO


-- Create uuid v7 from v8 ms sql server.
-- Based on https://github.com/osexpert/GuidPhantom/blob/main/GuidPhantom/GuidKit.cs
CREATE FUNCTION uuid_v7_from_v8mssql(@uuid_in uniqueidentifier)
RETURNS uniqueidentifier
WITH EXECUTE AS CALLER
AS
BEGIN
	declare @uuid binary(16) = dbo.uuid_swap_endian(@uuid_in)
	declare @first6 binary(6) = SUBSTRING(@uuid, 1, 6)
	declare @last6 binary(6) = SUBSTRING(@uuid, 11, 6)
	declare @bytes_6_org binary = SUBSTRING(@uuid, 7, 1)
	declare @bytes_7_org binary = SUBSTRING(@uuid, 8, 1)
	declare @bytes_8_org binary = SUBSTRING(@uuid, 9, 1)
	declare @bytes_9_org binary = SUBSTRING(@uuid, 10, 1)
	declare @ver_org tinyint = (@bytes_6_org & 240) / 16

	declare @ver_new tinyint
	if @ver_org = 8
		set @ver_new = 7
	else
		set @ver_new = 42 / 0 -- Version must be version 8, so force an error (divide by zero)

	-- swap middle 4 bytes
	declare	@bytes_6 binary = (@ver_new * 16) | ((@bytes_8_org / 4) & 15)
	declare	@bytes_7 binary =  ((@bytes_8_org * 64) & 192) | ((@bytes_9_org / 4) & 63)
	declare	@bytes_8 binary =  (@bytes_8_org & 192) | ((@bytes_9_org * 16) & 48) | ((@bytes_7_org / 16) & 15)
	declare	@bytes_9 binary =  ((@bytes_7_org * 16) & 240) | (@bytes_6_org & 15)

	-- combine
	set @uuid = @last6 + @bytes_6 + @bytes_7 + @bytes_8 + @bytes_9 + @first6
	-- swap result
	return dbo.uuid_swap_endian(@uuid)
END
GO

-- Create uuid v8 ms sql server from uuid v7.
-- Based on https://github.com/osexpert/GuidPhantom/blob/main/GuidPhantom/GuidKit.cs
CREATE FUNCTION uuid_v8mssql_from_v7(@uuid_in uniqueidentifier)
RETURNS uniqueidentifier
WITH EXECUTE AS CALLER
AS
BEGIN
	declare @uuid binary(16) = dbo.uuid_swap_endian(@uuid_in)
	declare @first6 binary(6) = SUBSTRING(@uuid, 1, 6)
	declare @last6 binary(6) = SUBSTRING(@uuid, 11, 6)
	declare @bytes_6_org binary = SUBSTRING(@uuid, 7, 1)
	declare @bytes_7_org binary = SUBSTRING(@uuid, 8, 1)
	declare @bytes_8_org binary = SUBSTRING(@uuid, 9, 1)
	declare @bytes_9_org binary = SUBSTRING(@uuid, 10, 1)
	declare @ver_org tinyint = (@bytes_6_org & 240) / 16

	declare @ver_new tinyint
	if @ver_org = 7
		set @ver_new = 8
	else
		set @ver_new = 42 / 0 -- Version must be version 7, so force an error (divide by zero)

	declare @bytes_8 binary = (@bytes_8_org & 192) | ((@bytes_6_org * 4) & 60) | ((@bytes_7_org / 64) & 3)
	declare @bytes_9 binary = ((@bytes_7_org * 4) & 252) | ((@bytes_8_org / 16) & 3)
	declare @bytes_7 binary = ((@bytes_8_org * 16) & 240) | ((@bytes_9_org / 16) & 15)
	declare @bytes_6 binary = (@ver_new * 16) | (@bytes_9_org & 15)

	-- combine
	set @uuid = @last6 + @bytes_6 + @bytes_7 + @bytes_8 + @bytes_9 + @first6

	-- swap result
	return dbo.uuid_swap_endian(@uuid)
END
GO

-- TESTING BELOW HERE

-- roundtrip
select case when dbo.uuid_v7_from_v8mssql(dbo.uuid_v8mssql_from_v7('017F22E2-79B0-7CC3-98C4-DC0C0C07398F')) = '017F22E2-79B0-7CC3-98C4-DC0C0C07398F' then 'pass' else 'fail' end
select case when dbo.uuid_v8mssql_from_v7(dbo.uuid_v7_from_v8mssql('dc0c0c07-398f-848c-b30d-017f22e279b0')) = 'dc0c0c07-398f-848c-b30d-017f22e279b0' then 'pass' else 'fail' end
-- defacto snapshot from GuidPhantom
select case when dbo.uuid_v7_from_v8mssql('dc0c0c07-398f-848c-b30d-017f22e279b0') = '017F22E2-79B0-7CC3-98C4-DC0C0C07398F' then 'pass' else 'fail' end
select case when dbo.uuid_v8mssql_from_v7('017F22E2-79B0-7CC3-98C4-DC0C0C07398F') = 'dc0c0c07-398f-848c-b30d-017f22e279b0' then 'pass' else 'fail' end

-- This time is not real, but has higher refresh rate than getdate()/sysdatetime()/etc.
-- It will never(?) return the same time twize (monotonic), like getdate()/sysdatetime() does, so it is usefull for testing
-- Uuid generation in tight loop where every Uuid will get a different time (test index fragmention)
IF OBJECT_ID('uuid_fake_time') IS NOT NULL DROP FUNCTION uuid_fake_time
GO
CREATE FUNCTION uuid_fake_time()
RETURNS datetime2
WITH EXECUTE AS CALLER
AS
BEGIN
	declare @ms_ticks_since_boot bigint
	declare @cpu_ticks_since_boot bigint
	declare @sqlserver_start_time datetime2
	select @ms_ticks_since_boot=ms_ticks,@cpu_ticks_since_boot=cpu_ticks,@sqlserver_start_time=sqlserver_start_time from sys.dm_os_sys_info
	declare @cpu_ticks_per_ms int = @cpu_ticks_since_boot / @ms_ticks_since_boot
	declare @ms_since_boot_float float =  @cpu_ticks_since_boot / convert(float, @cpu_ticks_per_ms)
	declare @since_boot_amp bigint = @ms_since_boot_float * 100;
	return DATEADD(ms, @since_boot_amp % 60000, DATEADD(minute, @since_boot_amp / 60000, @sqlserver_start_time))
END
GO

IF OBJECT_ID('uuid_v7_fake_time') IS NOT NULL DROP FUNCTION uuid_v7_fake_time
GO
CREATE FUNCTION uuid_v7_fake_time()
RETURNS uniqueidentifier
WITH EXECUTE AS CALLER
AS
BEGIN
	declare @rand binary(10)
	select @rand = rand_10 from uuid_v7_data
	declare @epoc_ms bigint = DATEDIFF_BIG(ms, '1970-01-01', dbo.uuid_fake_time())
	declare @time binary(6) = cast(@epoc_ms as binary(6))
	declare @uuid binary(16) = @time + @rand

	-- set version 7
	declare @byte7 binary = (SUBSTRING(@uuid, 7, 1) & 15) | (7 * 16)
	set @uuid = SUBSTRING(@uuid, 1, 6) + @byte7 + SUBSTRING(@uuid, 8, 9)

	-- set variant 1
	declare @byte9 binary = (SUBSTRING(@uuid, 9, 1) & 63) | 128
	set @uuid = SUBSTRING(@uuid, 1, 8) + @byte9 + SUBSTRING(@uuid, 10, 7)

	-- swap result
	return dbo.uuid_swap_endian(@uuid)
END
GO

IF OBJECT_ID('uuid_v8mssql_fake_time') IS NOT NULL DROP FUNCTION uuid_v8mssql_fake_time
GO
CREATE FUNCTION uuid_v8mssql_fake_time()
RETURNS uniqueidentifier
WITH EXECUTE AS CALLER
AS
BEGIN
	declare @rand binary(10)
	select @rand = rand_10 from uuid_v7_data
	declare @epoc_ms bigint = DATEDIFF_BIG(ms, '1970-01-01', dbo.uuid_fake_time())
	declare @time binary(6) = cast(@epoc_ms as binary(6))
	declare @uuid binary(16) = @rand + @time

	-- set version 8
	declare @byte7 binary = (SUBSTRING(@uuid, 7, 1) & 15) | (8 * 16)
	set @uuid = SUBSTRING(@uuid, 1, 6) + @byte7 + SUBSTRING(@uuid, 8, 9)

	-- set variant 1
	declare @byte9 binary = (SUBSTRING(@uuid, 9, 1) & 63) | 128
	set @uuid = SUBSTRING(@uuid, 1, 8) + @byte9 + SUBSTRING(@uuid, 10, 7)

	-- swap result
	return dbo.uuid_swap_endian(@uuid)
END
GO

IF OBJECT_ID('UuidFragTest') IS NOT NULL drop table UuidFragTest
GO
CREATE TABLE UuidFragTest
(
	Id UNIQUEIDENTIFIER CONSTRAINT PK_UuidFragTest PRIMARY KEY (Id),
	ts datetime2,
	row_num INT IDENTITY(1, 1)
)

-- test: insert 100k rows
declare @i int = 0
set nocount on
while (@i < 100000)
begin
    insert into UuidFragTest values(
    	dbo.uuid_v8mssql_fake_time(),
    	SYSUTCDATETIME()
	)
    set @i = @i + 1
end

-- check frag
select * from sys.dm_db_index_physical_stats (DB_ID(), NULL, NULL, NULL, NULL)
where object_id = object_id('UuidFragTest')
-- dbo.uuid_v8mssql(): 25-28%
-- dbo.uuid_v8mssql_fake_time(): 0.4%
-- dbo.uuid_v7(): 98-100%
-- dbo.uuid_v7_fake_time: 99%
-- newid(): 98%

IF OBJECT_ID('UuidAsStringFragTest') IS NOT NULL drop table UuidAsStringFragTest
GO
CREATE TABLE UuidAsStringFragTest
(
	Id char(37) CONSTRAINT PK_UuidAsStringFragTest PRIMARY KEY (Id),
	ts datetime2,
	row_num INT IDENTITY(1, 1)
)

-- test: insert 100k rows
declare @i int = 0
set nocount on
while (@i < 100000)
begin
    insert into UuidAsStringFragTest values(
    	cast(dbo.uuid_v7() as char(37)),
    	SYSUTCDATETIME()
	)
    set @i = @i + 1
end

-- check frag
select * from sys.dm_db_index_physical_stats (DB_ID(), NULL, NULL, NULL, NULL)
where object_id = object_id('UuidAsStringFragTest')
-- dbo.uuid_v7(): 19-28%
-- dbo.uuid_v8mssql(): 98-100%
-- dbo.uuid_v7_fake_time(): 67% (WHY SO HIGH? Seems Uuid doesnt sort well lexically. I see that Ulid (subset of Uuid) is made to fix this)
-- newid(): 98%
-- More Ulid stuff here: https://github.com/rmalayter/ulid-mssql/tree/master

IF OBJECT_ID('UuidAsArrayFragTest') IS NOT NULL drop table UuidAsArrayFragTest
GO
CREATE TABLE UuidAsArrayFragTest
(
	Id binary(16) CONSTRAINT PK_UuidAsArrayFragTest PRIMARY KEY (Id),
	ts datetime2,
	row_num INT IDENTITY(1, 1)
)

-- test: insert 100k rows
declare @i int = 0
set nocount on
while (@i < 100000)
begin
    insert into UuidAsArrayFragTest values(
    	dbo.uuid_swap_endian(dbo.uuid_v7_fake_time()),
    	SYSUTCDATETIME()
	)
    set @i = @i + 1
end

-- check frag
select * from sys.dm_db_index_physical_stats (DB_ID(), NULL, NULL, NULL, NULL)
where object_id = object_id('UuidAsArrayFragTest')
-- dbo.uuid_swap_endianess(dbo.uuid_v7()): 25%
-- dbo.uuid_swap_endianess(dbo.uuid_v7_fake_time()): 0.4%
-- dbo.uuid_v7_fake_time(): 98%
-- dbo.uuid_v7: 99%
-- newid(): 99%
-- dbo.uuid_v8mssql(): 99%
-- dbo.uuid_swap_endianess(dbo.uuid_v8mssql()): 99%
