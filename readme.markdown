
RavenSessionStateStoreProvider
==============================

An ASP.NET [session-state store-provider](http://msdn.microsoft.com/en-us/library/ms178587.aspx) implementation using [RavenDB](http://ravendb.net/) for persistence.

NuGet
===========================
The easiest way to install is via the NuGet package (http://nuget.org/List/Packages/Raven.AspNet.SessionState).

Configuration
=============================
    <configuration>

        <connectionStrings>
                <add name="SessionState" connectionString="Url = http://localhost:8080; DefaultDatabase=SessionState;" />
        </connectionStrings>


        <system.web>
            <sessionState  mode="Custom" customProvider="RavenSessionStateStore">
                    <providers>
                        <add name="RavenSessionStateStore" type="Raven.AspNet.SessionState.RavenSessionStateStoreProvider" connectionStringName="SessionState" />
                    </providers>
                </sessionState>
        </system.web>

    </configuration>


Notes
=============================
To have RavenDb automatically remove expired session state, install the [Expiration Bundle](http://ravendb.net/bundles/expiration).


Instrumentation
==============================
Run-time instrumentation is provided via [NLog](http://nlog-project.org)

NLog
----------------
The RavenDB SessionStateStoreProvider logs via NLog. See [NLog documentation](http://nlog-project.org/wiki/Documentation) for details on how to configure NLog.

For example, to log to a text file, place a file called NLog.config in your bin directory, with the contents:


    <?xml version="1.0" encoding="utf-8"?>
    <nlog xmlns="http://www.nlog-project.org/schemas/NLog.xsd" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
    
    <targets>
        <target xsi:type="File" name="file" layout="${longdate}|${level:uppercase=true}|${logger}|${message}${exception:format=tostring}"
                fileName="${basedir}/ravenDbSessionState.log"></target>
    </targets>
    <rules>
        <logger name="Raven.AspNet.SessionState.*" minlevel="Debug" writeTo="file" />
    </rules>
    </nlog>



