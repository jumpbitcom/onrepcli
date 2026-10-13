---
name: client
description: >
  Making client-side development easier and more efficient.
---

# Client Skill

## When to use
* it requires only client side workloads, no backend. Direct execution on the client hardware is necessary, no browser interpreter.
* it needs to run entirely in the client's environment, however it can still interact with external services if necessary.
* it should minimize dependencies on server-side components to reduce latency and improve responsiveness.
* it should support offline functionality to ensure usability even without an active internet connection.
* Single Sign-On (SSO) should be supported to streamline user authentication and improve security.

## Steps
1. Set up the client-side environment, ensuring all necessary dependencies and tools are installed.
2. Configure offline support and caching mechanisms to enhance performance and usability without an active internet connection.
3. Implement Single Sign-On (SSO) for streamlined user authentication and improved security.
4. Validate the client-side setup to ensure all configurations, offline support, and SSO integrations are functioning correctly.
5. Style the client-side interface to ensure a consistent and user-friendly experience. Use the styling skill.

## Details
- Use Avalonia for building cross-platform client-side applications.
- Use C Sharp (C#) for developing client-side applications with Avalonia.
- Work with local config file to manage client-side settings and preferences. Use a simple parameter = value format for easy parsing and modification as plan text config.
