## 0.0.3
* Make CreateVersion7 and CreateVersion8MsSql monotonic (in same process), so remove CreateVersion7Sequence and CreateVersion8MsSqlSequence.
* Change ms sql scripts to be monotonic for uuid_v7 and uuid_v8mssql (in same session).

## 0.0.2
* Bitswapping was not correct for v7<->v8MsSql. bytes[6] and bytes[7] were swapped (endianess).
* CreateVersion8MsSql: forgot to preserve 2 random bits in bytes[9].
* Increase the counter size in sequential v7 and v8mssql from 12 to 26 bits (include all bits from 4 middle bytes).
* Instead of +1 in the counter, add a random byte (1-255).

## 0.0.1
* First release.
* Create Uuid v1 and v6. Convert between v1 and v6.
* Create Uuid v7 and v8MsSql. Convert between v7 and v8MsSql.
* Create Uuid v3 (MD5) and v5 (SHA1)
* Create Uuid v8SHA256 and v8SHA512
* Get info about a Guid (dissect)
* Create monotonic sequence of v7 and v8MsSql Guid's
* Create NEWSEQUENTIALID (ms sql)
* Create/reverse Xor Guid
* Create/reverse numeric Guid
* Create/reverse incremented Guid

