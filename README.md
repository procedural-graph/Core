# Procedural Graph

Procedural Graph is a highly optimized, design-time procedural generation framework built for modern game development. Developed in C# and multi-targeting .NET Standard 2.1 and .NET 9.0, it delivers a strictly game-engine agnostic architecture. Whether you are building in Flax Engine, Unity, or a custom engine, Procedural Graph seamlessly integrates powerful procedural logic directly into your native workflows.

## Features

### Zero Release Bloat

Procedural metadata often pollutes final scene graphs, creating heavy release builds and requiring tedious cleanup. Procedural Graph solves this by deeply decoupling procedural graph entities from actual scene members. Your designers edit vanilla objects natively in the editor, while the procedural generation data lives in a parallel graph that can be completely stripped out at build time.

### Engine Agnostic & Future-Proof

The core generation logic is entirely standalone. By relying on open generic parameters and sophisticated translation layers, porting the system to new engines requires only a new set of lightweight converters—protecting your studio's tooling investments and saving your team massive rewrite costs.

### Real-Time Iteration & Responsiveness

Designers need tools that keep up with their creativity. The framework utilizes advanced asynchronous programming, background cancellation tokens, and intelligent debouncing to gracefully batch rapid UI inputs. This ensures that even the heaviest procedural rebuilds never freeze the editor thread.

### Extreme Performance Foundations

Procedural generation requires manipulating massive data sets, such as terrain heightmaps or noise textures. Procedural Graph is built to minimize Garbage Collection (GC) pressure by leveraging zero-allocation abstractions, unmanaged memory blocks, and SIMD-friendly generic math. Heavy geometric and pixel-level operations are safely managed and aggressively optimized by the JIT compiler.

## Support

- **Unity 6**:\
  [Procedural Graph: Core](https://github.com/will11600/Procedural-Graph-Unity)\
  Procedural Graph: Terrain
- **Godot 4 (.NET 9.0)**\
  Procedural Graph: Core
- **Flax 1.11**:\
  [Procedural Graph: Core](https://github.com/will11600/Procedural-Graph-Core)\
  [Procedural Graph: Terrain](https://github.com/will11600/Procedural-Graph-Terrain-FlaxEngine)\
  Procedural Graph: Roads
- **Stride 4.3**\
  Procedural Graph: Core
