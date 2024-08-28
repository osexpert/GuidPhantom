IF OBJECT_ID('uuid_v7_array') IS NOT NULL drop function uuid_v7_array
IF OBJECT_ID('uuid_v7') IS NOT NULL drop function uuid_v7
IF OBJECT_ID('uuid_v8mssql_array') IS NOT NULL drop function uuid_v8mssql_array
IF OBJECT_ID('uuid_v8mssql') IS NOT NULL drop function uuid_v8mssql
IF OBJECT_ID('uuid_v8mssql_from_v7') IS NOT NULL drop function uuid_v8mssql_from_v7
IF OBJECT_ID('uuid_v7_from_v8mssql') IS NOT NULL drop function uuid_v7_from_v8mssql
IF OBJECT_ID('uuid_swap_endian') IS NOT NULL drop function uuid_swap_endian
IF OBJECT_ID('uuid_v7_data') IS NOT NULL DROP VIEW uuid_v7_data
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
	CRYPT_GEN_RANDOM(10) AS rand_10
GO

-- uuid v7: time + 13bit random / counter + random
-- Not ordered in ms sql server if stored as uniqueidentifier.
-- Based on https://github.com/osexpert/GuidPhantom/blob/main/GuidPhantom/GuidKit.cs
-- Returns big endian array
CREATE FUNCTION uuid_v7_array()
RETURNS binary(16)
WITH EXECUTE AS CALLER
AS
BEGIN
	declare @rand binary(10)
	declare @utc_now datetime2
	select @utc_now = utc_now, @rand = rand_10 from uuid_v7_data
	declare @now_ts bigint = DATEDIFF_BIG(ms, '1970-01-01', @utc_now)

	declare @prev_ts bigint = convert(bigint, SESSION_CONTEXT(N'uuidv7.prev_ts'))
	declare @calc_ts bigint = convert(bigint, SESSION_CONTEXT(N'uuidv7.calc_ts'))
	declare @seq int = convert(int, SESSION_CONTEXT(N'uuidv7.sequence'))

	declare @set_sequence bit = 0
	if (@now_ts < @prev_ts)
		set @calc_ts = @now_ts -- clock going back (do not try to handle)
	else if (@now_ts <= @calc_ts)
	begin
		set @seq += 1
		if (@seq > 8191)
			set @calc_ts += 1
		else
			set @set_sequence = 1
	end
	else
		set @calc_ts = @now_ts
	
	declare @bytes_6 binary = SUBSTRING(@rand, 1, 1)
	declare @bytes_7 binary = SUBSTRING(@rand, 2, 1)
	declare @bytes_8 binary = SUBSTRING(@rand, 3, 1)
	declare @bytes_9 binary = SUBSTRING(@rand, 4, 1)

	-- set version 7
	set @bytes_6 = (@bytes_6 & 15) | (7 * 16)
	-- set variant IETF
	set @bytes_8 = (@bytes_8 & 63) | 128

	if (@set_sequence = 1)
	begin
		if (@seq < 0 or @seq > 8191) set @seq = 42 / 0 -- generate div by zero
		set @bytes_6 = (@bytes_6 & 240) | ((@seq / 512) & 15)
		set @bytes_7 = @seq / 2
		set @bytes_8 = (@bytes_8 & 223) | ((@seq * 32) & 32)
	end
	else
		set @seq = ((@bytes_6 & 15) * 512) | (@bytes_7 * 2) | ((@bytes_8 & 32) / 32)

	EXEC sp_set_session_context 'uuidv7.prev_ts', @now_ts;  
	EXEC sp_set_session_context 'uuidv7.calc_ts', @calc_ts;
	EXEC sp_set_session_context 'uuidv7.sequence', @seq;

	declare @time binary(6) = cast(@calc_ts as binary(6))
	declare @uuid binary(16) = @time + @bytes_6 + @bytes_7 + @bytes_8 + @bytes_9 + SUBSTRING(@rand, 5, 6)
	return @uuid
END
GO

-- Version 7 big endian array. Double swapping sucks, but optional function arguments sucks too (need to use 'default' for optional arguments)
CREATE FUNCTION uuid_v7()
RETURNS uniqueidentifier
WITH EXECUTE AS CALLER
AS
BEGIN
-- PS: seems to be a bug in ms sql. If these two function calls are combined in the same statement, stuff are executed out of order... Its a mystery.
-- Splitting them and the problem is gone...
	declare @uuid binary(16) = dbo.uuid_v7_array()
	return dbo.uuid_swap_endian(@uuid)
END
GO

-- Like uuid v7, but swap rand and time to make it ordered in ms sql server when stored as uniqueidentifier.
-- Based on https://github.com/mareek/UUIDNext/blob/main/Src/UUIDNext/Generator/UuidV8SqlServerGenerator.cs
-- Based on https://github.com/osexpert/GuidPhantom/blob/main/GuidPhantom/GuidKit.cs
-- Returns big endian array
CREATE FUNCTION uuid_v8mssql_array()
RETURNS binary(16)
WITH EXECUTE AS CALLER
AS
BEGIN
	declare @rand binary(10)
	declare @utc_now datetime2
	select @utc_now = utc_now, @rand = rand_10 from uuid_v7_data
	declare @now_ts bigint = DATEDIFF_BIG(ms, '1970-01-01', @utc_now)

	declare @prev_ts bigint = convert(bigint, SESSION_CONTEXT(N'uuidv7.prev_ts'))
	declare @calc_ts bigint = convert(bigint, SESSION_CONTEXT(N'uuidv7.calc_ts'))
	declare @seq int = convert(int, SESSION_CONTEXT(N'uuidv7.sequence'))

	declare @set_sequence bit = 0
	if (@now_ts < @prev_ts)
		set @calc_ts = @now_ts -- clock going back (do not try to handle)
	else if (@now_ts <= @calc_ts)
	begin
		set @seq += 1
		if (@seq > 8191)
			set @calc_ts += 1
		else
			set @set_sequence = 1
	end
	else
		set @calc_ts = @now_ts

	declare @bytes_6 binary = SUBSTRING(@rand, 7, 1)
	declare @bytes_7 binary = SUBSTRING(@rand, 8, 1)
	declare @bytes_8 binary = SUBSTRING(@rand, 9, 1)
	declare @bytes_9 binary = SUBSTRING(@rand, 10, 1)

	-- set version 8
	set @bytes_6 = (@bytes_6 & 15) | (8 * 16)
	-- set variant IETF
	set @bytes_8 = (@bytes_8 & 63) | 128

	if (@set_sequence = 1)
	begin
		if (@seq < 0 or @seq > 8191) set @seq = 42 / 0 -- generate div by zero
		set @bytes_8 = (@bytes_8 & 192) | ((@seq / 128) & 63)
		set @bytes_9 = (@bytes_9 & 1) | ((@seq * 2) & 254)
	end
	else
		set @seq = ((@bytes_8 & 63) * 128) | ((@bytes_9 & 254) / 2)

	EXEC sp_set_session_context 'uuidv7.prev_ts', @now_ts;  
	EXEC sp_set_session_context 'uuidv7.calc_ts', @calc_ts;
	EXEC sp_set_session_context 'uuidv7.sequence', @seq;

	declare @time binary(6) = cast(@calc_ts as binary(6))
	declare @uuid binary(16) = SUBSTRING(@rand, 1, 6) + @bytes_6 + @bytes_7 + @bytes_8 + @bytes_9 + @time
	return @uuid
END
GO

CREATE FUNCTION uuid_v8mssql()
RETURNS uniqueidentifier
WITH EXECUTE AS CALLER
AS
BEGIN
-- PS: seems to be a bug in ms sql. If these two function calls are combined in the same statement, stuff are executed out of order... Its a mystery.
-- Splitting them and the problem is gone...
	declare @uuid binary(16) = dbo.uuid_v8mssql_array()
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

EXEC sp_set_session_context 'uuidv7.prev_ts', NULL  
EXEC sp_set_session_context 'uuidv7.calc_ts', NULL
EXEC sp_set_session_context 'uuidv7.sequence', NULL

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
    	dbo.uuid_v8mssql(),
    	SYSUTCDATETIME()
	)
    set @i = @i + 1
end

-- check frag
select * from sys.dm_db_index_physical_stats (DB_ID(), NULL, NULL, NULL, NULL)
where object_id = object_id('UuidFragTest')
-- dbo.uuid_v8mssql(): 0.6%
-- dbo.uuid_v7(): 99%
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
-- dbo.uuid_v7(): 20-40%
-- dbo.uuid_v8mssql(): 99%
-- newid(): 98%
-- dbo.uuid_v7() is better than newid(), but not great. Ulid produces proper lexical order: https://github.com/rmalayter/ulid-mssql/tree/master

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
    	dbo.uuid_v7_array(),
    	SYSUTCDATETIME()
	)
    set @i = @i + 1
end

-- check frag
select * from sys.dm_db_index_physical_stats (DB_ID(), NULL, NULL, NULL, NULL)
where object_id = object_id('UuidAsArrayFragTest')
-- dbo.uuid_v7_array(): 0.4%
-- dbo.uuid_v7(): 99%
-- newid(): 99%
-- dbo.uuid_v8mssql(): 99%
-- dbo.uuid_v8mssql_array(): 99%


select case when count(*) = 0 then 'pass' else 'fail' end from (
select ROW_NUMBER() over (order by Id asc) as rnum,* from UuidFragTest
) x where x.row_num <> x.rnum
-- 0

select case when count(*) > 0 then 'pass' else 'fail' end from (
select ROW_NUMBER() over (order by Id asc) as rnum,* from UuidAsStringFragTest
) x where x.row_num <> x.rnum
-- many

select case when count(*) = 0 then 'pass' else 'fail' end from (
select ROW_NUMBER() over (order by Id asc) as rnum,* from UuidAsArrayFragTest
) x where x.row_num <> x.rnum
-- 0
