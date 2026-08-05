# Auth0 BFF Web application (Angular & ASP.NET Core) using downstream API protected with OAuth DPoP

![Authn](https://github.com/damienbod/Auth0BffDpopApi/blob/main/images/system.context.drawio.png)

## Features
- Using OpenID Connect with client assertions (private key JWT)
- Using OAuth DPoP (Demonstrating Proof of Possession) for enhanced security
- Using OAuth PAR (Pushed Authorization Requests) for enhanced security
- Confidential client with client assertion and private key JWT
- Using YARP Reverse Proxy to forward requests to the API

- TODO Implement production YARP for proxied requests to the API with DPoP and PAR
- TODO Debug if User info endpoint is working with private key JWT, DPoP and PAR
- TODO Support mixed APIs


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
- Duende FOSS packages

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