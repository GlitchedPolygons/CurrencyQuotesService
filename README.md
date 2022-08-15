[![NuGet](https://img.shields.io/nuget/v/GlitchedPolygons.Services.CurrencyQuotes.svg)](https://www.nuget.org/packages/GlitchedPolygons.Services.CurrencyQuotes) 
[![Build Status](https://travis-ci.org/GlitchedPolygons/CurrencyQuotesService.svg?branch=master)](https://travis-ci.org/GlitchedPolygons/CurrencyQuotesService)

# Currency Quotes API service
With this useful service wrapper you can query exchange quotes for most of the world's currencies.
API Key and other params are to be fed into the service class ctor. 

Intended use is within ASP.NET Core apps using MVC; inject the service into the DI container 
(inside _Startup.cs_ use `services.AddTransient`) and then use it in your code.

Current implementations use the [CurrencyLayer Web API](currencylayer.com).
