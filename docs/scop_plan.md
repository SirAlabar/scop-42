# SCOP — Project Planning Document

**Subject version:** 4.1  
**Language:** C#  
**Rendering API:** OpenGL 4.1 Core Profile  
**Platform:** Linux (42 cluster)  

---

## Table of Contents

1. [Project Summary](#1-project-summary)
2. [Technical Stack](#2-technical-stack)
3. [Constraints Analysis](#3-constraints-analysis)
4. [Project Structure](#4-project-structure)
5. [Development Phases — Mandatory](#5-development-phases--mandatory)
6. [Development Phases — Bonus](#6-development-phases--bonus)
7. [Architecture — Component Overview](#7-architecture--component-overview)
8. [UML — Class Diagram](#8-uml--class-diagram)
9. [UML — Sequence Diagram: Render Loop](#9-uml--sequence-diagram-render-loop)
10. [UML — State Diagram: Texture Toggle](#10-uml--state-diagram-texture-toggle)
11. [UML — State Diagram: Mouse Drag Rotation](#11-uml--state-diagram-mouse-drag-rotation)
12. [Module Breakdown](#12-module-breakdown)
13. [Controls Specification](#13-controls-specification)
14. [Mandatory Checklist](#14-mandatory-checklist)
15. [Bonus Checklist](#15-bonus-checklist)
16. [Build System](#16-build-system)

> **New bonus phases (portfolio extras):** 5–Wireframe, 6–Backface Culling, 7–Screenshot, 8–Auto-Scale, 9–Shading Toggle, 10–Phong Lighting, 11–Dual Model

---

## 1. Project Summary

SCOP is a mini 3D viewer built entirely from scratch.  
It must:

- Parse a `.obj` file manually (no external 3D loading library)
- Display the object in **perspective projection**
- **Auto-rotate** the object around its geometric center (not a corner)
- Color each face with a distinct **shade of gray**
- Allow **movement** on all 3 axes in both directions
- Toggle a **texture** on/off with a smooth animated blend (no hard cut)
- Show the **42 logo** spinning during defense

---

## 2. Technical Stack

| Layer | Choice | Reason |
|---|---|---|
| Language | C# (.NET 8) | Cross-platform, Unity career relevance |
| Rendering API | OpenGL 4.1 Core | Required by subject, widely supported |
| Window & Events | OpenTK 4 (GLFW-backed) | Allowed by subject — window/event only |
| Math library | Hand-rolled | Prohibited by subject to use external |
| OBJ loader | Hand-rolled | Prohibited by subject to use external |
| Texture loader | Hand-rolled BMP + PPM | Prohibited by subject to use external |
| Build | Makefile wrapping dotnet | Subject requires classic Makefile |
| GLSL shaders | Loaded from files manually | Prohibited to use external shader loaders |

---

## 3. Constraints Analysis

### What is ALLOWED

- OpenTK → only for **window creation** and **event polling**
- Any language (C, C++, Rust, or other — C# chosen)
- OpenGL, Vulkan, or Metal (OpenGL chosen)

### What is PROHIBITED

- External library for loading `.obj` files
- External library for matrix/vector math
- External library for loading shaders
- External library for loading texture images

### Gray area handled

- OpenTK includes OpenGL bindings → these are **just function pointers**, not a rendering abstraction. They are equivalent to manually loading `libGL.so`. Acceptable.
- `dotnet build` instead of raw `gcc` → wrapped in a standard Makefile. Acceptable.


---

## 3b. Design Patterns

Three patterns applied deliberately — each solves a real problem in this project.

---

### Pattern 1 — Strategy (Triangulation + UV Mapping)

**Problem:** Two triangulation algorithms (fan = mandatory, ear-clipping = bonus) and two UV mappers (box = mandatory, face-normal = bonus). Without a pattern, `ObjParser` would be full of `if (bonusEnabled)` branches.

**Solution:** Define an interface for each behaviour. `ObjParser` receives whichever implementation is injected — it never knows which one it's using.

```
ITriangulator
  ├── FanTriangulator       ← mandatory, injected by default
  └── EarClipper            ← bonus 1, injected when enabled

IUvMapper
  ├── BoxUvMapper           ← mandatory, injected by default
  └── FaceNormalUvMapper    ← bonus 2, injected when enabled
```

Switching from mandatory to bonus is **one line in App.cs**:
```csharp
// Mandatory
ObjParser.Parse(path, new FanTriangulator(), new BoxUvMapper());

// With bonuses
ObjParser.Parse(path, new EarClipper(), new FaceNormalUvMapper());
```

---

### Pattern 2 — Factory Method (Texture loading)

**Problem:** Two-step construction is error-prone:
```csharp
Texture t = new Texture();
t.LoadBMP("path");          // easy to forget, no return value check
```

**Solution:** Static factory method handles construction, format detection, and fallback in one call:
```csharp
ITexture t = Texture.FromFile("path");  // detects .bmp or .ppm by extension
                                         // returns NullTexture if load fails
```

The factory also returns `ITexture` — not `Texture` — so the caller never needs to know whether it got a real texture or a null one.

---

### Pattern 3 — Null Object (NullTexture)

**Problem:** If texture loading fails, `_texture` would be null. Every render frame needs a null check:
```csharp
if (_texture != null && _texture.IsLoaded)  // repeated 60x per second
{
    _texture.Bind(0);
}
```

**Solution:** `NullTexture` implements `ITexture` with silent no-ops. The factory returns it on failure. `App.cs` never checks for null:
```csharp
_texture.Bind(0);    // always safe — real texture or silent no-op
_mesh.Draw();
_texture.Unbind();   // always safe
```

```
ITexture (interface)
  ├── Texture      → real GPU texture, Bind() activates it
  └── NullTexture  → no-op, Bind() does nothing, IsLoaded = false
```

---

## 4. Project Structure

```
scop/
├── Makefile                        ← all / clean / fclean / re
├── scop.csproj                     ← .NET 8 project, OpenTK NuGet ref
├── README.md
│
├── src/
│   ├── Program.cs                  ← entry point, arg parsing
│   ├── App.cs                      ← GameWindow subclass, main loop
│   │
│   ├── Math/
│   │   ├── Vec2.cs                 ← 2D float vector
│   │   ├── Vec3.cs                 ← 3D float vector
│   │   ├── Vec4.cs                 ← 4D float vector
│   │   └── Mat4.cs                 ← 4×4 column-major matrix
│   │
│   ├── Parsing/
│   │   ├── Vertex.cs               ← interleaved GPU vertex struct
│   │   └── ObjParser.cs            ← .obj parser + triangulator
│   │
│   └── Rendering/
│       ├── Shader.cs               ← GLSL load / compile / link
│       ├── Texture.cs              ← BMP + PPM parsers + GPU upload
│       └── Mesh.cs                 ← VAO / VBO / EBO + draw call
│
├── shaders/
│   ├── vertex.glsl                 ← MVP transform, pass color + UV
│   └── fragment.glsl               ← mix(color, texture, blendFactor)
│
├── objects/
│   └── 42logo.obj                  ← provided in subject resources
│
└── textures/
    └── ponies.bmp                  ← cheerful texture (subject requirement)
```

---

## 5. Development Phases — Mandatory

### Phase 1 — Math Foundation
**Goal:** implement all matrix and vector types from scratch.

| Task | Detail |
|---|---|
| `Vec2` | X, Y — used for UV coordinates |
| `Vec3` | X, Y, Z — positions, colors, normals |
| `Vec4` | X, Y, Z, W — homogeneous coords |
| `Mat4` | 4×4 column-major — all transforms |
| `Mat4::Identity` | identity matrix |
| `Mat4::Translate` | translation matrix |
| `Mat4::RotateX/Y/Z` | rotation around each axis |
| `Mat4::Scale` | scale matrix |
| `Mat4::Perspective` | perspective projection (fovY, aspect, near, far) |
| `Mat4::LookAt` | view matrix from eye/center/up vectors |
| `Mat4 * Mat4` | matrix multiplication |
| `Mat4 * Vec4` | matrix-vector multiplication |
| `Mat4::ToArray()` | export to `float[16]` for `GL.UniformMatrix4` |

**Acceptance criteria:** all transforms produce correct OpenGL-ready column-major matrices.

---

### Phase 2 — OBJ Parser
**Goal:** read a `.obj` file and produce a GPU-ready vertex/index buffer.

| Task | Detail |
|---|---|
| Parse `v` lines | collect 3D vertex positions |
| Parse `vt` lines | collect UV texture coordinates |
| Parse `vn` lines | collect vertex normals |
| Parse `f` lines | support `v`, `v/vt`, `v/vt/vn`, `v//vn` formats |
| Handle relative indices | negative indices (e.g. `f -1/-1/-1`) |
| Fan triangulation | convert n-gons to triangles via fan method |
| Centroid computation | average of all vertex positions |
| Center geometry | subtract centroid from every vertex |
| Bounding box | compute min/max after centering |
| Per-face color | assign one shade of gray per original face index |
| Fallback UV | box-mapping when file has no `vt` data |

**Acceptance criteria:** the 42 logo loads, centers on screen, all faces have distinct gray shades.

---

### Phase 3 — Texture Loaders
**Goal:** parse image files without any external library.

#### BMP Parser
| Task | Detail |
|---|---|
| Read file header | validate `BM` signature, locate pixel data offset |
| Read DIB header | extract width, height, bits-per-pixel, compression |
| Support 24-bit | BGR → RGB channel swap |
| Support 32-bit | BGRA → RGB channel swap |
| Handle bottom-up | flip rows when height is positive |
| Handle top-down | no flip when height is negative |
| Reject compressed | error on compression ≠ 0 |

#### PPM Parser
| Task | Detail |
|---|---|
| P6 binary | read raw bytes, normalize to 255 |
| P3 ASCII | read space-delimited integers |
| Skip comments | ignore lines starting with `#` |
| 8-bit maxval | direct copy or normalize |
| 16-bit maxval | big-endian two-byte read, normalize |

**Acceptance criteria:** cheerful texture loads and displays correctly on the object.

---

### Phase 4 — GPU Pipeline
**Goal:** upload geometry and shader to the GPU.

| Task | Detail |
|---|---|
| Shader loading | read `.glsl` files manually with `File.ReadAllText` |
| Shader compilation | `GL.CompileShader`, capture and print errors |
| Program linking | `GL.LinkProgram`, capture and print errors |
| Uniform setters | `SetInt`, `SetFloat`, `SetVec3`, `SetMat4` |
| VAO creation | `GL.GenVertexArray` |
| VBO upload | interleaved vertex data (position + color + UV) |
| EBO upload | triangle index list |
| Attribute layout | location 0 = position, 1 = color, 2 = UV |
| Texture upload | `GL.TexImage2D`, generate mipmaps |
| Texture parameters | repeat wrap, linear mipmap filtering |

---

### Phase 5 — Application Loop
**Goal:** tie everything together in a live render loop.

| Task | File | Detail |
|---|---|---|
| Window creation | `App.cs` | OpenTK `GameWindow`, OpenGL 4.1 Core, 1280×720 |
| `OnLoad` | `App.cs` | load shader, parse OBJ, upload mesh, load texture |
| `OnUpdateFrame` | `App.cs` | calls `InputHandler.Update()`, `Renderer` animation |
| `OnRenderFrame` | `App.cs` | calls `Renderer.Draw()`, swap buffers |
| `OnResize` | `App.cs` | update `GL.Viewport` |
| `OnUnload` | `App.cs` | dispose shader, mesh, texture |
| Keyboard input | `InputHandler.cs` | arrow keys, Q/E, T, ESC — mutates app state |
| Mouse input | `InputHandler.cs` | scroll zoom (bonus 3), drag rotation (bonus 4) |
| MVP matrices | `Renderer.cs` | `BuildModel`, `BuildView`, `BuildProjection` |
| Uniform upload | `Renderer.cs` | `SetMat4`, `SetFloat`, `SetInt` per frame |
| Draw call | `Renderer.cs` | `Texture.Bind` → `Mesh.Draw` → `Texture.Unbind` |

---

### Phase 6 — GLSL Shaders
**Goal:** write the vertex and fragment shaders.

#### Vertex shader
- Inputs: `a_position` (vec3), `a_color` (vec3), `a_texCoord` (vec2)
- Uniforms: `u_model`, `u_view`, `u_projection` (mat4)
- Outputs: `v_color`, `v_texCoord` to fragment stage
- Transform: `gl_Position = u_projection * u_view * u_model * vec4(a_position, 1.0)`

#### Fragment shader
- Inputs: `v_color` (vec3), `v_texCoord` (vec2)
- Uniforms: `u_texture` (sampler2D), `u_blendFactor` (float)
- Output: `frag_color = mix(vec4(v_color, 1.0), texture(u_texture, v_texCoord), u_blendFactor)`
- When `u_blendFactor = 0.0` → pure face color
- When `u_blendFactor = 1.0` → pure texture
- Between 0 and 1 → smooth animated transition

---

## 6. Development Phases — Bonus

> These phases are only evaluated if every mandatory requirement is perfect.  
> Planned bonus features: ear-clipping triangulation, smart texture mapping, zoom, mouse drag rotation,
> wireframe toggle, backface culling, screenshot, auto-scaling, shading toggle, Phong lighting, dual model.

---

### Bonus Phase 1 — Ear-Clipping Triangulation

**Goal:** correctly triangulate concave and non-planar polygons that fan-triangulation breaks.

**Why fan triangulation fails:**  
Fan triangulation connects every polygon vertex to vertex 0. On convex faces this works.  
On concave faces it produces triangles that cross outside the polygon boundary — visible artifacts on the original teapot.

**Ear-clipping algorithm:**

```
An "ear" is a triangle formed by three consecutive vertices (A, B, C) where:
  1. The triangle ABC is convex (B is a "left turn" from A→C)
  2. No other vertex of the polygon lies inside triangle ABC

Algorithm:
  WHILE polygon has more than 3 vertices:
    FOR each consecutive triple (A, B, C):
      IF B is an ear:
        emit triangle (A, B, C)
        remove B from the polygon
        BREAK
  emit the remaining triangle
```

| Task | Detail |
|---|---|
| `IsConvexVertex` | cross product of edges A→B and B→C — positive Z = left turn = convex |
| `PointInTriangle` | barycentric coordinate test — check no other vertex inside ear |
| `EarClip(polygon)` | main loop — find ears, emit triangles, shrink polygon |
| Integrate into `ObjParser` | replace fan call with `EarClip` for all faces with 4+ vertices |
| Handle non-coplanar faces | project face onto its dominant axis (XY, XZ, or YZ) before clipping, then unproject positions |
| Edge case: collinear vertices | skip degenerate ears (zero-area triangles) |
| Edge case: self-intersecting | detect and skip, emit error to stderr |

**Dominant axis projection for non-coplanar faces:**

```
1. Compute face normal = cross(v1-v0, v2-v0)
2. Find dominant axis = largest absolute component of normal
   |normal.X| > |normal.Y| && |normal.X| > |normal.Z|  → project onto YZ
   |normal.Y| > |normal.Z|                               → project onto XZ
   else                                                  → project onto XY
3. Run ear-clipping in 2D on projected coordinates
4. Use original 3D positions for the emitted triangles
```

**Acceptance criteria:** the original teapot (quirky edge effects version) renders without visible triangulation artifacts.

---

### Bonus Phase 2 — Smart Texture Mapping

**Goal:** apply UV coordinates without ugly stretching, using the best available data.

**Two-tier strategy:**

```
IF the .obj file contains vt coordinates for this face
  → use them directly (author-provided UVs are always best)
ELSE
  → generate UVs via per-face box mapping
```

#### Tier 1 — Use .obj `vt` data

| Task | Detail |
|---|---|
| Store `vt` per vertex | already parsed — ensure `ObjParser` always fills `TexCoord` from `vt` when available |
| Handle mixed faces | some faces in a file have `vt`, others do not — fall through to tier 2 for the ones without |
| Handle `v//vn` format | no `vt` provided — use tier 2 |

#### Tier 2 — Per-face box mapping (no stretching)

Box mapping picks the best projection plane per face based on the face normal, so every face gets a full-range [0,1] × [0,1] UV square with minimal distortion.

```
For each face:
  1. Compute face normal N = normalize(cross(edge1, edge2))
  2. Find dominant axis:
       |N.X| >= |N.Y| && |N.X| >= |N.Z|  → project onto YZ plane
         U = (pos.Y - minY) / rangeY
         V = (pos.Z - minZ) / rangeZ
       |N.Y| >= |N.Z|                     → project onto XZ plane
         U = (pos.X - minX) / rangeX
         V = (pos.Z - minZ) / rangeZ
       else                               → project onto XY plane
         U = (pos.X - minX) / rangeX
         V = (pos.Y - minY) / rangeY
  3. Assign resulting (U, V) to each vertex of the face
```

| Task | Detail |
|---|---|
| `ComputeFaceNormal` | cross product of first two edges of the face |
| `DominantAxis` | enum: X, Y, Z — based on largest abs component of normal |
| `BoxMapFace` | project all vertices of a face onto dominant plane, normalize to [0,1] |
| Integrate into `ObjParser` | replace `BoxUV` (global) with `BoxMapFace` (per face) |
| Per-face bounding box | min/max computed per face, not globally, for correct normalization |

**Acceptance criteria:** the 42 logo texture shows no visible stretching on side faces. Each face fills the texture correctly.

---

### Bonus Phase 3 — Zoom with Scroll Wheel

**Goal:** zoom in/out by moving the camera along the Z axis using the mouse scroll wheel.

**Implementation approach:** modify the camera Z position (`cameraZ`) rather than scaling the object. This preserves perspective depth cues correctly — scaling the model would just make it bigger without the perspective "zoom" feel.

| Task | Detail |
|---|---|
| `OnMouseWheel` override | OpenTK `GameWindow` provides `MouseWheelChanged` event |
| `cameraZ` field | replaces the hardcoded `5.0f` in `LookAt` |
| Scroll delta | `cameraZ -= scrollDelta * ZoomSpeed` |
| `ZoomSpeed` constant | `0.3f` per scroll tick — tune to feel responsive |
| Clamp range | `cameraZ = Clamp(cameraZ, 0.5f, 50.0f)` — prevent going through the object or too far |
| Update `LookAt` call | use `new Vec3(0, 0, cameraZ)` instead of hardcoded value |

**State:**

```
cameraZ : float = 5.0f    // initial distance
ZoomMin : float = 0.5f    // closest allowed
ZoomMax : float = 50.0f   // farthest allowed
ZoomSpeed : float = 0.3f
```

**Acceptance criteria:** scroll up zooms in smoothly, scroll down zooms out, stops at limits without crashing.

---

### Bonus Phase 4 — Mouse Drag to Rotate Manually

**Goal:** click and drag the mouse to freely rotate the object on X and Y axes. Auto-rotation pauses while dragging.

**Implementation approach:** accumulate rotation angles from mouse delta, pause the auto Y-rotation while the mouse button is held, resume when released.

| Task | Detail |
|---|---|
| `OnMouseDown` override | record drag start position, set `isDragging = true`, pause auto-rotation |
| `OnMouseMove` override | compute delta from last position, accumulate into `manualRotX` and `manualRotY` |
| `OnMouseUp` override | set `isDragging = false`, resume auto-rotation |
| `mouseLastPos` field | `Vec2` storing previous frame mouse position |
| `manualRotX` field | float, rotation around X axis from drag |
| `manualRotY` field | float, rotation around Y axis from drag |
| `isDragging` field | bool — gates auto-rotation |
| `DragSensitivity` | `0.005f` radians per pixel — tune to feel natural |
| Model matrix update | `model = Translate * RotateY(manualRotY + autoRot) * RotateX(manualRotX)` |
| Auto-rotation gate | `if (!isDragging) autoRot += RotSpeed * dt` |

**State diagram:**

```
IDLE (auto-rotating)
  │
  │  Left mouse button down
  ▼
DRAGGING
  │  mouse move → delta → update manualRotX / manualRotY
  │  auto-rotation paused
  │
  │  Left mouse button up
  ▼
IDLE (auto-rotation resumes from current angle)
```

**Model matrix with both rotations:**

```
autoRot    += RotSpeed * dt    (only when !isDragging)
manualRotY += deltaX * DragSensitivity
manualRotX += deltaY * DragSensitivity

model = Mat4.Translate(position)
      * Mat4.RotateY(autoRot + manualRotY)
      * Mat4.RotateX(manualRotX)
```

**Acceptance criteria:** left-click drag rotates the object freely on both axes. Releasing the mouse resumes auto-rotation from the current orientation. No jitter on button release.

---

---

### Bonus Phase 5 — Wireframe Toggle

**Goal:** switch between solid fill and wireframe rendering with `W`.

**Implementation:**

```csharp
// In InputHandler — toggle wireframe state
if (keyboard.IsKeyPressed(Keys.W))
{
    state.Wireframe = !state.Wireframe;
}

// In Renderer — apply before draw call
if (state.Wireframe)
{
    GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);
}
else
{
    GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
}
```

| Task | Detail |
|---|---|
| `Wireframe` bool in `AppState` | toggles between Line and Fill mode |
| `W` key binding in `InputHandler` | flip the bool |
| `GL.PolygonMode` call in `Renderer` | applied before every draw call |
| Restore Fill after draw | always reset to Fill so other features work correctly |

**Portfolio value:** shows understanding of polygon topology and rasterization modes.  
**Acceptance criteria:** `W` toggles wireframe on/off cleanly at runtime.

---

### Bonus Phase 6 — Backface Culling Toggle

**Goal:** toggle culling of back-facing triangles with `C`. Reduces GPU work by ~50% on closed meshes.

**Implementation:**

```csharp
// AppState
public bool Culling = false;

// Renderer
if (state.Culling)
{
    GL.Enable(EnableCap.CullFace);
    GL.CullFace(CullFaceMode.Back);
    GL.FrontFace(FrontFaceDirection.Ccw);
}
else
{
    GL.Disable(EnableCap.CullFace);
}
```

| Task | Detail |
|---|---|
| `Culling` bool in `AppState` | gates `GL.Enable(CullFace)` |
| `C` key binding in `InputHandler` | flip the bool |
| `GL.FrontFace(Ccw)` | define winding order — must match how OBJ parser emits triangles |
| Visual effect | interior of 42 logo becomes invisible when viewed from behind |

**Portfolio value:** fundamental real-time rendering optimization. Every game engine uses this.  
**Acceptance criteria:** `C` toggles culling. Back faces disappear. Performance improves on heavy models.

---

### Bonus Phase 7 — Screenshot

**Goal:** press `P` to save the current frame as a PPM file.

**How framebuffer read works:**

```
GL.ReadPixels() → reads current framebuffer into a byte[]
Flip rows vertically → OpenGL origin is bottom-left, PPM is top-left
Write PPM P6 header + raw RGB bytes → file on disk
```

| Task | Detail |
|---|---|
| `P` key binding | trigger screenshot in `InputHandler` |
| `GL.ReadPixels()` | read `GL_RGB` bytes from current framebuffer |
| Flip rows | reverse row order — OpenGL bottom-up vs PPM top-down |
| Auto-filename | `screenshot_001.ppm`, `screenshot_002.ppm` — increment counter |
| PPM P6 write | `P6
W H
255
` + raw bytes — reuse existing PPM knowledge |
| Console feedback | print filename to stdout on save |

```csharp
// Core read
byte[] pixels = new byte[width * height * 3];
GL.ReadPixels(0, 0, width, height, PixelFormat.Rgb, PixelType.UnsignedByte, pixels);

// Flip rows (OpenGL is bottom-up)
// Write PPM P6 to disk
```

**Portfolio value:** shows you can read the framebuffer — a concept used in render-to-texture, post-processing, and GPU readback.  
**Acceptance criteria:** `P` saves a readable PPM file. Image matches what's on screen (not upside down).

---

### Bonus Phase 8 — Auto-Scaling on Load

**Goal:** normalize every loaded object to fit consistently in view, regardless of original size.

**Problem:** some OBJ files are 0.001 units (jewelry), some are 10,000 units (city blocks). Without scaling, tiny objects are invisible, huge ones fill the screen wrong.

**Implementation:**

```
After centering on centroid:
1. Compute max extent = max(maxBound.X - minBound.X,
                            maxBound.Y - minBound.Y,
                            maxBound.Z - minBound.Z)
2. Scale factor = TARGET_SIZE / maxExtent   (TARGET_SIZE = 2.0f)
3. Multiply every vertex position by scale factor
```

| Task | Detail |
|---|---|
| Compute `maxExtent` after bounding box pass | already have min/max bounds |
| `ScaleFactor = 2.0f / maxExtent` | object fits in a 2-unit cube |
| Apply in `BakeVertices` | `pos = (positions[vi-1] - centroid) * scaleFactor` |
| `TARGET_SIZE` constant in `ObjParser` | easy to tune |

**Portfolio value:** shows professional thinking — a viewer that works consistently on any input.  
**Acceptance criteria:** the 42 logo and a giant city model both fit in view without manual zoom.

---

### Bonus Phase 9 — Flat vs Smooth Shading Toggle

**Goal:** toggle between flat shading (current — per face) and smooth shading (per vertex normal interpolation) with `S`.

**Flat shading (current):** every pixel in a triangle gets the same color → sharp edges, faceted look.

**Smooth shading:** normals are interpolated across the triangle → curved surfaces look round.

**What changes:**

Vertex struct gains a normal:
```
Offset  0 → position  (Vec3 = 12 bytes)
Offset 12 → color     (Vec3 = 12 bytes)
Offset 24 → texCoord  (Vec2 =  8 bytes)
Offset 32 → normal    (Vec3 = 12 bytes)   ← new, stride = 44
```

Vertex shader passes normal to fragment stage:
```glsl
out vec3 v_normal;
v_normal = mat3(transpose(inverse(u_model))) * a_normal;
```

Fragment shader uses normal for smooth interpolation:
```glsl
// Smooth: use interpolated normal (automatic in GLSL)
// Flat: use flat keyword to disable interpolation
flat in vec3 v_normal;   // flat shading
     in vec3 v_normal;   // smooth shading
```

| Task | Detail |
|---|---|
| Add `Normal : Vec3` to `Vertex` struct | stride changes from 32 to 44 |
| Update `Mesh.Upload()` attribute layout | add location 3 for normal |
| Pass normals from `ObjParser` | use `vn` data when available, compute face normal as fallback |
| `ShadingMode` bool in `AppState` | gates flat vs smooth |
| `S` key in `InputHandler` | flip the bool |
| Shader toggle via uniform | `u_flatShading` int — 0 smooth, 1 flat |

**Portfolio value:** directly relevant to Unity — this is exactly what the Smooth/Flat shading option does in the mesh renderer.  
**Acceptance criteria:** `S` toggles between faceted and smooth appearance. Teapot looks like a disco ball (flat) vs a smooth pot (smooth).

---

### Bonus Phase 10 — Phong Lighting with Moveable Light

**Goal:** implement Phong lighting model in GLSL. Move the light source with right mouse button drag. Toggle with `L`.

**Phong lighting — three components:**

```
ambient  = Ka × lightColor                          // base light, never pure black
diffuse  = Kd × lightColor × max(dot(N, L), 0)     // angle between normal and light
specular = Ks × lightColor × pow(dot(R, V), shine) // reflection highlight
```

Where:
- `N` = surface normal (from vertex)
- `L` = direction from fragment to light
- `R` = reflection of L around N
- `V` = direction from fragment to camera

**Fragment shader additions:**
```glsl
uniform vec3  u_lightPos;
uniform vec3  u_lightColor;
uniform float u_ambient;
uniform int   u_lightingOn;

vec3 norm     = normalize(v_normal);
vec3 lightDir = normalize(u_lightPos - v_fragPos);
vec3 viewDir  = normalize(u_viewPos - v_fragPos);
vec3 reflDir  = reflect(-lightDir, norm);

float diff    = max(dot(norm, lightDir), 0.0);
float spec    = pow(max(dot(viewDir, reflDir), 0.0), 32.0);

vec3 ambient  = 0.15 * u_lightColor;
vec3 diffuse  = diff * u_lightColor;
vec3 specular = spec * u_lightColor * 0.5;

vec3 lighting = (ambient + diffuse + specular) * objectColor.rgb;
```

**Light source control:**
```
Right mouse button drag → move light in XY plane
Scroll while RMB held  → move light on Z axis
L key                  → toggle lighting on/off
```

| Task | Detail |
|---|---|
| `LightPos : Vec3` in `AppState` | initial position `(2, 2, 3)` |
| `LightOn : bool` in `AppState` | gates lighting calculation |
| `RMBDragging` bool | separate from LMB drag |
| `OnMouseDown` RMB branch | start light drag |
| `OnMouseMove` RMB branch | `lightPos.X += dx * 0.01f`, `lightPos.Y -= dy * 0.01f` |
| `u_lightPos`, `u_lightColor`, `u_lightingOn` uniforms | set in `Renderer` every frame |
| Normal attribute | requires Bonus Phase 9 to be done first |
| `u_viewPos` uniform | camera eye position for specular calculation |

**Portfolio value:** highest of all features. Phong lighting is the foundation of all real-time rendering. Every Unity material uses a variant of this.  
**Acceptance criteria:** light visibly affects shading. Moving the light changes highlight position in real time. `L` toggles between lit and unlit.

---

### Bonus Phase 11 — Dual Model Rendering

**Goal:** load two `.obj` files simultaneously, render them side by side in split viewport. Tab switches which model is active for input.

**Viewport split:**
```
Left half  → model A (viewport: x=0,     y=0, w=W/2, h=H)
Right half → model B (viewport: x=W/2,   y=0, w=W/2, h=H)
```

**State changes in `AppState`:**
```csharp
public Mesh      MeshA    = null!;
public Mesh      MeshB    = null!;
public ITexture  TextureA = null!;
public ITexture  TextureB = null!;
public int       ActiveModel = 0;     // 0 = left, 1 = right

// Each model has independent transform
public Vec3      PositionA;    public Vec3  PositionB;
public float     RotAngleA;    public float RotAngleB;
public float     ManualRotXA;  public float ManualRotXB;
public float     ManualRotYA;  public float ManualRotYB;
```

**Renderer — draw twice:**
```csharp
// Left half
GL.Viewport(0, 0, width / 2, height);
DrawModel(state.MeshA, state.TextureA, BuildModel(state, 0), view, proj);

// Right half
GL.Viewport(width / 2, 0, width / 2, height);
DrawModel(state.MeshB, state.TextureB, BuildModel(state, 1), view, proj);

// Restore
GL.Viewport(0, 0, width, height);
```

**Program.cs — accept 4 arguments:**
```bash
./scop model1.obj texture1.bmp model2.obj texture2.bmp
# Or 2 models, shared texture:
./scop model1.obj model2.obj texture.bmp
```

| Task | Detail |
|---|---|
| `AppState` dual mesh/texture fields | separate transform per model |
| `Tab` key in `InputHandler` | flip `ActiveModel` 0↔1 |
| `Renderer.Draw()` split viewport | `GL.Viewport` called twice |
| `Program.cs` argument parsing | handle 2, 3, or 4 arguments |
| Visual indicator | window title shows active model: `SCOP-42 [A]` / `SCOP-42 [B]` |
| Independent auto-rotation | both models spin simultaneously |

**Portfolio value:** shows understanding that the render loop is just "draw things multiple times with different transforms" — the foundation of scene graphs in Unity.  
**Acceptance criteria:** two models visible simultaneously. Tab switches control. Both spin independently.

---


## 7. Architecture — Component Overview

```
┌─────────────────────────────────────────────────────────────────┐
│                          Program.cs                             │
│                    (entry point, args check)                    │
└───────────────────────────┬─────────────────────────────────────┘
                            │ creates
                            ▼
┌─────────────────────────────────────────────────────────────────┐
│                            App                                  │
│                (extends OpenTK GameWindow)                      │
│                                                                 │
│   ┌─────────────┐  ┌─────────────┐  ┌─────────────────────┐   │
│   │   Shader    │  │    Mesh     │  │       Texture        │   │
│   │  (Rendering)│  │  (Rendering)│  │     (Rendering)      │   │
│   └──────┬──────┘  └──────┬──────┘  └──────────────────────┘   │
│          │                │                                     │
│          │         ┌──────▼──────┐                              │
│          │         │   ObjMesh   │◄── ObjParser.Parse()         │
│          │         │  (Parsing)  │        (Parsing)             │
│          │         └──────┬──────┘                              │
│          │                │                                     │
│          │         ┌──────▼──────┐                              │
│          │         │   Vertex    │                              │
│          │         │  (Parsing)  │                              │
│          │         └─────────────┘                              │
│          │                                                      │
│   uses   │         ┌─────────────────────────────────────┐     │
│          └────────►│             Mat4                    │     │
│                    │  Translate · RotateY · Perspective  │     │
│                    │           LookAt                    │     │
│                    └──────────────┬──────────────────────┘     │
│                                   │ uses                        │
│                    ┌──────────────▼──────────────────────┐     │
│                    │          Vec3 / Vec2                 │     │
│                    └─────────────────────────────────────┘     │
└─────────────────────────────────────────────────────────────────┘

External (window + events only):
┌─────────────────────────────────────────────────────────────────┐
│                        OpenTK 4                                 │
│              GameWindow · GL bindings · GLFW                   │
└─────────────────────────────────────────────────────────────────┘
```

---

## 8. UML — Class Diagram

```
┌─────────────────────────────────────────────┐
│                    App                       │
│  extends GameWindow (OpenTK)                 │
├─────────────────────────────────────────────┤
│ - shader      : Shader                       │
│ - mesh        : Mesh                         │
│ - texture     : Texture                      │
│ - rotAngle    : float                        │
│ - position    : Vec3                         │
│ - blendFactor : float                        │
│ - blendTarget : float                        │
│ - textureOn   : bool                         │
├─────────────────────────────────────────────┤
│ + OnLoad()                                   │
│ + OnUpdateFrame(FrameEventArgs)              │
│ + OnRenderFrame(FrameEventArgs)              │
│ + OnResize(ResizeEventArgs)                  │
│ + OnUnload()                                 │
└───────┬───────────┬──────────────┬───────────┘
        │           │              │
        ▼           ▼              ▼
┌──────────┐ ┌──────────┐ ┌──────────────┐
│  Shader  │ │   Mesh   │ │   Texture    │
├──────────┤ ├──────────┤ ├──────────────┤
│ - id:int │ │ - vao    │ │ - id : int   │
├──────────┤ │ - vbo    │ │ - width      │
│ + Load() │ │ - ebo    │ │ - height     │
│ + Use()  │ ├──────────┤ ├──────────────┤
│ + SetInt │ │+Upload() │ │ + LoadBMP()  │
│ + SetFlt │ │+ Draw()  │ │ + LoadPPM()  │
│ + SetMat4│ └────┬─────┘ │ + Bind()     │
└──────────┘      │       └──────────────┘
                  │ uses
                  ▼
        ┌──────────────────┐
        │     ObjMesh      │◄──── ObjParser
        ├──────────────────┤      (static class)
        │ Vertices:List<V> │      + Parse(path)
        │ Indices:List<uint│           : ObjMesh
        └────────┬─────────┘
                 │ contains
                 ▼
        ┌──────────────────┐
        │     Vertex       │
        ├──────────────────┤
        │ Position : Vec3  │
        │ Color    : Vec3  │
        │ TexCoord : Vec2  │
        │ Stride   = 32    │
        └──────────────────┘

        ┌──────────────────────────────────────┐
        │               Mat4                   │
        ├──────────────────────────────────────┤
        │ M00..M33 : float (column-major)       │
        ├──────────────────────────────────────┤
        │ + Identity()          : Mat4          │
        │ + Translate(Vec3)     : Mat4          │
        │ + Scale(Vec3)         : Mat4          │
        │ + RotateX(rad)        : Mat4          │
        │ + RotateY(rad)        : Mat4          │
        │ + RotateZ(rad)        : Mat4          │
        │ + Perspective(...)    : Mat4          │
        │ + LookAt(eye,ctr,up)  : Mat4          │
        │ + operator*(Mat4)     : Mat4          │
        │ + operator*(Vec4)     : Vec4          │
        │ + ToArray()           : float[16]     │
        └──────────────────────────────────────┘

        ┌───────────┐    ┌───────────┐
        │   Vec3    │    │   Vec2    │
        ├───────────┤    ├───────────┤
        │ X,Y,Z:flt │    │ X,Y:float │
        ├───────────┤    └───────────┘
        │ + Length()│
        │ + Normal()│
        │ + Dot()   │
        │ + Cross() │
        └───────────┘
```

---

## 9. UML — Sequence Diagram: Render Loop

```
Program      App          ObjParser    Shader    Mesh    Texture   OpenGL
   │           │               │          │        │         │        │
   │─ new ────►│               │          │        │         │        │
   │─ Run() ──►│               │          │        │         │        │
   │           │               │          │        │         │        │
   │       [OnLoad]            │          │        │         │        │
   │           │─ Parse() ────►│          │        │         │        │
   │           │◄─ ObjMesh ────│          │        │         │        │
   │           │─ Load() ──────────────►  │        │         │        │
   │           │─ Upload() ──────────────────────► │         │        │
   │           │─ LoadBMP/PPM ────────────────────────────►  │        │
   │           │                          │        │         │        │
   │       [OnUpdateFrame × 60fps]        │        │         │        │
   │           │ rotAngle += dt           │        │         │        │
   │           │ position += input        │        │         │        │
   │           │ blendFactor → target     │        │         │        │
   │           │                          │        │         │        │
   │       [OnRenderFrame × 60fps]        │        │         │        │
   │           │─────────────────────────────────────────────────── Clear()
   │           │ build model = Translate * RotateY │         │        │
   │           │ build view  = LookAt     │        │         │        │
   │           │ build proj  = Perspective│        │         │        │
   │           │─ Use() ───────────────► │         │         │        │
   │           │─ SetMat4(model/view/proj)──────►  │         │        │
   │           │─ SetFloat(blendFactor) ─────────► │         │        │
   │           │─ Bind() ────────────────────────────────── ►│        │
   │           │─ Draw() ───────────────────────────────────────── DrawElements()
   │           │─────────────────────────────────────────────────── SwapBuffers()
```

---

## 10. UML — State Diagram: Texture Toggle

```
         App starts
              │
              ▼
    ┌──────────────────┐
    │   COLOR mode     │  blendFactor = 0.0
    │   blendTarget = 0│  full gray shading
    └────────┬─────────┘
             │
             │  T key pressed
             ▼
    ┌──────────────────┐
    │  TRANSITIONING   │  blendFactor moves toward 1.0
    │  → TEXTURE       │  speed: +2.0 per second (~0.5s)
    └────────┬─────────┘
             │
             │  blendFactor == 1.0
             ▼
    ┌──────────────────┐
    │  TEXTURE mode    │  blendFactor = 1.0
    │  blendTarget = 1 │  full texture
    └────────┬─────────┘
             │
             │  T key pressed
             ▼
    ┌──────────────────┐
    │  TRANSITIONING   │  blendFactor moves toward 0.0
    │  → COLOR         │  speed: -2.0 per second (~0.5s)
    └────────┬─────────┘
             │
             │  blendFactor == 0.0
             ▼
    ┌──────────────────┐
    │   COLOR mode     │  back to start
    └──────────────────┘

GLSL implementation:
    frag_color = mix(colorSample, textureSample, u_blendFactor)
```

---

## 11. UML — State Diagram: Mouse Drag Rotation

```
┌──────────────────────────────┐
│  IDLE                        │
│  autoRot += RotSpeed * dt    │
│  model uses autoRot only     │
└──────────────┬───────────────┘
               │  LMB down
               │  record mouseLastPos
               ▼
┌──────────────────────────────┐
│  DRAGGING                    │
│  autoRot frozen              │
│  manualRotY += dx * Sens     │◄──── OnMouseMove
│  manualRotX += dy * Sens     │
│  model uses autoRot          │
│         + manualRotY         │
│         + manualRotX         │
└──────────────┬───────────────┘
               │  LMB up
               ▼
┌──────────────────────────────┐
│  IDLE                        │
│  autoRot resumes             │
│  manualRot stays accumulated │
└──────────────────────────────┘

Model matrix:
  model = Translate(position)
        * RotateY(autoRot + manualRotY)
        * RotateX(manualRotX)
```

---

## 12. Module Breakdown

### Math module

All structs, all operators, no dependencies on any library.

| File | Responsibility |
|---|---|
| `Vec2.cs` | UV coordinates, 2D float arithmetic |
| `Vec3.cs` | Positions, colors, normals. Dot, Cross, Normalize, Length |
| `Vec4.cs` | Homogeneous coordinates for matrix multiplication |
| `Mat4.cs` | All 3D transforms + perspective + view. Column-major storage. |

Column-major memory layout (matches OpenGL):

```
Index:  0   1   2   3   4   5   6   7   8   9  10  11  12  13  14  15
        M00 M10 M20 M30 M01 M11 M21 M31 M02 M12 M22 M32 M03 M13 M23 M33
         ↑col0↑              ↑col1↑              ↑col2↑              ↑col3↑
```

---

### Parsing module

| File | Responsibility |
|---|---|
| `Vertex.cs` | Interleaved struct: `Vec3 Position` + `Vec3 Color` + `Vec2 TexCoord`. Stride = 32 bytes |
| `ObjParser.cs` | Full `.obj` parser. Fan triangulation. Centroid centering. Box UV fallback. Per-face gray palette |

Gray palette (8 shades, assigned by face index mod 8):

```
Index 0 → 0.15  (darkest)
Index 1 → 0.25
Index 2 → 0.35
Index 3 → 0.45
Index 4 → 0.55
Index 5 → 0.65
Index 6 → 0.75
Index 7 → 0.85  (lightest)
```

---

### Rendering module

| File | Responsibility |
|---|---|
| `Shader.cs` | Read GLSL from disk. Compile vert + frag. Link program. Uniform setters |
| `Texture.cs` | Parse BMP/PPM manually. Upload `GL_RGB` to GPU. Generate mipmaps |
| `Mesh.cs` | Create VAO/VBO/EBO. Define vertex attribute layout. `Draw()` call |

Vertex attribute layout (VBO interleaved, stride = 32 bytes):

```
Offset  0 → location 0 → position  (3 × float = 12 bytes)
Offset 12 → location 1 → color     (3 × float = 12 bytes)
Offset 24 → location 2 → texCoord  (2 × float =  8 bytes)
```

---

### App module

| File | Responsibility |
|---|---|
| `Program.cs` | Parse `argv`, print usage, instantiate `App`, call `Run()` |
| `App.cs` | Owns shader + mesh + texture. Full render loop. Keyboard input. Blend animation |

MVP matrix build order every frame:

```
model      = Mat4.Translate(position) * Mat4.RotateY(rotAngle)
view       = Mat4.LookAt(eye: (0,0,5), center: (0,0,0), up: (0,1,0))
projection = Mat4.Perspective(fovY: 45°, aspect: 1280/720, near: 0.1, far: 100)
```

---

## 13. Controls Specification

| Key / Input | Action | Implementation |
|---|---|---|
| `←` `→` | Move X | `position.X ±= 0.05f` |
| `↑` `↓` | Move Y | `position.Y ±= 0.05f` |
| `Q` `E` | Move Z | `position.Z ±= 0.05f` |
| `T` | Toggle texture | flip `blendTarget` 0↔1 |
| `W` | Wireframe toggle | `GL.PolygonMode` Line↔Fill |
| `C` | Backface culling | `GL.Enable/Disable(CullFace)` |
| `S` | Shading mode | flat↔smooth normal interpolation |
| `L` | Lighting toggle | Phong on/off |
| `P` | Screenshot | `GL.ReadPixels` → PPM file |
| `Tab` | Switch active model | `ActiveModel` 0↔1 (dual model mode) |
| `ESC` | Quit | `Close()` |
| Scroll | Zoom | `cameraZ -= offset * 0.3f`, clamped [0.5, 50] |
| LMB drag | Rotate object | `manualRotY/X += delta * 0.005f` |
| RMB drag | Move light | `lightPos.X/Y += delta * 0.01f` |

Auto-rotation:

```
if (!isDragging)
    rotAngle += 1.0f * deltaTime   // 1 radian/sec ≈ one full spin every 6.28 seconds
```

Blend animation:

```
if blendFactor < blendTarget → blendFactor += 2.0f * deltaTime
if blendFactor > blendTarget → blendFactor -= 2.0f * deltaTime
// Full transition in ~0.5 seconds
```

---

## 14. Mandatory Checklist

- [ ] Project compiles with `make`
- [ ] Makefile has `all`, `clean`, `fclean`, `re`
- [ ] `.obj` file is parsed manually (no library)
- [ ] Object displays in perspective projection
- [ ] Object rotates around its **geometric centroid** (not a corner)
- [ ] Different faces have visually distinct colors (gray shades)
- [ ] Object is movable on X axis (both directions)
- [ ] Object is movable on Y axis (both directions)
- [ ] Object is movable on Z axis (both directions)
- [ ] Pressing T applies texture
- [ ] Pressing T again returns to colored view
- [ ] Transition between texture and color is smooth (no hard cut)
- [ ] Matrix math is hand-rolled (no library)
- [ ] Shader loading is hand-rolled (no library)
- [ ] Texture image loading is hand-rolled (no library)
- [ ] 42 logo renders and spins around its center during defense
- [ ] Gray shades visible on logo sides
- [ ] Cheerful texture (ponies / kittens / unicorns)

---

## 15. Bonus Checklist

> Bonus only evaluated if mandatory part is PERFECT.

**Bonus 1 — Ear-clipping triangulation**
- [ ] `IsConvexVertex` implemented (cross product sign test)
- [ ] `PointInTriangle` implemented (barycentric test)
- [ ] `EarClip` main loop implemented
- [ ] Dominant axis projection for non-coplanar faces
- [ ] Integrated into `ObjParser` replacing fan triangulation
- [ ] Original teapot renders without artifacts
- [ ] Degenerate / zero-area ears skipped correctly

**Bonus 2 — Smart texture mapping**
- [ ] `.obj vt` coordinates used when available (per vertex)
- [ ] Mixed faces handled (some with `vt`, some without)
- [ ] `ComputeFaceNormal` implemented
- [ ] `DominantAxis` selection implemented
- [ ] `BoxMapFace` per-face UV projection implemented
- [ ] Per-face bounding box normalization (not global)
- [ ] No visible stretching on 42 logo side faces

**Bonus 3 — Zoom with scroll wheel**
- [ ] `OnMouseWheel` override implemented
- [ ] `cameraZ` field replaces hardcoded `5.0f`
- [ ] Zoom speed: `0.3f` per tick
- [ ] Zoom clamped to `[0.5f, 50.0f]`
- [ ] `LookAt` uses `cameraZ` dynamically
- [ ] Scroll up = zoom in, scroll down = zoom out

**Bonus 4 — Mouse drag rotation**
- [ ] `OnMouseDown` sets `isDragging = true`, records start pos
- [ ] `OnMouseMove` accumulates `manualRotX` / `manualRotY` from delta
- [ ] `OnMouseUp` sets `isDragging = false`
- [ ] Auto-rotation paused while dragging
- [ ] Auto-rotation resumes on mouse release
- [ ] Model matrix combines auto + manual rotation
- [ ] No jitter on button release


**Bonus 5 — Wireframe toggle**
- [ ] `Wireframe` bool in `AppState`
- [ ] `W` key binding in `InputHandler`
- [ ] `GL.PolygonMode` applied before draw in `Renderer`
- [ ] Toggles cleanly at runtime

**Bonus 6 — Backface culling**
- [ ] `Culling` bool in `AppState`
- [ ] `C` key binding in `InputHandler`
- [ ] `GL.Enable/Disable(CullFace)` in `Renderer`
- [ ] `GL.FrontFace(Ccw)` winding order set correctly

**Bonus 7 — Screenshot**
- [ ] `P` key binding in `InputHandler`
- [ ] `GL.ReadPixels` reads current framebuffer
- [ ] Rows flipped (OpenGL bottom-up → PPM top-down)
- [ ] Auto-incrementing filename (`screenshot_001.ppm` etc.)
- [ ] File written as valid PPM P6

**Bonus 8 — Auto-scaling**
- [ ] `maxExtent` computed from bounding box
- [ ] Scale factor applied in `BakeVertices`
- [ ] All objects fit consistently in view on load

**Bonus 9 — Flat vs smooth shading**
- [ ] `Normal : Vec3` added to `Vertex` struct (stride = 44)
- [ ] `Mesh.Upload()` attribute layout updated
- [ ] Normals passed from `ObjParser`
- [ ] `ShadingMode` bool in `AppState`
- [ ] `S` key binding in `InputHandler`
- [ ] Vertex shader passes normal to fragment stage
- [ ] Fragment shader uses `flat` keyword when flat mode active

**Bonus 10 — Phong lighting**
- [ ] Requires Bonus 9 (normals) to be completed first
- [ ] `LightPos`, `LightColor`, `LightOn` in `AppState`
- [ ] `RMBDragging` + `OnMouseDown/Move/Up` RMB branch
- [ ] `L` key binding toggles lighting
- [ ] RMB drag moves light in XY
- [ ] Fragment shader: ambient + diffuse + specular
- [ ] `u_lightPos`, `u_lightColor`, `u_viewPos`, `u_lightingOn` uniforms
- [ ] Light visibly affects shading in real time

**Bonus 11 — Dual model rendering**
- [ ] `MeshA/B`, `TextureA/B`, separate transforms in `AppState`
- [ ] `Tab` key switches `ActiveModel`
- [ ] `Renderer` calls `GL.Viewport` twice (left/right split)
- [ ] Both models spin independently
- [ ] `Program.cs` handles 2–4 arguments
- [ ] Window title shows active model

---

## 16. Build System

### Makefile targets

| Target | Action |
|---|---|
| `make` / `make all` | `dotnet publish -c Release -o ./build` + symlink `./scop` |
| `make clean` | `dotnet clean` + remove `obj/` |
| `make fclean` | clean + remove `bin/`, `build/`, `./scop` |
| `make re` | `fclean` then `all` |

### Dependencies to install on 42 cluster

```bash
sudo apt-get install dotnet-sdk-8.0 libgl1 libglu1-mesa
```

OpenTK is fetched automatically via NuGet on first `dotnet restore`.

### Run

```bash
./scop objects/42logo.obj textures/ponies.bmp
./scop objects/cube.obj   textures/test.ppm
```