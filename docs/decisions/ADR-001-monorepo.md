# ADR-001 - Monorepo and 3-phase architecture

Status: Accepted

## Context
The project combines offline computer vision, Unity runtime, and post-hoc analytics.

## Decision
Use a monorepo with three main layers:
- python/offline
- unity/AOI360Runtime
- python/analytics

## Consequences
Better reproducibility, shared schemas, and easier validation across phases.
