# MSSQLTableValuedFunctions.NHibernate.Linq

A plugin for NHiberate to support [MSSQL Table-Valued Functions](https://learn.microsoft.com/en-us/sql/relational-databases/user-defined-functions/user-defined-functions?view=sql-server-ver16#table-valued-functions) in Linq-Queries. 

### Architecture

As far as I know, NHibernate doesn't support MSSQL Table-Valued Functions (TVF) natively. However, it is possible to call 
a TVF via a [Named Query](https://nhibernate.info/doc/nhibernate-reference/querysql.html). 

This plugin provides a way to call TVFs via Linq-Queries by implementing a custom Linq-Query - Provider. Two extension 
points from the NHibernate configuration are used to set the linq provider and a translator factory: 
```xml
<property name="query.linq_provider_class"/>
<property name="query.factory_class"/>
```

The TVFs must be properly mapped in Nhibernate which is shown in the usage section below. 

### Usage

1. NHibernate configuration

At first it is necessary to set the linq provider and the translator factory via the NHibenrate configuration:
```xml
<property name="query.linq_provider_class">MSSQLTableValuedFunctions.NHibernate.Linq.TvfQueryProvider, MSSQLTableValuedFunctions.NHibernate.Linq</property>
<property name="query.factory_class">MSSQLTableValuedFunctions.NHibernate.Linq.Hql.TvfQueryTranslatorFactory, MSSQLTableValuedFunctions.NHibernate.Linq</property>
```

2. TVF mapping

This is a simple MSSQL Table-Valued Function which returns a list of dates for the given input date range:
```sql
USE [TableValuedFunctionTests]
GO

/****** Object:  UserDefinedFunction [dbo].[DateRange]    Script Date: 08/01/2023 00:07:40 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


CREATE FUNCTION [dbo].[DateRange] (
@StartDate DATETIME,
@EndDate DATETIME)
RETURNS
@SelectedRange TABLE (ForDate DATETIME)
AS
BEGIN
	;WITH cteRange(DateRange) AS (
		SELECT @StartDate
		UNION ALL
		SELECT DATEADD(dd, 1, DateRange)
		FROM cteRange
		WHERE DateRange <= DATEADD(dd, -1, @EndDate)
	)
	INSERT INTO @SelectedRange (ForDate)
	SELECT DateRange
	FROM cteRange
	OPTION (MAXRECURSION 3660);
	RETURN
END
GO
```

This is the NHibernate mapping to the above TVF:
```xml
<?xml version="1.0" encoding="utf-8" ?>
<hibernate-mapping xmlns="urn:nhibernate-mapping-2.2"
                   assembly="MSSQLTableValuedFunctions.NHibernate.Linq.Tests"
                   namespace="MSSQLTableValuedFunctions.NHibernate.Linq.Tests.Tests.SimpleTableValuedFunction">

    <class name="Date" table="DateRange(:startDate, :endDate)">
        <id name="ForDate" type="DateTime"/>
    </class>
    
</hibernate-mapping>
```

3. Call in code

```csharp
var dateRange = session.Query<Date>()
                       .SetParameter("startDate", DateTime.Today)
                       .SetParameter("endDate", DateTime.Today.AddDays(7))
                       .ToList();
```

This is the query executed by NHibernate when the show_sql configuration option is enabled:
````sql
select date0_.ForDate as fordate1_0_ from DateRange(@p0, @p1) date0_;
@p0 = 2025-01-01T00:00:00.0000000+01:00 [Type: DateTime2 (8:0:0)],
@p1 = 2025-01-08T00:00:00.0000000+01:00 [Type: DateTime2 (8:0:0)]
````

### Executing the Tests
1. Create a database named TableValuedFunctionTests.
2. Create the TVFs

You can also restore the included sql backup file.

```sql
USE [TableValuedFunctionTests]
GO

/****** Object:  UserDefinedFunction [dbo].[DateRange]    Script Date: 08/01/2023 00:07:40 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


CREATE FUNCTION [dbo].[DateRange] (
@StartDate DATETIME,
@EndDate DATETIME)
RETURNS
@SelectedRange TABLE (ForDate DATETIME)
AS
BEGIN
	;WITH cteRange(DateRange) AS (
		SELECT @StartDate
		UNION ALL
		SELECT DATEADD(dd, 1, DateRange)
		FROM cteRange
		WHERE DateRange <= DATEADD(dd, -1, @EndDate)
	)
	INSERT INTO @SelectedRange (ForDate)
	SELECT DateRange
	FROM cteRange
	OPTION (MAXRECURSION 3660);
	RETURN
END
GO

```
```sql
USE [TableValuedFunctionTests]
GO

/****** Object:  UserDefinedFunction [dbo].[DateRangeNullableParameter]    Script Date: 08/01/2023 00:42:54 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


CREATE FUNCTION [dbo].[DateRangeNullableParameter] (
@StartDate DATETIME,
@EndDate DATETIME,
@NullableDate DateTime)
RETURNS
@SelectedRange TABLE (ForDate DATETIME)
AS
BEGIN
	;WITH cteRange(DateRange) AS (
		SELECT @StartDate
		UNION ALL
		SELECT DATEADD(dd, 1, DateRange)
		FROM cteRange
		WHERE DateRange <= DATEADD(dd, -1, @EndDate)
	)
	INSERT INTO @SelectedRange (ForDate)
	SELECT DateRange
	FROM cteRange
	OPTION (MAXRECURSION 3660);
	RETURN
END
GO
```

3. Configure NHibernate and set the proper connection string
```xml
<?xml version="1.0" encoding="utf-8" ?>
<hibernate-configuration xmlns="urn:nhibernate-configuration-2.2">
    <session-factory>
        <property name="connection.provider">NHibernate.Connection.DriverConnectionProvider</property>
        <property name="dialect">NHibernate.Dialect.MsSql2012Dialect</property>
        <property name="connection.driver_class">NHibernate.Driver.SqlClientDriver</property>
        <property name="connection.connection_string">Server=localhost;Database=TableValuedFunctionTests;Integrated Security=SSPI</property>
        <property name="query.linq_provider_class">MSSQLTableValuedFunctions.NHibernate.Linq.TvfQueryProvider, MSSQLTableValuedFunctions.NHibernate.Linq</property>
        <property name="query.factory_class">MSSQLTableValuedFunctions.NHibernate.Linq.Hql.TvfQueryTranslatorFactory, MSSQLTableValuedFunctions.NHibernate.Linq</property>
    </session-factory>
</hibernate-configuration>
```
