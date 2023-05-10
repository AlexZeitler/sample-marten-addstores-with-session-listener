# MartenDB multiple databases per Host and IConfigureMarten

This repo contains samples how to use [Marten](https://martendb.io)
with [multiple databases per Host](https://martendb.io/configuration/hostbuilder.html#working-with-multiple-marten-databases) (
this is not [multi-tenancy](#multi-tenancy-with-database-per-tenant) but multiple
event/document store configurations per host).

It also shows how to register `IDocumentSessionListener`|s and `IConfigureMarten` [docs](https://martendb.io/configuration/hostbuilder.html#composite-configuration-with-configuremarten) per store.