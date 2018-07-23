# Currency Quotes API service for ASP.NET Core apps

With this useful service you can query exchange quotes for most of the world's currencies.
API Key and other params are to be fed into the service class ctor. 

Intended use is within ASP.NET Core apps using MVC; inject the service into the DI container 
(inside Startup.cs use services.AddTransient) and then use it in your code.

Current implementation uses CurrencyLayer.com
