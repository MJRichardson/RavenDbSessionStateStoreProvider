RavenSessionStateStoreProvider
==============================

An ASP.NET session-state store-provider (http://msdn.microsoft.com/en-us/library/ms178587.aspx) implementation using RavenDB (http://ravendb.net/) for persistence.

============================
Configuration:
=============================
<configuration>

	<connectionStrings>
    		<add name="SessionState" connectionString="Url = http://localhost:8080; DefaultDatabase=SessionState;" />
	</connectionStrings>


	<system.web>
 		<sessionState  mode="Custom" customProvider="RavenSessionStateStore">
      			<providers>
        			<add name="RavenSessionStateStore" type="Raven.AspNet.RavenSessionStateStoreProvider" connectionStringName="SessionState" />
      			</providers>
    		</sessionState>
	</system.web>

</configuration>






