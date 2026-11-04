# Build and publish

```bash
dotnet test Dloizides.Erasure.slnx
```

```powershell
.\publish.ps1 -NoBump
.\publish.ps1 -Bump patch
```

The API key loads from `SaaS/.env.local`. Propagation on nuget.org takes a few minutes.
