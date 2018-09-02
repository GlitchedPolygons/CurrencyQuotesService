[![CircleCI](https://circleci.com/gh/GlitchedPolygons/CurrencyQuotesService.svg?style=svg)](https://circleci.com/gh/GlitchedPolygons/CurrencyQuotesService)
# Currency Quotes API service
With this useful service you can query exchange quotes for most of the world's currencies.
API Key and other params are to be fed into the service class ctor. 

Intended use is within ASP.NET Core apps using MVC; inject the service into the DI container 
(inside _Startup.cs_ use `services.AddTransient`) and then use it in your code.

Current implementations use the [CurrencyLayer Web API](currencylayer.com).
