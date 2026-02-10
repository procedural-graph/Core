# What Is Procedural Graph?

A framework for building design-time procedural generation systems.

# Project Goals/Features

- Respond to user input in real time, allowing designers to see the results of their changes immediately.
- Decoupled from - but tightly integrated with the scene graph. Generation parameters are stored separately from the scene and don't need to be tediously unpicked from it before shipping. However, this is all meticulously abstracted away from the artist to create a native-feeling experience of editing the vanilla scene graph.
- An extensible architecture.
- A modular and customizable design system.
- Game engine agnostic, can be easily ported to any engine that supports .NET.

# Supported Engines

- Flax: currently supported
- Unity: planned (unconfirmed)
- Godot: planned (unconfirmed)
- Stride: possible
