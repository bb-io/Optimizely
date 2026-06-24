# Blackbird.io Optimizely

Blackbird is the new automation backbone for the language technology industry. Blackbird provides enterprise-scale automation and orchestration with a simple no-code/low-code platform. Blackbird enables ambitious organizations to identify, vet and automate as many processes as possible. Not just localization workflows, but any business and IT process. This repository represents an application that is deployable on Blackbird and usable inside the workflow editor.

## Introduction

<!-- begin docs -->

Optimizely Content Management System (CMS) is a digital experience platform for managing multilingual website content. This app connects Blackbird to the Optimizely CMS Management API so you can search content, export localizable fields into a Blackbird-compatible HTML file, translate that file in a TMS, and upload the translated result back to Optimizely.

## Before setting up

Before you connect the app, make sure that:

- Your Optimizely instance is reachable from Blackbird.
- You have a username/password that can access the CMS management API.
- You have a **Client ID** and **Client secret** registered in Optimizely (see below).
- The languages you want to update already exist in the Optimizely site configuration.

### Where do the Client ID and Client secret come from?

Unlike many services, Optimizely does **not** give you a Client ID and Client secret from a settings page. Instead, a developer with access to your Optimizely solution **defines these values themselves** when registering an API client in the application's startup code. Think of it as creating a dedicated "login" that Blackbird (and only Blackbird) will use to talk to your CMS.

So the Client ID and Client secret are simply two values your team chooses and registers in Optimizely. The Client ID is a readable name (for example `blackbird-integration`), and the Client secret is a long, random, secret string — treat it like a password.

To register them, your developer adds an OpenID Connect application during service configuration (usually in `Startup.cs`). The Content Management API must also be enabled and pointed at the OpenID Connect authentication scheme. A minimal example:

```csharp
public void ConfigureServices(IServiceCollection services)
{
    // ... existing CMS / MVC setup ...

    // 1. Enable the Content Management API and tell it to authenticate
    //    using OpenID Connect bearer tokens.
    services.AddContentManagementApi(
        OpenIDConnectOptionsDefaults.AuthenticationScheme);

    // 2. Configure OpenID Connect and register the Blackbird client.
    services.AddOpenIDConnect<ApplicationUser>(
        useDevelopmentCertificate: true,   // dev only; use real certificates in production
        signingCertificate: null,
        encryptionCertificate: null,
        createSchema: true,
        options =>
        {
            // Local/dev only — keep RequireHttps = true in production.
            options.RequireHttps = false;

            // These two values are your Client ID and Client secret.
            options.Applications.Add(new OpenIDConnectApplication
            {
                ClientId = "blackbird-integration", // `blackbird-integration` -> this is just an example; you can choose any name
                ClientSecret = "replace-with-a-long-random-secret",
                Scopes =
                {
                    "openid",
                    "offline_access",
                    "profile",
                    "email",
                    "roles",
                    ContentManagementApiOptionsDefaults.Scope, // allows content management
                }
            });

            // The app signs in with a CMS username/password, so allow that flow.
            options.AllowResourceOwnerPasswordFlow = true;
        });
}
```

After deploying this change, give Blackbird the four values:

- `Client ID` → the `ClientId` you set above (`blackbird-integration`)
- `Client secret` → the `ClientSecret` you set above
- `Username` / `Password` → a CMS account that is allowed to use the management API

> **Note:** the exact property names can vary slightly between versions of the `EPiServer.OpenIDConnect` and `EPiServer.ContentManagementApi` packages, so adjust to match your installed version. Keep the Client secret out of source control (use environment variables or a secret store).

## Connecting

1. Navigate to Apps and search for `Optimizely`.
2. Click _Add connection_.
3. Name the connection for future reference.
4. Fill in the following fields:
   `Base URL`: your Optimizely instance URL, for example `https://localhost:5000`
   `Client ID`
   `Client secret`
   `Username`
   `Password`
5. Save the connection.

The app validates the connection by requesting an access token from `/api/episerver/connect/token`.

## Actions

### Content

- **Search content** walks the content tree below the selected root content ID and filters the returned nodes in memory by GUID, content type, name, category, locale, publish window, and publication status. You can also cap the traversal with `Max depth` and `Max results`.
- **Download content** downloads the selected content item from `/api/episerver/v3.0/contentmanagement/{contentId}` and converts the selected localizable fields into a Blackbird interoperable HTML file. You can also choose reference fields so the linked content entries are embedded into the exported file.
- **Upload content** accepts a translated `.html`, `.xlf`, or `.xliff` file and patches the selected language variant in Optimizely. Reference entries are updated after the main content item. If a reference update fails, the action returns a partial-failure result instead of throwing for the entire upload.

### Languages

- **Search languages** lists the languages available in the Optimizely site configuration. This is useful for validating language setup and for debugging localization flows.

## Feedback

Do you want to use this app or do you have feedback on our implementation? Reach out to us using the [established channels](https://www.blackbird.io/) or create an issue.

<!-- end docs -->
