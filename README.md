# What is this ?

Simple library to Log event/errors into memory to be used in ASP.NET Core 2.x Projects

By using this library you will have the following features in your project:

- No need any database config
- Automatically logging all of the errors and events for each request based on LogLevel.
- Url to see logs, with search and filtering capabilities.
- ...
# Install via NuGet

To install InMemoryLogger.Core, run the following command in the Package Manager Console.
```code
pm> Install-Package InMemoryLogger.Core
```
You can also view the [package page](https://www.nuget.org/packages/InMemoryLogger.Core) on NuGet.

# How to use ?


1- install package from nuget.

2- add required services to Startup class as below :

```code
     services.AddInMemoryLogger(options=> 
            {
                options.Filter = (loggerName, loglevel) => loglevel >= LogLevel.Error;
                options.Path = "/InMemoryLogs";
            });
```

 
3- add middleware to Startup class as below :

```code
   app.UseInMemoryLogger();
```

4- To view list of logs, enter the url defined at step 2 in browser. like : www.site.com/InMemoryLogs

 
# Thanks
[Sem Shekhovtsov](https://www.codeproject.com/script/Membership/View.aspx?mid=12906893)
