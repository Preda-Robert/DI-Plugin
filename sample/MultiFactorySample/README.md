# MultiFactorySample

**Data import + export** jobs in one workspace — tests factory generation when **multiple** types qualify.

## What to test

| Command | Expected |
|---------|----------|
| **DI: Suggest factories for workspace** | `ImportPipelineFactory` **and** `ExportPipelineFactory` |
| **DI: Generate builders/factories files** | Two `*Factory.cs` files; AppBuilder registers factories + interfaces, not pipeline concretes + factories |
| **DI: Suggest registrations for workspace** | Missing bindings for pipeline dependencies |

## Domain

- **ImportPipeline** — reads, validates, persists (3 interface deps).
- **ExportPipeline** — loads, formats, delivers (3 interface deps).
- **JobScheduler** — 2 deps only → no factory.

Open this folder as the workspace root.
