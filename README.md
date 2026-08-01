# Auth0 BFF DPoP API

## Debugging

Start the Angular project from the ui folder

```
ng serve --ssl
```

## Start the ASP.NET Core project from the server folder

```
dotnet run
```

Or just open Visual Studio and run the solution.

## Credits and used libraries
- NetEscapades.AspNetCore.SecurityHeaders
- Yarp.ReverseProxy
- ASP.NET Core
- Angular
- Auth0 NuGet packages

## UI Angular setup using Angular CLI

```
npm install -g @angular/cli latest

ng update

ng update @angular/cli @angular/core
```

## Links

https://auth0.com/docs/quickstart/webapp/aspnet-core

https://auth0.com/blog/backend-for-frontend-pattern-with-auth0-and-dotnet

https://github.com/damienbod/bff-auth0-aspnetcore-angular

https://github.com/damienbod/DPOP-aspnetcore-idp

https://auth0.com/docs/secure/sender-constraining/demonstrating-proof-of-possession-dpop

https://auth0.com/blog/implementing-dpop-with-auth0

https://auth0.com/docs/quickstart/backend/aspnet-core-webapi#using-dpop-for-enhanced-security