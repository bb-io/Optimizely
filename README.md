# Blackbird.io Optimizely

Blackbird is the new automation backbone for the language technology industry. Blackbird provides enterprise-scale automation and orchestration with a simple no-code/low-code platform. Blackbird enables ambitious organizations to identify, vet and automate as many processes as possible. Not just localization workflows, but any business and IT process. This repository represents an application that is deployable on Blackbird and usable inside the workflow editor.

## Introduction

<!-- begin docs -->

Optimizely Content Management System (CMS) is a digital experience platform for managing multilingual website content. This app connects Blackbird to the Optimizely CMS Management API so you can search content, export localizable fields into a Blackbird-compatible HTML file, translate that file in a TMS, and upload the translated result back to Optimizely.

## Before setting up

Before you connect the app, make sure that:

- Your Optimizely instance is reachable from Blackbird.
- You have a username/password that can access the CMS management API.
- You have a client ID and client secret for the Optimizely Connect token endpoint.
- The languages you want to update already exist in the Optimizely site configuration.

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
