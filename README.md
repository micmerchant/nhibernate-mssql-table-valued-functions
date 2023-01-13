# MSSQLTableValuedFunctions.NHibernate.Linq

A plugin for NHiberate to support [MSSQL Table-Valued Functions](https://learn.microsoft.com/en-us/sql/relational-databases/user-defined-functions/user-defined-functions?view=sql-server-ver16#table-valued-functions) in Linq-Queries. 

### Architecture

As far as I know, NHibernate doesn't support MSSQL Table-Valued Functions (TVF) natively. However, it is possible to call a TVF via a [Named Query](https://nhibernate.info/doc/nhibernate-reference/querysql.html). 

This plugin provides a way to call TVFs via Linq-Queries by implementing a custom Linq-Query - Provider. Two extension points from the NHibernate configuration are used to set the linq provider and a translator factory: 
```xml
<property name="query.linq_provider_class"/>
<property name="query.factory_class"/>
```

NHibernate was designed before Dependency Injection frameworks began to shine. Therefore it is quite hard to extend NHibernate beside the configuration options. So the first version of the plugin uses reflection quite heavily to pass the TVF parameters down to the query provider. Non public fields are replaced by a custom implementation and even generated backing fields are accessed via reflection due to the lack of proper injection points.

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

### Executing the Tests
1. Create a database named TableValuedFunctionTests
2. Create the TVFs

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

### Known Issues
I've checked the plugin against the NHibernate tests:

```
Errors, Failures and Warnings

1) Failed : NHibernate.Test.NHSpecificTest.GH3030.ByCodeFixture.LinqShouldNotLeakEntityParameters
  Expected: null
  But was:  <NHibernate.Test.NHSpecificTest.GH3030.ByCodeFixture+Entity>
   at NHibernate.Test.NHSpecificTest.GH3030.ByCodeFixture.LinqShouldNotLeakEntityParameters() in C:\git\Repositories\Tools\NHibernate\Default\Core\src\NHibernate.Test\NHSpecificTest\GH3030\ByCodeFixture.cs:line 63

2) Failed : NHibernate.Test.QueryTest.NamedParametersFixture.TestNullNamedParameter
should throw if can't guess the type of parameter
   at NHibernate.Test.QueryTest.NamedParametersFixture.TestNullNamedParameter() in C:\git\Repositories\Tools\NHibernate\Default\Core\src\NHibernate.Test\QueryTest\NamedParametersFixture.cs:line 58

Run Settings
    DisposeRunners: True
    WorkDirectory: C:\git\Repositories\Tools\NHibernate\Default\Core
    MaxAgents: 1
    BasePath: C:\git\Repositories\Tools\NHibernate\Default\Core
    AutoBinPath: True
    RuntimeFramework: net-4.0
    ProcessModel: Multiple
    ImageRuntimeVersion: 4.0.30319
    ImageTargetFrameworkName: .NETFramework,Version=v4.8
    ImageRequiresX86: False
    ImageRequiresDefaultAppDomainAssemblyResolver: False
    NumberOfTestWorkers: 16

Test Run Summary
  Overall result: Failed
  Test Count: 13604, Passed: 12830, Failed: 2, Warnings: 0, Inconclusive: 66, Skipped: 706
    Failed Tests - Failures: 2, Errors: 0, Invalid: 0
    Skipped Tests - Ignored: 483, Explicit: 223, Other: 0
  Start time: 2023-01-13 08:25:12Z
    End time: 2023-01-13 08:34:57Z
    Duration: 584.361 seconds
    
```

1) Failed : NHibernate.Test.NHSpecificTest.GH3030.ByCodeFixture.LinqShouldNotLeakEntityParameters
I've no idea who is holding a reference to the test object.

2) Failed : NHibernate.Test.QueryTest.NamedParametersFixture.TestNullNamedParameter
Is ignored for the default query translator factory and fails now with a custom factory.

### Planned Improvments
I don't really like the heavy usage of reflection. Therefore I' am working on a [pull request](https://github.com/nhibernate/nhibernate-core/pull/3209) for NHibernate to get rid off the reflection calls. I want to thank [@fredericDelaporte](https://github.com/fredericDelaporte) for his inputs and reviews and I hope that the PR makes it into the next minor release of NHibernate. 
