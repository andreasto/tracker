# Tracker - Nutrition & Meal Planning Application

This application is built with [Akka.NET](https://getakka.net/) and ASP.NET Web APIs for tracking nutrition, managing meal plans, and creating scalable recipes.

See https://github.com/akkadotnet/akkadotnet-templates/blob/dev/docs/tracker.md for complete and current documentation on this template.

## Features

### Recipe Scaling System
The application includes a sophisticated recipe scaling system that allows recipes to be dynamically adjusted to match different users' calorie requirements. This means a single recipe can serve all users, regardless of their individual caloric needs.

**Key capabilities:**
- **Automatic Scaling**: Recipes automatically scale ingredients and macros based on target calories
- **Universal Access**: Users with higher or lower calorie needs can use the same recipes
- **Intelligent Matching**: Find recipes that best fit your calorie targets
- **Nutritional Accuracy**: All macronutrients scale proportionally

📚 **Documentation:**
- [Recipe Scaling Feature Overview](RECIPE_SCALING_FEATURE.md) - Complete feature documentation
- [Recipe Scaling Examples](RECIPE_SCALING_EXAMPLES.md) - Practical usage examples with curl commands

## Key HTTP Routes

* https://localhost:{ASP_NET_PORT}/swagger/index.html - Swagger endpoint for testing out Akka.NET-powered APIs
* https://localhost:{ASP_NET_PORT}/healthz/akka - Akka.HealthCheck HTTP endpoint

## Petabridge.Cmd Support

This project is designed to work with [Petabridge.Cmd](https://cmd.petabridge.com/). For instance, if you want to check with the status of your Akka.NET Cluster, just run:

```shell
pbm cluster show
```

> NOTE: Petabridge.Cmd binds to [0.0.0.0:9110] on all hosts by default - if you launch multiple instances of this application on the same host you'll see "socket already in use" exceptions raised by the .NET runtime. These are fine - it just means that we can't open Petabridge.Cmd again on that process.
> 
> You can configure the Petabridge.Cmd host to run on port 0 if you want it to be accessible across multiple instances on the same host.