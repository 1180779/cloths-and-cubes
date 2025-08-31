# Physics Engine (WIP)

A general-purpose, real-time physics engine focused on cloth simulation and simple rigid bodies (cubes, spheres), with
plans for high-quality rendering and a user-friendly scene editor.

This project is currently in active development. The initial goal is an educational yet performant engine that
demonstrates the fundamentals of physics simulation, collision detection, and rendering, then evolves into a more
complete toolkit with scene management and modern graphics.

---

## Highlights

- Rigid body dynamics for simple primitives (spheres, boxes)
- Cloth simulation (mass-spring constraints; extensible to FEM later)
- Contact generation and resolution
- Deterministic, fixed-step simulation loop
- Modular architecture designed for future expansion

Planned:

- Broad-phase acceleration structure (BVH-like tree) for fast collision culling
- More control/UI options (pause, step, restart, debug overlays, per-object tuning)
- Scene saving/loading and scene creation tools
- Modern rendering: PBR materials, shadows, post-processing, GPU instancing
- Profiling overlays (CPU/GPU timing, contact counts, BVH stats)
- Cross-platform builds

---

## Tech Stack

- Language: C# (modern features)
- Framework: .NET 9
- Windowing/Input/GL: OpenTK (planned/assumed)
- Rendering: OpenGL (current), with planned PBR pipeline
- Serialization: JSON (planned; subject to change)

---

## Getting Started

Prerequisites:

- .NET SDK 9.0 or newer
- A GPU/driver supporting modern OpenGL (if using the default renderer)

Build and run:

1. Clone the repository
2. From the repository root, run:
    - Debug: `dotnet run --project <PathToYourAppProject>`
    - Release: `dotnet run -c Release --project <PathToYourAppProject>`
3. If multiple sample apps exist, choose the one you want to start by specifying its project path.

Notes:

- On first run, shaders may compile at startup.
- If you encounter issues with the graphics context on Linux, ensure required OpenGL packages/drivers are installed.

---

## Controls (subject to change)

- R: Reset scene/simulation
- Esc: Exit
- Mouse/Keyboard: Camera orbit/pan/zoom (planned)
- Space/P: Pause/Resume (planned)
- N: Single-step simulation when paused (planned)
- F1: Toggle debug overlays (planned)

---

## Architecture Overview

- Physics Core
    - Rigid bodies: linear and angular motion integration, basic constraints
    - Cloth: grid/mesh-based particles with structural, shear, and bend constraints
    - Contact resolution: iterative solver with configurable iteration counts

- Collision Detection
    - Narrow-phase for spheres/boxes, cloth-particle vs. primitives
    - Broad-phase (planned): BVH-like tree for scalable collision culling across many objects
    - Extensible contact manifolds and customizable tolerances

- Simulation Loop
    - Fixed or clamped timestep for stability
    - Order: Integrate → Detect Contacts → Resolve Contacts → Render

- Rendering
    - forward rendering with basic materials
    - PBR (BRDF-based), HDR, tone mapping, shadow mapping, sky/IBL, per-pixel normals and roughness (planned)

- UI and Tools
    - In-app controls for simulation, object spawning, and parameters
    - Debug draw for colliders, normals, BVH nodes, constraints (planned)
    - Scene graph with entity/components and transforms (planned)

- Scene IO
    - Save/Load scenes (JSON or similar), including transforms, materials, and simulation parameters
    - Asset references for meshes and textures (planned)

---

## Performance Considerations

- Broad-phase acceleration structure (BVH) to reduce narrow-phase cost in dense scenes
- Sleeping/deactivation for stable rigid bodies (planned)
- Constraint batching and warm starting (planned)
- GPU-friendly layouts where beneficial (planned)
- Reduced allocations per frame to minimize GC pressure

---

## Roadmap

- [ ] Rigid body primitives: spheres, boxes (polish + tests)
- [ ] Cloth: stable constraints with tunable stiffness/damping
- [ ] Narrow-phase contact generation improvements
- [ ] BVH-based broad-phase with dynamic updates
- [ ] Debug visualization (contact points, normals, AABBs/BVH)
- [ ] Pause/step/reset UI controls and overlays
- [ ] Scene saving/loading and template scenes
- [ ] Rendering: PBR materials, shadow mapping, IBL

---

## License

TBD

---

## Acknowledgements

- Community knowledge and research in real-time physics
- Open-source ecosystem around .NET and graphics tools