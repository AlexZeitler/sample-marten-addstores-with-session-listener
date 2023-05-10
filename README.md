# MartenDB multiple databases per Host and IConfigureMarten

This repo contains samples how to use [Marten](https://martendb.io)
with [multiple databases per Host](https://martendb.io/configuration/hostbuilder.html#working-with-multiple-marten-databases) (
this is not [multi-tenancy](#multi-tenancy-with-database-per-tenant) but multiple
event/document store configurations per host).

It also shows how to register `IDocumentSessionListener`|s and `IConfigureMarten` ([docs](https://martendb.io/configuration/hostbuilder.html#composite-configuration-with-configuremarten)) per store.

## Usage

```bash
cd test-database
docker compose up -d
cd ..
dotnet test
```

## License

```text
MIT License

Copyright (c) 2023 Alexander Zeitler

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```
