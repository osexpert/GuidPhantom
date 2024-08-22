IF OBJECT_ID('uuid_v5') IS NOT NULL DROP FUNCTION uuid_v5
IF OBJECT_ID('uuid_v3') IS NOT NULL DROP FUNCTION uuid_v3
IF OBJECT_ID('uuid_ns') IS NOT NULL DROP FUNCTION uuid_ns
IF OBJECT_ID('uuid_swap_endian') IS NOT NULL DROP FUNCTION uuid_swap_endian
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

CREATE FUNCTION uuid_ns (@ns uniqueidentifier, @name varchar(max), @algo varchar(4), @version tinyint)
RETURNS uniqueidentifier
WITH EXECUTE AS CALLER
AS
BEGIN
	-- swap ns
	set @ns = dbo.uuid_swap_endian(@ns)
	-- start with ns + hash(name)
	declare @uuid binary(16) = SUBSTRING(HASHBYTES(@algo, cast(@ns as binary(16)) + cast(@name as varbinary(max))), 1, 16)
	-- set version
	declare @byte7 binary = (SUBSTRING(@uuid, 7, 1) & 15) | (@version * 16)
	select @uuid = SUBSTRING(@uuid, 1, 6) + @byte7 + SUBSTRING(@uuid, 8, 9)
	-- set variant 1
	declare @byte9 binary = (SUBSTRING(@uuid, 9, 1) & 63) | 128
	select @uuid = SUBSTRING(@uuid, 1, 8) + @byte9 + SUBSTRING(@uuid, 10, 7)
	-- swap result
	return dbo.uuid_swap_endian(@uuid)
END
GO

CREATE FUNCTION uuid_v3 (@ns uniqueidentifier, @name varchar(max))
RETURNS uniqueidentifier
WITH EXECUTE AS CALLER
AS
BEGIN
	return dbo.uuid_ns(@ns, @name, 'MD5', 3)
END
GO

CREATE FUNCTION uuid_v5 (@ns uniqueidentifier, @name varchar(max))
RETURNS uniqueidentifier
WITH EXECUTE AS CALLER
AS
BEGIN
	return dbo.uuid_ns(@ns, @name, 'SHA1', 5)
END
GO

-- sanity checks, matches what is generated here https://www.uuidtools.com/generate/v3 and https://www.uuidtools.com/generate/v5
select case when dbo.uuid_v3('E11EAC0E-4D75-4567-BA60-683D357A9227', 'Test42') = '0dd552e7-647f-3045-86f2-c006e1e17a89' then 'pass' else 'fail' end
select case when dbo.uuid_v5('E11EAC0E-4D75-4567-BA60-683D357A9227', 'Test42') = '73cf5b24-114a-5a5b-837c-64cf22468258' then 'pass' else 'fail' end